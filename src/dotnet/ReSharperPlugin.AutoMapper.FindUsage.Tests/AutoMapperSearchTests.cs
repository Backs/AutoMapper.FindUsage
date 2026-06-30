using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Impl.Occurrences;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework.Projects;
using NUnit.Framework;
using JetBrains.ReSharper.Psi.Search;
using ReSharperPlugin.AutoMapper.FindUsage.Registrations;
using JetBrains.Application.Threading;
using FluentAssertions;

namespace ReSharperPlugin.AutoMapper.FindUsage.Tests;

[ReuseSolution(false)]
public class AutoMapperSearchTests : BaseTestWithSingleProject
{
    protected override string RelativeTestDataPath => "Navigation";
        
    [Test]
    public void PropertyNavigationFromSourceTest()
    {
        WithSingleProject(["OneWayMapping.cs"], (_, solution, _) =>
        {
            var locks = solution.GetComponent<IShellLocks>();
            locks.ExecuteWithReadLock(() =>
            {
                var searchFactory = solution.GetComponent<AutoMapperSearchFactory>();

                var source = PsiTestHelper.GetTypeElement(solution, "TestNamespace.Source");
                source.Should().NotBeNull("Source type not found");
                solution.GetComponent<AutoMapperRegistrationFinder>().EnsureProjectMappings(source);

                var results = searchFactory.GetRelatedFindResults(source).ToList();
                results.Should().BeEmpty("Should NOT find Destination when searching for Source in one-way mapping (no ReverseMap)");

                var destination = PsiTestHelper.GetTypeElement(solution, "TestNamespace.Destination");
                destination.Should().NotBeNull("Destination type not found");
                
                var resultsForDest = searchFactory.GetRelatedFindResults(destination).ToList();
                resultsForDest.Should().NotBeEmpty("Should find Source when searching for Destination (one-way mapping)");

                var sourceDecls = source.GetDeclarations().ToList();
                var foundSource = resultsForDest
                    .OfType<FindResultInitializer>()
                    .Any(r => sourceDecls.Any(d => d.Contains(r.Declaration)));
                foundSource.Should().BeTrue("Should find Source when searching for Destination (one-way mapping)");
            });
        });
    }

    [Test]
    public void PropertyNavigationFromDestinationTest()
    {
        WithSingleProject(["OneWayMapping.cs"], (_, solution, _) =>
        {
            var locks = solution.GetComponent<IShellLocks>();
            locks.ExecuteWithReadLock(() =>
            {
                var searchFactory = solution.GetComponent<AutoMapperSearchFactory>();

                var dest = PsiTestHelper.GetTypeElement(solution, "TestNamespace.Destination");
                dest.Should().NotBeNull("Destination type not found");
                solution.GetComponent<AutoMapperRegistrationFinder>().EnsureProjectMappings(dest);

                var nameProp = dest.Properties.FirstOrDefault(p => p.ShortName == "Name");
                nameProp.Should().NotBeNull("Destination.Name not found");

                var results = searchFactory.GetRelatedFindResults(nameProp).ToList();
                results.Should().NotBeEmpty("Expected related results for Destination.Name (one-way mapping)");

                var source = PsiTestHelper.GetTypeElement(solution, "TestNamespace.Source");
                var sourceNameProp = source.Properties.FirstOrDefault(p => p.ShortName == "Name");
                
                var foundSource = results.Any(r => 
                    r is FindResultDeclaredElement { DeclaredElement: var de } && de.Equals(sourceNameProp) ||
                    r is FindResultInitializer { Declaration: var node } && sourceNameProp.GetDeclarations().Any(d => d.Contains(node)));
                
                foundSource.Should().BeTrue("Should find Source.Name when searching for Destination.Name (one-way mapping)");
            });
        });
    }

