using System.Collections.Generic;
using JetBrains.Serialization;

namespace ReSharperPlugin.AutoMapper.FindUsage.Registrations;

public class SerializableMapping
{
    public string SourceTypeClrName;
    public string DestinationTypeClrName;
    public List<string> IgnoredProperties;
    public bool HasReverseMap;

    public void Write(UnsafeWriter writer)
    {
        writer.WriteString(SourceTypeClrName);
        writer.WriteString(DestinationTypeClrName);
        writer.WriteInt32(IgnoredProperties.Count);
        foreach (var prop in IgnoredProperties) 
            writer.WriteString(prop);
        writer.WriteBoolean(HasReverseMap);
    }

    public static SerializableMapping Read(UnsafeReader reader)
    {
        var mapping = new SerializableMapping
        {
            SourceTypeClrName = reader.ReadString(),
            DestinationTypeClrName = reader.ReadString()
        };
        var count = reader.ReadInt32();
        mapping.IgnoredProperties = new List<string>(count);
        for (var i = 0; i < count; i++) 
            mapping.IgnoredProperties.Add(reader.ReadString());
        mapping.HasReverseMap = reader.ReadBoolean();
        return mapping;
    }
}
