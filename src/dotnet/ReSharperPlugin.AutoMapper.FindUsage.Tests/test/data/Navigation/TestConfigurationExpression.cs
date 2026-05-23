using AutoMapper;

namespace TestProject;

public class SourceDto { public string Name { get; set; } }
public class DestinationDto { public string Name { get; set{on}; } }

public class Startup
{
    public void Configure()
    {
        var config = new MapperConfiguration(cfg => 
        {
            cfg.CreateMap<SourceDto, DestinationDto>();
        });
    }
}
