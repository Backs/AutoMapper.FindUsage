using System.Collections.Generic;
using JetBrains.Serialization;
using JetBrains.Util;

namespace ReSharperPlugin.AutoMapper.FindUsage.Registrations;

public class SerializableMapping
{
    public string SourceTypeClrName;
    public string DestinationTypeClrName;
    public List<string> IgnoredProperties;
    public int InvocationOffset;
    public bool HasReverseMap;

    public void Write(UnsafeWriter writer)
    {
        writer.Write(SourceTypeClrName);
        writer.Write(DestinationTypeClrName);
        writer.Write(IgnoredProperties.Count);
        foreach (var prop in IgnoredProperties) writer.Write(prop);
        writer.Write(InvocationOffset);
        writer.Write(HasReverseMap);
    }

    public static SerializableMapping Read(UnsafeReader reader)
    {
        var mapping = new SerializableMapping();
        mapping.SourceTypeClrName = reader.ReadString();
        mapping.DestinationTypeClrName = reader.ReadString();
        int count = reader.ReadInt32();
        mapping.IgnoredProperties = new List<string>(count);
        for (int i = 0; i < count; i++) mapping.IgnoredProperties.Add(reader.ReadString());
        mapping.InvocationOffset = reader.ReadInt32();
        mapping.HasReverseMap = reader.ReadBoolean();
        return mapping;
    }
}
