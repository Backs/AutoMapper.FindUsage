using AutoMapper;

namespace TestNamespaceReverse
{
    public class Source
    {
        public string Name { get; set; }
    }

    public class Destination
    {
        public string Name { get; set; }
    }

    public class ReverseProfile : Profile
    {
        public ReverseProfile()
        {
            CreateMap<Source, Destination>().ReverseMap();
        }
    }
}
