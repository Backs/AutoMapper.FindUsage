using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.Util;

namespace ReSharperPlugin.AutoMapper.FindUsage.Registrations;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class AutoMapperRegistrationFinder
{
    private readonly ISolution _solution;

    public AutoMapperRegistrationFinder(ISolution solution)
    {
        _solution = solution;
    }

    public IReadOnlyCollection<AutoMapperMapping> FindMappingsForType(ITypeElement typeElement)
    {
        var psiServices = _solution.GetPsiServices();
        var symbolScope = psiServices.Symbols.GetSymbolScope(LibrarySymbolScope.FULL, true);

        var mappings = new List<AutoMapperMapping>();
        var processedTypes = new HashSet<ITypeElement>();

        // CreateMap<TSource, TDest> is defined as an extension method in ProfileExtensions
        // and also directly on Profile base class
        TryFindIn("AutoMapper.ProfileExtensions");
        TryFindIn("AutoMapper.Profile");
        TryFindIn("AutoMapper.IProfileExpression");
        TryFindIn("AutoMapper.IMapperConfigurationExpression");

        return mappings;

        void TryFindIn(string clrName)
        {
            var type = symbolScope.GetTypeElementByCLRName(clrName);
            if (type != null && processedTypes.Add(type))
            {
                mappings.AddRange(FindMappingsInType(type, typeElement));
            }
        }
    }

    private IReadOnlyCollection<AutoMapperMapping> FindMappingsInType(ITypeElement ownerType, ITypeElement targetType)
    {
        var psiServices = _solution.GetPsiServices();

        var createMapMethods = ownerType.Methods
            .Where(m => m.ShortName == "CreateMap")
            .ToList();

        var searchDomain = psiServices.SearchDomainFactory.CreateSearchDomain(_solution, includeLibraries: false);
        var results = new List<AutoMapperMapping>(4);

        foreach (var method in createMapMethods)
        {
            psiServices.SingleThreadedFinder.FindReferences(method, searchDomain,
                new ReferenceConsumer(results, targetType),
                JetBrains.Application.Progress.NullProgressIndicator.Create());
        }

        return results;
    }

    private class ReferenceConsumer : IFindResultConsumer<IReference>
    {
        private readonly List<AutoMapperMapping> _results;
        private readonly ITypeElement _targetType;

        public ReferenceConsumer(List<AutoMapperMapping> results, ITypeElement targetType)
        {
            _results = results;
            _targetType = targetType;
        }

        public IReference Build(FindResult result)
        {
            return (result as FindResultReference)?.Reference;
        }

        public FindExecution Merge(IReference result)
        {
            if (result == null) return FindExecution.Continue;
            var invocation = result.GetTreeNode().GetContainingNode<IInvocationExpression>();
            if (invocation != null)
            {
                var resolveResult = invocation.Reference.Resolve();
                if (resolveResult.DeclaredElement is IMethod m)
                {
                    if (TryGetMappingTypes(resolveResult.Substitution, m, out var tSource, out var tDest))
                    {
                        if (IsTargetType(tDest, _targetType))
                        {
                            var ignoredProperties = GetIgnoredProperties(invocation);
                            var mapping = new AutoMapperMapping(tSource, tDest, invocation, ignoredProperties);
                            _results.Add(mapping);
                        }

                        if (HasReverseMap(invocation) && IsTargetType(tSource, _targetType))
                        {
                            var mapping = new AutoMapperMapping(tDest, tSource, invocation);
                            _results.Add(mapping);
                        }
                    }
                }
            }

            return FindExecution.Continue;
        }

        private static bool TryGetMappingTypes(ISubstitution substitution, IMethod method, out IType sourceType,
            out IType destinationType)
        {
            sourceType = null;
            destinationType = null;

            // Standard generic method case: CreateMap<TSource, TDestination>
            if (method.TypeParametersCount >= 2)
            {
                sourceType = substitution[method.TypeParameters[0]];
                destinationType = substitution[method.TypeParameters[1]];
            }

            // Reduced extension method/fallback case: look up by generic parameter names
            sourceType ??= FindSubstitutedTypeByNames(substitution, "TSource");
            destinationType ??= FindSubstitutedTypeByNames(substitution, "TDestination", "TDest");

            return sourceType != null && destinationType != null;
        }

        private static IType FindSubstitutedTypeByNames(ISubstitution substitution, params string[] names)
        {
            return substitution.Domain.Where(typeParameter => names.Contains(typeParameter.ShortName))
                .Select(typeParameter => substitution[typeParameter]).FirstOrDefault();
        }
    }

    private static bool IsTargetType(IType type, ITypeElement targetType)
    {
        return (type as IDeclaredType)?.GetTypeElement()?.Equals(targetType) == true;
    }

    private static bool HasReverseMap(IInvocationExpression invocation)
    {
        // Usually it's .CreateMap<S, D>().ReverseMap()
        // In PSI it looks like ReverseMap(CreateMap(S, D))

        var current = invocation.Parent;
        while (current != null)
        {
            if (current is IReferenceExpression refExp && refExp.Reference.GetName() == "ReverseMap")
            {
                return true;
            }

            if (current is IInvocationExpression { InvokedExpression: IReferenceExpression re } &&
                re.Reference.GetName() == "ReverseMap")
            {
                return true;
            }

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
                    {
                        ignoredProperties.Add(propertyName);
                    }
                }
            }

            if (current is IExpressionStatement) break;
            current = current.Parent;
        }

        return ignoredProperties;
    }

    private static bool IsIgnore(IInvocationExpression forMemberInvocation)
    {
        // ForMember(it => it.Prop, opt => opt.Ignore())
        if (forMemberInvocation.Arguments.Count < 2) return false;

        var optArg = forMemberInvocation.Arguments[1].Expression;
        if (optArg is ILambdaExpression lambda)
        {
            var body = lambda.BodyExpression;
            // Handle opt => opt.Ignore()
            if (body is IInvocationExpression
                {
                    InvokedExpression: IReferenceExpression { Reference: var reference }
                } && reference.GetName() == "Ignore")
            {
                return true;
            }
        }

        return false;
    }

    private static string GetPropertyName(IInvocationExpression forMemberInvocation)
    {
        // ForMember(it => it.Prop, ...)
        if (forMemberInvocation.Arguments.Count < 1) return null;

        var propArg = forMemberInvocation.Arguments[0].Expression;
        if (propArg is ILambdaExpression { BodyExpression: IReferenceExpression refExp })
        {
            return refExp.Reference.GetName();
        }

        return null;
    }
}