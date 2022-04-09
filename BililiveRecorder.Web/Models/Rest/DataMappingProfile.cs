using AutoMapper;
using BililiveRecorder.Core;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.Core.Event;

namespace BililiveRecorder.Web.Models.Rest
{
    public class DataMappingProfile : Profile
    {
        public DataMappingProfile()
        {
            this.CreateMap<IRoom, RoomDto>()
                .ForMember(x => x.RoomId, x => x.MapFrom(s => s.RoomConfig.RoomId))
                .ForMember(x => x.AutoRecord, x => x.MapFrom(s => s.RoomConfig.AutoRecord))
                .ForMember(x => x.IoStats, x => x.MapFrom(s => s.Stats))
                .ForMember(x => x.RecordingStats, x => x.MapFrom(s => s.Stats))
                ;

            this.CreateMap<RoomStats, RoomRecordingStatsDto>()
                .ForMember(x => x.SessionDuration, x => x.MapFrom(s => s.SessionDuration.TotalMilliseconds))
                .ForMember(x => x.SessionMaxTimestamp, x => x.MapFrom(s => s.SessionMaxTimestamp.TotalMilliseconds))
                .ForMember(x => x.FileMaxTimestamp, x => x.MapFrom(s => s.FileMaxTimestamp.TotalMilliseconds))
                ;

            this.CreateMap<RoomStats, RoomIOStatsDto>()
                .ForMember(x => x.Duration, x => x.MapFrom(s => s.Duration.TotalMilliseconds))
                .ForMember(x => x.DiskWriteDuration, x => x.MapFrom(s => s.DiskWriteDuration.TotalMilliseconds))
                ;

            this.CreateMap<RecordingStatsEventArgs, RoomRecordingStatsDto>();

            this.CreateMap<RoomConfig, RoomConfigDto>();

            this.CreateMap<GlobalConfig, GlobalConfigDto>();
        }
    }
}
