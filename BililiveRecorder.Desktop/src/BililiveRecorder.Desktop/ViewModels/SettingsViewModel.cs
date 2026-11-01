using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.Desktop.Localization;

#nullable enable
namespace BililiveRecorder.Desktop.ViewModels
{
    /// <summary>全局设置页的数据上下文：全局配置 + 本地化。</summary>
    public class SettingsViewModel
    {
        public SettingsViewModel(GlobalConfig config, ILocalizationService loc)
        {
            this.Config = config;
            this.Loc = loc;
        }

        public GlobalConfig Config { get; }

        public ILocalizationService Loc { get; }
    }
}
