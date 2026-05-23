using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.AutoMapper.FindUsage.Navigation;

namespace ReSharperPlugin.AutoMapper.FindUsage.Tests.Navigation;

[TestFixture]
public class AutoMapperNavigationActionWithoutAutoMapperAvailabilityTest
    : CSharpContextActionAvailabilityTestBase<AutoMapperNavigationAction>
{
    protected override string RelativeTestDataPath => "Navigation";
    protected override string ExtraPath => "";

    [Test] public void TestNotAvailableWithoutAutoMapper() => DoNamedTest();
}
