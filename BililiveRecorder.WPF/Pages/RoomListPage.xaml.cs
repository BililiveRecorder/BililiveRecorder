using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using BililiveRecorder.Core;
using BililiveRecorder.WPF.Controls;
using ModernWpf.Controls;
using Serilog;

#nullable enable
namespace BililiveRecorder.WPF.Pages
{
    /// <summary>
    /// Interaction logic for RoomList.xaml
    /// </summary>
    public partial class RoomListPage
    {
        private static readonly ILogger logger = Log.ForContext<RoomListPage>();
        private static readonly Regex RoomIdRegex
            = new Regex(@"^(?:https?:\/\/)?live\.bilibili\.com\/(?:blanc\/|h5\/)?(\d*)(?:\?.*)?$",
                RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        private readonly IRoom?[] NullRoom = new IRoom?[] { null };

        private readonly KeyIndexMappingReadOnlyList NullRoomWithMapping;

        public RoomListPage()
        {
            this.NullRoomWithMapping = new KeyIndexMappingReadOnlyList(this.NullRoom);

            this.DataContextChanged += this.RoomListPage_DataContextChanged;

            this.InitializeComponent();
        }

        private void RoomListPage_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is IRecorder data_old) ((INotifyCollectionChanged)data_old.Rooms).CollectionChanged -= this.DataSource_CollectionChanged;
            if (e.NewValue is IRecorder data_new) ((INotifyCollectionChanged)data_new.Rooms).CollectionChanged += this.DataSource_CollectionChanged;
            this.ApplySort();
        }

        public static readonly DependencyProperty RoomListProperty =
         DependencyProperty.Register(
             nameof(RoomList),
             typeof(object),
             typeof(RoomListPage),
             new PropertyMetadata(OnPropertyChanged));

        public object RoomList
        {
            get => this.GetValue(RoomListProperty);
            set => this.SetValue(RoomListProperty, value);
        }

        public static readonly DependencyProperty SortByProperty =
              DependencyProperty.Register(
                  nameof(SortBy),
                  typeof(SortedBy),
                  typeof(RoomListPage),
                  new PropertyMetadata(OnPropertyChanged));

        public SortedBy SortBy
        {
            get => (SortedBy)this.GetValue(SortByProperty);
            set
            {
                this.SetValue(SortByProperty, value);
                this.ApplySort();
            }
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((RoomListPage)d).PrivateOnPropertyChanged(e);

        private void PrivateOnPropertyChanged(DependencyPropertyChangedEventArgs e) { }

        private void DataSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => this.ApplySort();

        private void ApplySort()
        {
            try
            {
                if (this.DataContext is not IRecorder recorder || recorder.Rooms.Count == 0)
                {
                    this.RoomList = this.NullRoomWithMapping;
                }
                else
                {
                    var data = recorder.Rooms;
                    IEnumerable<IRoom> orderedData = this.SortBy switch
                    {
                        SortedBy.RoomId => data.OrderBy(x => x.ShortId == 0 ? x.RoomConfig.RoomId : x.ShortId),
                        SortedBy.Status => from x in data orderby x.Recording descending, x.RoomConfig.AutoRecord descending, x.Streaming descending select x,
                        _ => data,
                    };
                    var result = new KeyIndexMappingReadOnlyList(orderedData.Concat(this.NullRoom).ToArray());
                    this.RoomList = result;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error Sorting");
            }
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void RoomCard_DeleteRequested(object sender, EventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            if (this.DataContext is IRecorder rec && sender is IRoom room)
            {
                try
                {
                    var dialog = new DeleteRoomConfirmDialog
                    {
                        DataContext = room
                    };

                    var result = await dialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        rec.RemoveRoom(room);
                    }
                }
                catch (Exception) { }
            }
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void RoomCard_ShowSettingsRequested(object sender, EventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                await new PerRoomSettingsDialog { DataContext = sender }.ShowAsync();
            }
            catch (Exception) { }
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void AddRoomCard_AddRoomRequested(object sender, string e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            var input = e.Trim();
            if (string.IsNullOrWhiteSpace(input) || this.DataContext is not IRecorder rec) return;

            if (!int.TryParse(input, out var roomid))
            {
                var m = RoomIdRegex.Match(input);
                if (m.Success && m.Groups.Count > 1 && int.TryParse(m.Groups[1].Value, out var result2))
                {
                    roomid = result2;
                }
                else
                {
                    try
                    {
                        await new AddRoomFailedDialog { DataContext = AddRoomFailedDialog.AddRoomFailedErrorText.InvalidInput }.ShowAsync();
                    }
                    catch (Exception) { }
                    return;
                }
            }

            if (roomid < 0)
            {
                try
                {
                    await new AddRoomFailedDialog { DataContext = AddRoomFailedDialog.AddRoomFailedErrorText.RoomIdNegative }.ShowAsync();
                }
                catch (Exception) { }
                return;
            }
            else if (roomid == 0)
            {
                try
                {
                    await new AddRoomFailedDialog { DataContext = AddRoomFailedDialog.AddRoomFailedErrorText.RoomIdZero }.ShowAsync();
                }
                catch (Exception) { }
                return;
            }

            if (rec.Rooms.Any(x => x.RoomConfig.RoomId == roomid || x.ShortId == roomid))
            {
                try
                {
                    await new AddRoomFailedDialog { DataContext = AddRoomFailedDialog.AddRoomFailedErrorText.Duplicate }.ShowAsync();
                }
                catch (Exception) { }
                return;
            }

            rec.AddRoom(roomid);
        }

        private void MenuItem_EnableAutoRecAll_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is not IRecorder rec) return;

            foreach (var room in rec.Rooms)
                room.RoomConfig.AutoRecord = true;

            rec.SaveConfig();
        }

        private void MenuItem_DisableAutoRecAll_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is not IRecorder rec) return;

