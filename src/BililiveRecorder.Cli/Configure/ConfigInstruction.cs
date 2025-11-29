using System;
using Spectre.Console;

namespace BililiveRecorder.Cli.Configure
{
    public abstract class ConfigInstructionBase<TConfig> where TConfig : class
    {
        protected readonly Action<TConfig>? useDefaultValue;

        public ConfigInstructionBase(Action<TConfig>? useDefaultValue)
        {
            this.useDefaultValue = useDefaultValue;
        }

        public string Name { get; set; } = string.Empty;

        public string Descrption { get; set; } = string.Empty;

        public bool CanBeOptional { get; set; }

        public void UseDefaultValue(TConfig config) => this.useDefaultValue?.Invoke(config);

        public abstract void PromptForCustomValue(TConfig config);

    }

    public class ConfigInstruction<TConfig, TValue> : ConfigInstructionBase<TConfig>
        where TConfig : class
        where TValue : notnull
    {
        private readonly Action<TConfig, TValue>? setCustomValue;

        public ConfigInstruction(Action<TConfig>? useDefaultValue, Action<TConfig, TValue>? setCustomValue) : base(useDefaultValue)
        {
            this.setCustomValue = setCustomValue;
        }

        public override void PromptForCustomValue(TConfig config)
        {
            var vtype = typeof(TValue);
            TValue value;
            if (vtype.IsEnum)
            {
                value = AnsiConsole.Prompt(new SelectionPrompt<TValue>().AddChoices((TValue[])Enum.GetValues(typeof(TValue))));
            }
            else if (vtype == typeof(int) || vtype == typeof(uint))
            {
                value = AnsiConsole.Ask<TValue>($"Enter a [blue]number[/] for [green]{this.Name.EscapeMarkup()}[/]");
            }
            else if (vtype == typeof(bool))
            {
                value = (TValue)(object)AnsiConsole.Confirm($"Enter a [blue]boolean[/] for [green]{this.Name.EscapeMarkup()}[/]");
            }
            else if (vtype == typeof(string))
            {
                value = AnsiConsole.Prompt(new TextPrompt<TValue>($"Enter a [blue]string[/] for [green]{this.Name.EscapeMarkup()}[/]").AllowEmpty());
            }
            else
            {
                AnsiConsole.MarkupLine("[red]This should not happen, send an issue.[/]");
                return;
            }

            this.setCustomValue?.Invoke(config, value);
        }
    }
}
