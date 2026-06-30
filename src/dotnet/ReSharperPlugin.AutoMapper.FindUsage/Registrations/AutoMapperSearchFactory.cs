using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl.Occurrences;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Search;

namespace ReSharperPlugin.AutoMapper.FindUsage.Registrations;

[PsiComponent(Instantiation.DemandAnyThreadSafe)]
public class AutoMapperSearchFactory : DomainSpecificSearcherFactoryBase
{
    private readonly AutoMapperRegistrationFinder _registrationFinder;

    public AutoMapperSearchFactory(AutoMapperRegistrationFinder registrationFinder)
    {
        _registrationFinder = registrationFinder;
    }

    public override bool IsCompatibleWithLanguage(PsiLanguageType languageType)
    {
        return languageType is CSharpLanguage;
    }

    public override IEnumerable<FindResult> GetRelatedFindResults(IDeclaredElement element)
    {
        if (element is IMethod method && GetPropertyFromAccessor(method) is { } ownerProperty)
            element = ownerProperty;

        var seenResults = new HashSet<FindResult>();

        return element switch
        {
            ITypeElement typeElement => GetTypeResults(typeElement, seenResults),
            IProperty { ContainingType: { } containingType } property => GetPropertyResults(containingType, property,
                seenResults),
            _ => []
        };
    }

    private IEnumerable<FindResult> GetPropertyResults(ITypeElement containingType, IProperty property,
        HashSet<FindResult> seenResults)
    {
        var mappings = _registrationFinder.FindMappingsForType(containingType);

        foreach (var mapping in mappings)
        {
            if (mapping.IgnoredProperties.Contains(property.ShortName))
            {
                continue;
            }

            var isSource = mapping.Source is IDeclaredType s && s.GetTypeElement()?.Equals(containingType) == true;
            var isDestination = mapping.Destination is IDeclaredType d && d.GetTypeElement()?.Equals(containingType) == true;

            if (isSource && mapping.HasReverseMap)
            {
                if (mapping.Destination is IDeclaredType destType && destType.GetTypeElement() is { } destElement && !destElement.Equals(containingType))
                {
                    foreach (var result in CreatePropertyRelatedFindResults(destElement, property.ShortName))
                        if (seenResults.Add(result)) yield return result;
                }
            }

            if (isDestination)
            {
                if (mapping.Source is IDeclaredType sourceType && sourceType.GetTypeElement() is { } sourceElement && !sourceElement.Equals(containingType))
                {
                    foreach (var result in CreatePropertyRelatedFindResults(sourceElement, property.ShortName))
                        if (seenResults.Add(result)) yield return result;
                }
            }
        }
    }

    private IEnumerable<FindResult> GetTypeResults(ITypeElement typeElement, HashSet<FindResult> seenResults)
    {
        var mappings = _registrationFinder.FindMappingsForType(typeElement);

        foreach (var mapping in mappings)
        {
            var isSource = mapping.Source is IDeclaredType s && s.GetTypeElement()?.Equals(typeElement) == true;
            var isDestination = mapping.Destination is IDeclaredType d && d.GetTypeElement()?.Equals(typeElement) == true;

            if (isSource && mapping.HasReverseMap)
            {
                if (mapping.Destination is IDeclaredType destType && destType.GetTypeElement() is { } destElement && !destElement.Equals(typeElement))
                {
                    foreach (var result in CreateFindResults(destElement))
                        if (seenResults.Add(result)) yield return result;
                }
            }

            if (isDestination)
            {
                if (mapping.Source is IDeclaredType sourceType && sourceType.GetTypeElement() is { } sourceElement && !sourceElement.Equals(typeElement))
                {
                    foreach (var result in CreateFindResults(sourceElement))
                        if (seenResults.Add(result)) yield return result;
                }
            }
        }
    }

    private static IEnumerable<FindResult> CreateFindResults(IDeclaredElement element)
    {
        foreach (var declaration in element.GetDeclarations())
        {
            yield return new FindResultInitializer(declaration);
        }
    }

    private static IEnumerable<FindResult> CreatePropertyRelatedFindResults(ITypeElement typeElement, string propertyName)
    {
        var property = typeElement.Properties.FirstOrDefault(p => p.ShortName == propertyName);
        if (property != null)
        {
            foreach (var declaration in property.GetDeclarations())
            {
                if (declaration is IPropertyDeclaration propertyDeclaration)
                {
                    var nameNode = propertyDeclaration.NameIdentifier;
                    if (nameNode != null)
                    {
                        yield return new FindResultInitializer(nameNode);
                        yield break;
                    }
                }
            }

            yield return new FindResultDeclaredElement(property);
        }
    }

    private static IProperty GetPropertyFromAccessor(IDeclaredElement element)
    {
        foreach (var declaration in element.GetDeclarations())
        {
            var propertyDeclaration = declaration.GetContainingNode<IPropertyDeclaration>(true);
            if (propertyDeclaration?.DeclaredElement is { } property)
                return property;
        }

        return null;
    }
}