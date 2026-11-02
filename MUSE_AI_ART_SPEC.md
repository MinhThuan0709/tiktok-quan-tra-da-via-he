# 🎨 Đặc Tả Tài Nguyên Đồ Họa Cho MUSE AI — Dự Án "Quán Trà Đá / Quán Nhậu Vỉa Hè" (TikTok Live Game)

> ## 🚨 CHỈ THỊ BẮT BUỘC DÀNH CHO MUSE AI (MANDATORY GIT COMMIT & PUSH INSTRUCTION)
> Sau khi tạo xong bộ ảnh PNG trong thư mục `Assets/Resources/TeaStall/Sprites/`, **MUSE AI BẮT BUỘC PHẢI COMMIT VÀ PUSH NGƯỢC TOÀN BỘ ẢNH LÊN LẠI KHO GITHUB NÀY (`origin/main`)** bằng các lệnh sau:
> ```bash
> git add Assets/Resources/TeaStall/Sprites/*.png
> git commit -m "art(muse-ai): upload generated 2D Chibi sprites and title badges to Assets/Resources/TeaStall/Sprites"
> git push origin main
> ```
> **Tuyệt đối không chỉ tạo ảnh ở máy cục bộ/sandbox của MUSE AI mà quên `git push` lên lại GitHub!** Hệ thống Unity bên máy chính đang lắng nghe nhánh `main` của repo `https://github.com/MinhThuan0709/tiktok-quan-tra-da-via-he` để tự động `git pull` ảnh về ngay khi MUSE AI push lên.

---

## 1. Thư Mục Ảnh Tham Chiếu Mẫu (Reference Images)
Các ảnh mẫu định hình phong cách nằm tại thư mục:
* `Assets/Resources/TeaStall/References/ref_01_quan_nhau_via_he_tiktok.png`: Bố cục chuẩn của game TikTok Live vỉa hè (Góc nhìn 2.5D từ trên xuống, sân lát gạch ấm, 2 dãy **bàn nhựa đỏ + ghế đẩu nhựa xanh**, nhân vật Chibi ngồi quanh bàn uống trà đá/bia, trên đầu có Avatar tròn + Tên + Khung Danh Hiệu).
* `Assets/Resources/TeaStall/References/ref_02_hang_rong_tuoi_tho_chibi.png`: Phong cách vẽ nhân vật Chibi 2D Việt Nam dễ thương (đầu tròn bầu bĩnh, mắt to long lanh hoặc mắt cười híp mí, má hồng, viền đậm rõ nét, đổ bóng phẳng cel-shading ấm áp).
* `Assets/Resources/TeaStall/References/ref_03_khu_pho_zingme.jpg`: Phong cách phụ kiện, cánh VIP và đường phố Việt Nam.

---

## 2. Danh Sách 14 File Ảnh Cần Tạo / Cập Nhật (`Assets/Resources/TeaStall/Sprites/`)

> **Lưu ý kỹ thuật bắt buộc cho MUSE AI:**
> - Tất cả các file nhân vật, đạo cụ và khung danh hiệu (từ mục `2` đến `14`) phải có **nền trong suốt (PNG 32-bit RGBA Transparent)** hoặc nền trắng thuần `#FFFFFF` tách bạch hoàn toàn.
> - Giữ nguyên **chính xác tên file** dưới đây để code C# (`TeaStallPalette.cs`, `TeaCustomerAgent.cs`, `StreetNpcAgent.cs`, `BuildTeaStallScene.cs`) tự động nạp qua `Resources.Load<Sprite>("TeaStall/Sprites/<tên_file>")`.

