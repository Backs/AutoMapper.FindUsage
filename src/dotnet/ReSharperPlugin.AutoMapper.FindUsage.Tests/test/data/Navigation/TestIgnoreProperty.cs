using AutoMapper;

namespace TestProject;

public class SourceDto
{
    public string Name { get; set; }
}

public class DestinationDto
{
    public string Name { get; set{off}; }
}

public class TestProfile : Profile
{
    public TestProfile()
    {
        CreateMap<SourceDto, DestinationDto>()
            .ForMember(it => it.Name, exp => exp.Ignore());
    }
}
