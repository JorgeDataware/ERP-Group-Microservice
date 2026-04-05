using AutoMapper;
using GroupsMicroservice.Models.DB;
using GroupsMicroservice.Models.Request;

namespace GroupsMicroservice.Mappers;

public class GroupMappers : Profile
{
    public GroupMappers()
    {
        CreateMap<AddGroupRequest, Group>().ReverseMap();
        CreateMap<EditGroupRequest, Group>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ReverseMap();
    }
}