| STT | Đường dẫn File trong Repo | Kích thước chuẩn | Định dạng | Mô tả Chi Tiết & Prompt Gợi Ý Cho MUSE AI |
| :---: | :--- | :---: | :---: | :--- |
| **1** | `Assets/Resources/TeaStall/Sprites/street_tea_bg.png` | `1080 x 1920` (Dọc 9:16) | PNG | **Bối cảnh nền Quán Trà Đá / Quán Nhậu Vỉa Hè ban đêm:** Góc nhìn 2.5D nghiêng từ trên xuống (top-down isometric). Phía trên là mặt tiền quán phố cổ ấm cúng, đèn lồng đỏ, dây đèn nháy vàng lung linh. Giữa màn hình là sân vỉa hè lát gạch rộng rãi để xếp các bàn nhựa đỏ và ghế nhựa xanh. Phía dưới mép đường có vỉa hè và chiếc xe máy Cub/Wave đỗ. |
| **2** | `Assets/Resources/TeaStall/Sprites/prop_tea_counter.png` | `512 x 512` | PNG (RGBA) | **Bàn Nhựa Đỏ & Ghế Đẩu Nhựa Xanh / Quầy Trà Đá Vỉa Hè:** Bàn nhựa đỏ thấp chữ nhật đặc trưng vỉa hè Việt Nam kèm ghế đẩu nhựa xanh biển có lỗ tròn ở giữa, trên bàn có cốc trà đá/bia và đĩa hướng dương/lạc rang. |
| **3** | `Assets/Resources/TeaStall/Sprites/npc_ba_tam.png` | `512 x 512` | PNG (RGBA) | **NPC Chủ Quán "Bà Tám Trà Đá":** Cô/Bà chủ quán Chibi 2D mặt tròn phúc hậu, tóc búi cài trâm, mặc áo hoa/bà ba kèm tạp dề hồng, tay bưng khay inox có ấm trà Bát Tràng và mấy cốc trà đá mát lạnh, miệng cười tươi hiếu khách. |
| **4** | `Assets/Resources/TeaStall/Sprites/char_tier1_toong.png` | `512 x 512` | PNG (RGBA) | **Khách Cấp 1 (Khách Dép Tổ Ong):** Nhân vật nam Chibi 2D dễ thương ngồi trên ghế đẩu nhựa xanh, mặc áo ba lỗ trắng (hoặc áo thun vàng), quần đùi, chân xỏ đôi **dép tổ ong màu vàng** huyền thoại, tay cầm cốc trà đá chanh có đá viên, mặt cười rạng rỡ. |
| **5** | `Assets/Resources/TeaStall/Sprites/char_tier3_street.png` | `512 x 512` | PNG (RGBA) | **Khách Cấp 3 (Dân Chơi Phố Cổ):** Nhân vật nữ/nam Chibi 2D sành điệu ngồi trên ghế nhựa xanh, mặc áo khoác streetwear cá tính, kính râm cài đầu, biểu cảm vui vẻ năng động, có thể kèm cún Corgi nhỏ. |
| **6** | `Assets/Resources/TeaStall/Sprites/char_tier6_vip.png` | `512 x 512` | PNG (RGBA) | **Khách Cấp 6 (Tổng Tài / Thần Tài Bao Quán VIP):** Nhân vật Chibi 2D Đại Gia ngồi trên Ngai Vàng mini lộng lẫy, mặc hoàng bào/vest đỏ vàng kim, đội vương miện, sau lưng bung **Đôi Cánh Thiên Thần Hoàng Kim phát sáng**, tay nâng cốc vàng. |
| **7** | `Assets/Resources/TeaStall/Sprites/npc_grab_driver.png` | `512 x 512` | PNG (RGBA) | **NPC Anh Tài Xế Xe Ôm Công Nghệ (Grab):** Chibi 2D mặc áo khoác xanh lá, đội mũ bảo hiểm xanh lá có sọc trắng, đeo khẩu trang xanh, ngồi trên chiếc xe máy tay ga/Wave màu đỏ, một tay vẫy chào thân thiện. |
| **8** | `Assets/Resources/TeaStall/Sprites/npc_lottery_lady.png` | `512 x 512` | PNG (RGBA) | **NPC Cô Bán Vé Số Dạo:** Chibi 2D đội nón lá truyền thống Việt Nam, mặc áo bà ba tím dịu dàng, tay xòe xấp vé số nhiều màu sắc, nụ cười hiền hậu. |
| **9** | `Assets/Resources/TeaStall/Sprites/npc_banh_mi_cart.png` | `512 x 512` | PNG (RGBA) | **NPC Bác Bán Bánh Mì Sài Gòn:** Chibi 2D đẩy chiếc xe gỗ tủ kính sáng đèn bên trong xếp đầy bánh mì vàng giòn, có gắn chiếc loa rao nhỏ. |
| **10** | `Assets/Resources/TeaStall/Sprites/npc_keo_keo_singer.png` | `512 x 512` | PNG (RGBA) | **NPC Anh Hát Rong Loa Kẹo Kéo:** Chibi 2D đeo kính râm ngầu, một tay cầm micro hát say sưa kèm nốt nhạc, một tay kéo thùng loa vali di động có đèn LED 7 màu. |
| **11** | `Assets/Resources/TeaStall/Sprites/npc_trat_tu_phuong.png` | `512 x 512` | PNG (RGBA) | **NPC Chú Trật Tự Phường:** Chibi 2D đội mũ cối xanh rêu gắn sao, mặc đồng phục xanh rêu, miệng thổi còi bạc phát sóng âm, tay cầm gậy chỉ huy sọc đỏ trắng. |
| **12** | `Assets/Resources/TeaStall/Sprites/badge_frame_wood.png` | `600 x 160` | PNG (RGBA) | **Khung Danh Hiệu Bậc 1–2 (Khung Gỗ Mộc):** Bảng danh hiệu ngang tỉ lệ 4:1 bằng gỗ sồi chạm khắc viền dây thừng và lá trà xanh ở góc, vùng trung tâm phẳng tối màu và **trống chữ** để Unity chèn TextMeshPro. |
| **13** | `Assets/Resources/TeaStall/Sprites/badge_frame_neon_wings.png` | `600 x 160` | PNG (RGBA) | **Khung Danh Hiệu Bậc 3–4 (Khung Neon Cánh Bạc):** Bảng danh hiệu ngang tỉ lệ 4:1 viền pha lê tím-xanh cyan phát sáng, hai bên gắn đôi cánh thiên thần bạc lấp lánh, vùng trung tâm phẳng tối và **trống chữ**. |
| **14** | `Assets/Resources/TeaStall/Sprites/badge_frame_royal_dragon.png` | `600 x 160` | PNG (RGBA) | **Khung Danh Hiệu Bậc 5–6 (Khung Rồng Vàng Đế Vương):** Bảng danh hiệu ngang tỉ lệ 4:1 viền vàng kim chạm khắc 2 đầu rồng vàng và đôi cánh hoàng kim hai bên, đỉnh gắn ngọc Ruby đỏ, vùng trung tâm phẳng đỏ sẫm và **trống chữ**. |

