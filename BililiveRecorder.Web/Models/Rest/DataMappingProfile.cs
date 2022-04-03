using AutoMapper;
using BililiveRecorder.Core;
using BililiveRecorder.Core.Config.V2;

namespace BililiveRecorder.Web.Models.Rest
{
    public class DataMappingProfile : Profile
    {
        public DataMappingProfile()
        {
            this.CreateMap<IRoom, RoomDto>()
                .ForMember(x => x.RoomId, x => x.MapFrom(s => s.RoomConfig.RoomId))
                .ForMember(x => x.AutoRecord, x => x.MapFrom(s => s.RoomConfig.AutoRecord))
                ;

            this.CreateMap<RecordingStats, RoomStatsDto>()
                .ForMember(x => x.SessionDuration, x => x.MapFrom(s => s.SessionDuration.TotalMilliseconds))
                .ForMember(x => x.SessionMaxTimestamp, x => x.MapFrom(s => s.SessionMaxTimestamp.TotalMilliseconds))
                .ForMember(x => x.FileMaxTimestamp, x => x.MapFrom(s => s.FileMaxTimestamp.TotalMilliseconds))
                ;

            this.CreateMap<RoomConfig, RoomConfigDto>();

            this.CreateMap<GlobalConfig, GlobalConfigDto>();
        }
    }
}
