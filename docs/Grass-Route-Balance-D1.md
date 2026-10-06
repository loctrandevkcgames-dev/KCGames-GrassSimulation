# Grass Route — Cân bằng D1

Mirror of the Google Sheet "Grass Route — Cân bằng D1" (exported 2026-10-06). The sheet is the source of truth; update this file when it changes.

## 01 Máy

Chỉ số máy

Ba máy dùng chung kích thước thân. Bán kính, tốc độ, sức cắt là chỉ số gốc trước nâng cấp và booster.

Bảng máy

| Key | Máy | Bán kính cắt (m) | Tốc độ tối đa (m/s) | Sức cắt | Bán kính thân (m) | Đường kính cắt (m) | Bản D1 | Mở khóa | Vai trò |
|---|---|---|---|---|---|---|---|---|---|
| standard | Standard | 0.65 | 4 | 1 | 0.3 | 1.3 | Có | Có sẵn | Cân bằng |
| wide | Wide | 0.78 | 3.6 | 1 | 0.3 | 1.56 | Có | Sau màn 10, miễn phí trong bản D1 | Phủ rộng, chuyển vùng chậm, dễ chạm luống |
| heavy | Heavy | 0.585 | 4 | 1.3 | 0.3 | 1.17 | Không | Để sau | Cắt cây bền nhanh, phủ hẹp |

## 02 Điều khiển

Điều khiển và camera

Giá trị khởi tạo cho cảm giác lái. Chỉnh trong playtest nội bộ trước khi test D1.

Thông số điều khiển

| Key | Thông số | Giá trị | Đơn vị | Mục đích |
|---|---|---|---|---|
| body.radius | Bán kính thân máy | 0.3 | m | Cả ba máy đi được cùng lối |
| move.accel | Gia tốc | 12 | m/s² | Khởi động nhanh nhưng không giật |
| move.decel | Giảm tốc | 18 | m/s² | Dừng nhanh hơn khởi động |
| move.turn_rate | Tốc độ xoay hình máy | 540 | độ/s | Đọc được hướng đi |
| joystick.deadzone | Vùng chết joystick | 0.1 | % bán kính joystick | Chạm nhẹ không làm máy trôi |
| joystick.radius | Bán kính joystick | 70 | dp | Kéo hết = tốc độ tối đa |
| camera.pitch | Góc camera | 60 | độ | Khoảng thử 55–65 |
| move.stop_time | Thời gian dừng từ tốc độ tối đa (Standard) | 0.222 | s | Tự tính, để kiểm tra cảm giác dừng |

## 03 Cấp & nâng cấp

Cấp máy và hai hướng nâng cấp

XP chỉ tồn tại trong lượt. Mỗi lần lên cấp chọn một nâng cấp; hai hướng mở cùng nhóm cây.

Mốc cấp

| Cấp | Nhóm cây mở | XP tích lũy | XP cần thêm | Quyền chọn |
|---|---|---|---|---|
| 1 | Cỏ thường, hoa thu hoạch | 0 | 0 | Máy gốc |
| 2 | Cỏ dày, bụi thấp, rau/quả vừa | 100 | 100 | 1 nâng cấp |
| 3 | Bụi lớn, dưa hấu, bí ngô | 260 | 160 | 1 nâng cấp |
| 4 | Cây ăn quả lớn, quả khổng lồ | 480 | 220 | 1 nâng cấp |

Hai hướng nâng cấp

Số lần tối đa = số lần lên cấp (cấp 2, 3, 4). Không có giới hạn riêng.

| Key | Nâng cấp | Bán kính + (m) | Sức cắt + | Tốc độ + (m/s) | Số lần tối đa | Tối đa: bán kính | Tối đa: sức cắt | Tối đa: tốc độ | Đánh đổi |
|---|---|---|---|---|---|---|---|---|---|
| upgrade.blade | Lưỡi rộng | 0.15 | 0 | 0 | 3 | 0.45 | 0 | 0 | Phủ rộng hơn; dễ chạm luống bảo vệ |
| upgrade.engine | Động cơ khỏe | 0 | 0.3 | 0.25 | 3 | 0 | 0.9 | 0.75 | Cắt cây bền nhanh hơn, vệt cắt xong rộng hơn khi đi nhanh; phạm vi lưỡi không đổi |

Phạm vi cắt lớn nhất trên sân

Dùng để đặt khoảng cách luống bảo vệ: bán kính lớn nhất máy có thể đạt khi màn cho lên tới cấp đó (chọn Lưỡi rộng mọi lần).

