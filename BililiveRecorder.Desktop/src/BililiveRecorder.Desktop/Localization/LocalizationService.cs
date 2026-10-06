using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;

#nullable enable
namespace BililiveRecorder.Desktop.Localization
{
    /// <summary>
    /// 读取链接进本项目的 WPF Strings.*.resx 资源（neutral 为 zh-Hans，附 en / en-PN / ja / zh-Hant）。
    /// 语言在启动时按系统 UI 文化选择；切换 CurrentCulture 会通知 XAML 绑定刷新。
    /// </summary>
    public sealed class LocalizationService : ILocalizationService
    {
        private const string ResourceBaseName = "BililiveRecorder.Desktop.Strings";

        private static readonly ResourceManager resourceManager = new ResourceManager(ResourceBaseName, typeof(LocalizationService).Assembly);

        public static LocalizationService Instance { get; } = new LocalizationService();

        private CultureInfo currentCulture = CultureInfo.CurrentUICulture;

        private LocalizationService() { }

        public CultureInfo CurrentCulture
        {
            get => this.currentCulture;
            set
            {
                if (Equals(this.currentCulture, value))
                    return;
                this.currentCulture = value;
                // "Item[]" 通知索引器绑定刷新（与 WPF 动态本地化相同的约定）
                this.OnPropertyChanged("Item[]");
                this.OnPropertyChanged(nameof(this.CurrentCulture));
            }
        }

        public string this[string key]
        {
            get
            {
                try
                {
                    return resourceManager.GetString(key, this.currentCulture) ?? key;
                }
                catch (Exception)
                {
                    return key;
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
