# Grass Route — Tracking events D1

Mirror of the Google Sheet "Grass Route — Tracking events D1" (exported 2026-10-06). The sheet is the source of truth; update this file when it changes.

## 01 Sự kiện

Danh sách sự kiện

P0 = bắt buộc trước khi chạy test D1. P1 = nên có nếu kịp. Cột Dev và QA để theo dõi tiến độ.

Sự kiện

| ID | Ưu tiên | Nhóm | Sự kiện | Nguồn tên | Khi nào gửi | Tham số | Câu hỏi trả lời được | Dev | QA |
|---|---|---|---|---|---|---|---|---|---|
| E01 | P0 | Phiên chơi | first_open / session_start | Firebase tự động | Tự động | — | Cài đặt, số phiên, D1 | Chưa làm | Chưa kiểm |
| E02 | P0 | Onboarding | tutorial_begin | Firebase khuyến nghị | Bắt đầu màn 1 lần đầu | — | Bao nhiêu người vào hướng dẫn | Chưa làm | Chưa kiểm |
| E03 | P0 | Onboarding | tutorial_complete | Firebase khuyến nghị | Thắng màn 3 lần đầu | duration_s | Bao nhiêu người qua hết phần không timer | Chưa làm | Chưa kiểm |
| E04 | P0 | Màn chơi | level_start | Firebase khuyến nghị | Bấm bắt đầu ở Preview/Loadout | level, attempt, machine, boosters_equipped, is_replay | Phễu màn; số lần thử mỗi màn | Chưa làm | Chưa kiểm |
| E05 | P0 | Màn chơi | level_end | Firebase khuyến nghị | Hiện kết quả hoặc thoát giữa lượt | level, success, result, attempt, duration_s, time_left_s, quota_pct, side_quota_done, xp, cut_level_reached, upgrades, protected_hits, stars_earned, assisted | Thắng/thua vì sao; màn nào quá khó | Chưa làm | Chưa kiểm |
| E06 | P0 | Màn chơi | upgrade_chosen | Riêng | Chọn nâng cấp khi lên cấp | level, new_cut_level, choice, time_in_level_s | Hai hướng nâng có cân bằng không | Chưa làm | Chưa kiểm |
| E07 | P0 | Màn chơi | booster_used | Riêng | Kích hoạt booster | level, booster, time_in_level_s, time_left_s | Booster có cứu được lượt thua không | Chưa làm | Chưa kiểm |
| E08 | P0 | Tiến trình | unlock | Riêng | Mở máy hoặc booster | item, level | Người chơi tới được các mốc mở khóa | Chưa làm | Chưa kiểm |
| E09 | P0 | Chế độ phụ | cleanup_end | Riêng | Thoát Cleanup | level, duration_s, cleared_pct | Người chơi có thích dọn tự do không | Chưa làm | Chưa kiểm |
| E10 | P1 | Màn chơi | protected_hit | Riêng | Mỗi lỗi bảo vệ | level, hit_index, blade_radius, speed | Lỗi do lưỡi rộng hay do đi nhanh | Chưa làm | Chưa kiểm |
| E11 | P1 | Phản hồi | slow_hint_shown | Riêng | Hiện tín hiệu đi chậm (tối đa 1 lần/loại cây/lượt) | level, plant_type, speed | Người chơi có hiểu cây bền không | Chưa làm | Chưa kiểm |
| E12 | P1 | Phản hồi | locked_plant_touch | Riêng | Chạm cây chưa đủ cấp lần đầu trong lượt | level, plant_type, cut_level | Người chơi có hiểu cấp cây không | Chưa làm | Chưa kiểm |
| E13 | P1 | Phản hồi | idle_hint_shown | Riêng | Đứng yên quá ngưỡng | level, time_in_level_s | Chỗ người chơi bị kẹt | Chưa làm | Chưa kiểm |
| E14 | P1 | Kỹ thuật | perf_sample | Riêng | Mỗi 30 s trong Playing | avg_fps, min_fps, device_model | Hiệu năng có làm hỏng cảm giác cắt không | Chưa làm | Chưa kiểm |

Tiến độ

| Ưu tiên | Tổng | Đã làm | Đã kiểm |
|---|---|---|---|
| P0 | 9 | 0 | 0 |
| P1 | 5 | 0 | 0 |

## 02 Tham số

Từ điển tham số

Một tham số có cùng tên, kiểu và nghĩa ở mọi sự kiện.

Tham số

