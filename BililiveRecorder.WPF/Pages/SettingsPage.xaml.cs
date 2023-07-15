using System;
using System.Threading.Tasks;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.Core.Templating;
using Newtonsoft.Json.Linq;
using Serilog;

#nullable enable
namespace BililiveRecorder.WPF.Pages
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage
    {
        private static readonly ILogger logger = Log.ForContext<SettingsPage>();

        private static readonly FileNameTemplateContext data = new()
        {
            Name = "3号直播间",
            RoomId = 23058,
            ShortId = 3,
            Uid = 11153765,
            Title = "mikufans音悦台",
            AreaParent = "电台",
            AreaChild = "唱见电台",
            Qn = 10000,
            Json = JObject.Parse(@"{""room_info"":{""uid"":11153765,""room_id"":23058,""short_id"":3,""title"":""mikufans音悦台"",""cover"":"""",""tags"":"""",""background"":""https://i0.hdslb.com/bfs/live/2836bb7b84c792e2c6aadfd4d1cce13484775fa3.jpg"",""description"":""\u003cp\u003e这里是mikufans官方音乐台喔！\u003c/p\u003e\u003cp\u003e一起来听音乐吧ε=ε=(ノ≧∇≦)ノ\u003c/p\u003e\u003cp\u003e没想到蒸汽配圣诞下装，意外的很暴露呢=3=\u003c/p\u003e\n"",""live_status"":1,""live_start_time"":1642502066,""live_screen_type"":0,""lock_status"":0,""lock_time"":0,""hidden_status"":0,""hidden_time"":0,""area_id"":190,""area_name"":""唱见电台"",""parent_area_id"":5,""parent_area_name"":""电台"",""keyframe"":""http://i0.hdslb.com/bfs/live-key-frame/keyframe05061651000000023058bkekzp.jpg"",""special_type"":0,""up_session"":""204681708782508562"",""pk_status"":0,""is_studio"":false,""pendants"":{""frame"":{""name"":"""",""value"":"""",""desc"":""""}},""on_voice_join"":0,""online"":6697,""room_type"":{""2-3"":0,""3-21"":0}},""anchor_info"":{""base_info"":{""uname"":""3号直播间"",""face"":""http://i2.hdslb.com/bfs/face/5d35da6e93fbfb1a77ad6d1f1004b08413913f9a.jpg"",""gender"":""保密"",""official_info"":{""role"":1,""title"":""mikufans直播 官方账号"",""desc"":"""",""is_nft"":0,""nft_dmark"":""https://i0.hdslb.com/bfs/live/9f176ff49d28c50e9c53ec1c3297bd1ee539b3d6.gif""}},""live_info"":{""level"":40,""level_color"":16746162,""score"":255237648,""upgrade_score"":0,""current"":[ 25000000, 147013810],""next"":[],""rank"":""\u003e10000""},""relation_info"":{""attention"":248859},""medal_info"":{""medal_name"":""电音"",""medal_id"":123,""fansclub"":1643}},""news_info"":{""uid"":11153765,""ctime"":""2021-09-24 12:49:50"",""content"":""3号大歌厅是音悦台特别推出的测试栏目，23日-25日11点-23点为期3天，希望大家支持！""},""rankdb_info"":{""roomid"":23058,""rank_desc"":""小时总榜"",""color"":""#FB7299"",""h5_url"":""https://live.bilibili.com/p/html/live-app-rankcurrent/index.html?is_live_half_webview=1\u0026hybrid_half_ui=1,5,85p,70p,FFE293,0,30,100,10;2,2,320,100p,FFE293,0,30,100,0;4,2,320,100p,FFE293,0,30,100,0;6,5,65p,60p,FFE293,0,30,100,10;5,5,55p,60p,FFE293,0,30,100,10;3,5,85p,70p,FFE293,0,30,100,10;7,5,65p,60p,FFE293,0,30,100,10;\u0026anchor_uid=11153765\u0026rank_type=master_realtime_hour_room\u0026area_hour=1\u0026area_v2_id=190\u0026area_v2_parent_id=5"",""web_url"":""https://live.bilibili.com/blackboard/room-current-rank.html?rank_type=master_realtime_hour_room\u0026area_hour=1\u0026area_v2_id=190\u0026area_v2_parent_id=5"",""timestamp"":1651234567},""area_rank_info"":{""areaRank"":{""index"":0,""rank"":""\u003e1000""},""liveRank"":{""rank"":""\u003e10000""}},""battle_rank_entry_info"":{""first_rank_img_url"":"""",""rank_name"":""尚无段位"",""show_status"":1},""tab_info"":{""list"":[{""type"":""seven-rank"",""desc"":""高能榜"",""isFirst"":1,""isEvent"":0,""eventType"":"""",""listType"":"""",""apiPrefix"":"""",""rank_name"":""room_7day""},{""type"":""guard"",""desc"":""大航海"",""isFirst"":0,""isEvent"":0,""eventType"":"""",""listType"":""top-list"",""apiPrefix"":"""",""rank_name"":""""}]},""activity_init_info"":{""eventList"":[],""weekInfo"":{""bannerInfo"":null,""giftName"":null},""giftName"":null,""lego"":{""timestamp"":1651234567,""config"":""[{\""name\"":\""frame-mng\"",\""url\"":\""https:\\/\\/live.bilibili.com\\/p\\/html\\/live-web-mng\\/index.html?roomid=#roomid#\u0026arae_id=#area_id#\u0026parent_area_id=#parent_area_id#\u0026ruid=#ruid#\"",\""startTime\"":1559544736,\""endTime\"":1877167950,\""type\"":\""frame-mng\""},{\""name\"":\""s10-fun\"",\""target\"":\""sidebar\"",\""icon\"":\""https:\\/\\/i0.hdslb.com\\/bfs\\/activity-plat\\/static\\/20200908\\/3435f7521efc759ae1f90eae5629a8f0\\/HpxrZ7SOT.png\"",\""text\"":\""\\u7545\\u73a9s10\"",\""url\"":\""https:\\/\\/live.bilibili.com\\/s10\\/fun\\/index.html?room_id=#roomid#\u0026width=376\u0026height=600\u0026source=sidebar\"",\""color\"":\""#2e6fc0\"",\""startTime\"":1600920000,\""endTime\"":1604721600,\""parentAreaId\"":2,\""areaId\"":86},{\""name\"":\""genshin-avatar\"",\""target\"":\""sidebar\"",\""icon\"":\""https:\\/\\/i0.hdslb.com\\/bfs\\/activity-plat\\/static\\/20210721\\/fa538c98e9e32dc98919db4f2527ad02\\/qWxN1d0ACu.jpg\"",\""text\"":\""\\u539f\\u77f3\\u798f\\u5229\"",\""url\"":\""https:\\/\\/live.bilibili.com\\/activity\\/live-activity-full\\/genshin_avatar\\/mobile.html?no-jump=1\u0026room_id=#roomid#\u0026width=376\u0026height=550#\\/\"",\""color\"":\""#2e6fc0\"",\""frameAllowNoBg\"":\""1\"",\""frameAllowDrag\"":\""1\"",\""startTime\"":1627012800,\""endTime\"":1630425540,\""parentAreaId\"":3,\""areaId\"":321}]""}},""voice_join_info"":{""status"":{""open"":0,""anchor_open"":0,""status"":0,""uid"":0,""user_name"":"""",""head_pic"":"""",""guard"":0,""start_at"":0,""current_time"":1651234567},""icons"":{""icon_close"":""https://i0.hdslb.com/bfs/live/a176d879dffe8de1586a5eb54c2a08a0c7d31392.png"",""icon_open"":""https://i0.hdslb.com/bfs/live/70f0844c9a12d29db1e586485954290144534be9.png"",""icon_wait"":""https://i0.hdslb.com/bfs/live/1049bb88f1e7afd839cc1de80e13228ccd5807e8.png"",""icon_starting"":""https://i0.hdslb.com/bfs/live/948433d1647a0704f8216f017c406224f9fff518.gif""},""web_share_link"":""https://live.bilibili.com/h5/23058""},""ad_banner_info"":{""data"":null},""skin_info"":{""id"":0,""skin_name"":"""",""skin_config"":"""",""show_text"":"""",""skin_url"":"""",""start_time"":0,""end_time"":0,""current_time"":1651234567},""web_banner_info"":{""id"":0,""title"":"""",""left"":"""",""right"":"""",""jump_url"":"""",""bg_color"":"""",""hover_color"":"""",""text_bg_color"":"""",""text_hover_color"":"""",""link_text"":"""",""link_color"":"""",""input_color"":"""",""input_text_color"":"""",""input_hover_color"":"""",""input_border_color"":"""",""input_search_color"":""""},""lol_info"":{""lol_activity"":{""status"":0,""guess_cover"":""http://i0.hdslb.com/bfs/live/61d1c4bcce470080a5408d6c03b7b48e0a0fa8d7.png"",""vote_cover"":""https://i0.hdslb.com/bfs/activity-plat/static/20190930/4ae8d4def1bbff9483154866490975c2/oWyasOpox.png"",""vote_h5_url"":""https://live.bilibili.com/p/html/live-app-wishhelp/index.html?is_live_half_webview=1\u0026hybrid_biz=live-app-wishhelp\u0026hybrid_rotate_d=1\u0026hybrid_half_ui=1,3,100p,360,0c1333,0,30,100;2,2,375,100p,0c1333,0,30,100;3,3,100p,360,0c1333,0,30,100;4,2,375,100p,0c1333,0,30,100;5,3,100p,360,0c1333,0,30,100;6,3,100p,360,0c1333,0,30,100;7,3,100p,360,0c1333,0,30,100;8,3,100p,360,0c1333,0,30,100;"",""vote_use_h5"":true}},""pk_info"":null,""battle_info"":null,""silent_room_info"":{""type"":"""",""level"":0,""second"":0,""expire_time"":0},""switch_info"":{""close_guard"":false,""close_gift"":false,""close_online"":false,""close_danmaku"":false},""record_switch_info"":{""record_tab"":false},""room_config_info"":{""dm_text"":""发个弹幕呗~""},""gift_memory_info"":{""list"":null},""new_switch_info"":{""room-socket"":1,""room-prop-send"":1,""room-sailing"":1,""room-info-popularity"":1,""room-danmaku-editor"":1,""room-effect"":1,""room-fans_medal"":1,""room-report"":1,""room-feedback"":1,""room-player-watermark"":1,""room-recommend-live_off"":1,""room-activity"":1,""room-web_banner"":1,""room-silver_seeds-box"":1,""room-wishing_bottle"":1,""room-board"":1,""room-supplication"":1,""room-hour_rank"":1,""room-week_rank"":1,""room-anchor_rank"":1,""room-info-integral"":1,""room-super-chat"":1,""room-tab"":1,""room-hot-rank"":1,""fans-medal-progress"":1,""gift-bay-screen"":1,""room-enter"":1,""room-my-idol"":1,""room-topic"":1,""fans-club"":1},""super_chat_info"":{""status"":1,""jump_url"":""https://live.bilibili.com/p/html/live-app-superchat2/index.html?is_live_half_webview=1\u0026hybrid_half_ui=1,3,100p,70p,ffffff,0,30,100,12,0;2,2,375,100p,ffffff,0,30,100,0,0;3,3,100p,70p,ffffff,0,30,100,12,0;4,2,375,100p,ffffff,0,30,100,0,0;5,3,100p,60p,ffffff,0,30,100,12,0;6,3,100p,60p,ffffff,0,30,100,12,0;7,3,100p,60p,ffffff,0,30,100,12,0"",""icon"":""https://i0.hdslb.com/bfs/live/0a9ebd72c76e9cbede9547386dd453475d4af6fe.png"",""ranked_mark"":0,""message_list"":[]},""online_gold_rank_info_v2"":{""list"":[{""uid"":20455817,""face"":""http://i2.hdslb.com/bfs/face/85b49d96bd506c84831eca97c35534cfb696b578.jpg"",""uname"":""咕咕Q"",""score"":""114"",""rank"":1,""guard_level"":0},{""uid"":6331378,""face"":""http://i1.hdslb.com/bfs/face/95d0f044829772cfc871008b56a3e8543f6d846f.jpg"",""uname"":""伊卡萌神"",""score"":""109"",""rank"":2,""guard_level"":0},{""uid"":85012323,""face"":""http://i0.hdslb.com/bfs/face/9d29c2d8f760afbda101788e3562cbd35773edba.jpg"",""uname"":""我会咬打火机"",""score"":""109"",""rank"":3,""guard_level"":0},{""uid"":8648034,""face"":""http://i0.hdslb.com/bfs/face/0e18dd3f02c5e64fd280d48ac7c50a3f2fbe8e0f.jpg"",""uname"":""在异世界开泥头车"",""score"":""107"",""rank"":4,""guard_level"":0},{""uid"":30377724,""face"":""http://i0.hdslb.com/bfs/face/2e699d1d14c0d812800c8d503b7f769ad9a2b3e7.jpg"",""uname"":""五十五层皇堡"",""score"":""104"",""rank"":5,""guard_level"":0},{""uid"":1613420120,""face"":""http://i0.hdslb.com/bfs/face/member/noface.jpg"",""uname"":""xieoevtstx"",""score"":""99"",""rank"":6,""guard_level"":0},{""uid"":10265491,""face"":""http://i1.hdslb.com/bfs/face/fee62e15c3b79f0a645eb2c6d91dce1c45e27e04.jpg"",""uname"":""我还活着~"",""score"":""59"",""rank"":7,""guard_level"":0}]},""dm_emoticon_info"":{""is_open_emoticon"":1,""is_shield_emoticon"":0},""dm_tag_info"":{""dm_tag"":0,""platform"":[],""extra"":"""",""dm_chronos_extra"":"""",""dm_mode"":[],""dm_setting_switch"":0,""material_conf"":null},""topic_info"":{""topic_id"":0,""topic_name"":""""},""game_info"":{""game_status"":0},""watched_show"":{""switch"":true,""num"":1206,""text_small"":""1206"",""text_large"":""1206人看过"",""icon"":"""",""icon_location"":0,""icon_web"":""""},""topic_room_info"":{""interactive_h5_url"":"""",""watermark"":1},""show_reserve_status"":false,""video_connection_info"":null,""player_throttle_info"":{""status"":1,""normal_sleep_time"":1800,""fullscreen_sleep_time"":3600,""tab_sleep_time"":1800,""prompt_time"":30},""guard_info"":{""count"":0,""anchor_guard_achieve_level"":0},""hot_rank_info"":null}")
        };

        private readonly GlobalConfig? globalConfig;

        public SettingsPage() : this((GlobalConfig?)(RootPage.ServiceProvider?.GetService(typeof(GlobalConfig))))
        {
        }

        public SettingsPage(GlobalConfig? globalConfig)
        {
            this.globalConfig = globalConfig;

            this.InitializeComponent();
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void TestFileNameTemplate_Button_Click(object sender, System.Windows.RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            if (this.globalConfig is not { } config)
                return;

            try
            {
                var output = await Task.Run(() =>
                {
                    var fileNameGenerator = new FileNameGenerator(config, null);
                    return fileNameGenerator.CreateFilePath(data);
                });

                this.FileNameTestResultArea.Visibility = System.Windows.Visibility.Visible;
                this.FileNameTestResultArea.DataContext = output;
            }
            catch (Exception ex)
            {
                logger.Debug(ex, "测试文件名模板时发生错误");
            }
        }
    }
}
