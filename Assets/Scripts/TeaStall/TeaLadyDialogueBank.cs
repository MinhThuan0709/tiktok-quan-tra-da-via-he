using System;
using System.Collections.Generic;
using UnityEngine;

namespace TikTokTeaStall
{
    public enum StreetNpcType
    {
        GrabDriver,
        LotteryVendor,
        BreadVendor,
        ShoeShiner,
        StreetSinger,
        WardPatrol
    }

    /// <summary>
    /// Kho thoại hóm hỉnh, đậm chất vỉa hè phố cổ Hà Nội của NPC Bà Tám Trà Đá & 6 NPC Đường phố Việt Nam.
    /// </summary>
    public static class TeaLadyDialogueBank
    {
        private static readonly System.Random Rnd = new System.Random();

        // ==================== CÂU CHÀO KHÁCH MỚI ====================
        private static readonly string[] WelcomeNormalLines = new string[]
        {
            "Ơ kìa {0}! Ngồi xuống cái ghế nhựa làm cốc trà đá mát rượi chém gió nào!",
            "Vào đây em ơi! Nắng cháy da cháy thịt, trà đá 3 ngàn bao mát bao phê!",
            "Nay rảnh rỗi thế {0}? Uống trà sen hay cắn đĩa hướng dương nào?",
            "Lựa ghế nhựa xanh hay đỏ ngồi đi {0}, bà rót trà liền đây!",
            "Kìa bạn hiền {0}! Ngồi vào bàn đầu chém gió với anh em cho xôm tụ nào!",
            "Trà đá vỉa hè phục vụ 24/7! Chào mừng {0} ghé tiệm Bà Tám!"
        };

        private static readonly string[] WelcomeVipLines = new string[]
        {
            "Ối dồi ôi ĐẠI GIA {0} ghé quán! Bà Tám dọn sẵn ghế VIP trải thảm đỏ cho bác đây!",
            "Chào mừng dân chơi {0}! Nay gọi bao nhiêu đĩa hướng dương với trà sen thượng hạng bác ơi?",
            "Bà con đứng dậy vỗ tay đón Thần Tài {0} vào bao cả quán nào!",
            "Trời đất ơi hào quang chói lóa quá! VIP {0} bước vào làm sáng rực cả con phố!",
            "Đại gia {0} vừa tới! Bác thích rít điếu cày hay loa kẹo kéo quẩy tưng bừng cứ bảo bà!"
        };

        public static string GetWelcomeLine(string nickname, int vipTier)
        {
            if (string.IsNullOrEmpty(nickname)) nickname = "bạn";
            string[] pool = (vipTier >= 3) ? WelcomeVipLines : WelcomeNormalLines;
            string template = pool[Rnd.Next(pool.Length)];
            return string.Format(template, nickname);
        }

