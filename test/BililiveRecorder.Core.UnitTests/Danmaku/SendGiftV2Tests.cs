using System;
using System.IO;
using BililiveRecorder.Core.Api.Danmaku;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace BililiveRecorder.Core.UnitTests.Danmaku
{
    public class SendGiftV2Tests
    {
        private static string BuildJson(byte[] pb) => new JObject
        {
            ["cmd"] = "SEND_GIFT_V2",
            ["danmu"] = new JObject { ["area"] = 0 },
            ["data"] = new JObject
            {
                ["dmscore"] = 476,
                ["pb"] = Convert.ToBase64String(pb),
            },
        }.ToString(Formatting.None);

        /// <summary>
        /// 构造 GiftItem。除录播姬使用的字段外，还包含网页端真实消息中出现的其他字段，用于确认解析时会跳过这些字段。
        /// </summary>
        private static ProtobufTestWriter GiftItem(long giftId, string giftName, int num, long price, string coinType) => new ProtobufTestWriter()
            .Varint(1, (ulong)giftId)
            .String(2, giftName)
            .Varint(3, (ulong)num)
            .Varint(4, 0) // demarcation
            .Varint(5, (ulong)price)
            .Varint(6, (ulong)price) // discount_price
            .Varint(7, (ulong)(price * num)) // total_coin
            .String(8, coinType)
            .String(9, "1757066308112500001") // tid
            .Varint(10, 1757066308) // timestamp
            .String(12, "batch:gift:combo_id:1:2:3:1757066308.1234") // batch_combo_id
            .Float(16, 1f) // magnification
            .Varint(17, 1) // show_batch_combo_send
            .String(18, "投喂") // action
            .Message(29, new ProtobufTestWriter().String(1, "主播").Varint(2, 654321)); // receive_user_info

        private static ProtobufTestWriter Broadcast(long uid, string uname, bool @switch, params ProtobufTestWriter[] gifts)
        {
            var writer = new ProtobufTestWriter()
                .Varint(1, (ulong)uid)
                .String(2, uname)
                .String(3, "http://i0.hdslb.com/bfs/face/member/noface.jpg")
                .String(4, string.Empty) // name_color
                .Varint(5, 0) // guard_level
                .Varint(6, 0) // svga_block
                .Message(8, new ProtobufTestWriter().Varint(1, 654321).String(3, "主播").Varint(5, 20).String(6, "粉丝牌")); // medal_info

            foreach (var gift in gifts)
                writer.Message(10, gift);

            writer.Varint(11, @switch ? 1UL : 0UL);
            writer.Varint(12, 0); // test
            writer.Message(15, new ProtobufTestWriter() // sender_uinfo
                .Varint(1, (ulong)uid)
                .Message(2, new ProtobufTestWriter().String(1, uname).String(2, "http://i0.hdslb.com/bfs/face/member/noface.jpg").Varint(3, 0).Varint(4, 0)));

            return writer;
        }

        [Fact]
        public void ParsesSingleGift()
        {
            var pb = Broadcast(12345, "测试用户", true, GiftItem(1, "辣条", 5, 100, "gold")).ToArray();

            var model = new DanmakuModel(BuildJson(pb));

            Assert.Equal(DanmakuMsgType.GiftSend, model.MsgType);
            Assert.Equal("测试用户", model.UserName);
            Assert.Equal(12345, model.UserID);
            Assert.Equal("辣条", model.GiftName);
            Assert.Equal(5, model.GiftCount);

            var gift = Assert.Single(model.GiftList!);
            Assert.Equal(1, gift.GiftId);
            Assert.Equal("辣条", gift.GiftName);
            Assert.Equal(5, gift.Num);
            Assert.Equal(100, gift.Price);
            Assert.Equal(500, gift.TotalCoin);
            Assert.Equal("gold", gift.CoinType);
        }

        [Fact]
        public void ParsesMultipleGifts()
        {
            var pb = Broadcast(12345, "测试用户", true,
                GiftItem(31036, "小花花", 1, 100, "gold"),
                GiftItem(31039, "牛哇牛哇", 2, 100, "gold"),
                GiftItem(1, "辣条", 10, 100, "silver")).ToArray();

            var model = new DanmakuModel(BuildJson(pb));

            Assert.Equal(DanmakuMsgType.GiftSend, model.MsgType);
            Assert.Equal("小花花", model.GiftName);
            Assert.Equal(1, model.GiftCount);

            Assert.NotNull(model.GiftList);
            Assert.Collection(model.GiftList!,
                x => { Assert.Equal("小花花", x.GiftName); Assert.Equal(1, x.Num); Assert.Equal("gold", x.CoinType); },
                x => { Assert.Equal("牛哇牛哇", x.GiftName); Assert.Equal(2, x.Num); Assert.Equal("gold", x.CoinType); },
                x => { Assert.Equal("辣条", x.GiftName); Assert.Equal(10, x.Num); Assert.Equal("silver", x.CoinType); });
        }

        [Fact]
        public void SkipsUnknownFieldsOfEveryWireType()
        {
            var gift = GiftItem(1, "辣条", 1, 100, "gold")
                .Varint(200, ulong.MaxValue)
                .Fixed64(201, 0x0123456789ABCDEF)
                .Bytes(202, new byte[] { 0xFF, 0x00, 0x80 })
                .Fixed32(203, 0xDEADBEEF);

            var pb = Broadcast(12345, "测试用户", true, gift)
                .Varint(300, 1)
                .Fixed64(301, 0)
                .Bytes(302, Array.Empty<byte>())
                .Fixed32(303, 0)
                .ToArray();

            var model = new DanmakuModel(BuildJson(pb));

            Assert.Equal(DanmakuMsgType.GiftSend, model.MsgType);
            Assert.Equal("测试用户", model.UserName);
            Assert.Equal("辣条", model.GiftName);
            Assert.Equal(1, model.GiftCount);
        }

        [Fact]
        public void ParsesAnonymousSenderWithoutError()
        {
            // 开启匿名功能的房间，消息的 sender_uinfo 中会包含 anon 信息。录播姬不解密该信息，此测试只要求解析不出错。
            var pb = Broadcast(0, "匿名用户", true, GiftItem(1, "辣条", 1, 100, "gold"))
                .Message(15, new ProtobufTestWriter()
                    .Varint(1, 0)
                    .Message(2, new ProtobufTestWriter().String(1, "匿名用户").Varint(4, 1)) // base.is_mystery
                    .Message(9, new ProtobufTestWriter() // anon
                        .Varint(1, 1)
                        .String(2, "anon_1234567890")
                        .String(3, "v1")
                        .String(4, Convert.ToBase64String(new byte[32]))
                        .Varint(5, 1)))
                .ToArray();

            var model = new DanmakuModel(BuildJson(pb));

            Assert.Equal(DanmakuMsgType.GiftSend, model.MsgType);
            Assert.Equal("匿名用户", model.UserName);
            Assert.Equal(0, model.UserID);
            Assert.Equal("辣条", model.GiftName);
        }

        [Fact]
        public void ParsesEmptyGiftListAsGiftSendWithNoGifts()
        {
            // 写入弹幕文件时以 GiftList 是否为 null 区分 SEND_GIFT_V2 和旧版 SEND_GIFT
            var pb = Broadcast(12345, "测试用户", true).ToArray();

            var model = new DanmakuModel(BuildJson(pb));

            Assert.Equal(DanmakuMsgType.GiftSend, model.MsgType);
            Assert.NotNull(model.GiftList);
            Assert.Empty(model.GiftList!);
            Assert.Null(model.GiftName);
            Assert.Equal(0, model.GiftCount);
        }

        [Fact]
        public void IgnoresMessageWhenSwitchIsFalse()
        {
            var pb = Broadcast(12345, "测试用户", false, GiftItem(1, "辣条", 1, 100, "gold")).ToArray();

            var model = new DanmakuModel(BuildJson(pb));

            Assert.Equal(DanmakuMsgType.Unknown, model.MsgType);
        }

        [Fact]
        public void IgnoresMessageWithoutPb()
        {
            var model = new DanmakuModel(@"{""cmd"":""SEND_GIFT_V2"",""danmu"":{""area"":0},""data"":{""dmscore"":476}}");

            Assert.Equal(DanmakuMsgType.Unknown, model.MsgType);
        }

        [Fact]
        public void ThrowsOnTruncatedPayload()
        {
            var pb = Broadcast(12345, "测试用户", true, GiftItem(1, "辣条", 1, 100, "gold")).ToArray();
            var truncated = new byte[pb.Length - 1];
            Array.Copy(pb, truncated, truncated.Length);

            Assert.Throws<InvalidDataException>(() => new DanmakuModel(BuildJson(truncated)));
        }

        [Fact]
        public void ThrowsOnFieldNumberOutOfRange()
        {
            var pb = Broadcast(12345, "测试用户", true, GiftItem(1, "辣条", 1, 100, "gold"))
                .Raw(0x88, 0x80, 0x80, 0x80, 0x80, 0x01, 0x2A) // 字段编号 2^32+1（截断为 int 后等于 1），varint 值 42
                .ToArray();

            Assert.Throws<InvalidDataException>(() => new DanmakuModel(BuildJson(pb)));
        }

        [Fact]
        public void ThrowsOnVarintOverflow()
        {
            var pb = Broadcast(12345, "测试用户", true, GiftItem(1, "辣条", 1, 100, "gold"))
                .Raw(0x08, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x02) // 字段 1，varint 值为 2^64
                .ToArray();

            Assert.Throws<InvalidDataException>(() => new DanmakuModel(BuildJson(pb)));
        }

        [Fact]
        public void AcceptsMaxTenByteVarint()
        {
            var pb = Broadcast(12345, "测试用户", true, GiftItem(1, "辣条", 1, 100, "gold"))
                .Raw(0x08, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01) // 字段 1，varint 值为 ulong.MaxValue
                .ToArray();

            var model = new DanmakuModel(BuildJson(pb));

            Assert.Equal(DanmakuMsgType.GiftSend, model.MsgType);
            Assert.Equal(-1, model.UserID);
        }

        [Fact]
        public void ThrowsOnInvalidBase64()
        {
            Assert.Throws<FormatException>(() => new DanmakuModel(@"{""cmd"":""SEND_GIFT_V2"",""data"":{""pb"":""not base64!""}}"));
        }
    }
}
