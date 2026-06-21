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

        switch (element)
        {
            case ITypeElement typeElement:
                return GetTypeResults(typeElement, seenResults);
            case IProperty { ContainingType: { } containingType } property:
                return GetPropertyResults(containingType, property, seenResults);
        }
        return [];
    }

    private IEnumerable<FindResult> GetPropertyResults(ITypeElement containingType, IProperty property, HashSet<FindResult> seenResults)
    {
        var mappings = _registrationFinder.FindMappingsForType(containingType);

        foreach (var mapping in mappings)
        {
            if (mapping.IgnoredProperties.Contains(property.ShortName))
            {
                continue;
            }

            foreach (var type in new[] { mapping.Source, mapping.Destination })
            {
                if (type is IDeclaredType declaredType && declaredType.GetTypeElement() is { } otherTypeElement && !otherTypeElement.Equals(containingType))
                {
                    var otherProperty = otherTypeElement.Properties.FirstOrDefault(p => p.ShortName == property.ShortName);
                    if (otherProperty != null)
                    {
                        foreach (var result in CreatePropertyRelatedFindResults(otherProperty))
                            if (seenResults.Add(result)) 
                                yield return result;
                    }
                }
            }
        }
    }

    private IEnumerable<FindResult> GetTypeResults(ITypeElement typeElement, HashSet<FindResult> seenResults)
    {
        var mappings = _registrationFinder.FindMappingsForType(typeElement);

        foreach (var mapping in mappings)
        {
            if (mapping.Source is IDeclaredType sourceDeclaredType && sourceDeclaredType.GetTypeElement() is { } sourceElement && !sourceElement.Equals(typeElement))
            {
                foreach (var result in CreateFindResults(sourceElement))
                    if (seenResults.Add(result)) yield return result;
            }

            if (mapping.Destination is IDeclaredType destinationDeclaredType && destinationDeclaredType.GetTypeElement() is { } destinationElement && !destinationElement.Equals(typeElement))
            {
                foreach (var result in CreateFindResults(destinationElement))
                    if (seenResults.Add(result)) yield return result;
            }
        }
    }

    private static IEnumerable<FindResult> CreateFindResults(IDeclaredElement element)
    {
        var hasDeclaration = false;
        foreach (var declaration in element.GetDeclarations())
        {
            yield return new FindResultInitializer(declaration);
            hasDeclaration = true;
        }

        if (!hasDeclaration)
            yield return new FindResultInitializer(element.GetSingleDeclaration());
    }

    private static IEnumerable<FindResult> CreatePropertyRelatedFindResults(IProperty property)
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