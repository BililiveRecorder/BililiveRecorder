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

            this.Field<ListGraphType<RoomType>>("rooms", resolve: context => this.recorder.Rooms);

            this.Field<RoomType>("room",
                arguments: new QueryArguments(
                    new QueryArgument<IdGraphType> { Name = "objectId" },
                    new QueryArgument<IntGraphType> { Name = "roomId" }
                ),
                resolve: context =>
                {
                    var objectId = context.GetArgument<Guid>("objectId");
                    var roomid = context.GetArgument<int>("roomId");

                    var room = objectId != default
                        ? this.recorder.Rooms.FirstOrDefault(x => x.ObjectId == objectId)
                        : this.recorder.Rooms.FirstOrDefault(x => x.RoomConfig.RoomId == roomid || x.ShortId == roomid);

                    return room;
                }
            );
        }
    }
}
