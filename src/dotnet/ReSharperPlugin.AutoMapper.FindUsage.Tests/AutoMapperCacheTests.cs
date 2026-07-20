using System.Collections.Generic;
using FluentAssertions;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework.Projects;
using NUnit.Framework;
using ReSharperPlugin.AutoMapper.FindUsage.Registrations;

namespace ReSharperPlugin.AutoMapper.FindUsage.Tests;

[ReuseSolution(false)]
public class AutoMapperCacheTests : BaseTestWithSingleProject
{
    protected override string RelativeTestDataPath => "Navigation";

    [Test]
    public void BuildOneWayMappingTest()
    {
        WithSingleProject(["OneWayMapping.cs"], (_, solution, project) =>
        {
            var locks = solution.GetComponent<IShellLocks>();
            locks.ExecuteWithReadLock(() =>
            {
                var cache = solution.GetComponent<AutoMapperCache>();
                var sourceFile = GetProjectSourceFile(project, "OneWayMapping.cs");

                var built = cache.Build(sourceFile, isPreParent: false) as List<SerializableMapping>;

                built.Should().BeEquivalentTo([
                    new SerializableMapping
                    {
                        DestinationTypeClrName = "TestNamespace.Destination",
                        HasReverseMap = false,
                        IgnoredProperties = [],
                        SourceTypeClrName = "TestNamespace.Source"
                    }
                ]);
            });
        });
    }

    [Test]
    public void BuildReverseMappingTest()
    {
        WithSingleProject(["ReverseMapping.cs"], (_, solution, project) =>
        {
            var locks = solution.GetComponent<IShellLocks>();
            locks.ExecuteWithReadLock(() =>
            {
                var cache = solution.GetComponent<AutoMapperCache>();
                var sourceFile = GetProjectSourceFile(project, "ReverseMapping.cs");

                var built = cache.Build(sourceFile, isPreParent: false) as List<SerializableMapping>;
                built.Should().BeEquivalentTo([
                    new SerializableMapping
                    {
                        DestinationTypeClrName = "TestNamespaceReverse.Destination",
                        HasReverseMap = true,
                        IgnoredProperties = new List<string>(),
                        SourceTypeClrName = "TestNamespaceReverse.Source"
                    }
                ]);
            });
        });
    }

    [Test]
    public void BuildsIgnoredPropertiesMappiingTest()
    {
        WithSingleProject(["IgnoredMapping.cs"], (_, solution, project) =>
        {
            var locks = solution.GetComponent<IShellLocks>();
            locks.ExecuteWithReadLock(() =>
            {
                var cache = solution.GetComponent<AutoMapperCache>();
                var sourceFile = GetProjectSourceFile(project, "IgnoredMapping.cs");

                var built = cache.Build(sourceFile, isPreParent: false) as List<SerializableMapping>;

                built.Should().BeEquivalentTo([
                    new SerializableMapping
                    {
                        DestinationTypeClrName = "TestNamespaceIgnored.Destination",
                        HasReverseMap = false,
                        IgnoredProperties = ["Ignored"],
                        SourceTypeClrName = "TestNamespaceIgnored.Source"
                    }
                ]);
            });
        });
    }

    private static IPsiSourceFile GetProjectSourceFile(IProject project, string fileName)
    {
        foreach (var pf in project.GetAllProjectFiles())
        {
            if (pf.Name == fileName)
                return pf.ToSourceFile();
        }

        return null;
    }
}