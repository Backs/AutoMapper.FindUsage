using System.IO;
using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.AutoMapper.FindUsage.Registrations;

namespace ReSharperPlugin.AutoMapper.FindUsage.Tests.Navigation;

[TestFixture]
[TestNet60]
public class AutoMapperSearchFactoryTest : BaseTestWithSingleProject
{
    protected override string RelativeTestDataPath => "Navigation";

    [TestCase("TestTypeMapping")]
    [TestCase("TestPropertyMapping")]
    [TestCase("TestPropertyAccessorMapping")]
    public void TestMapping(string fileName)
    {
        DoTestSolution(fileName + ".cs");
    }

    protected override void DoTest(Lifetime lifetime, IProject project)
    {
        var solution = project.GetSolution();
        var projectFile = project.GetAllProjectFiles().First();
        var sourceFile = projectFile.ToSourceFile();
        var factory = solution.GetComponent<AutoMapperSearchFactory>();
        
        ExecuteWithGold(sourceFile, writer =>
        {
            var cache = solution.GetComponent<AutoMapperCache>();
            writer.WriteLine("Mappings in cache:");
            foreach (var entry in cache.Map)
            {
                foreach (var mapping in entry.Value)
                {
                    writer.WriteLine($"  {mapping.SourceTypeClrName} -> {mapping.DestinationTypeClrName}");
                }
            }

            var psiFile = sourceFile.GetPrimaryPsiFile();
            psiFile.ProcessDescendants(new MyProcessor(factory, writer));
        });
    }

    private class MyProcessor : IRecursiveElementProcessor
    {
        private readonly AutoMapperSearchFactory _factory;
        private readonly TextWriter _writer;

        public MyProcessor(AutoMapperSearchFactory factory, TextWriter writer)
        {
            _factory = factory;
            _writer = writer;
        }

        public bool ProcessingIsFinished => false;
        public bool InteriorShouldBeProcessed(ITreeNode element) => true;

        public void ProcessBeforeInterior(ITreeNode element)
        {
            IDeclaredElement declaredElement = null;
            if (element is ITypeDeclaration typeDeclaration)
                declaredElement = typeDeclaration.DeclaredElement;
            else if (element is IPropertyDeclaration propertyDeclaration)
                declaredElement = propertyDeclaration.DeclaredElement;
            else if (element is IAccessorDeclaration accessorDeclaration)
                declaredElement = accessorDeclaration.DeclaredElement;

            if (declaredElement != null)
            {
                var clrName = (declaredElement as ITypeElement)?.GetClrName()?.FullName ?? "N/A";
                _writer.WriteLine($"Element: {declaredElement.ShortName}, CLR: {clrName}");
                var related = _factory.GetRelatedFindResults(declaredElement).ToList();
                if (related.Count > 1) // 1 because it always returns itself
                {
                    _writer.WriteLine($"Element: {declaredElement.ShortName} ({declaredElement.GetElementType().PresentableName})");
                    foreach (var result in related.OrderBy(r => r.ToString()))
                    {
                        if (result is FindResultDeclaredElement frde && !Equals(frde.DeclaredElement, declaredElement))
                        {
                             _writer.WriteLine($"  Related: {frde.DeclaredElement.ShortName} ({frde.DeclaredElement.GetElementType().PresentableName})");
                        }
                    }
                }
            }
        }

        public void ProcessAfterInterior(ITreeNode element) {}
    }
}