        // ==================== BẮT LỆNH & THOẠI ĐỐI ĐÁP THÔNG MINH ====================
        public static string GetSmartReplyForComment(string nickname, string message, out string specialCommand)
        {
            specialCommand = null;
            if (string.IsNullOrEmpty(nickname)) nickname = "Khách";
            if (string.IsNullOrEmpty(message)) message = "";

            string lower = message.ToLower().Trim();

            // 1. Kiểm tra LỆNH ĐẶC BIỆT (#...)
            if (lower.StartsWith("#trada"))
            {
                specialCommand = "BADGE:🍵 NGHIỆN TRÀ ĐÁ";
                return $"{nickname} đã được cấp chứng nhận: 🍵 NGHIỆN TRÀ ĐÁ THÂM NIÊN!";
            }
            if (lower.StartsWith("#docthan"))
            {
                specialCommand = "BADGE:💔 ĐỘC THÂN VUI TÍNH";
                return $"Độc thân cho nó thanh thản {nickname} ơi! Tiền để dành uống trà đá!";
            }
            if (lower.StartsWith("#chemgio"))
            {
                specialCommand = "BADGE:🌬️ THÁNH NỔ VỈA HÈ";
                return $"Khu này gió giật cấp 12 rồi! Chắc do {nickname} mở đại hội chém gió!";
            }
            if (lower.StartsWith("#notien"))
            {
                specialCommand = "BADGE:💸 TRÙM NỢ TIỀN NƯỚC";
                return $"Nợ thì nợ chứ gặp Bà Tám là cuối tháng tính lãi bằng nụ cười nha {nickname}!";
            }
            if (lower.StartsWith("#vip"))
            {
                string customBadge = message.Substring(Math.Min(4, message.Length)).Trim();
                if (string.IsNullOrEmpty(customBadge)) customBadge = "🌟 KHÁCH VIP VỈA HÈ";
                if (customBadge.Length > 20) customBadge = customBadge.Substring(0, 20);
                specialCommand = "CUSTOM_BADGE:" + customBadge;
                return $"Danh hiệu mới '{customBadge}' của {nickname} nhìn chất chơi người dơi đấy!";
            }
            if (lower.StartsWith("#veso"))
            {
                specialCommand = "ACTION:BUY_TICKET";
                return $"Bốc ngay tờ vé số đuôi may mắn! Chiều nay trúng số nhớ bao cả quán nhá {nickname}!";
            }
            if (lower.StartsWith("#banhmi"))
            {
                specialCommand = "ACTION:ORDER_BREAD";
                return $"Bánh mì Sài Gòn đặc ruột thơm bơ nóng giòn tới ngay bàn của {nickname}!";
            }

            // 2. Bắt TỪ KHÓA ĐỜI THƯỜNG
            if (lower.Contains("trà") || lower.Contains("nước") || lower.Contains("khát") || lower.Contains("đá"))
            {
                string[] replies = new string[]
                {
                    "Nước mát đá lạnh đây! Một ngụm là tỉnh cả người, hai ngụm là quên sầu!",
                    "Uống từ từ thôi kẻo buốt óc đấy con ơi! Trà sen bà pha bằng nước giếng Ngọc đấy!",
                    "Trà đá vỉa hè Hà Nội không nơi nào sánh bằng đâu {0} ơi!",
                    "Cốc đầy ụ đá cho mát ruột nào {0}!"
                };
                return string.Format(replies[Rnd.Next(replies.Length)], nickname);
            }

            if (lower.Contains("giá") || lower.Contains("nhiêu") || lower.Contains("tiền") || lower.Contains("đồng"))
            {
                string[] replies = new string[]
                {
                    "Trà đá 3 khìn một cốc, hướng dương 10 khìn, nợ thì ghi sổ đầu tháng trả!",
                    "Hạt dẻ nhất quả đất rồi, vừa túi tiền học sinh sinh viên tài xế!",
                    "Quán bà bán vì đam mê chứ lời lãi gì ba cái ngụm trà con ơi!",
                    "Cứ uống tẹt ga đi {0}, giá cả bình dân vỉa hè bao năm không tăng giá!"
                };
                return string.Format(replies[Rnd.Next(replies.Length)], nickname);
            }

            if (lower.Contains("yêu") || lower.Contains("ế") || lower.Contains("crush") || lower.Contains("bồ") || lower.Contains("chia tay"))
            {
                string[] replies = new string[]
                {
                    "Người yêu không có thì có trà đá! Trà đá chung thủy không bao giờ cắm sừng mình!",
                    "Yêu đương gì tầm này, ngồi cắn hướng dương hóng biến phố phường vui hơn nhiều!",
                    "Ế là xu thế đỉnh cao của thời đại đấy {0} à, tha hồ tự do!",
                    "Buồn tình thì rít điếu cày, khói bay nghi ngút tình bay theo mây ngay!"
                };
                return string.Format(replies[Rnd.Next(replies.Length)], nickname);
            }

            if (lower.Contains("nghèo") || lower.Contains("nợ") || lower.Contains("hết tiền") || lower.Contains("lương"))
            {
                string[] replies = new string[]
                {
                    "Nghèo thì cho chịu, chiều trúng đề trả sau! Quán Bà Tám có tình có nghĩa!",
                    "Còn thở là còn gỡ, làm ngụm trà lấy lại nhuệ khí chiến đấu nào!",
                    "Tiền bạc là phù du, sức khỏe và cốc trà đá vỉa hè mới là vĩnh cửu!",
                    "Cứ ngồi đây uống trà chém gió, tí đại gia vào bao quán là lại no say!"
                };
                return string.Format(replies[Rnd.Next(replies.Length)], nickname);
            }

            if (lower.Contains("thuốc lào") || lower.Contains("điếu cày") || lower.Contains("phê") || lower.Contains("rít"))
            {
                string[] replies = new string[]
                {
                    "Điếu cày nỏ đồng Tiên Lãng chính gốc, rít một hơi là thấy 9 tầng mây luôn!",
                    "Cẩn thận bắn khói vào mắt bà con bên cạnh nha ông thần {0}!",
                    "Rít một hơi nhẹ thôi, rít sâu quá tí ngã ngửa ra vỉa hè bà không đỡ nổi đâu!",
                    "Làm hơi thuốc lào trà đặc, sảng khoái đời trai vỉa hè!"
                };
                return string.Format(replies[Rnd.Next(replies.Length)], nickname);
            }

            if (lower.Contains("xinh") || lower.Contains("đẹp") || lower.Contains("duyên") || lower.Contains("trẻ"))
            {
                string[] replies = new string[]
                {
                    "Khen khéo thế này tí bà rót cho cốc trà đầy ắp nhiều đá nhất quán nhé!",
                    "Cái nết đã đẹp mồm lại còn dẻo, thảo nào ai đi qua cũng phải liếc nhìn {0}!",
                    "Bà Tám tuy hoa râm nhưng tâm hồn tuổi 18 đôi mươi nha con!",
                    "Được lời như cởi tấm lòng! Tặng ngay nụ cười tỏa nắng cho {0}!"
                };
                return string.Format(replies[Rnd.Next(replies.Length)], nickname);
            }

            if (lower.Contains("buồn") || lower.Contains("mệt") || lower.Contains("chán") || lower.Contains("stress") || lower.Contains("áp lực"))
            {
                string[] replies = new string[]
                {
                    "Đời là mấy tí, vứt hết âu lo ra vỉa hè làm cốc trà đá là đời lại tươi ngay!",
                    "Mệt thì ngả lưng vào tường, hít thở mùi gió chiều phố cổ nào {0}!",
                    "Cuộc sống có lúc thăng lúc trầm, nhưng cốc trà đá thì lúc nào cũng thanh mát!",
                    "Cứ ra đây ngồi với bà, nghe chuyện tiếu lâm thiên hạ là quên hết ưu phiền!"
                };
                return string.Format(replies[Rnd.Next(replies.Length)], nickname);
            }

            if (lower.Contains("haha") || lower.Contains("kaka") || lower.Contains("hihi") || lower.Contains("cười") || lower.Contains("lol"))
            {
                string[] replies = new string[]
                {
                    "Cười tươi như hoa thế kia là lộc sắp về đầy túi đấy!",
                    "Cười to lên cho cả góc phố nó rực rỡ và ấm áp nào {0}!",
                    "Một nụ cười bằng mười cốc trà đá bổ dưỡng!",
                    "Haha! Hôm nay quán ta vui như hội làng mở hội!"
                };
                return string.Format(replies[Rnd.Next(replies.Length)], nickname);
            }

            // Mặc định câu tiếp chuyện duyên dáng
            string[] defaultReplies = new string[]
            {
                "Hay lắm {0}! Bàn bên cạnh đang vểnh tai hóng chuyện của em đấy!",
                "Chí lý chí lý! Làm ngụm trà nuốt trôi chân lý nào {0}!",
                "Nghe câu này của {0} là biết dân sành sỏi phố cổ rồi!",
                "Thế á? Chuyện này Bà Tám mới nghe lần đầu đấy, kể tiếp nghe coi!",
                "Chí phải! Vỉa hè này là nơi tinh hoa hội tụ mà!"
            };
            return string.Format(defaultReplies[Rnd.Next(defaultReplies.Length)], nickname);
        }