    [Test]
    public void PropertyNavigationFromSourcePropertyTest()
    {
        WithSingleProject(["OneWayMapping.cs"], (_, solution, _) =>
        {
            var locks = solution.GetComponent<IShellLocks>();
            locks.ExecuteWithReadLock(() =>
            {
                var searchFactory = solution.GetComponent<AutoMapperSearchFactory>();

                var source = PsiTestHelper.GetTypeElement(solution, "TestNamespace.Source");
                source.Should().NotBeNull("Source type not found");
                solution.GetComponent<AutoMapperRegistrationFinder>().EnsureProjectMappings(source);

                var nameProp = source.Properties.FirstOrDefault(p => p.ShortName == "Name");
                nameProp.Should().NotBeNull("Source.Name not found");

                var results = searchFactory.GetRelatedFindResults(nameProp).ToList();
                results.Should().BeEmpty("Expected no results for Source.Name in one-way mapping (no ReverseMap)");

                var dest = PsiTestHelper.GetTypeElement(solution, "TestNamespace.Destination");
                var foundDest = results.Any(r => IsRelatedToProperty(r, dest, "Name"));

                foundDest.Should().BeFalse("Should NOT find Destination.Name when searching for Source.Name in one-way mapping");
            });
        });
    }

    [Test]
    public void PropertyNavigationNoDuplicatesWithReverseMapTest()
    {
        WithSingleProject(["ReverseMapping.cs"], (_, solution, _) =>
        {
            var locks = solution.GetComponent<IShellLocks>();
            locks.ExecuteWithReadLock(() =>
            {
                var searchFactory = solution.GetComponent<AutoMapperSearchFactory>();

                var source = PsiTestHelper.GetTypeElement(solution, "TestNamespaceReverse.Source");
                source.Should().NotBeNull("Source type not found");
                solution.GetComponent<AutoMapperRegistrationFinder>().EnsureProjectMappings(source);

                var nameProp = source.Properties.FirstOrDefault(p => p.ShortName == "Name");
                nameProp.Should().NotBeNull("Source.Name not found");

                var results = searchFactory.GetRelatedFindResults(nameProp).ToList();
                results.Should().HaveCount(1, "Should not have duplicate results for ReverseMap");

                var dest = PsiTestHelper.GetTypeElement(solution, "TestNamespaceReverse.Destination");
                var foundDest = results.Any(r => IsRelatedToProperty(r, dest, "Name"));

                foundDest.Should().BeTrue("Should find Destination.Name when searching for Source.Name via ReverseMap");
            });
        });
    }

    [Test] 
    public void PropertyNavigationViiaReverseMapTest()
    {
        WithSingleProject(["ReverseMapping.cs"], (_, solution, _) =>
        {
            var locks = solution.GetComponent<IShellLocks>();
            locks.ExecuteWithReadLock(() =>
            {
                var searchFactory = solution.GetComponent<AutoMapperSearchFactory>();

                var dest = PsiTestHelper.GetTypeElement(solution, "TestNamespaceReverse.Destination");
                dest.Should().NotBeNull("Destination type not found");
                solution.GetComponent<AutoMapperRegistrationFinder>().EnsureProjectMappings(dest);

                var nameProp = dest.Properties.FirstOrDefault(p => p.ShortName == "Name");
                nameProp.Should().NotBeNull("Destination.Name not found");

                var nameResults = searchFactory.GetRelatedFindResults(nameProp).ToList();
                nameResults.Should().NotBeEmpty("Expected related results for Destination.Name via ReverseMap");
            });
        });
    }

    [Test]
    public void PropertyNavigationDoesNotFindIgnoredPropertyTest()
    {
        WithSingleProject(["IgnoredMapping.cs"], (_, solution, _) =>
        {
            var locks = solution.GetComponent<IShellLocks>();
            locks.ExecuteWithReadLock(() =>
            {
                var searchFactory = solution.GetComponent<AutoMapperSearchFactory>();

                var dest = PsiTestHelper.GetTypeElement(solution, "TestNamespaceIgnored.Destination");
                dest.Should().NotBeNull("Destination type not found");
                solution.GetComponent<AutoMapperRegistrationFinder>().EnsureProjectMappings(dest);

                var ignoredProp = dest.Properties.FirstOrDefault(p => p.ShortName == "Ignored");
                ignoredProp.Should().NotBeNull("Destination.Ignored not found");

                var ignoredResults = searchFactory.GetRelatedFindResults(ignoredProp).ToList();
                ignoredResults.Should().BeEmpty("Ignored property should produce no related results");
            });
        });
    }

