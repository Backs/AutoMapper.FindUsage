using AutoMapper;

namespace TestNamespaceIgnored
{
    public class Source
    {
        public string Ignored { get; set; }
    }

    public class Destination
    {
        public string Ignored { get; set; }
    }

    public class IgnoredProfile : Profile
    {
        public IgnoredProfile()
        {
            CreateMap<Source, Destination>()
                .ForMember(d => d.Ignored, opt => opt.Ignore());
        }
    }
}