        // ==================== KHỊA YÊU KHI QUÁN NGỒI ĐỒNG IM ẮNG ====================
        private static readonly string[] IdleTeaseLines = new string[]
        {
            "Này này mấy đứa, ngồi im thin thít thế? Trà tan hết đá thành nước lọc rồi kìa!",
            "Ai đó chém gió câu gì nghe giải nhiệt cuộc sống coi!",
            "Trời mát thế này mà không rít điếu cày hay cắn tí hạt dẻ thì phí của giời!",
            "Bà Tám quạt nan mỏi cả tay mà chưa thấy ai gọi thêm nem chua rán thế nhờ!",
            "Im ắng thế này lại tưởng quán cà phê thiền! Đây là Trà Đá Vỉa Hè bà con ơi!",
            "Mấy bác bàn bên sao cắn hạt hướng dương chậm chạp thế, để thi với bà không?"
        };

        public static string GetIdleTeaseLine()
        {
            return IdleTeaseLines[Rnd.Next(IdleTeaseLines.Length)];
        }

        // ==================== CÂU THOẠI PHẢN HỒI QUÀ TẶNG (DONATE) ====================
        public static string GetGiftReactionLine(string nickname, string itemName, int coins)
        {
            if (string.IsNullOrEmpty(nickname)) nickname = "Đại gia";

            if (coins >= 100 || itemName.Contains("Bao") || itemName.Contains("Quán"))
            {
                return $"🔥 ỐI TRỜI ƠI! ĐẠI GIA {nickname} VỪA BAO CẢ QUÁN! MỌI NGƯỜI UỐNG TRÀ CẮN HƯỚNG DƯƠNG MIỄN PHÍ TOÀN TẬP!";
            }
            if (coins >= 50 || itemName.Contains("Loa") || itemName.Contains("Kẹo"))
            {
                return $"🎉 ĐẠI GIA {nickname} VỪA THUÊ LOA KẸO KÉO! TOÀN BỘ VỈA HÈ ĐỨNG LÊN QUẨY TƯNG BỪNG NÀO BÀ CON!";
            }
            if (coins >= 10 || itemName.Contains("Nem"))
            {
                return $"🥢 Mẹt nem chua rán giòn rụm nóng hổi bưng ngay tới bàn bác {nickname}! Cảm ơn đại gia hào sảng!";
            }
            if (coins >= 1 || itemName.Contains("Thuốc") || itemName.Contains("Điếu"))
            {
                return $"💨 Điếu cày Tiên Lãng nỏ đồng trao tận tay {nickname}! Rít một hơi phê lên tận nóc nhà!";
            }

            // Mặc định tim / hướng dương
            return $"🌻 Đĩa hướng dương đầy ụ mời bàn {nickname}! Chúc đại gia cắn hạt mỏi răng đời thăng hoa!";
        }

