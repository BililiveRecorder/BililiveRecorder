using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AutoMapper;
using BililiveRecorder.Core;
using BililiveRecorder.Web.Models;
using BililiveRecorder.Web.Models.Rest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BililiveRecorder.Web.Api
{
    [ApiController, Route("api/[controller]", Name = "[controller] [action]")]
    public class RoomController : ControllerBase
    {
        private readonly IMapper mapper;
        private readonly IRecorder recorder;

        public RoomController(IMapper mapper, IRecorder recorder)
        {
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            this.recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IRoom? FetchRoom(int roomId) => this.recorder.Rooms.FirstOrDefault(x => x.ShortId == roomId || x.RoomConfig.RoomId == roomId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IRoom? FetchRoom(Guid objectId) => this.recorder.Rooms.FirstOrDefault(x => x.ObjectId == objectId);

        /// <summary>
        /// 列出所有直播间
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public RoomDto[] GetRooms() => this.mapper.Map<RoomDto[]>(this.recorder.Rooms);

        #region Create & Delete

        /// <summary>
        /// 添加直播间
        /// </summary>
        /// <param name="createRoom"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RestApiError), StatusCodes.Status400BadRequest)]
        public ActionResult<RoomDto> CreateRoom([FromBody] CreateRoomDto createRoom)
        {
            if (createRoom.RoomId <= 0)
                return this.BadRequest(new RestApiError { Code = RestApiErrorCode.RoomidOutOfRange, Message = "Roomid must be greater than 0." });

            var room = this.FetchRoom(createRoom.RoomId);

            if (room is not null)
            {
                if (room.RoomConfig.AutoRecord != createRoom.AutoRecord)
                    room.RoomConfig.AutoRecord = createRoom.AutoRecord;
            }
            else
            {
                room = this.recorder.AddRoom(createRoom.RoomId, createRoom.AutoRecord);
            }

            return this.mapper.Map<RoomDto>(room);
        }

        /// <summary>
        /// 删除直播间
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        [HttpDelete("{roomId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RestApiError), StatusCodes.Status404NotFound)]
        public ActionResult<RoomDto> DeleteRoom(int roomId)
        {
            var room = this.FetchRoom(roomId);

            if (room is null)
                return this.NotFound(new RestApiError { Code = RestApiErrorCode.RoomNotFound, Message = "Room not found" });

            this.recorder.RemoveRoom(room);

            return this.mapper.Map<RoomDto>(room);
        }

        /// <summary>
        /// 删除直播间
        /// </summary>
        /// <param name="objectId"></param>
        /// <returns></returns>
        [HttpDelete("{objectId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RestApiError), StatusCodes.Status404NotFound)]
        public ActionResult<RoomDto> DeleteRoom(Guid objectId)
        {
            var room = this.FetchRoom(objectId);

            if (room is null)
                return this.NotFound(new RestApiError { Code = RestApiErrorCode.RoomNotFound, Message = "Room not found" });

            this.recorder.RemoveRoom(room);

            return this.mapper.Map<RoomDto>(room);
        }

        #endregion
        #region Get Room

        /// <summary>
        /// 读取一个直播间
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        [HttpGet("{roomId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RestApiError), StatusCodes.Status404NotFound)]
        public ActionResult<RoomDto> GetRoom(int roomId)
        {
            var room = this.FetchRoom(roomId);
            if (room is null)
                return this.NotFound(new RestApiError { Code = RestApiErrorCode.RoomNotFound, Message = "Room not found" });
            return this.mapper.Map<RoomDto>(room);
        }

        /// <summary>
        /// 读取一个直播间
        /// </summary>
        /// <param name="objectId"></param>
        /// <returns></returns>
        [HttpGet("{objectId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RestApiError), StatusCodes.Status404NotFound)]
        public ActionResult<RoomDto> GetRoom(Guid objectId)
        {
            var room = this.FetchRoom(objectId);
            if (room is null)
                return this.NotFound(new RestApiError { Code = RestApiErrorCode.RoomNotFound, Message = "Room not found" });
            return this.mapper.Map<RoomDto>(room);
        }

        #endregion
        #region Get Room Stats

        /// <summary>
        /// 读取直播间统计信息
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        [HttpGet("{roomId:int}/stats")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RestApiError), StatusCodes.Status404NotFound)]
        public ActionResult<RoomRecordingStatsDto> GetRoomStats(int roomId)
        {
            var room = this.FetchRoom(roomId);
            if (room is null)
                return this.NotFound(new RestApiError { Code = RestApiErrorCode.RoomNotFound, Message = "Room not found" });
            return this.mapper.Map<RoomRecordingStatsDto>(room.Stats);
        }

        /// <summary>
        /// 读取直播间统计信息
        /// </summary>
        /// <param name="objectId"></param>
        /// <returns></returns>
        [HttpGet("{objectId:guid}/stats")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RestApiError), StatusCodes.Status404NotFound)]
        public ActionResult<RoomRecordingStatsDto> GetRoomStats(Guid objectId)
        {
            var room = this.FetchRoom(objectId);
            if (room is null)
                return this.NotFound(new RestApiError { Code = RestApiErrorCode.RoomNotFound, Message = "Room not found" });
            return this.mapper.Map<RoomRecordingStatsDto>(room.Stats);
        }

        #endregion
        #region Room Config

        /// <summary>
        /// 读取直播间设置
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        [HttpGet("{roomId:int}/config")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RestApiError), StatusCodes.Status404NotFound)]
        public ActionResult<RoomConfigDto> GetRoomConfig(int roomId)
        {
            var room = this.FetchRoom(roomId);
            if (room is null)
                return this.NotFound(new RestApiError { Code = RestApiErrorCode.RoomNotFound, Message = "Room not found" });
            return this.mapper.Map<RoomConfigDto>(room.RoomConfig);
        }

        /// <summary>
        /// 读取直播间设置
        /// </summary>
        /// <param name="objectId"></param>
        /// <returns></returns>
        [HttpGet("{objectId:guid}/config")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RestApiError), StatusCodes.Status404NotFound)]
        public ActionResult<RoomConfigDto> GetRoomConfig(Guid objectId)
        {
            var room = this.FetchRoom(objectId);
            if (room is null)
                return this.NotFound(new RestApiError { Code = RestApiErrorCode.RoomNotFound, Message = "Room not found" });
            return this.mapper.Map<RoomConfigDto>(room.RoomConfig);
        }

        /// <summary>
        /// 修改直播间设置
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        [HttpPost("{roomId:int}/config")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RestApiError), StatusCodes.Status404NotFound)]
        public ActionResult<RoomConfigDto> SetRoomConfig(int roomId, [FromBody] SetRoomConfig config)
        {
            var room = this.FetchRoom(roomId);
            if (room is null)
                return this.NotFound(new RestApiError { Code = RestApiErrorCode.RoomNotFound, Message = "Room not found" });

            config.ApplyTo(room.RoomConfig);

            return this.mapper.Map<RoomConfigDto>(room.RoomConfig);
        }

        /// <summary>
        /// 修改直播间设置
        /// </summary>
        /// <param name="objectId"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        [HttpPost("{objectId:guid}/config")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RestApiError), StatusCodes.Status404NotFound)]
        public ActionResult<RoomConfigDto> SetRoomConfig(Guid objectId, [FromBody] SetRoomConfig config)
        {
            var room = this.FetchRoom(objectId);
            if (room is null)
                return this.NotFound(new RestApiError { Code = RestApiErrorCode.RoomNotFound, Message = "Room not found" });

            config.ApplyTo(room.RoomConfig);

            return this.mapper.Map<RoomConfigDto>(room.RoomConfig);
        }

        #endregion
        #region Room Action

        /// <summary>
        /// 开始录制
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        [HttpPost("{roomId:int}/start")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RestApiError), StatusCodes.Status404NotFound)]
        public ActionResult<RoomDto> StartRecording(int roomId)
        {
            var room = this.FetchRoom(roomId);
            if (room is null)
                return this.NotFound(new RestApiError { Code = RestApiErrorCode.RoomNotFound, Message = "Room not found" });

            room.StartRecord();

            return this.mapper.Map<RoomDto>(room);
        }

        /// <summary>
        /// 开始录制
        /// </summary>
        /// <param name="objectId"></param>
        /// <returns></returns>
        [HttpPost("{objectId:guid}/start")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RestApiError), StatusCodes.Status404NotFound)]
        public ActionResult<RoomDto> StartRecording(Guid objectId)
        {
            var room = this.FetchRoom(objectId);
            if (room is null)
                return this.NotFound(new RestApiError { Code = RestApiErrorCode.RoomNotFound, Message = "Room not found" });

            room.StartRecord();

            return this.mapper.Map<RoomDto>(room);
        }

        /// <summary>
        /// 停止录制
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        [HttpPost("{roomId:int}/stop")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RestApiError), StatusCodes.Status404NotFound)]
        public ActionResult<RoomDto> StopRecording(int roomId)
        {
            var room = this.FetchRoom(roomId);
            if (room is null)
                return this.NotFound(new RestApiError { Code = RestApiErrorCode.RoomNotFound, Message = "Room not found" });

            room.StopRecord();

            return this.mapper.Map<RoomDto>(room);
        }

        /// <summary>
        /// 停止录制
        /// </summary>
        /// <param name="objectId"></param>
        /// <returns></returns>
        [HttpPost("{objectId:guid}/stop")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RestApiError), StatusCodes.Status404NotFound)]
        public ActionResult<RoomDto> StopRecording(Guid objectId)
        {
            var room = this.FetchRoom(objectId);
            if (room is null)
                return this.NotFound(new RestApiError { Code = RestApiErrorCode.RoomNotFound, Message = "Room not found" });

            room.StopRecord();

            return this.mapper.Map<RoomDto>(room);
        }

        /// <summary>
        /// 手动分段
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        [HttpPost("{roomId:int}/split")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RestApiError), StatusCodes.Status404NotFound)]
        public ActionResult<RoomDto> SplitRecording(int roomId)
        {
            var room = this.FetchRoom(roomId);
            if (room is null)
                return this.NotFound(new RestApiError { Code = RestApiErrorCode.RoomNotFound, Message = "Room not found" });

            room.SplitOutput();

            return this.mapper.Map<RoomDto>(room);
        }

        /// <summary>
        /// 手动分段
        /// </summary>
        /// <param name="objectId"></param>
        /// <returns></returns>
        [HttpPost("{objectId:guid}/split")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RestApiError), StatusCodes.Status404NotFound)]
        public ActionResult<RoomDto> SplitRecording(Guid objectId)
        {
            var room = this.FetchRoom(objectId);
            if (room is null)
                return this.NotFound(new RestApiError { Code = RestApiErrorCode.RoomNotFound, Message = "Room not found" });

            room.SplitOutput();

            return this.mapper.Map<RoomDto>(room);
        }

        /// <summary>
        /// 刷新直播间信息
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        [HttpPost("{roomId:int}/refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RestApiError), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RoomDto>> RefreshRecordingAsync(int roomId)
        {
            var room = this.FetchRoom(roomId);
            if (room is null)
                return this.NotFound(new RestApiError { Code = RestApiErrorCode.RoomNotFound, Message = "Room not found" });

            await room.RefreshRoomInfoAsync().ConfigureAwait(false);

            return this.mapper.Map<RoomDto>(room);
        }

        /// <summary>
        /// 刷新直播间信息
        /// </summary>
        /// <param name="objectId"></param>
        /// <returns></returns>
        [HttpPost("{objectId:guid}/refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RestApiError), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RoomDto>> RefreshRecordingAsync(Guid objectId)
        {
            var room = this.FetchRoom(objectId);
            if (room is null)
                return this.NotFound(new RestApiError { Code = RestApiErrorCode.RoomNotFound, Message = "Room not found" });

            await room.RefreshRoomInfoAsync().ConfigureAwait(false);

            return this.mapper.Map<RoomDto>(room);
        }

        #endregion
    }
}
