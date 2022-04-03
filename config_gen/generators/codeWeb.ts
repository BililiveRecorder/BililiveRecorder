import { ConfigEntry, ConfigEntryType } from "../types"
import { trimEnd } from "../utils";

export default function (data: ConfigEntry[]): string {
    let result = `using BililiveRecorder.Core.Config;
using BililiveRecorder.Core.Config.V3;
using GraphQL.Types;
using HierarchicalPropertyDefault;
#nullable enable
`;
    function write_query_graphType_property(r: ConfigEntry) {
        if (r.configType == "roomOnly") {
            result += `this.Field(x => x.${r.name});\n`;
        } else {
            result += `this.Field(x => x.Optional${r.name}, type: typeof(HierarchicalOptionalType<${trimEnd(r.type, '?')}>));\n`;
        }
    }

    function write_rest_dto_property(r: ConfigEntry) {
        if (r.configType == "roomOnly") {
            result += `public ${r.type} ${r.name} { get; set; }\n`;
        } else {
            result += `public Optional<${r.type}> Optional${r.name} { get; set; }\n`;
        }
    }

    function write_mutation_graphType_property(r: ConfigEntry) {
        if (r.configType == "roomOnly") {
            result += `this.Field(x => x.${r.name}, nullable: true);\n`;
        } else {
            result += `this.Field(x => x.Optional${r.name}, nullable: true, type: typeof(HierarchicalOptionalInputType<${trimEnd(r.type, '?')}>));\n`;
        }
    }

    function write_mutation_dataType_property(r: ConfigEntry) {
        if (r.configType == "roomOnly") {
            result += `public ${r.type}? ${r.name} { get; set; }\n`;
        } else {
            result += `public Optional<${r.type}>? Optional${r.name} { get; set; }\n`;
        }
    }

    function write_mutation_apply_method(r: ConfigEntry) {
        if (r.configType == "roomOnly") {
            result += `if (this.${r.name}.HasValue) config.${r.name} = this.${r.name}.Value;\n`;
        } else {
            result += `if (this.Optional${r.name}.HasValue) config.Optional${r.name} = this.Optional${r.name}.Value;\n`;
        }
    }

    // +++++++++++++++++ Shared +++++++++++++++++

    result += 'namespace BililiveRecorder.Web.Models\n{\n'

    { // ====== SetRoomConfig ======
        result += "public class SetRoomConfig\n{\n"

        data.filter(x => x.configType != "globalOnly" && !x.webReadonly)
            .forEach(r => write_mutation_dataType_property(r));

        result += "\npublic void ApplyTo(RoomConfig config)\n{\n";

        data.filter(x => x.configType != "globalOnly" && !x.webReadonly)
            .forEach(r => write_mutation_apply_method(r));

        result += "}\n}\n\n";
    }

    { // ====== SetGlobalConfig ======
        result += "public class SetGlobalConfig\n{\n"

        data.filter(r => r.configType != "roomOnly" && !r.webReadonly)
            .forEach(r => write_mutation_dataType_property(r));

        result += "\npublic void ApplyTo(GlobalConfig config)\n{\n";

        data.filter(r => r.configType != "roomOnly" && !r.webReadonly)
            .forEach(r => write_mutation_apply_method(r));

        result += "}\n}\n\n";
    }

    // +++++++++++++++++ REST +++++++++++++++++
    result += '}\n\nnamespace BililiveRecorder.Web.Models.Rest\n{\n'

    { // ====== RoomConfigDto ======
        result += "public class RoomConfigDto\n{\n"

        data.filter(x => x.configType != "globalOnly" && !x.webReadonly)
            .forEach(r => write_rest_dto_property(r));

        result += "}\n\n";
    }

    { // ====== GlobalConfigDto ======
        result += "public class GlobalConfigDto\n{\n"

        data.filter(r => r.configType != "roomOnly" && !r.webReadonly)
            .forEach(r => write_rest_dto_property(r));

        result += "}\n\n";
    }

    // +++++++++++++++++ Graphql +++++++++++++++++
    result += '}\n\nnamespace BililiveRecorder.Web.Models.Graphql\n{\n'

    { // ====== RoomConfigType ======
        result += "internal class RoomConfigType : ObjectGraphType<RoomConfig>\n{\n";
        result += "public RoomConfigType()\n{\n"

        data.filter(r => r.configType != "globalOnly").forEach(r => write_query_graphType_property(r));

        result += "}\n}\n\n";
    }

    { // ====== GlobalConfigType ======
        result += "internal class GlobalConfigType : ObjectGraphType<GlobalConfig>\n{\n"
        result += "public GlobalConfigType()\n{\n";

        data.filter(r => r.configType != "roomOnly")
            .forEach(r => write_query_graphType_property(r));

        result += "}\n}\n\n";
    }

    { // ====== DefaultConfigType ======
        result += "internal class DefaultConfigType : ObjectGraphType<DefaultConfig>\n{\n"
        result += "public DefaultConfigType()\n{\n";

        data.filter(r => r.configType != "roomOnly")
            .forEach(r => {
                result += `this.Field(x => x.${r.name});\n`;
            });

        result += "}\n}\n\n";
    }

    { // ====== SetRoomConfigType ======
        result += "internal class SetRoomConfigType : InputObjectGraphType<SetRoomConfig>\n{\n"
        result += "public SetRoomConfigType()\n{\n";

        data.filter(x => x.configType != "globalOnly" && !x.webReadonly)
            .forEach(r => write_mutation_graphType_property(r));

        result += "}\n}\n\n";
    }


    { // ====== SetGlobalConfigType ======
        result += "internal class SetGlobalConfigType : InputObjectGraphType<SetGlobalConfig>\n{\n"
        result += "public SetGlobalConfigType()\n{\n";

        data.filter(r => r.configType != "roomOnly" && !r.webReadonly)
            .forEach(r => write_mutation_graphType_property(r));

        result += "}\n}\n\n";
    }

    result += `}\n`;
    return result;
}
