using AutoMapper;

namespace TestNamespaceInit
{
    public class Source
    {
        public string Name { get; set; }
    }

    public class Destination
    {
        public string Name { get; init; }
    }

    public class InitProfile : Profile
    {
        public InitProfile()
        {
            CreateMap<Source, Destination>();
        }
    }
}
