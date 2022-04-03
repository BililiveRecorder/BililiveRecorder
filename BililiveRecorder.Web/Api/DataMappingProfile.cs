using AutoMapper;
using BililiveRecorder.Core;
using BililiveRecorder.Core.Config.V2;
using BililiveRecorder.Web.Models;

namespace BililiveRecorder.Web.Api
{
    public class DataMappingProfile : Profile
    {
        public DataMappingProfile()
        {
            this.CreateMap<IRoom, RoomDto>()
                .ForMember(x => x.RoomId, x => x.MapFrom(s => s.RoomConfig.RoomId))
                .ForMember(x => x.AutoRecord, x => x.MapFrom(s => s.RoomConfig.AutoRecord));

            this.CreateMap<RecordingStats, RoomStatsDto>();

            this.CreateMap<RoomConfig, RoomConfigDto>();

            this.CreateMap<GlobalConfig, GlobalConfigDto>();
        }
    }
}
