using System;
using BililiveRecorder.Core;
using GraphQL.Types;

namespace BililiveRecorder.Web.Graphql
{
    public class RecorderSchema : Schema
    {
        public RecorderSchema(IServiceProvider services, IRecorder recorder) : base(services)
        {
            this.Query = new RecorderQuery(recorder);
            this.Mutation = new RecorderMutation(recorder);
            //this.Subscription = new RecorderSubscription();
        }
    }
}
