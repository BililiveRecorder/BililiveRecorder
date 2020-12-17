using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using ModernWpf.Controls;

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
            ExecuteDelegate = async o => { try { await (o as ContentDialog)?.ShowAsync(); } catch (Exception) { } }
        };

        public static Commands Copy { get; } = new Commands
        {
            ExecuteDelegate = e => { try { if (e is string str) Clipboard.SetText(str); } catch (Exception) { } }
        };

        #endregion

        public Predicate<object> CanExecuteDelegate { get; set; }
        public Action<object> ExecuteDelegate { get; set; }

        #region ICommand Members

        public void Execute(object parameter) => ExecuteDelegate?.Invoke(parameter);

        public bool CanExecute(object parameter) => CanExecuteDelegate switch
        {
            null => true,
            _ => CanExecuteDelegate(parameter),
        };

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        #endregion
    }
}