| Máy | Cấp tối đa của màn | Bán kính lớn nhất (m) | Khoảng cách luống tối thiểu (m) |
|---|---|---|---|
| Standard | 2 | 0.8 | 0.9 |
| Standard | 3 | 0.95 | 1.05 |
| Standard | 4 | 1.1 | 1.2 |
| Wide | 2 | 0.93 | 1.03 |
| Wide | 3 | 1.08 | 1.18 |
| Wide | 4 | 1.23 | 1.33 |

## 04 Cây

Loại cây

Cây nhận tiến độ khi lưỡi chạm vùng cắt (GDD mục 5). Thời gian cắt = độ bền ÷ sức cắt. Quota và XP cộng một lần khi cắt xong.

Bảng cây

| Key | Loại cây | Cấp yêu cầu | Độ bền | XP / đơn vị | Kích thước vùng cắt (m) | Bán kính vùng cắt (m) | Thời gian cắt ở sức 1,0 (s) | Tốc độ tối đa vẫn cắt xong khi đi qua tâm, Standard gốc (m/s) | Thời gian / đơn vị, người chơi giỏi (s) | Ghi chú |
|---|---|---|---|---|---|---|---|---|---|---|
| grass_common | Cỏ thường | 1 | 0.12 | 1 | 0.5 | 0.25 | 0.12 | 15 | 0.1 | 1 đơn vị = 1 cụm 0,5 × 0,5 m |
| flower_small | Hoa / nấm nhỏ | 1 | 0.18 | 1 | 0.5 | 0.25 | 0.18 | 10 | 0.12 | Thu hoạch đúng loại |
| grass_thick | Cỏ dày | 2 | 0.3 | 2 | 0.5 | 0.25 | 0.3 | 6 | 0.18 | Không bắt buộc vệt liên tục ở tốc độ tối đa |
| bush_low | Bụi thấp | 2 | 0.45 | 3 | 0.8 | 0.4 | 0.45 | 4.667 | 0.7 |  |
| veg_medium | Cà rốt / bắp cải / dâu lớn | 2 | 0.55 | 4 | 0.8 | 0.4 | 0.55 | 3.818 | 0.8 | Một vật thể là một mục tiêu |
| bush_big | Bụi lớn / bụi cứng | 3 | 0.65 | 4 | 1.2 | 0.6 | 0.65 | 3.846 | 0.9 |  |
| melon | Dưa hấu / bí ngô | 3 | 0.9 | 8 | 1.2 | 0.6 | 0.9 | 2.778 | 1.2 | Đích nổi bật trong sân |
| fruit_tree | Cây ăn quả lớn | 4 | 2 | 0 | 1.2 | 0.6 | 2 | 1.25 | 3 | Vùng cắt = gốc, không tính tán. Không cho XP. Cây táo ×5 cộng 5 quả một lần. |
| fruit_giant | Quả khổng lồ | 4 | 2.5 | 0 | 2.5 | 1.25 | 2.5 | 1.52 | 3 | Không cho XP. Đích thị giác |
| flower_protected | Hoa bảo vệ | — | — | 0 | theo luống | — | — | — | — | Không cắt được. Một luống = một lỗi. Không XP, không quota. |

## 05 Vệt cắt

Dải cắt xong sau một lần đi qua ở tốc độ tối đa

Tự tính từ tab 01, 03, 04. Không sửa tay.

Bảng dải cắt

Giá trị = độ rộng dải (m) mà nếu tâm máy đi qua trong dải đó ở tốc độ tối đa thì cây được cắt xong trong một lần. Công thức: 2·√((R + r)² − (v·t/2)²), R = bán kính lưỡi, r = bán kính vùng cắt của cây, t = độ bền ÷ sức cắt. "Phải chậm" = không cắt xong khi đi tốc độ tối đa, kể cả đi qua tâm: người chơi phải đi chậm hoặc dừng, game hiện tín hiệu "đi chậm lại". "khóa" = chưa đủ cấp.

