using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;

namespace ReSharperPlugin.AutoMapper.FindUsage.Tests;

public static class PsiTestHelper
{
    public static ITypeElement GetTypeElement(ISolution solution, string clrName)
    {
        var psiServices = solution.GetPsiServices();
        foreach (var module in psiServices.Modules.GetModules())
        {
            var scope = psiServices.Symbols.GetSymbolScope(module, true, true);
            var type = scope.GetTypeElementByCLRName(clrName);
            if (type != null) return type;
        }
        return null;
    }
}
