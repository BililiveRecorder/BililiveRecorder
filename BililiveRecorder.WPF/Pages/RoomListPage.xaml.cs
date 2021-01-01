using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BililiveRecorder.Core;
using BililiveRecorder.WPF.Controls;
using ModernWpf.Controls;
using NLog;

namespace BililiveRecorder.WPF.Pages
{
    /// <summary>
    /// Interaction logic for RoomList.xaml
    /// </summary>
    public partial class RoomListPage
    {
        private static readonly Regex RoomIdRegex
            = new Regex(@"^(?:https?:\/\/)?live\.bilibili\.com\/(?:blanc\/|h5\/)?(\d*)(?:\?.*)?$",
                RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        public RoomListPage()
        {
            this.InitializeComponent();

            this.SortedRoomList = new SortedItemsSourceView(this.DataContext);
            DataContextChanged += this.RoomListPage_DataContextChanged;
        }

        private void RoomListPage_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            (this.SortedRoomList as SortedItemsSourceView).Data = e.NewValue as ICollection<IRecordedRoom>;
        }

        public static readonly DependencyProperty SortedRoomListProperty =
           DependencyProperty.Register(
               nameof(SortedRoomList),
               typeof(object),
               typeof(RoomListPage),
               new PropertyMetadata(OnPropertyChanged));

        public object SortedRoomList
        {
            get => this.GetValue(SortedRoomListProperty);
            set => this.SetValue(SortedRoomListProperty, value);
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }

        private async void RoomCard_DeleteRequested(object sender, EventArgs e)
        {
            if (this.DataContext is IRecorder rec && sender is IRecordedRoom room)
            {
                var dialog = new DeleteRoomConfirmDialog
                {
                    DataContext = room
                };

                var result = await dialog.ShowAsync();

                if (result == ModernWpf.Controls.ContentDialogResult.Primary)
                {
                    rec.RemoveRoom(room);
                    rec.SaveConfigToFile();
                }
            }
        }

        private async void RoomCard_ShowSettingsRequested(object sender, EventArgs e)
        {
            try
            {
                await new PerRoomSettingsDialog { DataContext = sender }.ShowAsync();
            }
            catch (Exception) { }
        }

