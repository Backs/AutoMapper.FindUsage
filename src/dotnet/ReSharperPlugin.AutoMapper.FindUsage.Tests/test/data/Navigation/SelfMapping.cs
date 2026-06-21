using AutoMapper;

namespace TestNamespaceSelf
{
    public class Source
    {
        public string Name { get; set; }
    }

    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Source, Source>().ReverseMap();
        }
    }
}
