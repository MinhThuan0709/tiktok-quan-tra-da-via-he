# 🎨 Đặc Tả Tài Nguyên Đồ Họa Cho MUSE AI — Dự Án "Quán Trà Đá / Quán Nhậu Vỉa Hè" (TikTok Live Game)

Tài liệu này hướng dẫn **MUSE AI** (hoặc bất kỳ công cụ AI Art nào kết nối qua GitHub) tạo và cập nhật trực tiếp bộ tài nguyên đồ họa 2D vào đúng đường dẫn trong dự án Unity để game tự động nhận diện.

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
