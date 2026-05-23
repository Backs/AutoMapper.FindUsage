using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
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
                            var mapping = new AutoMapperMapping(tSource, tDest, invocation);
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

            if (current is IInvocationExpression inv && inv.InvokedExpression is IReferenceExpression re &&
                re.Reference.GetName() == "ReverseMap")
            {
                return true;
            }

            if (current is IExpressionStatement) break;
            current = current.Parent;
        }

        return false;
    }
}

public class AutoMapperMapping
{
    public IType Source { get; }
    public IType Destination { get; }
    public ITreeNode Registration { get; }

    public AutoMapperMapping(IType source, IType destination, ITreeNode registration)
    {
        Source = source;
        Destination = destination;
        Registration = registration;
    }
}