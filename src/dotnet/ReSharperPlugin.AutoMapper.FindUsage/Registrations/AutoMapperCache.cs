using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Serialization;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi.Files;

namespace ReSharperPlugin.AutoMapper.FindUsage.Registrations;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class AutoMapperCache : SimpleICache<List<SerializableMapping>>
{
    public AutoMapperCache(Lifetime lifetime, IShellLocks shellLocks, IPersistentIndexManager persistentIndexManager)
        : base(lifetime, shellLocks, persistentIndexManager, Marshaller.Instance)
    {
    }

    private class Marshaller : IUnsafeMarshaller<List<SerializableMapping>>
    {
        public static readonly Marshaller Instance = new Marshaller();

        public void Marshal(UnsafeWriter writer, List<SerializableMapping> value)
        {
            writer.Write(value.Count);
            foreach (var mapping in value)
                mapping.Write(writer);
        }

        public List<SerializableMapping> Unmarshal(UnsafeReader reader)
        {
            int count = reader.ReadInt32();
            var result = new List<SerializableMapping>(count);
            for (int i = 0; i < count; i++)
                result.Add(SerializableMapping.Read(reader));
            return result;
        }
    }

    public override string Version => "5";

    protected override bool IsApplicable(IPsiSourceFile sourceFile)
    {
        return true;
    }

    public override object Build(IPsiSourceFile sourceFile, bool isPreParent)
    {
        var results = new List<SerializableMapping>();
        foreach (var file in sourceFile.GetPsiFiles<CSharpLanguage>())
        {
            if (file is ICSharpFile sharpFile)
            {
                sharpFile.ProcessDescendants(new AutoMapperProcessor(results));
            }
        }

        return results.Count > 0 ? results : null;
    }

    public IEnumerable<(IPsiSourceFile, SerializableMapping)> GetMappingsForType(string typeClrName)
    {
        foreach (var entry in Map)
        {
            var sourceFile = entry.Key;
            var mappings = entry.Value;
            if (mappings == null) continue;
            foreach (var mapping in mappings)
            {
                if (string.Equals(mapping.SourceTypeClrName, typeClrName, StringComparison.Ordinal) || 
                    string.Equals(mapping.DestinationTypeClrName, typeClrName, StringComparison.Ordinal))
                {
                    yield return (sourceFile, mapping);
                }
            }
        }
    }

    private class AutoMapperProcessor : IRecursiveElementProcessor
    {
        private readonly List<SerializableMapping> _results;

        public AutoMapperProcessor(List<SerializableMapping> results)
        {
            _results = results;
        }

        public bool ProcessingIsFinished => false;
        public bool InteriorShouldBeProcessed(ITreeNode element) => true;

        public void ProcessBeforeInterior(ITreeNode element)
        {
            if (element is IInvocationExpression invocation)
            {
                var reference = invocation.Reference;
                if (reference == null || reference.GetName() != "CreateMap") return;

                var resolveResult = reference.Resolve();
                var method = resolveResult.DeclaredElement as IMethod;
                
                if (method != null && !IsAutoMapperMethod(method))
                    return;

                if (TryGetMappingTypes(resolveResult.Substitution, method, invocation, out var tSource, out var tDest))
                {
                    var sourceName = GetTypeClrName(tSource);
                    var destName = GetTypeClrName(tDest);

                    if (sourceName != null && destName != null)
                    {
                        var ignoredProperties = GetIgnoredProperties(invocation);
                        _results.Add(new SerializableMapping
                        {
                            SourceTypeClrName = sourceName,
                            DestinationTypeClrName = destName,
                            IgnoredProperties = ignoredProperties.ToList(),
                            InvocationOffset = invocation.GetTreeStartOffset().Offset,
                            HasReverseMap = HasReverseMap(invocation)
                        });
                    }
                }
            }
        }

        private static string GetTypeClrName(IType type)
        {
            var scalarType = type.GetScalarType();
            if (scalarType == null) return null;
            
            var clrName = scalarType.GetClrName().FullName;
            if (!string.IsNullOrEmpty(clrName)) return clrName;
            
            return "DEBUG:" + scalarType.ToString();
        }

