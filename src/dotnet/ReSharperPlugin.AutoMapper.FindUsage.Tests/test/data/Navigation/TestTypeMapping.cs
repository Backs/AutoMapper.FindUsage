namespace AutoMapper
{
    public class Profile
    {
        protected void CreateMap<TSource, TDestination>() {}
    }
}

public class SourceType
{
}

public class DestinationType
{
}

public class MyProfile : AutoMapper.Profile
{
    public MyProfile()
    {
        CreateMap<SourceType, DestinationType>();
    }
}