        // ==================== KHO THOẠI 6 NPC ĐƯỜNG PHỐ VIỆT NAM ====================
        private static readonly Dictionary<StreetNpcType, string[]> StreetNpcDialogues = new Dictionary<StreetNpcType, string[]>()
        {
            {
                StreetNpcType.GrabDriver,
                new string[]
                {
                    "Bà Tám ơi cho ly trà đá nhiều đá lẹ em còn chạy cuốc xe ghép nắng cháy đầu!",
                    "Ting ting! Nổ cuốc đi Hồ Gươm 85 khìn rồi bà con ơi!",
                    "Ai đi xe ôm công nghệ 5 sao đón tận cửa không nào!",
                    "Chạy từ sáng giờ mới được tạt vào làm ngụm trà mát ruột mát gan!",
                    "Bác nào bấm app gọi cuốc trà đá Bà Tám đấy, em tới nơi rồi nè!"
                }
            },
            {
                StreetNpcType.LotteryVendor,
                new string[]
                {
                    "Mua giùm cô tờ vé số chiều xổ chiều lấy tiền đây con ơi! Còn 2 tờ số đẹp thần tài 79 này!",
                    "Vé số kiến thiết miền Bắc, chiều nay 6 rưỡi thành tỷ phú bao cả quán luôn!",
                    "Số 68 lộc phát, 79 thần tài lớn, ai rước về đổi đời đi con!",
                    "Một tờ mười ngàn đổi lấy biệt thự xe sang, ngại gì không thử vận may con ơi!",
                    "Bà Tám lấy tờ lấy hên khai trương chiều nay trúng độc đắc nào!"
                }
            },
            {
                StreetNpcType.BreadVendor,
                new string[]
                {
                    "Ai bánh mì Sài Gòn... đặc ruột thơm bơ... nóng giòn một ổ hai mươi ngàn đây!",
                    "Bánh mì pate trứng chả giò nóng hổi vừa thổi vừa ăn đê!",
                    "Trà đá bánh mì là combo huyền thoại bất bại của người lao động Việt Nam!",
                    "Bánh mì nóng giòn rụm, thơm nức mũi cả góc phố cổ đây bà con!",
                    "Bà Tám ăn ổ bánh mì bơ sữa lấy sức pha trà không bà ơi?"
                }
            },
            {
                StreetNpcType.ShoeShiner,
                new string[]
                {
                    "Anh giai ơi đánh bóng đôi dép tổ ong cho nó sáng loáng đi tán gái không anh?",
                    "Giày da dép nhựa đánh xi đen bóng soi gương được luôn nha anh em!",
                    "Ngồi uống trà chém gió để em chà bóng đôi giày lấy hên trúng hợp đồng nào!",
                    "Xi Thái Lan loại một, đánh xong đi mưa không sợ ướt dính bùn đất đâu anh!",
                    "Anh đẹp trai bàn đầu đánh giày không anh ơi, năm ngàn một đôi lẹ làng!"
                }
            },
            {
                StreetNpcType.StreetSinger,
                new string[]
                {
                    "Alo 1 2 3 4! Tình nghĩa anh em có chắc bền lâu... Bà con vỉa hè đứng lên quẩy với em nào!",
                    "Nhạc remix cực căng phục vụ bà con phố cổ đây! Đưa tay lên nào anh em ơi!",
                    "Bà con cho em xin tràng pháo tay và ủng hộ em gói kẹo kéo kéo dài thanh xuân!",
                    "Hôm nay trời đẹp trăng thanh, em xin gửi tới bà con bản tình ca vỉa hè nồng thắm!",
                    "Cảm ơn quý đại gia đã donate cho ban nhạc kẹo kéo lưu động đường phố!"
                }
            },
            {
                StreetNpcType.WardPatrol,
                new string[]
                {
                    "Tuýt tuýt! Đề nghị quán Bà Tám xếp ghế gọn vào trong vạch sơn trắng nhá, không lấn chiếm lòng lề đường!",
                    "Tuýt tuýt! Ai để xe máy dưới lòng đường dắt gọn lên hè ngay kẻo bị cẩu xe!",
                    "Nhắc nhở nhẹ nhàng lần đầu, vỉa hè dành cho người đi bộ nha bà con!",
                    "Tuýt tuýt! Trả lại sự thông thoáng cho lòng đường phố cổ nào!",
                    "Xếp bàn ghế sát tường lại, lát đội kiểm tra quay lại là tịch thu ghế nhựa đấy nhá!"
                }
            }
        };

        public static string GetStreetNpcLine(StreetNpcType npcType)
        {
            if (StreetNpcDialogues.TryGetValue(npcType, out string[] list) && list.Length > 0)
            {
                return list[Rnd.Next(list.Length)];
            }
            return "Xin chào bà con vỉa hè phố cổ!";
        }
    }
}
