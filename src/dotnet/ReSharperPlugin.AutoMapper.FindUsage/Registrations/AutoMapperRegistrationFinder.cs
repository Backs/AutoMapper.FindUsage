using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Files;

namespace ReSharperPlugin.AutoMapper.FindUsage.Registrations;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class AutoMapperRegistrationFinder
{
    private readonly ISolution _solution;
    private readonly AutoMapperCache _cache;

    public AutoMapperRegistrationFinder(ISolution solution, AutoMapperCache cache)
    {
        _solution = solution;
        _cache = cache;
    }

    public IReadOnlyCollection<AutoMapperMapping> FindMappingsForType(ITypeElement typeElement)
    {
        return FindMappingsForType(typeElement.GetClrName().FullName);
    }

    public IReadOnlyCollection<AutoMapperMapping> FindMappingsForType(string typeClrName)
    {
        var results = new List<AutoMapperMapping>();

        foreach (var tuple in _cache.GetMappingsForType(typeClrName))
        {
            var sourceFile = tuple.Item1;
            var serializableMapping = tuple.Item2;

            // Try to get PSI file more reliably
            var psiFile = sourceFile.GetPrimaryPsiFile() as ICSharpFile;
            if (psiFile == null) continue;

            var node = psiFile.FindTokenAt(new TreeOffset(serializableMapping.InvocationOffset));
            var invocation = node?.GetContainingNode<IInvocationExpression>();
            if (invocation == null) continue;

            var sourceType =
                TypeFactory.CreateTypeByCLRName(serializableMapping.SourceTypeClrName, sourceFile.PsiModule);
            var destType =
                TypeFactory.CreateTypeByCLRName(serializableMapping.DestinationTypeClrName, sourceFile.PsiModule);

            if (serializableMapping.DestinationTypeClrName == typeClrName)
            {
                results.Add(new AutoMapperMapping(sourceType, destType, invocation,
                    serializableMapping.IgnoredProperties.ToHashSet()));
            }

            if (serializableMapping.SourceTypeClrName == typeClrName)
            {
                results.Add(new AutoMapperMapping(destType, sourceType, invocation,
                    serializableMapping.HasReverseMap ? null : serializableMapping.IgnoredProperties.ToHashSet()));
            }
        }

        return results;
    }
}