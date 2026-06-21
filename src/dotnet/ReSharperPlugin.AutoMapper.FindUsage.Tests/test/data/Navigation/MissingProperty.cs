using AutoMapper;

namespace TestNamespaceMissing
{
    public class Source
    {
        public string Name { get; set; }
    }

    public class Destination
    {
        public string Name { get; set; }
        public string Missing { get; set; }
    }

    public class MissingProfile : Profile
    {
        public MissingProfile()
        {
            CreateMap<Source, Destination>();
        }
    }
}
