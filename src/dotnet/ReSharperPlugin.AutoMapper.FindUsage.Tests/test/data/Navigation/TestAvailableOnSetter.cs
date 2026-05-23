using AutoMapper;

namespace TestProject;

public class SourceDto
{
    public string Name { get; set; }
}

public class DestinationDto
{
    public string Name { get; set{on}; }
}

public class TestProfile : Profile
{
    public TestProfile()
    {
        CreateMap<SourceDto, DestinationDto>();
    }
}
