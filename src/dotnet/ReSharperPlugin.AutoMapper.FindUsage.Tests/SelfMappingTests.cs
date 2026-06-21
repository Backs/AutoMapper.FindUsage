using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework.Projects;
using NUnit.Framework;
using ReSharperPlugin.AutoMapper.FindUsage.Registrations;
using FluentAssertions;

namespace ReSharperPlugin.AutoMapper.FindUsage.Tests;

[ReuseSolution(false)]
public class SelfMappingTests : BaseTestWithSingleProject
{
    protected override string RelativeTestDataPath => "Navigation";

    [Test]
    public void PropertyNavigationFromSelfMappingTest()
    {
        WithSingleProject(["SelfMapping.cs"], (_, solution, _) =>
        {
            var locks = solution.GetComponent<IShellLocks>();
            locks.ExecuteWithReadLock(() =>
            {
                var searchFactory = solution.GetComponent<AutoMapperSearchFactory>();

                var source = PsiTestHelper.GetTypeElement(solution, "TestNamespaceSelf.Source");
                source.Should().NotBeNull("Source type not found");
                var registrationFinder = solution.GetComponent<AutoMapperRegistrationFinder>();
                registrationFinder.EnsureProjectMappings(source);

                var mappings = registrationFinder.FindMappingsForType(source);
                mappings.Should().BeEmpty("Should NOT have mappings to itself in RegistrationFinder");

                var results = searchFactory.GetRelatedFindResults(source).ToList();
                results.Should().BeEmpty("Should NOT find itself when searching for mappings");

                var nameProp = source.Properties.FirstOrDefault(p => p.ShortName == "Name");
                nameProp.Should().NotBeNull("Source.Name not found");

                var nameResults = searchFactory.GetRelatedFindResults(nameProp).ToList();
                nameResults.Should().BeEmpty("Should NOT find itself for property when searching for mappings");
            });
        });
    }
}