| Trạng thái máy |  |  |  |  | Chỉ số hiệu lực |  |  |  | Dải cắt xong theo loại cây (m) |  |  |  |  |  |  |  |  |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Trạng thái | Máy | Cấp | Lần Lưỡi rộng | Lần Động cơ | R (m) | v (m/s) | Sức cắt | Đường kính lưỡi (m) | Cỏ thường | Hoa / nấm nhỏ | Cỏ dày | Bụi thấp | Cà rốt / bắp cải / dâu lớn | Bụi lớn / bụi cứng | Dưa hấu / bí ngô | Cây ăn quả lớn | Quả khổng lồ |
| Standard cấp 1 | Standard | 1 | 0 | 0 | 0.65 | 4 | 1 | 1.3 | 1.735 | 1.65 | khóa | khóa | khóa | khóa | khóa | khóa | khóa |
| Standard cấp 2 + Lưỡi | Standard | 2 | 1 | 0 | 0.8 | 4 | 1 | 1.6 | 2.044 | 1.973 | 1.723 | 1.587 | 0.959 | khóa | khóa | khóa | khóa |
| Standard cấp 2 + Động cơ | Standard | 2 | 0 | 1 | 0.65 | 4.25 | 1.3 | 1.3 | 1.757 | 1.701 | 1.509 | 1.499 | 1.085 | khóa | khóa | khóa | khóa |
| Standard cấp 3 + 2 Lưỡi | Standard | 3 | 2 | 0 | 0.95 | 4 | 1 | 1.9 | 2.352 | 2.289 | 2.078 | 2.012 | 1.565 | 1.688 | 0 | khóa | khóa |
| Standard cấp 3 + Lưỡi + Động cơ | Standard | 3 | 1 | 1 | 0.8 | 4.25 | 1.3 | 1.6 | 2.063 | 2.016 | 1.857 | 1.896 | 1.59 | 1.823 | 0 | khóa | khóa |
| Standard cấp 3 + 2 Động cơ | Standard | 3 | 0 | 2 | 0.65 | 4.5 | 1.6 | 1.3 | 1.768 | 1.727 | 1.59 | 1.676 | 1.42 | 1.705 | 0 | khóa | khóa |
| Standard cấp 4 + 3 Lưỡi | Standard | 4 | 3 | 0 | 1.1 | 4 | 1 | 2.2 | 2.657 | 2.602 | 2.419 | 2.4 | 2.04 | 2.191 | 0 | 0 | 0 |
| Standard cấp 4 + 2 Lưỡi + Động cơ | Standard | 4 | 2 | 1 | 0.95 | 4.25 | 1.3 | 1.9 | 2.368 | 2.327 | 2.19 | 2.264 | 2.014 | 2.257 | 0.976 | 0 | 0 |
| Standard cấp 4 + Lưỡi + 2 Động cơ | Standard | 4 | 1 | 2 | 0.8 | 4.5 | 1.6 | 1.6 | 2.073 | 2.038 | 1.923 | 2.039 | 1.835 | 2.121 | 1.197 | 0 | 0 |
| Standard cấp 4 + 3 Động cơ | Standard | 4 | 0 | 3 | 0.65 | 4.75 | 1.9 | 1.3 | 1.775 | 1.743 | 1.636 | 1.773 | 1.587 | 1.9 | 1.09 | 0 | 0 |
| Wide cấp 1 | Wide | 1 | 0 | 0 | 0.78 | 3.6 | 1 | 1.56 | 2.014 | 1.955 | khóa | khóa | khóa | khóa | khóa | khóa | khóa |
| Wide cấp 2 + Lưỡi | Wide | 2 | 1 | 0 | 0.93 | 3.6 | 1 | 1.86 | 2.32 | 2.269 | 2.098 | 2.11 | 1.776 | khóa | khóa | khóa | khóa |
| Wide cấp 2 + Động cơ | Wide | 2 | 0 | 1 | 0.78 | 3.85 | 1.3 | 1.56 | 2.029 | 1.99 | 1.859 | 1.948 | 1.708 | khóa | khóa | khóa | khóa |
| Wide cấp 3 + 2 Lưỡi | Wide | 3 | 2 | 0 | 1.08 | 3.6 | 1 | 2.16 | 2.625 | 2.58 | 2.431 | 2.477 | 2.2 | 2.411 | 0.89 | khóa | khóa |
| Wide cấp 3 + Lưỡi + Động cơ | Wide | 3 | 1 | 1 | 0.93 | 3.85 | 1.3 | 1.86 | 2.333 | 2.299 | 2.186 | 2.302 | 2.103 | 2.379 | 1.503 | khóa | khóa |
| Wide cấp 3 + 2 Động cơ | Wide | 3 | 0 | 2 | 0.78 | 4.1 | 1.6 | 1.56 | 2.037 | 2.008 | 1.911 | 2.059 | 1.893 | 2.201 | 1.516 | khóa | khóa |
| Wide cấp 4 + 3 Lưỡi | Wide | 4 | 3 | 0 | 1.23 | 3.6 | 1 | 2.46 | 2.928 | 2.888 | 2.756 | 2.829 | 2.59 | 2.814 | 1.702 | 0 | 0 |
| Wide cấp 4 + 2 Lưỡi + Động cơ | Wide | 4 | 2 | 1 | 1.08 | 3.85 | 1.3 | 2.16 | 2.636 | 2.606 | 2.507 | 2.643 | 2.472 | 2.754 | 2.046 | 0 | 0 |
| Wide cấp 4 + Lưỡi + 2 Động cơ | Wide | 4 | 1 | 2 | 0.93 | 4.1 | 1.6 | 1.86 | 2.34 | 2.314 | 2.231 | 2.397 | 2.256 | 2.567 | 2.011 | 0 | 0 |
| Wide cấp 4 + 3 Động cơ | Wide | 4 | 0 | 3 | 0.78 | 4.35 | 1.9 | 1.56 | 2.042 | 2.018 | 1.942 | 2.123 | 1.996 | 2.324 | 1.836 | 0 | 0 |

