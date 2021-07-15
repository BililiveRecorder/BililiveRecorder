using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using BililiveRecorder.Core.Config;
using BililiveRecorder.Core.Config.V2;
using Spectre.Console;

namespace BililiveRecorder.Cli.Configure
{
    public class ConfigureCommand : Command
    {
        public ConfigureCommand() : base("configure", "Interactively configure config.json")
        {
            this.AddArgument(new Argument("path") { Description = "Path to work directory or config.json" });
            this.Handler = CommandHandler.Create<string>(Run);
        }

        private static int Run(string path)
        {
            if (!FindConfig(path, out var config, out var fullPath))
                return 1;

            ShowRootMenu(config, fullPath);

            return 0;
        }

        private static void ShowRootMenu(ConfigV2 config, string fullPath)
        {
            while (true)
            {
                var selection = PromptEnumSelection<RootMenuSelection>();
                AnsiConsole.Clear();
                switch (selection)
                {
                    case RootMenuSelection.ListRooms:
                        ListRooms(config);
                        break;
                    case RootMenuSelection.AddRoom:
                        AddRoom(config);
                        break;
                    case RootMenuSelection.DeleteRoom:
                        DeleteRoom(config);
                        break;
                    case RootMenuSelection.SetRoomConfig:
                        // TODO
                        AnsiConsole.MarkupLine("[bold red]Not Implemented Yet[/]");
                        break;
                    case RootMenuSelection.SetGlobalConfig:
                        // TODO
                        AnsiConsole.MarkupLine("[bold red]Not Implemented Yet[/]");
                        break;
                    case RootMenuSelection.SetJsonSchema:
                        SetJsonSchema(config);
                        break;
                    case RootMenuSelection.Exit:
                        return;
                    case RootMenuSelection.SaveAndExit:
                        if (SaveConfig(config, fullPath))
                            return;
                        else
                            break;
                    default:
                        break;
                }
            }
        }

        private static void ListRooms(ConfigV2 config)
        {
            var table = new Table()
                .AddColumns("Roomid", "AutoRecord")
                .Border(TableBorder.Rounded);

            foreach (var room in config.Rooms)
            {
                table.AddRow(room.RoomId.ToString(), room.AutoRecord ? "[green]Enabled[/]" : "[red]Disabled[/]");
            }

            AnsiConsole.Render(table);
        }

        private static void AddRoom(ConfigV2 config)
        {
            while (true)
            {
                var roomid = AnsiConsole.Prompt(new TextPrompt<int>("[grey](type 0 to cancel)[/] [green]Roomid[/]:").Validate(x => x switch
                {
                    < 0 => ValidationResult.Error("[red]Roomid can't be negative[/]"),
                    _ => ValidationResult.Success(),
                }));

                if (roomid == 0)
                    break;

                if (config.Rooms.Any(x => x.RoomId == roomid))
                {
                    AnsiConsole.MarkupLine("[red]Room already exist[/]");
                    continue;
                }

                var autoRecord = AnsiConsole.Confirm("Enable auto record?");

                config.Rooms.Add(new RoomConfig { RoomId = roomid, AutoRecord = autoRecord });

                AnsiConsole.MarkupLine("[green]Room {0} added to config[/]", roomid);
            }
        }

        private static void DeleteRoom(ConfigV2 config)
        {
            var toBeDeleted = AnsiConsole.Prompt(new MultiSelectionPrompt<RoomConfig>()
                .Title("Delete rooms")
                .NotRequired()
                .UseConverter(r => r.RoomId.ToString())
                .PageSize(15)
                .MoreChoicesText("[grey](Move up and down to reveal more rooms)[/]")
                .InstructionsText("[grey](Press [blue]<space>[/] to toggle selection, [green]<enter>[/] to delete)[/]")
                .AddChoices(config.Rooms));

            for (var i = 0; i < toBeDeleted.Count; i++)
                config.Rooms.Remove(toBeDeleted[i]);

            AnsiConsole.MarkupLine("[green]{0} rooms deleted[/]", toBeDeleted.Count);
        }

        private static void SetJsonSchema(ConfigV2 config)
        {
            var selection = PromptEnumSelection<JsonSchemaSelection>();
            switch (selection)
            {
                case JsonSchemaSelection.Default:
                    config.DollarSignSchema = "https://raw.githubusercontent.com/Bililive/BililiveRecorder/dev-1.3/configV2.schema.json";
                    break;
                case JsonSchemaSelection.Custom:
                    config.DollarSignSchema = AnsiConsole.Prompt(new TextPrompt<string>("[green]JSON Schema[/]:").AllowEmpty());
                    break;
                default:
                    break;
            }
        }

        private static bool SaveConfig(ConfigV2 config, string fullPath)
        {
            try
            {
                var json = ConfigParser.SaveJson(config);
                using var file = new StreamWriter(File.Open(fullPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None));
                file.Write(json);
                return true;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]Write config failed[/]");
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths | ExceptionFormats.ShowLinks);
                return false;
            }
        }

        private static bool FindConfig(string path, [NotNullWhen(true)] out ConfigV2? config, out string fullPath)
        {
            if (File.Exists(path))
            {
                fullPath = Path.GetFullPath(path);
                goto readFile;
            }
            else if (Directory.Exists(path))
            {
                fullPath = Path.GetFullPath(Path.Combine(path, ConfigParser.CONFIG_FILE_NAME));
                goto readFile;
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Path does not exist.[/]");
                config = null;
                fullPath = string.Empty;
                return false;
            }

        readFile:
            config = ConfigParser.LoadJson(File.ReadAllText(fullPath, Encoding.UTF8));
            var result = config != null;
            if (!result)
                AnsiConsole.MarkupLine("[red]Load failed.\nBroken or corrupted file, or no permission.[/]");
            return result;
        }

        private static string EnumToDescriptionConverter<T>(T value) where T : struct, Enum
        {
            var type = typeof(T);
            var attrs = type.GetMember(Enum.GetName(type, value)!)[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
            return (attrs.Length > 0) ? ((DescriptionAttribute)attrs[0]).Description : string.Empty;
        }

        private static T PromptEnumSelection<T>() where T : struct, Enum => AnsiConsole.Prompt(new SelectionPrompt<T>().AddChoices(Enum.GetValues<T>()).UseConverter(EnumToDescriptionConverter));
    }
}
