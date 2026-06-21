using AutoMapper;

namespace TestNamespace
{
    public class Source
    {
        public string Name { get; set; }
    }

    public class Destination
    {
        public string Name { get; set; }
    }

    public class OneWayProfile : Profile
    {
        public OneWayProfile()
        {
            CreateMap<Source, Destination>();
        }
    }
}
