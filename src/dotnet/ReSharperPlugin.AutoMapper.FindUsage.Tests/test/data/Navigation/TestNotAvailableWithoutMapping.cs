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
        // No CreateMap here
    }
}
