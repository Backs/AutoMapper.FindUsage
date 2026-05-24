using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.AutoMapper.FindUsage.Registrations;

public sealed class AutoMapperMapping
{
    public IType Source { get; }
    public IType Destination { get; }
    public ITreeNode Registration { get; }
    public ISet<string> IgnoredProperties { get; }

    public AutoMapperMapping(IType source, IType destination, ITreeNode registration,
        ISet<string> ignoredProperties = null)
    {
        Source = source;
        Destination = destination;
        Registration = registration;
        IgnoredProperties = ignoredProperties ?? ImmutableHashSet<string>.Empty;
    }
}