            foreach (var room in rec.Rooms)
                room.RoomConfig.AutoRecord = false;

            rec.SaveConfig();
        }

        private void MenuItem_SortBy_Click(object sender, RoutedEventArgs e) => this.SortBy = (SortedBy)((MenuItem)sender).Tag;

        private void MenuItem_ShowLog_Click(object sender, RoutedEventArgs e)
        {
            this.Splitter.Visibility = Visibility.Visible;
            this.LogElement.Visibility = Visibility.Visible;
            this.RoomListRowDefinition.Height = new GridLength(1, GridUnitType.Star);
            this.LogRowDefinition.Height = new GridLength(1, GridUnitType.Star);
        }

        private void MenuItem_HideLog_Click(object sender, RoutedEventArgs e)
        {
            this.Splitter.Visibility = Visibility.Collapsed;
            this.LogElement.Visibility = Visibility.Collapsed;
            this.RoomListRowDefinition.Height = new GridLength(1, GridUnitType.Star);
            this.LogRowDefinition.Height = new GridLength(0);
        }

        private void Log_ScrollViewer_Loaded(object sender, RoutedEventArgs e) => (sender as ScrollViewer)?.ScrollToEnd();

        private void TextBlock_Copy_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (sender is TextBlock textBlock)
                {
                    Clipboard.SetText(textBlock.Text);
                }
            }
            catch (Exception)
            {
            }
        }

        private void MenuItem_OpenWorkDirectory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.DataContext is IRecorder rec)
                    Process.Start("explorer.exe", rec.Config.Global.WorkDirectory);
            }
            catch (Exception)
            {
            }
        }

        private void MenuItem_SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.DataContext is IRecorder rec)
                    rec.SaveConfig();
            }
            catch (Exception)
            {
            }
        }

        private void MenuItem_ChangeWorkPath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                logger.Debug("ChangeWorkPath menu button invoked");
                Process.Start(typeof(RoomListPage).Assembly.Location, "run --ask-path");
                (Application.Current.MainWindow as NewMainWindow)?.CloseWithoutConfirmAction();
            }
            catch (Exception)
            { }
        }

        private void MenuItem_ShowHideTitleArea_Click(object sender, RoutedEventArgs e)
        {
            if (((MenuItem)sender).Tag is bool b && this.DataContext is IRecorder rec)
                rec.Config.Global.WpfShowTitleAndArea = b;
        }
    }

    public enum SortedBy
    {
        None = 0,
        RoomId,
        Status,
    }

    internal class KeyIndexMappingReadOnlyList : IReadOnlyList<IRoom?>, IKeyIndexMapping
    {
        private readonly IReadOnlyList<IRoom?> data;

        public KeyIndexMappingReadOnlyList(IReadOnlyList<IRoom?> data)
        {
            this.data = data;
        }

        public IRoom? this[int index] => this.data[index];

        public int Count => this.data.Count;

        public IEnumerator<IRoom?> GetEnumerator() => this.data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.data).GetEnumerator();

        #region IKeyIndexMapping

        private int lastRequestedIndex = IndexNotFound;
        private const int IndexNotFound = -1;

        // When UniqueIDs are supported, the ItemsRepeater caches the unique ID for each item
        // with the matching UIElement that represents the item.  When a reset occurs the
        // ItemsRepeater pairs up the already generated UIElements with items in the data
        // source.
        // ItemsRepeater uses IndexForUniqueId after a reset to probe the data and identify
        // the new index of an item to use as the anchor.  If that item no
        // longer exists in the data source it may try using another cached unique ID until
        // either a match is found or it determines that all the previously visible items
        // no longer exist.
        public int IndexFromKey(string uniqueId)
        {
            // We'll try to increase our odds of finding a match sooner by starting from the
            // position that we know was last requested and search forward.
            var start = this.lastRequestedIndex;
            for (var i = start; i < this.Count; i++)
            {
                if ((this[i]?.ObjectId ?? Guid.Empty).Equals(uniqueId))
                    return i;
            }

            // Then try searching backward.
            start = Math.Min(this.Count - 1, this.lastRequestedIndex);
            for (var i = start; i >= 0; i--)
            {
                if ((this[i]?.ObjectId ?? Guid.Empty).Equals(uniqueId))
                    return i;
            }

            return IndexNotFound;
        }

        public string KeyFromIndex(int index)
        {
            var key = this[index]?.ObjectId ?? Guid.Empty;
            this.lastRequestedIndex = index;
            return key.ToString();
        }

        #endregion
    }
}
