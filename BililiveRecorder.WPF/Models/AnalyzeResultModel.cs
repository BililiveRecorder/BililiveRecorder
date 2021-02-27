using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

#nullable enable
namespace BililiveRecorder.WPF.Models
{
    internal class AnalyzeResultModel : INotifyPropertyChanged
    {
        private string file = string.Empty;
        private bool needFix;
        private bool unrepairable;
        private int issueTypeOther;
        private int issueTypeUnrepairable;
        private int issueTypeTimestampJump;
        private int issueTypeDecodingHeader;
        private int issueTypeRepeatingData;

        public string File { get => this.file; set => this.SetField(ref this.file, value); }
        public bool NeedFix { get => this.needFix; set => this.SetField(ref this.needFix, value); }
        public bool Unrepairable { get => this.unrepairable; set => this.SetField(ref this.unrepairable, value); }

        public int IssueTypeOther { get => this.issueTypeOther; set => this.SetField(ref this.issueTypeOther, value); }
        public int IssueTypeUnrepairable { get => this.issueTypeUnrepairable; set => this.SetField(ref this.issueTypeUnrepairable, value); }
        public int IssueTypeTimestampJump { get => this.issueTypeTimestampJump; set => this.SetField(ref this.issueTypeTimestampJump, value); }
        public int IssueTypeDecodingHeader { get => this.issueTypeDecodingHeader; set => this.SetField(ref this.issueTypeDecodingHeader, value); }
        public int IssueTypeRepeatingData { get => this.issueTypeRepeatingData; set => this.SetField(ref this.issueTypeRepeatingData, value); }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) { return false; }
            field = value; this.OnPropertyChanged(propertyName); return true;
        }
    }
}
