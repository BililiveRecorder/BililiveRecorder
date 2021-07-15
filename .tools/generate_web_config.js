"use strict";

export default function generate_web_config(data) {
    let result = `using BililiveRecorder.Core.Config.V2;
using GraphQL.Types;
using HierarchicalPropertyDefault;

#nullable enable
namespace BililiveRecorder.Web.Schemas.Types
{
`;

    function write_query_graphType_property(r) {
        if (r.without_global) {
            result += `this.Field(x => x.${r.name});\n`;
        } else {
            result += `this.Field(x => x.Optional${r.name}, type: typeof(HierarchicalOptionalType<${r.type}>));\n`;
        }
    }

    function write_mutation_graphType_property(r) {
        if (r.without_global) {
            result += `this.Field(x => x.${r.name}, nullable: true);\n`;
        } else {
            result += `this.Field(x => x.Optional${r.name}, nullable: true, type: typeof(HierarchicalOptionalInputType<${r.type}>));\n`;
        }
    }

    function write_mutation_dataType_property(r) {
        if (r.without_global) {
            result += `public ${r.type + (r.nullable ? '?' : '')}? ${r.name} { get; set; }\n`;
        } else {
            result += `public Optional<${r.type + (r.nullable ? '?' : '')}>? Optional${r.name} { get; set; }\n`;
        }
    }

    function write_mutation_apply_method(r) {
        if (r.without_global) {
            result += `if (this.${r.name}.HasValue) config.${r.name} = this.${r.name}.Value;\n`;
        } else {
            result += `if (this.Optional${r.name}.HasValue) config.Optional${r.name} = this.Optional${r.name}.Value;\n`;
        }
    }

    { // ====== RoomConfigType ======
        result += "internal class RoomConfigType : ObjectGraphType<RoomConfig>\n{\n";
        result += "public RoomConfigType()\n{\n"

        data.room.forEach(r => write_query_graphType_property(r));

        result += "}\n}\n\n";
    }

    { // ====== GlobalConfigType ======
        result += "internal class GlobalConfigType : ObjectGraphType<GlobalConfig>\n{\n"
        result += "public GlobalConfigType()\n{\n";

        data.global
            .concat(data.room.filter(x => !x.without_global))
            .forEach(r => write_query_graphType_property(r));

        result += "}\n}\n\n";
    }

    { // ====== DefaultConfigType ======
        result += "internal class DefaultConfigType : ObjectGraphType<DefaultConfig>\n{\n"
        result += "public DefaultConfigType()\n{\n";

        data.global
            .concat(data.room.filter(x => !x.without_global))
            .forEach(r => {
                result += `this.Field(x => x.${r.name});\n`;
            });

        result += "}\n}\n\n";
    }

    { // ====== SetRoomConfig ======
        result += "internal class SetRoomConfig\n{\n"

        data.room.filter(x => !x.web_readonly)
            .forEach(r => write_mutation_dataType_property(r));

        result += "\npublic void ApplyTo(RoomConfig config)\n{\n";

        data.room.filter(x => !x.web_readonly)
            .forEach(r => write_mutation_apply_method(r));

        result += "}\n}\n\n";
    }

    { // ====== SetRoomConfigType ======
        result += "internal class SetRoomConfigType : InputObjectGraphType<SetRoomConfig>\n{\n"
        result += "public SetRoomConfigType()\n{\n";

        data.room.filter(x => !x.web_readonly)
            .forEach(r => write_mutation_graphType_property(r));

        result += "}\n}\n\n";
    }

    { // ====== SetGlobalConfig ======
        result += "internal class SetGlobalConfig\n{\n"

        data.global
            .concat(data.room.filter(x => !x.without_global))
            .filter(x => !x.web_readonly)
            .forEach(r => write_mutation_dataType_property(r));

        result += "\npublic void ApplyTo(GlobalConfig config)\n{\n";

        data.global
            .concat(data.room.filter(x => !x.without_global))
            .filter(x => !x.web_readonly)
            .forEach(r => write_mutation_apply_method(r));

        result += "}\n}\n\n";
    }

    { // ====== SetGlobalConfigType ======
        result += "internal class SetGlobalConfigType : InputObjectGraphType<SetGlobalConfig>\n{\n"
        result += "public SetGlobalConfigType()\n{\n";

        data.global
            .concat(data.room.filter(x => !x.without_global))
            .filter(x => !x.web_readonly)
            .forEach(r => write_mutation_graphType_property(r));

        result += "}\n}\n\n";
    }

    result += `}\n`;
    return result;
}
