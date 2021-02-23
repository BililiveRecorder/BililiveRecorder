using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using ModernWpf.Controls;

#nullable enable
namespace BililiveRecorder.WPF.Models
{
    public class Commands : ICommand
    {
        #region Static Commands

        public static Commands OpenLink { get; } = new Commands
        {
            ExecuteDelegate = o => { try { Process.Start(o.ToString()); } catch (Exception) { } }
        };

        public static Commands OpenContentDialog { get; } = new Commands
        {
#pragma warning disable VSTHRD101 // Avoid unsupported async delegates
            ExecuteDelegate = async o => { try { await (o as ContentDialog)!.ShowAsync(); } catch (Exception) { } }
#pragma warning restore VSTHRD101 // Avoid unsupported async delegates
        };

        public static Commands Copy { get; } = new Commands
        {
            ExecuteDelegate = e => { try { if (e is string str) Clipboard.SetText(str); } catch (Exception) { } }
        };

        #endregion

        public Predicate<object>? CanExecuteDelegate { get; set; }
        public Action<object>? ExecuteDelegate { get; set; }

        #region ICommand Members

        public void Execute(object parameter) => this.ExecuteDelegate?.Invoke(parameter);

        public bool CanExecute(object parameter) => this.CanExecuteDelegate switch
        {
            null => true,
            _ => this.CanExecuteDelegate(parameter),
        };

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        #endregion
    }
}