Đọc nhanh cho level design

| Cỏ, hoa, cỏ dày | Luôn cắt xong khi đi tốc độ tối đa, dải rộng hơn đường kính lưỡi: giữ được cam kết "vệt sạch liên tục". |
|---|---|
| Bụi thấp, rau, bụi lớn | Cắt xong khi đi nhanh nếu đi gần tâm; dải hẹp hơn cỏ. Lưỡi rộng mở rộng dải rõ nhất. |
| Dưa/bí | Phần lớn trạng thái phải đi chậm. Chỉ khi có Động cơ khỏe (hoặc máy Wide) mới cắt xong lúc đi nhanh: phần thưởng riêng cho hướng Động cơ. |
| Cây quả, quả khổng lồ | Luôn phải dừng để cắt: đúng ý đồ "đích lớn cần dừng lại cắt". |

## 06 Booster

Booster

Bản D1 không có shop: booster được tặng khi mở khóa. Dùng bất kỳ booster nào làm lượt thành Assisted (chỉ nhận sao 1).

Bảng booster

| Key | Booster | Thời lượng (s) | Tốc độ × | Sức cắt × | Cấp cắt + | Cộng thời gian (s) | Mở sau khi thắng màn | Số lượng tặng | Bản D1 | Vai trò |
|---|---|---|---|---|---|---|---|---|---|---|
| booster.turbo | Turbo | 8 | 1.25 | 1.5 | 0 | 0 | 6 | 3 | Có | Dọn nhanh hoặc chuyển vùng; không vượt cấp |
| booster.extra_time | Extra Time | 0 | 1 | 1 | 0 | 15 | 4 | 3 | Có | Cứu phần quota còn thiếu |
| booster.power_blade | Power Blade | 8 | 1 | 1.3 | 1 | 0 | — | 2 | Không | Cắt một nhóm cây cao hơn cấp thực |

Luật dùng booster

| 1 | Mỗi loại mang tối đa 1, dùng tối đa 1 lần mỗi lượt; mang theo không bị tiêu. |
|---|---|
| 2 | Turbo và Power Blade không chạy cùng lúc. Extra Time dùng được bất cứ lúc nào trong Playing. |
| 3 | Pause và màn chọn nâng cấp dừng thời lượng booster. Kết thúc lượt, chơi lại hoặc vào Cleanup xóa hiệu ứng. |

## 07 Luật

Thông số luật và thiết kế màn

Giá trị dùng chung cho luật chơi và quy tắc level design.

Bảng thông số

