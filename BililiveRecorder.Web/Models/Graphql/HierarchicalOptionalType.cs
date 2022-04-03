using GraphQL.Types;
using HierarchicalPropertyDefault;

namespace BililiveRecorder.Web.Models.Graphql
{
    public class HierarchicalOptionalType<TValue> : ObjectGraphType<Optional<TValue>>
    {
        public HierarchicalOptionalType()
        {
            this.Name = "HierarchicalOptional_" + typeof(TValue).Name + "_Type";

            this.Field(x => x.HasValue)
                .Description("Use 'value' when 'hasValue' is true, or use the value from parent object when 'hasValue' is false.");

            this.Field(x => x.Value, nullable: typeof(TValue) == typeof(string))
                .Description("NOTE: The value of this field is ignored when 'hasValue' is false.");
        }
    }
}
