using System.Threading;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework;
using JetBrains.TestFramework.Application.Zones;
using NUnit.Framework;

[assembly: Apartment(ApartmentState.STA)]

namespace ReSharperPlugin.AutoMapper.FindUsage.Tests
{
    [ZoneDefinition]
    public class AutoMapperFindUsageTestEnvironmentZone : ITestsEnvZone, IRequire<PsiFeatureTestZone>, IRequire<IAutoMapperFindUsageZone> { }

    [ZoneMarker]
    public class ZoneMarker : IRequire<ICodeEditingZone>, IRequire<ILanguageCSharpZone>, IRequire<AutoMapperFindUsageTestEnvironmentZone> { }

    [SetUpFixture]
    public class AutoMapperFindUsageTestsAssembly : ExtensionTestEnvironmentAssembly<AutoMapperFindUsageTestEnvironmentZone> { }
}
