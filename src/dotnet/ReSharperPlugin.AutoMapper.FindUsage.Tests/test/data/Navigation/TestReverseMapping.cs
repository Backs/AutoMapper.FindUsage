namespace AutoMapper
{
    public class Profile
    {
        protected IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>() => null;
    }

    public interface IMappingExpression<TSource, TDestination>
    {
        IMappingExpression<TSource, TDestination> ReverseMap();
    }
}

public class User
{
    public string Name { get; set; }
}

public class UserDto
{
    public string Name { get; set; }
}

public class UserWithReverse
{
    public string Name { get; set; }
}

public class UserDtoWithReverse
{
    public string Name { get; set; }
}

public class MyProfile : AutoMapper.Profile
{
    public MyProfile()
    {
        // One-way mapping: User -> UserDto
        // Navigation: UserDto -> User (OK), User -> UserDto (NO)
        CreateMap<User, UserDto>();

        // Two-way mapping: UserWithReverse <-> UserDtoWithReverse
        // Navigation: UserWithReverse <-> UserDtoWithReverse (OK both ways)
        CreateMap<UserWithReverse, UserDtoWithReverse>().ReverseMap();
    }
}
