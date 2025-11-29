using System;
using System.Linq;
using BililiveRecorder.Core;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.Web.Models;
using BililiveRecorder.Web.Models.Graphql;
using GraphQL;
using GraphQL.Types;

namespace BililiveRecorder.Web.Graphql
{
    internal class RecorderQuery : ObjectGraphType
    {
        private readonly IRecorder recorder;

        public RecorderQuery(IRecorder recorder)
        {
            this.recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));

            this.SetupFields();
        }

        private void SetupFields()
        {
            this.Field<RecorderVersionType>("version", resolve: context => RecorderVersion.Instance);

            this.Field<GlobalConfigType>("config", resolve: context => this.recorder.Config.Global);

            this.Field<DefaultConfigType>("defaultConfig", resolve: context => DefaultConfig.Instance);

            this.Field<ListGraphType<RoomType>>("rooms", arguments: new QueryArguments(
                    new QueryArgument<ListGraphType<IdGraphType>> { Name = "objectIds" },
                    new QueryArgument<ListGraphType<IntGraphType>> { Name = "roomIds" }
                ),
                resolve: context =>
                {
                    var objectIds = context.GetArgument<Guid[]>("objectIds");
                    var roomIds = context.GetArgument<int[]>("roomIds");

                    // If no arguments are provided, return all rooms
                    if (objectIds == null && roomIds == null)
                        return this.recorder.Rooms;

                    // Remove any "0" from the roomIds
                    roomIds = roomIds?.Where(x => x != 0).ToArray();

                    // Otherwise, filter the rooms
                    return this.recorder.Rooms.Where(x =>
                        (objectIds?.Contains(x.ObjectId) ?? false) ||
                        (roomIds?.Contains(x.RoomConfig.RoomId) ?? false) ||
                        (roomIds?.Contains(x.ShortId) ?? false)
                    );
                });

            this.Field<RoomType>("room",
                arguments: new QueryArguments(
                    new QueryArgument<IdGraphType> { Name = "objectId" },
                    new QueryArgument<IntGraphType> { Name = "roomId" }
                ),
                resolve: context =>
                {
                    var objectId = context.GetArgument<Guid>("objectId");
                    var roomId = context.GetArgument<int>("roomId");

                    IRoom? room;
                    if (objectId != default)
                        room = this.recorder.Rooms.FirstOrDefault(x => x.ObjectId == objectId);
                    else if (roomId != 0)
                        room = this.recorder.Rooms.FirstOrDefault(x => x.RoomConfig.RoomId == roomId || x.ShortId == roomId);
                    else
                        room = null;

                    return room;
                }
            );
        }
    }
}
