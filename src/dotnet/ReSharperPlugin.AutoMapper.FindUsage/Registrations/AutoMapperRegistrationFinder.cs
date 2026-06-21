using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;

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
        var typeClrName = typeElement.GetClrName().FullName;
        var fromIndex = new List<AutoMapperMapping>();
        var seenFromIndex = new HashSet<string>();

        foreach (var (_, serializableMapping) in _cache.GetMappingsForType(typeClrName))
        {
            AddIfMatches(typeElement, typeClrName, serializableMapping, fromIndex, seenFromIndex);
        }

        return fromIndex;
    }

    internal void EnsureProjectMappings(ITypeElement typeElement)
    {
        _cache.EnsureMappingsBuilt(GetProjectSourceFiles(typeElement));
    }

    private static void AddIfMatches(ITypeElement contextTypeElement, string typeClrName,
        SerializableMapping serializableMapping,
        List<AutoMapperMapping> results, HashSet<string> seen)
    {
        if (serializableMapping.SourceTypeClrName == serializableMapping.DestinationTypeClrName)
            return;

        if (serializableMapping.DestinationTypeClrName != typeClrName &&
            serializableMapping.SourceTypeClrName != typeClrName)
            return;

        var key = serializableMapping.SourceTypeClrName + "->" + serializableMapping.DestinationTypeClrName;
        if (!seen.Add(key))
            return;

        var sourceType =
            TypeFactory.CreateTypeByCLRName(serializableMapping.SourceTypeClrName, contextTypeElement.Module);
        var destType =
            TypeFactory.CreateTypeByCLRName(serializableMapping.DestinationTypeClrName, contextTypeElement.Module);

        if (sourceType.GetTypeElement() == null || destType.GetTypeElement() == null)
        {
            return;
        }

        results.Add(new AutoMapperMapping(sourceType, destType, serializableMapping.HasReverseMap,
            serializableMapping.IgnoredProperties));
    }


    private static IEnumerable<IPsiSourceFile> GetProjectSourceFiles(ITypeElement typeElement)
    {
        foreach (var declaration in typeElement.GetDeclarations())
        {
            var sourceFile = declaration.GetSourceFile();
            var projectFile = sourceFile?.ToProjectFile();
            var project = projectFile?.GetProject();
            if (project == null)
                continue;

            foreach (var pf in project.GetAllProjectFiles())
            {
                var candidate = pf.ToSourceFile();
                if (candidate != null)
                    yield return candidate;
            }
        }
    }
}