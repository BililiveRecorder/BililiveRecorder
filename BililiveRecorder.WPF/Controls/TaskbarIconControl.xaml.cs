using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BililiveRecorder.Core;
using BililiveRecorder.WPF.Models;

#nullable enable
namespace BililiveRecorder.WPF.Controls
{
    /// <summary>
    /// Interaction logic for TaskbarIconControl.xaml
    /// </summary>
    public partial class TaskbarIconControl : UserControl
    {
        private RootModel? _rootModel;
        private IRecorder? _recorder;
        private System.Drawing.Icon _normalIcon = null!;
        private System.Drawing.Icon _recordingIcon = null!;

        public TaskbarIconControl()
        {
            this.InitializeComponent();

            using (var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/BililiveRecorder.WPF;component/ico.ico")).Stream)
            {
                this._normalIcon = new System.Drawing.Icon(iconStream);
            }
            using (var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/BililiveRecorder.WPF;component/recording.ico")).Stream)
            {
                this._recordingIcon = new System.Drawing.Icon(iconStream);
            }

            this.TaskbarIcon.Icon = this._normalIcon;

            this.DataContextChanged += this.TaskbarIconControl_DataContextChanged;
            this.Unloaded += this.TaskbarIconControl_Unloaded;

            // AddHandler(NewMainWindow.ShowBalloonTipEvent, (RoutedEventHandler)UserControl_ShowBalloonTip);
            if (Application.Current.MainWindow is NewMainWindow nmw)
            {
                nmw.ShowBalloonTipCallback = (title, msg, sym) =>
                {
                    this.TaskbarIcon.ShowBalloonTip(title, msg, sym);
                };
            }

            // DataContext 可能在 InitializeComponent 时已通过 XAML 绑定设置，此时 DataContextChanged 不会触发
            // if (this.DataContext is RootModel model)
                // this.AttachRootModel(model);
            this.AttachRootModel(this.DataContext as RootModel);
        }

        /// <summary>
        /// RootModel 当前使用的 Recorder 发生变化时，切换到新的 Recorder 继续监听。
        /// </summary>
        private void OnRootModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RootModel.Recorder) && this.DataContext is RootModel model)
                this.AttachRecorder(model.Recorder);
        }

        /// <summary>
        /// DataContext 变化时切换到新的 RootModel，避免继续监听旧的数据对象。
        /// </summary>
        private void TaskbarIconControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.AttachRootModel(e.NewValue as RootModel);
        }

        /// <summary>
        /// 控件卸载时解绑所有事件，避免托盘控件继续持有页面数据对象。
        /// </summary>
        private void TaskbarIconControl_Unloaded(object sender, RoutedEventArgs e)
        {
            this.AttachRootModel(null);
        }

        /// <summary>
        /// 绑定新的 RootModel，并把旧的 RootModel/Recorder 相关事件一起解绑。
        /// </summary>
        private void AttachRootModel(RootModel? model)
        {
            if (ReferenceEquals(this._rootModel, model))
                return;

            if (this._rootModel is not null)
                this._rootModel.PropertyChanged -= this.OnRootModelPropertyChanged;

            this._rootModel = model;

            if (model is not null)
                model.PropertyChanged += this.OnRootModelPropertyChanged;

            this.AttachRecorder(model?.Recorder);
        }

        /// <summary>
        /// 绑定新的 Recorder，并同步更新房间列表及每个房间的录制状态监听。
        /// </summary>
        private void AttachRecorder(IRecorder? recorder)
        {
            if (this._recorder is not null)
            {
                ((INotifyCollectionChanged)this._recorder.Rooms).CollectionChanged -= this.OnRoomsChanged;
                foreach (var room in this._recorder.Rooms)
                    room.PropertyChanged -= this.OnRoomPropertyChanged;
            }

            this._recorder = recorder;

            if (recorder is not null)
            {
                ((INotifyCollectionChanged)recorder.Rooms).CollectionChanged += this.OnRoomsChanged;
                foreach (var room in recorder.Rooms)
                    room.PropertyChanged += this.OnRoomPropertyChanged;
            }

            this.UpdateIcon();
        }
        /// <summary>
        /// 房间增删时补上或移除对应房间的 Recording 监听，并刷新一次托盘图标。
        /// </summary>
        private void OnRoomsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is not null)
                foreach (IRoom room in e.OldItems)
                    room.PropertyChanged -= this.OnRoomPropertyChanged;

            if (e.NewItems is not null)
                foreach (IRoom room in e.NewItems)
                    room.PropertyChanged += this.OnRoomPropertyChanged;

            this.UpdateIcon();
        }

        /// <summary>
        /// 任意房间的 Recording 状态变化时，立即重新计算托盘图标。
        /// </summary>
        private void OnRoomPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IRoom.Recording))
                this.UpdateIcon();
        }

        /// <summary>
        /// 核心逻辑：只要有任意房间正在录制，就切换为 recording.ico，否则恢复默认图标。
        /// </summary>
        private void UpdateIcon()
        {
            _ = this.Dispatcher.BeginInvoke(new Action(() =>
            {
                var isRecording = this._recorder?.Rooms.Any(r => r.Recording) ?? false;
                this.TaskbarIcon.Icon = isRecording ? this._recordingIcon : this._normalIcon;
            }));
        }

        private void TaskbarIcon_TrayLeftMouseUp(object sender, RoutedEventArgs e)
        {
            // RaiseEvent(new RoutedEventArgs(NewMainWindow.SuperActivateEvent));
            (Application.Current.MainWindow as NewMainWindow)?.SuperActivateAction();
        }

        private void MenuItem_OpenMainWindow_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current.MainWindow as NewMainWindow)?.SuperActivateAction();
        }

        private void MenuItem_Quit_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current.MainWindow as NewMainWindow)?.CloseWithoutConfirmAction();
        }

        /*
        private void UserControl_ShowBalloonTip(object sender, RoutedEventArgs e)
        {
            var f = e as NewMainWindow.ShowBalloonTipRoutedEventArgs;
            TaskbarIcon.ShowBalloonTip(f.Title, f.Message, f.Symbol);
        }
        */
    }
}
