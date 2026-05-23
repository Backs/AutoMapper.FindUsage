using AutoMapper;

namespace TestProject;

public class Source1Dto { public string Name { get; set; } }
public class Source2Dto { public string Name { get; set; } }

public class DestinationDto
{
    public string Name { get; set{on}; }
}

public class TestProfile : Profile
{
    public TestProfile()
    {
        CreateMap<Source1Dto, DestinationDto>();
        CreateMap<Source2Dto, DestinationDto>();
    }
}
