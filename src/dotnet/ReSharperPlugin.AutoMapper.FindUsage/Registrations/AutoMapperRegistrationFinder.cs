using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Files;

namespace ReSharperPlugin.AutoMapper.FindUsage.Registrations;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class AutoMapperRegistrationFinder
{
    private readonly AutoMapperCache _cache;

    public AutoMapperRegistrationFinder(AutoMapperCache cache)
    {
        _cache = cache;
    }

    public IReadOnlyCollection<AutoMapperMapping> FindMappingsForType(ITypeElement typeElement)
    {
        return FindMappingsForType(typeElement.GetClrName().FullName);
    }

    private IReadOnlyCollection<AutoMapperMapping> FindMappingsForType(string typeClrName)
    {
        var results = new List<AutoMapperMapping>();

        foreach (var (sourceFile, serializableMapping) in _cache.GetMappingsForType(typeClrName))
        {
            if (sourceFile.GetPrimaryPsiFile() is not ICSharpFile) 
                continue;

            var sourceType =
                TypeFactory.CreateTypeByCLRName(serializableMapping.SourceTypeClrName, sourceFile.PsiModule);
            var destType =
                TypeFactory.CreateTypeByCLRName(serializableMapping.DestinationTypeClrName, sourceFile.PsiModule);

            if (serializableMapping.DestinationTypeClrName == typeClrName)
            {
                results.Add(new AutoMapperMapping(sourceType, destType,
                    serializableMapping.IgnoredProperties));
            }

            if (serializableMapping.SourceTypeClrName == typeClrName && serializableMapping.HasReverseMap)
            {
                results.Add(new AutoMapperMapping(destType, sourceType, null));
            }
        }

        return results;
    }
}