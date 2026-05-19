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
    public class AutoMapper.FindUsageTestEnvironmentZone : ITestsEnvZone, IRequire<PsiFeatureTestZone>, IRequire<IAutoMapper.FindUsageZone> { }

    [ZoneMarker]
    public class ZoneMarker : IRequire<ICodeEditingZone>, IRequire<ILanguageCSharpZone>, IRequire<AutoMapper.FindUsageTestEnvironmentZone> { }

    [SetUpFixture]
    public class AutoMapper.FindUsageTestsAssembly : ExtensionTestEnvironmentAssembly<AutoMapper.FindUsageTestEnvironmentZone> { }
}