        private async void AddRoomCard_AddRoomRequested(object sender, string e)
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
                    await new AddRoomFailedDialog { DataContext = "请输入B站直播房间号或直播间链接" }.ShowAsync();
                    return;
                }
            }

            if (roomid < 0)
            {
                await new AddRoomFailedDialog { DataContext = "房间号不能是负数" }.ShowAsync();
                return;
            }
            else if (roomid == 0)
            {
                await new AddRoomFailedDialog { DataContext = "房间号不能是 0" }.ShowAsync();
                return;
            }

            if (rec.Any(x => x.RoomId == roomid || x.ShortRoomId == roomid))
            {
                await new AddRoomFailedDialog { DataContext = "这个直播间已经被添加过了" }.ShowAsync();
                return;
            }

            rec.AddRoom(roomid);
            rec.SaveConfigToFile();
        }

        private async void MenuItem_EnableAutoRecAll_Click(object sender, RoutedEventArgs e)
        {
            if (!(this.DataContext is IRecorder rec)) return;

            await Task.WhenAll(rec.ToList().Select(rr => Task.Run(() => rr.Start())));
            rec.SaveConfigToFile();
        }

        private async void MenuItem_DisableAutoRecAll_Click(object sender, RoutedEventArgs e)
        {
            if (!(this.DataContext is IRecorder rec)) return;

            await Task.WhenAll(rec.ToList().Select(rr => Task.Run(() => rr.Stop())));
            rec.SaveConfigToFile();
        }

        private void MenuItem_SortBy_Click(object sender, RoutedEventArgs e)
        {
            (this.SortedRoomList as SortedItemsSourceView).SortedBy = (SortedBy)((MenuItem)sender).Tag;
        }

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

        private void Log_ScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            (sender as ScrollViewer)?.ScrollToEnd();
        }

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
    }

    internal enum SortedBy
    {
        None = 0,
        RoomId,
        Status,
    }

    internal class SortedItemsSourceView : IList, IReadOnlyList<IRecordedRoom>, IKeyIndexMapping, INotifyCollectionChanged
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private ICollection<IRecordedRoom> _data;
        private SortedBy sortedBy;

        private readonly IRecordedRoom[] NullRoom = new IRecordedRoom[] { null };

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public SortedItemsSourceView(object data)
        {
            if (data is not null)
            {
                if (data is IList<IRecordedRoom> list)
                {
                    if (list is INotifyCollectionChanged n) n.CollectionChanged += this.Data_CollectionChanged;
                    this._data = list;
                }
                else
                {
                    throw new ArgumentException("Type not supported.", nameof(data));
                }
            }
            this.Sort();
        }

        private void Data_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => this.Sort();

        public ICollection<IRecordedRoom> Data
        {
            get => this._data;
            set
            {
                if (this._data is INotifyCollectionChanged n1) n1.CollectionChanged -= this.Data_CollectionChanged;
                if (value is INotifyCollectionChanged n2) n2.CollectionChanged += this.Data_CollectionChanged;
                this._data = value;
                this.Sort();
            }
        }

        public SortedBy SortedBy { get => this.sortedBy; set { this.sortedBy = value; this.Sort(); } }

        public List<IRecordedRoom> Sorted { get; private set; }

        private int sortSeboucneCount = int.MinValue;
        private readonly SemaphoreSlim sortSemaphoreSlim = new SemaphoreSlim(1, 1);

        private async void Sort()
        {
            // debounce && lock
            logger.Debug("Sort called.");
            var callCount = Interlocked.Increment(ref this.sortSeboucneCount);
            await Task.Delay(200);
            if (this.sortSeboucneCount != callCount)
            {
                logger.Debug("Sort cancelled by debounce.");
                return;
            }

            await this.sortSemaphoreSlim.WaitAsync();
            try { this.SortImpl(); }
            finally { this.sortSemaphoreSlim.Release(); }
        }

        private void SortImpl()
        {
            logger.Debug("SortImpl called with {sortedBy} and {count} rooms.", this.SortedBy, this.Data?.Count ?? -1);

            if (this.Data is null)
            {
                this.Sorted = this.NullRoom.ToList();
                logger.Debug("SortImpl returned NullRoom.");
            }
            else
            {
                IEnumerable<IRecordedRoom> orderedData = this.SortedBy switch
                {
                    SortedBy.RoomId => this.Data.OrderBy(x => x.ShortRoomId == 0 ? x.RoomId : x.ShortRoomId),
                    SortedBy.Status => this.Data.OrderByDescending(x => x.IsRecording).ThenByDescending(x => x.IsMonitoring),
                    _ => this.Data,
                };
                var result = orderedData.Concat(this.NullRoom).ToList();
                logger.Debug("SortImpl sorted with {count} items.", result.Count);

                { // 崩溃问题信息收集。。虽然不觉得是这里的问题
                    var dup = result.GroupBy(x => x?.Guid ?? Guid.Empty).Where(x => x.Count() != 1);
                    if (dup.Any())
                    {
                        var sb = new StringBuilder("排序调试信息\n重复:\n");
                        foreach (var item in dup)
                        {
                            sb.Append("-Guid: ");
                            sb.AppendLine(item.Key.ToString());
                            foreach (var room in item)
                            {
                                sb.Append("RoomId: ");
                                sb.AppendLine(room?.RoomId.ToString());
                            }
                        }
                        sb.Append("原始:");
                        foreach (var room in result)
                        {
                            sb.Append("-Guid: ");
                            sb.AppendLine((room?.Guid ?? Guid.Empty).ToString());
                            sb.Append("RoomId: ");
                            sb.AppendLine(room?.RoomId.ToString());
                        }
                        logger.Debug(sb.ToString());

                        // trigger sentry
                        logger.Error(new SortedItemsSourceViewException(), "排序房间时发生了错误");
                        return;
                    }
                }

                this.Sorted = result;
            }

            // Instead of tossing out existing elements and re-creating them,
            // ItemsRepeater will reuse the existing elements and match them up
            // with the data again.
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public IRecordedRoom this[int index] => this.Sorted != null ? this.Sorted[index] : throw new IndexOutOfRangeException();
        public int Count => this.Sorted != null ? this.Sorted.Count : 0;

        public bool IsReadOnly => ((IList)this.Sorted).IsReadOnly;

        public bool IsFixedSize => ((IList)this.Sorted).IsFixedSize;

        public object SyncRoot => ((ICollection)this.Sorted).SyncRoot;

        public bool IsSynchronized => ((ICollection)this.Sorted).IsSynchronized;

        object IList.this[int index] { get => ((IList)this.Sorted)[index]; set => ((IList)this.Sorted)[index] = value; }

        public IEnumerator<IRecordedRoom> GetEnumerator() => this.Sorted.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

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
                if ((this[i]?.Guid ?? Guid.Empty).Equals(uniqueId))
                    return i;
            }

            // Then try searching backward.
            start = Math.Min(this.Count - 1, this.lastRequestedIndex);
            for (var i = start; i >= 0; i--)
            {
                if ((this[i]?.Guid ?? Guid.Empty).Equals(uniqueId))
                    return i;
            }

            return IndexNotFound;
        }

        public string KeyFromIndex(int index)
        {
            var key = this[index]?.Guid ?? Guid.Empty;
            this.lastRequestedIndex = index;
            return key.ToString();
        }

        public int Add(object value)
        {
            return ((IList)this.Sorted).Add(value);
        }

        public bool Contains(object value)
        {
            return ((IList)this.Sorted).Contains(value);
        }

        public void Clear()
        {
            ((IList)this.Sorted).Clear();
        }

        public int IndexOf(object value)
        {
            return ((IList)this.Sorted).IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            ((IList)this.Sorted).Insert(index, value);
        }

        public void Remove(object value)
        {
            ((IList)this.Sorted).Remove(value);
        }

        public void RemoveAt(int index)
        {
            ((IList)this.Sorted).RemoveAt(index);
        }

        public void CopyTo(Array array, int index)
        {
            ((ICollection)this.Sorted).CopyTo(array, index);
        }

        #endregion

        public class SortedItemsSourceViewException : Exception
        {
            public SortedItemsSourceViewException() { }
            public SortedItemsSourceViewException(string message) : base(message) { }
            public SortedItemsSourceViewException(string message, Exception innerException) : base(message, innerException) { }
            protected SortedItemsSourceViewException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }
    }
}
