using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.Core.Templating;
using BililiveRecorder.Desktop.Localization;
using BililiveRecorder.Desktop.ViewModels;
using Newtonsoft.Json.Linq;
using Serilog;

#nullable enable
namespace BililiveRecorder.Desktop.Views
{
    public partial class SettingsView : UserControl
    {
        private static readonly ILogger logger = Log.ForContext<SettingsView>();

        public SettingsView()
        {
            this.InitializeComponent();
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void TestTemplateButton_Click(object? sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                if (this.DataContext is not SettingsViewModel vm)
                    return;

                var config = new ConfigV3();
                // 复制当前模板设置用于预览（与 WPF 版相同的示例数据）
                var generatorConfig = vm.Config;

                var data = new FileNameTemplateContext
                {
                    Name = "rua",
                    RoomId = 952,
                    ShortId = 7540,
                    Uid = 2928870,
                    Title = "【工作の记录】" + new string('a', 10),
                    AreaParent = "网游",
                    AreaChild = "英雄联盟",
                    PartIndex = 1,
                    Qn = 250,
                    Json = new JObject
                    {
                        ["qn"] = 250,
                        ["max_qn"] = 400,
                    },
                };

                var generator = new FileNameGenerator(generatorConfig, null);
                var output = generator.CreateFilePath(data);

                this.FileNameTestError.Text = output.ErrorMessage;
                this.FileNameTestResult.Text = output.RelativePath;
                this.FileNameTestResultArea.IsVisible = true;
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error testing file name template");
                this.FileNameTestError.Text = ex.Message;
                this.FileNameTestResult.Text = string.Empty;
                this.FileNameTestResultArea.IsVisible = true;
            }
        }
    }
}
