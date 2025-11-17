using AutoMapper;
using CareWork.Infrastructure.Models;
using CareWork.API.Models.DTOs;

namespace CareWork.API.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Checkin, CheckinDto>();
        CreateMap<Tip, TipDto>();
    }
}

