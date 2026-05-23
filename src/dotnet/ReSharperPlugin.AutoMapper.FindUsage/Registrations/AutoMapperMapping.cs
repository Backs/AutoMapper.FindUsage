using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.AutoMapper.FindUsage.Registrations;

public sealed class AutoMapperMapping
{
    public IType Source { get; }
    public IType Destination { get; }
    public ITreeNode Registration { get; }

    public AutoMapperMapping(IType source, IType destination, ITreeNode registration)
    {
        Source = source;
        Destination = destination;
        Registration = registration;
    }
}