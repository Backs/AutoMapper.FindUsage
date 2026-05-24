using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.AutoMapper.FindUsage.Navigation;

namespace ReSharperPlugin.AutoMapper.FindUsage.Tests.Navigation;

[TestFixture]
[TestPackages("AutoMapper")]
public class AutoMapperNavigationActionAvailabilityTest
    : CSharpContextActionAvailabilityTestBase<AutoMapperNavigationAction>
{
    protected override string RelativeTestDataPath => "Navigation";
    protected override string ExtraPath => "";

    [Test] public void TestAvailableOnSetter() => DoNamedTest();
    
    [Test] public void TestAvailableOnInit() => DoNamedTest();
    
    [Test] public void TestReverseMap() => DoNamedTest();

    [Test] public void TestReverseMapChain() => DoNamedTest();

    [Test] public void TestMultipleMappings() => DoNamedTest();

    [Test] public void TestConfigurationExpression() => DoNamedTest();

    [Test] public void TestIgnoreProperty() => DoNamedTest();

    [Test] public void TestNotAvailableOnGetter() => DoNamedTest();

    [Test] public void TestNotAvailableWithoutMapping() => DoNamedTest();

    [Test] public void TestNotAvailableOnUnmappedType() => DoNamedTest();
}