| Tham số | Kiểu | Giá trị / miền | Ví dụ | Dùng trong |
|---|---|---|---|---|
| level | int | 1–50 | 6 | level_start, level_end, upgrade_chosen, booster_used, unlock, cleanup_end, protected_hit, slow_hint_shown, locked_plant_touch, idle_hint_shown |
| attempt | int | Lần thử thứ mấy của màn, bắt đầu từ 1 | 2 | level_start, level_end |
| is_replay | bool | Màn đã thắng trước đó | false | level_start |
| machine | string | standard / wide | wide | level_start |
| boosters_equipped | string | Danh sách cách nhau bởi dấu phẩy | turbo,extra_time | level_start |
| success | bool | Thắng hay không | true | level_end |
| result | string | win / time_out / protected_limit / quit | time_out | level_end |
| duration_s | int | Giây trong Playing, không tính Pause và chọn nâng cấp | 84 | tutorial_complete, level_end, cleanup_end |
| time_left_s | int | Giây còn lại khi kết thúc hoặc khi dùng booster | 21 | level_end, booster_used |
| quota_pct | float | 0–1, tiến độ trung bình các quota chính | 0.85 | level_end |
| side_quota_done | bool | Hoàn thành quota phụ | false | level_end |
| xp | int | XP cuối lượt | 214 | level_end |
| cut_level_reached | int | 1–4 | 2 | level_end |
| upgrades | string | Chuỗi lựa chọn theo thứ tự: B = Lưỡi rộng, E = Động cơ | B,E | level_end |
| protected_hits | int | Số lỗi bảo vệ trong lượt | 1 | level_end |
| stars_earned | int | Số sao mới nhận lượt này (0–3) | 2 | level_end |
| assisted | bool | Lượt có dùng booster | false | level_end |
| new_cut_level | int | 2–4 | 3 | upgrade_chosen |
| choice | string | blade / engine | engine | upgrade_chosen |
| time_in_level_s | int | Giây từ đầu lượt | 37 | upgrade_chosen, booster_used, idle_hint_shown |
| booster | string | turbo / extra_time | extra_time | booster_used |
| item | string | wide / turbo / extra_time | wide | unlock |
| cleared_pct | float | 0–1, diện tích đã dọn | 0.97 | cleanup_end |
| hit_index | int | Lỗi thứ mấy trong lượt | 2 | protected_hit |
| blade_radius | float | Bán kính cắt hiện tại (m) | 0.95 | protected_hit |
| speed | float | Tốc độ hiện tại (m/s) | 3.8 | protected_hit, slow_hint_shown |
| plant_type | string | Key cây ở file Cân bằng, tab 04 | bush_low | slow_hint_shown, locked_plant_touch |
| cut_level | int | Cấp cắt hiện tại | 1 | locked_plant_touch |
| avg_fps / min_fps | int | Khung hình trong 30 s | 58 / 41 | perf_sample |
| device_model | string | Tên thiết bị | SM-A145F | perf_sample |

## 03 User property

User property

Gắn vào mọi sự kiện để tách nhóm khi đọc kết quả.

Thuộc tính

| Thuộc tính | Kiểu | Giá trị | Ví dụ | Dùng để |
|---|---|---|---|---|
| ab_variant | string | Tên biến thể remote config | control | Tách kết quả A/B |
| build_version | string | Phiên bản build | 0.3.1 | Loại bỏ dữ liệu build lỗi |
| device_tier | string | low / mid / high | mid | Kiểm tra hiệu năng ảnh hưởng giữ chân |
| max_level_won | int | Màn cao nhất đã thắng | 12 | Phân nhóm theo tiến trình |

## 04 KPI

KPI cho test D1

Ngưỡng là đề xuất nội bộ, cần chốt cùng publisher/UA. Cột Tham khảo là số thị trường, có nguồn.

KPI

| KPI | Cách tính từ sự kiện | Đạt (đề xuất) | Làm lại (đề xuất) | Tham khảo thị trường | Nguồn |
|---|---|---|---|---|---|
| D1 retention (Android) | Người có session_start ở ngày 1 ÷ first_open | ≥ 30% | < 25% | Trung vị toàn thị trường D1 ~22%; casual/puzzle Android 28–32% (AppsFlyer Q3/2022); nên làm lại trước khi chi UA nếu D1 Android < 25% | gamegrowthadvisor.com, Mobile game KPIs benchmarks 2026 |
| Thời gian chơi ngày 0 | Tổng level_end.duration_s ngày 0 ÷ người chơi | ≥ 15 phút | < 8 phút | Trung vị thời gian chơi mỗi ngày ~12 phút (GameAnalytics) | gamegrowthadvisor.com, Mobile game KPIs benchmarks 2026 |
| Qua hướng dẫn | tutorial_complete ÷ tutorial_begin | ≥ 85% | < 70% | — |  |
| Tới màn 10 | Người có level_start level = 10 ÷ first_open | ≥ 40% | < 25% | — |  |
| Tới màn 25 (hết onboarding) | Người có level_start level = 25 ÷ first_open | ≥ 25% | < 15% | — |  |
| Tới màn 50 (hết nội dung) | Người có level_start level = 50 ÷ first_open | Theo dõi | — | Nếu nhiều người hết màn trong ngày 0, D1 có thể thấp vì thiếu nội dung |  |
| Thắng lần đầu mỗi màn | level_end success, attempt = 1 ÷ level_start attempt = 1 | 60–90% | < 50% ở bất kỳ màn nào | — |  |
| Lý do thua | Tỷ lệ result trong các level_end thua | quit < 30% | quit ≥ 50% | — |  |
| Cân bằng nâng cấp | choice = blade ÷ tổng upgrade_chosen | 35–65% | < 20% hoặc > 80% | — |  |
| Cỡ mẫu | Số lượt cài mua mỗi biến thể | ~1.000 | — | Đọc D1 quanh 30% với sai số ±3 điểm (95%) cần ~896 lượt cài | gamegrowthadvisor.com, Mobile game KPIs benchmarks 2026 |

## 99 Lịch sử

Lịch sử thay đổi

Ghi mỗi lần đổi số có ảnh hưởng tới gameplay. Mới nhất ở trên.

| Phiên bản | Ngày | Người sửa | Thay đổi |
|---|---|---|---|
| 1.3 | 06/10/2026 |  | level nhận 1–50; thêm KPI tới màn 25 và màn 50. |
| 1.2 | 06/10/2026 |  | Bỏ khóa cột để tiêu đề không bị cắt trong Google Sheets. |
| 1.1 | 06/10/2026 |  | Dựng lại bố cục: ID sự kiện, cột Dev/QA, từ điển tham số, user property, công thức KPI theo sự kiện. |
| 1.0 | 06/10/2026 |  | Bản đầu: 14 sự kiện, 8 KPI. |
