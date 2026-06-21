using AutoMapper;

namespace TestNamespaceMultiple
{
    public class Source1
    {
        public string Name { get; set; }
    }

    public class Source2
    {
        public string Name { get; set; }
    }

    public class Destination
    {
        public string Name { get; set; }
    }

    public class MultipleProfile : Profile
    {
        public MultipleProfile()
        {
            CreateMap<Source1, Destination>();
            CreateMap<Source2, Destination>();
        }
    }
}