| Key | Nhóm | Thông số | Giá trị | Đơn vị | Ghi chú |
|---|---|---|---|---|---|
| protected.mode | Hoa bảo vệ | Chế độ mặc định bản D1 | warn | warn / fail | warn: lỗi chỉ làm mất sao 2. fail: vượt giới hạn thì thua |
| protected.fail_limit | Hoa bảo vệ | Số lỗi được phép (khi mode = fail) | 3 | lỗi | Lỗi thứ 4 thì thua |
| protected.retrigger | Hoa bảo vệ | Rời luống bao lâu thì chạm lại tính lỗi mới | 1 | s |  |
| protected.clearance | Hoa bảo vệ | Khoảng dư từ mép luống tới phạm vi cắt lớn nhất trên lối chính | 0.1 | m | Xem tab 03, bảng phạm vi cắt lớn nhất |
| path.min_width | Lối đi | Độ rộng tối thiểu lối chính | 0.85 | m | Đường kính thân + 0,25 m |
| cut.slow_hint | Phản hồi cắt | Ngưỡng hiện tín hiệu "đi chậm lại" | 0.8 | × tốc độ cắt xong ở tâm | Tốc độ cắt xong ở tâm = 2R × sức cắt ÷ độ bền |
| cut.auto_slow | Phản hồi cắt | Tự giảm tốc khi cắt cây bền | off | on / off | Thử trong playtest nội bộ |
| timer.untimed_levels | Timer | Màn không có timer | 1–3 | màn | Màn hướng dẫn, không thể thua |
| timer.warning | Timer | Cảnh báo sắp hết giờ | 15 | s |  |
| timer.min | Timer | Timer tối thiểu | 60 | s |  |
| star2.time_left | Sao | Sao 2: thời gian còn lại tối thiểu | 0.2 | % timer | Và không có lỗi bảo vệ |
| design.quota_surplus | Thiết kế màn | Số cây trên sân tối thiểu so với quota | 1.1 | × |  |
| design.xp_surplus | Thiết kế màn | XP từ cây cấp thấp hơn so với mốc cấp cần đạt | 1.2 | × | Không bắt người chơi cắt sạch mới lên cấp |
| design.zone_switch | Thiết kế màn | Thời gian mỗi lần chuyển vùng (ước tính) | 8 | s | Dùng để ước tính timer |
| design.xp_rate | Thiết kế màn | Tốc độ kiếm XP khi bổ sung (ước tính) | 8 | XP/s | Dùng để ước tính timer |
| onboarding.idle_hint | Onboarding | Đứng yên bao lâu thì gợi ý | 8 | s |  |
| cleanup.finish_area | Cleanup | Hiện nút "dọn nốt" khi diện tích còn lại dưới | 0.01 | % diện tích hợp lệ | Và mỗi cụm còn tối đa 4 đơn vị |
| xp.tier4 | Cây cấp 4 | XP từ cây cấp 4 | 0 | XP | Đã chốt hướng (b): không cho XP |

## 08 Remote config

Remote config cho bản test D1

Đổi luật mà không cần build lại. Mỗi lần A/B chỉ đổi một key.

Danh sách key

| Key | Kiểu | Mặc định | Giá trị A/B | Liên quan | Ảnh hưởng |
|---|---|---|---|---|---|
| protected_mode | string | warn | fail | 07 Luật | Hoa bảo vệ chỉ cảnh báo hay gây thua |
| protected_fail_limit | int | 3 | — | 07 Luật | Số lỗi được phép khi protected_mode = fail |
| timer_enabled | bool | true | false | GDD mục 12 | Tắt timer: sao 2 tính theo thời gian hoàn thành, ẩn Extra Time |
| timer_multiplier | float | 1 | 1.2 | Danh sách màn | Nhân toàn bộ timer |
| auto_slow_cut | bool | false | true | 07 Luật | Tự giảm tốc khi lưỡi đang cắt cây bền |
| slow_hint_threshold | float | 0.8 | — | 07 Luật | Ngưỡng hiện tín hiệu đi chậm |
| booster_gift_turbo | int | 3 | — | 06 Booster | Số Turbo tặng khi mở |
| booster_gift_extratime | int | 3 | — | 06 Booster | Số Extra Time tặng khi mở |

## 99 Lịch sử

Lịch sử thay đổi

Ghi mỗi lần đổi số có ảnh hưởng tới gameplay. Mới nhất ở trên.

| Phiên bản | Ngày | Người sửa | Thay đổi |
|---|---|---|---|
| 1.3 | 06/10/2026 |  | Cập nhật tham chiếu: danh sách màn 50 màn, GDD v2.2. Số cân bằng không đổi. |
| 1.2 | 06/10/2026 |  | Sửa mô hình dải cắt: tính cả kích thước vùng cắt của cây (đúng GDD mục 5). Thêm trạng thái cấp 4 và mọi tổ hợp nâng cấp, thêm cột cây cấp 4. Rau: vùng cắt 0,6 → 0,8 m (trước đó khó hơn cả bụi lớn cấp 3). Quả khổng lồ: độ bền 1,5 → 2,5 để luôn phải dừng cắt. Bỏ khóa cột gây cắt chữ trong Google Sheets. Sửa đơn vị % ở tab 02 và 07. |
| 1.1 | 06/10/2026 |  | Dựng lại bố cục: tab đánh số, key cho dev, quy ước màu, khóa tiêu đề. Số liệu giữ nguyên bản 1.0. |
| 1.0 | 06/10/2026 |  | Bản đầu: giá trị khởi tạo cho bản test D1, chốt hoa bảo vệ chỉ cảnh báo, cây cấp 4 không cho XP. |
