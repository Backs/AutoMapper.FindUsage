using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.ReSharper.Psi;

namespace ReSharperPlugin.AutoMapper.FindUsage.Registrations;

public sealed class AutoMapperMapping
{
    public IType Source { get; }
    public IType Destination { get; }
    public ICollection<string> IgnoredProperties { get; }

    public AutoMapperMapping(IType source, IType destination, ICollection<string> ignoredProperties = null)
    {
        Source = source;
        Destination = destination;
        IgnoredProperties = ignoredProperties ?? ImmutableHashSet<string>.Empty;
    }
}