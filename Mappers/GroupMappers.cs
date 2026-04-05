using AutoMapper;
using GroupsMicroservice.Models.DB;
using GroupsMicroservice.Models.Request;

namespace GroupsMicroservice.Mappers;

public class GroupMappers : Profile
{
    public GroupMappers()
    {
        CreateMap<AddGroupRequest, Group>().ReverseMap();
    }
}
