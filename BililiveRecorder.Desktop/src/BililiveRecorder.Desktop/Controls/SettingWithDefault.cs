using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using BililiveRecorder.Desktop.Localization;

#nullable enable
namespace BililiveRecorder.Desktop.Controls
{
    /// <summary>
    /// 三态设置项：标签 + 内容 + "默认"复选框。
    /// IsSettingNotUsingDefault 绑定到配置对象的 HasXxx 属性；
    /// 未覆盖默认值时内容禁用。与 WPF 版 SettingWithDefault 相同的语义。
    /// IsInlineContent=true 时内容与标题同行（开关/单行输入）；否则内容换行。
    /// 外壳定义在 App.axaml 的 ControlTheme 模板中。
    /// </summary>
    public class SettingWithDefault : ContentControl
    {
        public static readonly StyledProperty<string> HeaderProperty =
            AvaloniaProperty.Register<SettingWithDefault, string>(nameof(Header), string.Empty);

        public static readonly StyledProperty<bool> IsSettingNotUsingDefaultProperty =
            AvaloniaProperty.Register<SettingWithDefault, bool>(
                nameof(IsSettingNotUsingDefault), defaultValue: false, defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<bool> IsInlineContentProperty =
            AvaloniaProperty.Register<SettingWithDefault, bool>(nameof(IsInlineContent), defaultValue: false);

        private CheckBox? defaultCheckBoxInline;
        private CheckBox? defaultCheckBoxBlock;
        private bool updatingCheckBox;

        public string Header
        {
            get => this.GetValue(HeaderProperty);
            set => this.SetValue(HeaderProperty, value);
        }

        public bool IsSettingNotUsingDefault
        {
            get => this.GetValue(IsSettingNotUsingDefaultProperty);
            set => this.SetValue(IsSettingNotUsingDefaultProperty, value);
        }

        public bool IsInlineContent
        {
            get => this.GetValue(IsInlineContentProperty);
            set => this.SetValue(IsInlineContentProperty, value);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            this.DetachCheckBoxes();

            this.defaultCheckBoxInline = e.NameScope.Find<CheckBox>("PART_DefaultCheckBox");
            this.defaultCheckBoxBlock = e.NameScope.Find<CheckBox>("PART_DefaultCheckBox_Block");
            this.AttachCheckBox(this.defaultCheckBoxInline);
            this.AttachCheckBox(this.defaultCheckBoxBlock);
            this.PushCheckedState();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == IsSettingNotUsingDefaultProperty && !this.updatingCheckBox)
                this.PushCheckedState();
        }

        private void AttachCheckBox(CheckBox? checkBox)
        {
            if (checkBox is null)
                return;
            checkBox.Content = LocalizationService.Instance["SettingWithDefault_Default"];
            checkBox.IsCheckedChanged += this.DefaultCheckBox_IsCheckedChanged;
        }

        private void DetachCheckBoxes()
        {
            if (this.defaultCheckBoxInline is not null)
                this.defaultCheckBoxInline.IsCheckedChanged -= this.DefaultCheckBox_IsCheckedChanged;
            if (this.defaultCheckBoxBlock is not null)
                this.defaultCheckBoxBlock.IsCheckedChanged -= this.DefaultCheckBox_IsCheckedChanged;
            this.defaultCheckBoxInline = null;
            this.defaultCheckBoxBlock = null;
        }

        private void PushCheckedState()
        {
            var isChecked = !this.IsSettingNotUsingDefault;
            if (this.defaultCheckBoxInline is not null)
                this.defaultCheckBoxInline.IsChecked = isChecked;
            if (this.defaultCheckBoxBlock is not null)
                this.defaultCheckBoxBlock.IsChecked = isChecked;
        }

        private void DefaultCheckBox_IsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            this.updatingCheckBox = true;
            try
            {
                var isChecked = (sender as CheckBox)?.IsChecked;
                this.IsSettingNotUsingDefault = isChecked != true;
                this.PushCheckedState();
            }
            finally
            {
                this.updatingCheckBox = false;
            }
        }
    }
}