---

## 3. Danh Sách 13 Asset Bổ Sung (WAVE 2 — Khung UI, Bong Bóng Chat, Skin C2/C4/C5 & Đạo Cụ Bàn)

> **Mục tiêu Wave 2:** Loại bỏ 100% các khung hộp UI phẳng (programmer art), bong bóng chat vẽ bằng hình chữ nhật thô cứng, và bổ sung đầy đủ trọn bộ 6 bậc khách hàng + 6 NPC đường phố + đạo cụ bàn nhậu.
> - **QUAN TRỌNG:** Các khung UI (`15` đến `21`) **TUYỆT ĐỐI KHÔNG VẼ CHỮ BÊN TRONG** (trống chữ hoàn toàn ở lòng khung) để Unity hiển thị chữ động sắc nét bên trên.
> - Tất cả 13 file dưới đây đều xuất định dạng **PNG 32-bit RGBA nền trong suốt (Transparent Background)**.
> - Sau khi tạo xong, **MUSE AI BẮT BUỘC `git add`, `git commit` và `git push origin main`** lên lại kho GitHub này!

| STT | Đường dẫn File trong Repo | Kích thước chuẩn | Định dạng | Mô tả Chi Tiết & Prompt Gợi Ý Cho MUSE AI |
| :---: | :--- | :---: | :---: | :--- |
| **15** | `Assets/Resources/TeaStall/Sprites/ui_speech_bubble_chat.png` | `480 x 180` | PNG (RGBA) | **Bong Bóng Chat Khách Hàng (Viewer Speech Bubble):** Khung bong bóng thoại hoạt hình 2D bo tròn mềm mại, có đuôi nhọn nhỏ chỉ xuống dưới ở chính giữa đáy, nền màu kem sáng ấm (`#FFFDF7`), viền nâu đen dày dặn kiểu game Chibi 2D, **lòng khung trống chữ hoàn toàn**, nền ngoài trong suốt RGBA. |
| **16** | `Assets/Resources/TeaStall/Sprites/ui_speech_bubble_npc.png` | `560 x 200` | PNG (RGBA) | **Bong Bóng Thoại Chủ Quán & NPC (NPC Speech Bubble):** Khung bong bóng thoại 2D bo tròn sang trọng ấm áp, có đuôi nhọn chỉ xuống dưới, nền vàng kem ấm áp hoặc nâu trầm viền vàng kim, góc trên bên trái đính icon chiếc đèn lồng đỏ hoặc ấm trà nhỏ xinh, **lòng khung trống chữ hoàn toàn**, nền ngoài trong suốt RGBA. |
| **17** | `Assets/Resources/TeaStall/Sprites/ui_panel_menu.png` | `520 x 640` | PNG (RGBA) | **Bảng Gỗ Thực Đơn Vỉa Hè (Menu Chalkboard Panel):** Bảng phấn xanh đen cổ điển của quán nước vỉa hè Việt Nam (`#0E221A`), bao quanh bởi khung gỗ sồi chạm khắc ấm áp, góc trên trang trí vài lá trà xanh và hoa hồi, **bề mặt bảng ở giữa phẳng tối màu và TRỐNG CHỮ HOÀN TOÀN** để Unity viết danh sách món, nền ngoài trong suốt RGBA. |
| **18** | `Assets/Resources/TeaStall/Sprites/ui_panel_leaderboard.png` | `520 x 640` | PNG (RGBA) | **Bảng Xếp Hạng Đại Gia Vỉa Hè (VIP Leaderboard Panel):** Khung bảng dọc 2D sang trọng viền vàng kim hoàng gia và gỗ đỏ, đỉnh khung chính giữa có chạm biểu tượng Vương Miện Vàng lấp lánh, lòng bảng màu tím than/nâu sẫm phẳng mịn (`#161126`) và **TRỐNG CHỮ HOÀN TOÀN** để Unity điền Top 5 Đại Gia, nền ngoài trong suốt RGBA. |
| **19** | `Assets/Resources/TeaStall/Sprites/ui_header_signboard.png` | `960 x 180` | PNG (RGBA) | **Biển Hiệu Đèn Lồng Đỉnh Màn Hình (Top Street Signboard Frame):** Khung biển hiệu ngang phong cách phố cổ Hà Nội / Sài Gòn ban đêm, viền gỗ kết hợp đèn Neon ấm, hai bên treo 2 chiếc đèn lồng đỏ nhỏ phát sáng và dây đèn nháy vàng phía trên, lòng biển hiệu tối sẫm phẳng và **TRỐNG CHỮ HOÀN TOÀN**, nền ngoài trong suốt RGBA. |
| **20** | `Assets/Resources/TeaStall/Sprites/ui_stat_pill.png` | `300 x 110` | PNG (RGBA) | **Khung Ô Thống Kê Nhỏ (Top Stat Counter Pill):** Khung thẻ ngang nhỏ bo góc bằng gỗ tối màu viền đồng vàng bóng bẩy (tỉ lệ ~3:1), lòng trong phẳng tối màu và **TRỐNG CHỮ HOÀN TOÀN** (dùng cho 4 ô ĐÃ BÁN, ĐANG NGỒI, LẦN DÔ, CHỜ BÀN), nền ngoài trong suốt RGBA. |
| **21** | `Assets/Resources/TeaStall/Sprites/ui_party_bar_frame.png` | `920 x 100` | PNG (RGBA) | **Khung Thanh Nhiệt Huyết Cụng Ly (Bottom Party Progress Bar Frame):** Khung viền thanh tiến trình nằm ngang phong cách lễ hội bia/trà đá vỉa hè, viền vàng đồng chạm bọt bia/lá trà hai đầu, lòng thanh tối màu rỗng để Unity làm thanh chạy phần trăm và **TRỐNG CHỮ HOÀN TOÀN**, nền ngoài trong suốt RGBA. |
| **22** | `Assets/Resources/TeaStall/Sprites/char_tier2_sunflower.png` | `512 x 512` | PNG (RGBA) | **Khách Cấp 2 (Thánh Cắn Hướng Dương):** Nhân vật Chibi 2D dễ thương ngồi trên ghế đẩu nhựa xanh, mặc áo sơ mi khoác ngoài áo phông trẻ trung, đội mũ lưỡi trai ngược, hai tay đang cầm hạt hướng dương cắn rôm rốp vui vẻ, nền trong suốt RGBA. |
| **23** | `Assets/Resources/TeaStall/Sprites/char_tier4_cyber.png` | `512 x 512` | PNG (RGBA) | **Khách Cấp 4 (Chiến Thần Chém Gió — Cánh Bạc Neon):** Nhân vật Chibi 2D cực ngầu ngồi trên ghế gỗ/nhựa đỏ, mặc đồ idol đường phố phát sáng viền neon xanh tím, sau lưng có **Đôi Cánh Thiên Thần Ánh Bạc / Băng Tuyết** nhỏ gọn lấp lánh, nền trong suốt RGBA. |
| **24** | `Assets/Resources/TeaStall/Sprites/char_tier5_phuba.png` | `512 x 512` | PNG (RGBA) | **Khách Cấp 5 (Phú Bà Vỉa Hè — Cánh Hồng Ngọc):** Nhân vật nữ Chibi 2D sang chảnh đáng yêu ngồi trên ghế bọc nhung đỏ, mặc váy dạ hội/áo dài cách tân đỏ Ruby quý phái, đeo kính thời trang, sau lưng bung **Đôi Cánh Thiên Thần Trắng Hồng**, tay ôm cún Bạch Tuyết nhỏ, nền trong suốt RGBA. |
| **25** | `Assets/Resources/TeaStall/Sprites/npc_shoe_shiner.png` | `512 x 512` | PNG (RGBA) | **NPC Chú Đánh Giày Dạo:** Nhân vật Chibi 2D đội mũ vải, mặc áo giản dị thân thiện, một tay xách hòm gỗ đánh giày truyền thống Việt Nam, một tay cầm bàn chải đánh giày vẫy chào khách, nền trong suốt RGBA. |
| **26** | `Assets/Resources/TeaStall/Sprites/prop_dieu_cay.png` | `256 x 256` | PNG (RGBA) | **Đạo Cụ Điếu Cày Thanh Hóa Đặt Trên Bàn:** Chiếc điếu cày bằng ống tre vàng óng truyền thống Việt Nam vẽ kiểu 2D Chibi dễ thương, có nõ điếu đỏ lửa nhẹ và làn khói trắng hình tròn nhỏ bay lên, nền trong suốt RGBA. |
| **27** | `Assets/Resources/TeaStall/Sprites/prop_nem_chua_met.png` | `256 x 256` | PNG (RGBA) | **Đạo Cụ Mẹt Nem Chua Rán & Khô Mực Đặt Trên Bàn:** Mẹt tre đan tròn bày những miếng nem chua rán vàng ruộm, dưa chuột, khô mực nướng và bát tương ớt đỏ hấp dẫn ở giữa, vẽ kiểu 2D Chibi sắc nét, nền trong suốt RGBA. |
