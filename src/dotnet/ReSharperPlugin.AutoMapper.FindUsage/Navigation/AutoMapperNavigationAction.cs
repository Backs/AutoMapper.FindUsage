using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.Util;
using ReSharperPlugin.AutoMapper.FindUsage.Registrations;

namespace ReSharperPlugin.AutoMapper.FindUsage.Navigation;

[ContextAction(Name = "AutoMapper Navigation", Description = "Navigate to AutoMapper source",
    GroupType = typeof(CSharpContextActions))]
public class AutoMapperNavigationAction : IContextAction
{
    private readonly ICSharpContextActionDataProvider _dataProvider;
    private readonly AutoMapperRegistrationFinder _registrationFinder;

    public AutoMapperNavigationAction(ICSharpContextActionDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
        _registrationFinder = dataProvider.Solution.GetComponent<AutoMapperRegistrationFinder>();
    }

    public IEnumerable<IntentionAction> CreateBulbItems()
    {
        return CreateBulbItemsInternal();
    }

    private IEnumerable<IntentionAction> CreateBulbItemsInternal()
    {
        var property = GetSelectedProperty();
        if (property == null) return EmptyList<IntentionAction>.Instance;

        var containingType = property.ContainingType;
        if (containingType == null) return EmptyList<IntentionAction>.Instance;

        var mappings = _registrationFinder.FindMappingsForType(containingType);
        if (mappings.Count == 0) return EmptyList<IntentionAction>.Instance;

        var itemsToCreate = new List<(IProperty property, IType type)>();
        var processedProperties = new HashSet<IProperty>();

        foreach (var mapping in mappings)
        {
            var otherType = mapping.Source;
            if (otherType == null) continue;

            var otherProperty = FindCorrespondingProperty(otherType, property.ShortName);
            if (otherProperty != null && processedProperties.Add(otherProperty))
            {
                itemsToCreate.Add((otherProperty, otherType));
            }
        }

        if (itemsToCreate.Count == 0) return EmptyList<IntentionAction>.Instance;

        var result = new List<IntentionAction>();
        var useFullName = itemsToCreate.Count > 1;

        foreach (var item in itemsToCreate)
        {
            var typeName = useFullName
                ? GetFullName(item.type, property.PresentationLanguage)
                : item.type.GetPresentableName(property.PresentationLanguage);

            var text = $"AutoMapper. Navigate to source: {typeName}.{property.ShortName}";
            result.Add(new IntentionAction(new NavigateToPropertyBulbAction(item.property, text),
                PsiSymbolsThemedIcons.Other.Id, IntentionsAnchors.ContextActionsAnchor));
        }

        return result;
    }

    public bool IsAvailable(IUserDataHolder cache)
    {
        var property = GetSelectedProperty();
        if (property == null) return false;

        // To avoid heavy computation in IsAvailable, we just return true if it's a property.
        // The actual search will happen in CreateBulbItems.
        return true;
    }

    private IProperty GetSelectedProperty()
    {
        var node = _dataProvider.GetSelectedTreeNode<ITreeNode>();
        if (node == null) return null;

        var accessorDeclaration = node.GetContainingNode<IAccessorDeclaration>(true);
        if (accessorDeclaration is { Kind: AccessorKind.SETTER })
        {
            var propertyDeclaration = accessorDeclaration.GetContainingNode<IPropertyDeclaration>();
            return propertyDeclaration?.DeclaredElement;
        }

        return null;
    }

    private static IProperty FindCorrespondingProperty(IType type, string propertyName)
    {
        var typeElement = (type as IDeclaredType)?.GetTypeElement();
        if (typeElement == null) return null;

        return typeElement.GetMembers().OfType<IProperty>().FirstOrDefault(p => p.ShortName == propertyName);
    }

    private static string GetFullName(IType type, PsiLanguageType language)
    {
        if (type is IDeclaredType declaredType)
        {
            var typeElement = declaredType.GetTypeElement();
            if (typeElement != null)
            {
                return typeElement.GetClrName().FullName;
            }
        }

        return type.GetPresentableName(language);
    }
}

public class NavigateToPropertyBulbAction : BulbActionBase
{
    private readonly IProperty _property;
    public override string Text { get; }

    public NavigateToPropertyBulbAction(IProperty property, string text)
    {
        _property = property;
        Text = text;
    }

    public override void Execute(ISolution solution, ITextControl textControl)
    {
        _property.Navigate(true);
    }

    protected override Action<ITextControl>
        ExecutePsiTransaction(ISolution solution, IProgressIndicator progress) => null;
}