# Grass Route Audio

Âm thanh cho phong cách thư giãn: âm ấm, không chói, attack mềm, có reverb nhẹ. Mọi file đều dùng được thương mại
mà không cần ghi công. File tự tổng hợp thuộc sở hữu studio; file tải về theo giấy phép CC0 (xem Credits).

Mã nguồn tổng hợp: [Tools/audio-synth/synth_grass_route.py](../../../Tools/audio-synth/synth_grass_route.py).
Chỉnh tham số và chạy lại để tạo biến thể. Sau khi chọn xong, xóa `_Alternatives/`.

## Ánh xạ sự kiện → file

| Sự kiện (message) | File chính | Ghi chú phát |
| --- | --- | --- |
| Cắt liên tục (`HarvestBatchedMsg`) | `Sfx/Gameplay/sfx_cut_loop_light/medium/dense` | Ba loop chạy cùng lúc, crossfade volume theo số ô cắt trong cửa sổ 0,1 s; pitch 0.95–1.05 theo mật độ cắt |
| Máy chạy | `Sfx/Gameplay/sfx_mower_hum_loop` | Volume thấp (~0.25), pitch 0.9–1.15 theo vận tốc |
| Cắt cụm/bụi lớn | `sfx_grass_snip_00..03`, `sfx_bush_trim_00..02` | Chọn ngẫu nhiên, pitch 0.92–1.08, cooldown 80 ms |
| Cắt trái cây (`FruitSliceEffects`) | `sfx_fruit_pop_00..02` | Ngẫu nhiên |
| Đạt quota (`QuotaCompletedMsg`) | `sfx_quota_complete` | Một lần mỗi quota |
| Lên cấp (`TierUpMsg`) | `sfx_tier_up` | Duck music −6 dB trong 1,5 s |
| Chọn nâng cấp (`UpgradeChosenMsg`) | `sfx_upgrade_chosen` | |
| Chạm vùng bảo vệ (`ProtectedHitMsg`) | `sfx_protected_hit` | Cooldown 300 ms, không lặp dồn |
| Bắt đầu màn (`LevelStartedMsg`) | `sfx_level_start` | |
| Timer 15 s cuối | `sfx_timer_tick`, `sfx_timer_tick_accent` (5 s cuối) | |
| Coin khi quyết toán | `sfx_coin_00..02` | Tăng dần theo thứ tự |
| Thắng / Thua (`LevelFinishedMsg`) | `Jingles/jingle_level_win`, `jingle_level_lose` | Fade music về 0 trong 0,4 s trước jingle |
| Sao kết quả | `Sfx/UI/ui_star_1..3` | Mỗi sao một file, cao dần |
| Nút bấm | `Sfx/UI/ui_tap_00..02` | |
| Mở/đóng popup | `ui_popup_open`, `ui_popup_close` | |
| Toggle (âm thanh, rung) | `ui_toggle_on`, `ui_toggle_off` | |
| Nhạc nền | `Music/music_morning_lawn_loop` (48 s, loop liền mạch) | Gameplay. Hai bài CC0 còn lại cho Home/Zen |
| Ambience | `Ambience/amb_park_birds_loop` + `amb_garden_breeze_loop` | Chơi song song, volume ~0.35 và ~0.2 |

## Mức âm lượng gợi ý (AudioMixer)

Music −14 dB, Ambience −20 dB, SFX cắt −16 dB, SFX sự kiện −8 dB, UI −12 dB. Tách group Music và SFX để làm
hai thanh chỉnh riêng như GDD yêu cầu.

## Import settings gợi ý

| Nhóm | Load Type | Compression | Khác |
| --- | --- | --- | --- |
| Music, Ambience | Streaming | Vorbis 70 | Load In Background |
| Loop cắt, hum | Decompress On Load | Vorbis 80 | |
| SFX ngắn, UI | Decompress On Load | ADPCM | Force To Mono (trừ jingle, tier up, quota) |

## Credits (CC0, không bắt buộc ghi công)

- Tự tổng hợp (sở hữu studio): mọi file `sfx_*`, `ui_*`, `jingle_*`, `music_morning_lawn_loop`, `amb_garden_breeze_loop`.
- Kenney (kenney.nl), CC0: `_Alternatives/**/kenney_*` từ Interface Sounds, Impact Sounds, RPG Audio, Music Jingles.
- thimras, "Park ambiences" (opengameart.org/content/park-ambiences), CC0: `amb_park_birds_loop`, `amb_park_wind_loop`,
  `_Alternatives/Sfx/amb_park_river_loop` (cắt 60 s, crossfade thành loop).
- isaiah658, "Ambient Bird Sounds", CC0: `amb_birds_soft`. "Ambient Relaxing Loop", CC0: `_Alternatives/Music/music_ambient_relaxing_loop`.
- Zane Little Music, "Apple Cider", CC0: `music_apple_cider`.
- emmntt, "I think I'd stay (jungle chill)", CC0: `music_i_think_id_stay`.
- cynicmusic, "Another August" và "Calm Piano 1 (Vaporware)", CC0: `_Alternatives/Music/`.
- spring-spring, "Birds and Wind – Ambient", CC0: `_Alternatives/Music/music_birds_wind_synth`.
- pwl, "Bell dings/chimes", CC0: `_Alternatives/Sfx/sfx_bell_ding_a/b`.