        public void ProcessAfterInterior(ITreeNode element)
        {
        }

        private static bool IsAutoMapperMethod(IMethod method)
        {
            var type = method.ContainingType;
            if (type == null) return false;
            var clrName = type.GetClrName().FullName;
            if (clrName == "AutoMapper.ProfileExtensions" ||
                clrName == "AutoMapper.Profile" ||
                clrName == "AutoMapper.IProfileExpression" ||
                clrName == "AutoMapper.IMapperConfigurationExpression")
            {
                return true;
            }

            // Fallback for tests or simplified stubs
            var shortName = type.ShortName;
            return shortName == "ProfileExtensions" ||
                   shortName == "Profile" ||
                   shortName == "IProfileExpression" ||
                   shortName == "IMapperConfigurationExpression";
        }

        private static bool TryGetMappingTypes(ISubstitution substitution, IMethod method, IInvocationExpression invocation,
            out IType sourceType, out IType destinationType)
        {
            sourceType = null;
            destinationType = null;

            if (method != null)
            {
                if (method.TypeParametersCount >= 2)
                {
                    sourceType = substitution[method.TypeParameters[0]];
                    destinationType = substitution[method.TypeParameters[1]];
                }

                sourceType ??= FindSubstitutedTypeByNames(substitution, "TSource");
                destinationType ??= FindSubstitutedTypeByNames(substitution, "TDestination", "TDest");
            }

            if ((sourceType == null || destinationType == null) && invocation.TypeArguments.Count >= 2)
            {
                sourceType = invocation.TypeArguments[0];
                destinationType = invocation.TypeArguments[1];
            }

            return sourceType != null && destinationType != null;
        }

        private static IType FindSubstitutedTypeByNames(ISubstitution substitution, params string[] names)
        {
            return substitution.Domain.Where(typeParameter => names.Contains(typeParameter.ShortName))
                .Select(typeParameter => substitution[typeParameter]).FirstOrDefault();
        }

        private static bool HasReverseMap(IInvocationExpression invocation)
        {
            var current = invocation.Parent;
            while (current != null)
            {
                if (current is IReferenceExpression refExp && refExp.Reference.GetName() == "ReverseMap")
                    return true;

                if (current is IInvocationExpression { InvokedExpression: IReferenceExpression re } &&
                    re.Reference.GetName() == "ReverseMap")
                    return true;

                if (current is IExpressionStatement) break;
                current = current.Parent;
            }

            return false;
        }

        private static ISet<string> GetIgnoredProperties(IInvocationExpression invocation)
        {
            var ignoredProperties = new HashSet<string>();
            var current = invocation.Parent;
            while (current != null)
            {
                if (current is IInvocationExpression forMemberInvocation &&
                    forMemberInvocation.InvokedExpression is IReferenceExpression { Reference: var reference } &&
                    reference.GetName() == "ForMember")
                {
                    if (IsIgnore(forMemberInvocation))
                    {
                        var propertyName = GetPropertyName(forMemberInvocation);
                        if (propertyName != null)
                            ignoredProperties.Add(propertyName);
                    }
                }

                if (current is IExpressionStatement) break;
                current = current.Parent;
            }

            return ignoredProperties;
        }

        private static bool IsIgnore(IInvocationExpression forMemberInvocation)
        {
            if (forMemberInvocation.Arguments.Count < 2) return false;
            var optArg = forMemberInvocation.Arguments[1].Expression;
            if (optArg is ILambdaExpression lambda)
            {
                var body = lambda.BodyExpression;
                if (body is IInvocationExpression { InvokedExpression: IReferenceExpression { Reference: var reference } } &&
                    reference.GetName() == "Ignore")
                    return true;
            }

            return false;
        }

        private static string GetPropertyName(IInvocationExpression forMemberInvocation)
        {
            if (forMemberInvocation.Arguments.Count < 1) return null;
            var propArg = forMemberInvocation.Arguments[0].Expression;
            if (propArg is ILambdaExpression { BodyExpression: IReferenceExpression refExp })
                return refExp.Reference.GetName();
            return null;
        }
    }
}