    [Test]
    public void PropertyNavigationDoesNotFindMissingPropertyTest()
    {
        WithSingleProject(["MissingProperty.cs"], (_, solution, _) =>
        {
            var locks = solution.GetComponent<IShellLocks>();
            locks.ExecuteWithReadLock(() =>
            {
                var searchFactory = solution.GetComponent<AutoMapperSearchFactory>();

                var dest = PsiTestHelper.GetTypeElement(solution, "TestNamespaceMissing.Destination");
                dest.Should().NotBeNull("Destination type not found");
                solution.GetComponent<AutoMapperRegistrationFinder>().EnsureProjectMappings(dest);

                var missingProp = dest.Properties.FirstOrDefault(p => p.ShortName == "Missing");
                missingProp.Should().NotBeNull("Destination.Missing not found");

                var results = searchFactory.GetRelatedFindResults(missingProp).ToList();
                results.Should().BeEmpty("Missing property should produce no related results");
            });
        });
    }

    [Test]
    public void MultipleSourcesMappingTest()
    {
        WithSingleProject(["MultipleSourcesMapping.cs"], (_, solution, _) =>
        {
            var locks = solution.GetComponent<IShellLocks>();
            locks.ExecuteWithReadLock(() =>
            {
                var searchFactory = solution.GetComponent<AutoMapperSearchFactory>();

                var dest = PsiTestHelper.GetTypeElement(solution, "TestNamespaceMultiple.Destination");
                dest.Should().NotBeNull("Destination type not found");
                solution.GetComponent<AutoMapperRegistrationFinder>().EnsureProjectMappings(dest);

                var nameProp = dest.Properties.FirstOrDefault(p => p.ShortName == "Name");
                nameProp.Should().NotBeNull("Destination.Name not found");

                var results = searchFactory.GetRelatedFindResults(nameProp).ToList();
                results.Should().HaveCount(2, "Expected results from both Source1 and Source2");

                var source1 = PsiTestHelper.GetTypeElement(solution, "TestNamespaceMultiple.Source1");
                var source2 = PsiTestHelper.GetTypeElement(solution, "TestNamespaceMultiple.Source2");

                var foundSource1 = results.Any(r => IsRelatedToProperty(r, source1, "Name"));
                var foundSource2 = results.Any(r => IsRelatedToProperty(r, source2, "Name"));

                foundSource1.Should().BeTrue("Should find Source1.Name");
                foundSource2.Should().BeTrue("Should find Source2.Name");
            });
        });
    }

    [Test]
    public void InitPropertyNavigationTest()
    {
        WithSingleProject(["InitPropertyMapping.cs"], (_, solution, _) =>
        {
            var locks = solution.GetComponent<IShellLocks>();
            locks.ExecuteWithReadLock(() =>
            {
                var searchFactory = solution.GetComponent<AutoMapperSearchFactory>();

                var dest = PsiTestHelper.GetTypeElement(solution, "TestNamespaceInit.Destination");
                dest.Should().NotBeNull("Destination type not found");
                solution.GetComponent<AutoMapperRegistrationFinder>().EnsureProjectMappings(dest);

                var nameProp = dest.Properties.FirstOrDefault(p => p.ShortName == "Name");
                nameProp.Should().NotBeNull("Destination.Name not found");

                // Get the init accessor
                var initAccessor = nameProp.Setter;
                initAccessor.Should().NotBeNull("Destination.Name should have an init accessor");

                var results = searchFactory.GetRelatedFindResults(initAccessor).ToList();
                results.Should().NotBeEmpty("Should find Source.Name when searching from init accessor");

                var source = PsiTestHelper.GetTypeElement(solution, "TestNamespaceInit.Source");
                var foundSource = results.Any(r => IsRelatedToProperty(r, source, "Name"));

                foundSource.Should().BeTrue("Should find Source.Name when searching from init accessor");
            });
        });
    }

    private static bool IsRelatedToProperty(FindResult result, ITypeElement type, string propertyName)
    {
        var property = type.Properties.FirstOrDefault(p => p.ShortName == propertyName);
        if (property == null) return false;

        return result is FindResultDeclaredElement { DeclaredElement: var de } && de.Equals(property) ||
               result is FindResultInitializer { Declaration: var node } && property.GetDeclarations().Any(d => d.Contains(node));
    }
}