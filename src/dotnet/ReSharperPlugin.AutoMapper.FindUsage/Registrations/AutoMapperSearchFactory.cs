using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
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


        if (element is ITypeElement typeElement)
        {
            var mappings = _registrationFinder.FindMappingsForType(typeElement);

            foreach (var mapping in mappings)
            {
                if (mapping.Source is IDeclaredType sourceDeclaredType && sourceDeclaredType.GetTypeElement() is { } sourceElement)
                {
                    yield return new FindResultDeclaredElement(sourceElement);
                }

                if (mapping.Destination is IDeclaredType destinationDeclaredType && destinationDeclaredType.GetTypeElement() is { } destinationElement)
                {
                    yield return new FindResultDeclaredElement(destinationElement);
                }
            }
        }
        else if (element is IProperty { ContainingType: { } containingType } property)
        {
            var mappings = _registrationFinder.FindMappingsForType(containingType);

            foreach (var mapping in mappings)
            {
                if (mapping.IgnoredProperties.Contains(property.ShortName))
                {
                    continue;
                }

                if (mapping.Source is IDeclaredType otherDeclaredType && otherDeclaredType.GetTypeElement() is { } otherTypeElement)
                {
                    var otherProperty = otherTypeElement.Properties.FirstOrDefault(p => p.ShortName == property.ShortName);
                    if (otherProperty != null)
                    {
                        yield return new FindResultDeclaredElement(otherProperty);
                    }
                }
            }
        }

        yield return new FindResultDeclaredElement(element);
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