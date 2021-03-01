using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BililiveRecorder.Core;
using BililiveRecorder.WPF.Pages;

#nullable enable
namespace BililiveRecorder.WPF.Models
{
    public class PollyPolicyModel : INotifyPropertyChanged
    {
        private readonly PollyPolicy? policy;

        public PollyPolicyModel() : this((PollyPolicy?)(RootPage.ServiceProvider?.GetService(typeof(PollyPolicy))))
        {
        }

        public PollyPolicyModel(PollyPolicy? policy)
        {
            this.policy = policy;

            this.ResetAllPolicy = new Commands
            {
                ExecuteDelegate = _ =>
                {
                    if (this.policy != null)
                    {
                        this.policy.IpBlockedHttp412CircuitBreakerPolicy.Reset();
                        this.policy.RequestFailedCircuitBreakerPolicy.Reset();
                    }
                }
            };
        }

        public ICommand ResetAllPolicy { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) { return false; }
            field = value; this.OnPropertyChanged(propertyName); return true;
        }
    }
}
