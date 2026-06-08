using System;

namespace AutoMapper
{
    public class Profile
    {
        protected void CreateMap<TSource, TDestination>() => throw new NotImplementedException();
    }
}

namespace TestNamespace
{
    public class SourceType
    {
        public string Name { get; set; }
    }

    public class DestinationType
    {
        public string Name { get; set; }
    }

    public class MyProfile : AutoMapper.Profile
    {
        public MyProfile()
        {
            CreateMap<SourceType, DestinationType>();
        }
    }
}
