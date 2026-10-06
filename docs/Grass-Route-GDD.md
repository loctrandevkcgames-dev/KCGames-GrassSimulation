# Grass Route

GDD v2.2 • Mobile màn hình dọc • 06/10/2026

# 1 Mục tiêu và phạm vi
Grass Route là game cắt cỏ. Người chơi lái máy để thu hoạch đúng loại cây, chọn đường đi để lấy XP và nâng cấp máy ngay trong lượt. Game cần mang lại ba cảm giác: cắt đã tay, điều khiển dễ hiểu, và mỗi lựa chọn đường đi đều có lợi ích rõ ràng.

Tài liệu này **chỉ mô tả gameplay**: luật chơi, hành vi của người chơi và kết quả mong đợi. Tài liệu không ghi số liệu cân bằng. Những chỗ cần số được đánh dấu **\[Cân bằng\]**; giá trị khởi tạo nằm ở file Cân bằng.

## Tài liệu liên quan
| File | Nội dung |
| :---- | :---- |
| [Grass Route — Cân bằng D1](Grass-Route-Balance-D1.md) | Giá trị khởi tạo cho mọi chỗ \[Cân bằng\], bảng vệt cắt, remote config |
| [Grass Route — Danh sách màn D1](Grass-Route-Levels-D1.md) | 50 màn, quota, timer và kiểm tra màn hợp lệ |
| [Grass Route — Tracking events D1](Grass-Route-Tracking-D1.md) | Sự kiện, tham số và KPI cho test D1 |
| Grass Route — Kế hoạch test D1 (Google Doc) | Mục tiêu, biến thể, cỡ mẫu, ngưỡng quyết định |

## Ngoài phạm vi tài liệu này
| Nội dung | Thuộc về |
| :---- | :---- |
| Tốc độ, bán kính, độ bền cây, mốc XP, timer, giới hạn lỗi | Tài liệu cân bằng |
| Coin, giá máy/booster, phần thưởng, quảng cáo, IAP, skin | Tài liệu economy và monetization |
| Art style, tỷ lệ hình ảnh, reveal sân vườn | Art direction (chốt cùng UA) |
| Giải pháp kỹ thuật | Tài liệu technical của team dev |

Baseline không có combat, đối thủ, nhiên liệu, kho chứa cỏ, xây dựng, story quest hay multiplayer. Nền tảng là mobile Android/iOS, màn hình dọc, một người chơi.

## Trạng thái quyết định
| Hạng mục | Quyết định cho bản test D1 | Còn cần kiểm chứng |
| :---- | :---- | :---- |
| Core gameplay | Lái, tự cắt, thu hoạch quota, XP, nâng cấp | Cảm giác cắt, đường đi có ý nghĩa không |
| Chế độ chính | Contract có timer; màn 1–3 không timer | Timer có nên là điều kiện thua (A/B sau) |
| Máy | Standard và Wide; Heavy để sau | Wide có đổi hành vi chơi rõ không |
| Booster | Turbo và Extra Time; Power Blade để sau | Booster có cứu được lượt thua không |
| Chế độ phụ | Cleanup sau khi thắng; Zen và sân thử để sau | Người chơi có nhu cầu dọn tự do không |
| Vùng bảo vệ | Chỉ cảnh báo, lỗi làm mất sao 2; chế độ thua để trong remote config | Có tạo quyết định hay chỉ gây mất vui |
| XP cây cấp 4 | Không cho XP (hướng b) | — |

# 2 Trải nghiệm và vòng lặp
## Bốn yêu cầu trải nghiệm
| Yêu cầu | Biểu hiện trong lượt |
| :---- | :---- |
| Cắt đã tay | Máy đi qua để lại vệt sạch rõ ràng; cây chưa cắt xong cho thấy tiến độ |
| Điều khiển có kiểm soát | Rẽ, dừng và đi sát mép luống được mà máy không bị trôi khó đoán |
| Đường đi có ý nghĩa | Chọn giữa đi thẳng lấy quota hoặc đi vòng lấy XP để mở vùng khó |
| Tăng trưởng có ích | Lên cấp mở loại cây mới; mỗi lựa chọn nâng cấp thay đổi cách dọn sân |

## Vòng lặp trong lượt
Xem map và quota → chọn máy và booster → lái tới vùng cây cắt được → cắt và nhận XP → lên cấp, chọn nâng cấp → quay lại vùng khó hoặc đi đường tối ưu tới mục tiêu → hoàn thành hoặc thất bại → xem kết quả, thử chiến thuật khác.

## Vòng lặp giữa các lượt
Thắng thì mở màn kế tiếp → mở máy có đặc tính khác → thử máy trên bố cục mới → quay lại màn cũ để lấy sao còn thiếu. Màn chính mở khi thắng màn trước, không cần đủ sao.

## Một quyết định điển hình
Quota yêu cầu hoa và bụi cấp 2\. Đi nhánh hoa thì có tiến độ quota ngay; đi nhánh cỏ thì lên cấp 2 sớm. Người chơi có thể lấy XP trước rồi cắt bụi và hoa, hoặc lấy hoa trước rồi nhặt thêm XP trên đường. Khoảng cách, mật độ cây và lựa chọn nâng cấp phải làm thứ tự này thực sự khác nhau.

# 3 Luồng lượt chơi và kết quả
| Trạng thái | Người chơi làm gì | Thời gian |
| :---- | :---- | :---- |
| Preview | Xem toàn map, quota, timer và mục tiêu phụ | Chưa chạy |
| Loadout | Chọn máy đã sở hữu, chọn booster mang theo | Chưa chạy |
| Playing | Lái, tự cắt, kích hoạt booster | Timer chạy |
| Chọn nâng cấp | Chọn Lưỡi rộng hoặc Động cơ khỏe | Timer và booster dừng |
| Pause | Tiếp tục, chơi lại, đổi cài đặt hoặc thoát | Timer và booster dừng |
| Thắng | Xem sao; chọn màn tiếp hoặc dọn tiếp (Cleanup) | Dừng |
| Thua | Xem nguyên nhân và quota còn thiếu; chơi lại hoặc đổi máy | Dừng |
| Cleanup | Dọn tiếp chính sân vừa thắng | Không timer |

**Thắng** khi hoàn thành tất cả quota chính. **Thua** khi hết giờ mà còn thiếu quota, hoặc khi vượt giới hạn lỗi bảo vệ nếu chế độ hoa bảo vệ gây thua đang bật. Nếu quota hoàn thành đúng lúc hết giờ thì tính thắng. Mục tiêu phụ không ảnh hưởng tới thắng thua.

Thứ tự xét kết quả: (1) vượt giới hạn lỗi bảo vệ thì thua; (2) nếu chưa vượt giới hạn lỗi và quota đã đủ thì thắng; (3) nếu quota chưa đủ và hết giờ thì thua. Sau khi kết quả hiện ra, không cộng thêm XP hay cây.

Khi người chơi rời ứng dụng rồi quay lại, game mở Pause. Nếu ứng dụng bị đóng giữa lượt, người chơi quay về màn chuẩn bị; baseline không có tiếp tục lượt dở. Booster đã dùng thì không hoàn lại.

**Chơi lại (Retry)** giữ nguyên map và máy đã chọn; reset sân, XP, cấp và timer. Booster chưa dùng vẫn còn trong kho; loại đã hết thì không tự trang bị lại. Có lựa chọn quay về Loadout để đổi chiến thuật.

# 4 Điều khiển và camera
Chạm vào vùng trống ở nửa dưới màn hình để tạo joystick nổi. Kéo để chọn hướng và tốc độ: kéo ít thì đi chậm, kéo hết thì đi tốc độ tối đa. Thả tay thì máy giảm tốc rồi dừng. Máy tự cắt, không cần nút cắt.

Nút pause và nút booster nhận thao tác riêng; chạm vào nút không tạo joystick. Joystick chỉ hiện khi chạm, mờ dần rồi ẩn khi thả. Sau khi đóng Pause hoặc màn chọn nâng cấp, input trở về trung tính để máy không lao đi ngoài ý muốn.

Hành vi điều khiển mong muốn (giá trị cụ thể là **\[Cân bằng\]**):

* Máy dừng nhanh hơn khởi động.

* Máy xoay hình theo hướng đi đủ nhanh để người chơi đọc được hướng.

* Có vùng chết ở tâm joystick để chạm nhẹ không làm máy trôi.

* Cả ba máy dùng chung một kích thước thân, nên đi được những lối như nhau.

**Va chạm:** thân máy chạm đá, hàng rào hoặc biên map thì trượt dọc theo mép, không bật lùi. **Lưỡi cắt không xuyên qua đá và hàng rào**: phần lưỡi nằm sau vật cản không cắt và không chạm cây bên kia. Cây chưa đủ cấp không chặn đường; máy đi qua được nhưng không cắt. Khi chạm vào, cây đó hiện ngắn biểu tượng cấp khóa.

**Camera** nhìn nghiêng từ trên xuống. Máy nằm gần tâm hoặc hơi thấp hơn tâm màn hình; luôn thấy vùng phía trước và toàn bộ phạm vi cắt lớn nhất. Camera không zoom gần hơn khi nâng cấp và không quay hướng đột ngột gây chóng mặt. Preview nhìn toàn sân. Mini-map chưa thuộc phạm vi ban đầu.

# 5 Cây và luật cắt
## Đơn vị thu hoạch
Một đơn vị thu hoạch là một mục tiêu cắt được: cụm cỏ nhỏ, bông hoa, quả, cây rau hoặc một cây lớn. Cỏ và hoa tính theo cụm nhỏ; quả và cây lớn tính theo từng vật thể. Một dưa hấu tính là một dưa hấu; một cây táo tính là một cây, hoặc số quả được ghi rõ trong quota. Hình ảnh to hơn không có nghĩa là được tính nhiều đơn vị hơn.

## Cách cắt
Cây nhận tiến độ cắt khi thỏa cả ba điều kiện: lưỡi chạm vùng cắt của cây, cây nằm trong khu được phép cắt, và cấp yêu cầu của cây không cao hơn cấp cắt hiện tại. Tiến độ tăng theo thời gian lưỡi tiếp xúc nhân sức cắt, chia độ bền của cây. Cây chỉ cộng quota và XP **một lần khi cắt xong**. Cây cắt dở giữ tiến độ tới hết lượt.

Hệ quả người chơi phải hiểu được: cây càng bền thì càng cần đi chậm, đi qua nhiều lần, hoặc có sức cắt cao. Khi đi nhanh, vệt cắt xong thực tế sẽ hẹp hơn phạm vi lưỡi, vì phần mép lưỡi chạm cây ít thời gian hơn phần giữa. Game phải cho người chơi thấy điều này:

* Cây cắt dở phải khác rõ với cây cắt xong (đổi hình, rung, vết cắt, tán lá giảm).

* Khi lưỡi đang chạm cây bền mà máy đi quá nhanh để cắt xong, có tín hiệu báo cần đi chậm lại.

* **Cần thử nghiệm:** máy tự giảm tốc khi đang cắt cây bền, so với để người chơi tự chỉnh.

Cỏ thường phải cắt xong liên tục ở tốc độ tối đa. Cỏ dày và cây cấp cao hơn thì không bắt buộc.

## Bảng cây
| Loại cây | Cấp yêu cầu | Độ bền | XP | Vai trò |
| :---- | :---- | :---- | :---- | :---- |
| Cỏ thường | 1 | Rất thấp | Thấp | Nguồn XP đầu lượt |
| Hoa / nấm nhỏ | 1 | Thấp | Thấp | Thu hoạch đúng loại |
| Cỏ dày | 2 | Trung bình | Trung bình | Nguồn XP sau cấp 2 |
| Bụi thấp | 2 | Trung bình | Trung bình | Mục tiêu cần chuẩn bị bằng XP |
| Cà rốt / bắp cải / dâu lớn | 2 | Trung bình | Trung bình | Mỗi vật thể là một mục tiêu |
| Bụi lớn / bụi cứng | 3 | Cao | Cao | Cần sức cắt |
| Dưa hấu / bí ngô | 3 | Cao | Cao | Đích nổi bật trong sân |
| Cây ăn quả lớn | 4 | Rất cao | Không | Rất lớn; cắt tại gốc |
| Quả khổng lồ | 4 | Rất cao | Không | Phần thưởng thị giác |
| Hoa bảo vệ | Không cắt được | — | Không | Luống phải tránh |

Giá trị cụ thể của độ bền và XP là **\[Cân bằng\]**. Bảng chỉ quy định thứ tự tương đối.

**XP cây cấp 4 (đã chốt hướng b):** cây cấp 4 không cho XP, chỉ cho quota và hiệu ứng hoàn thành. Lý do: cấp 4 là cấp cao nhất nên XP này vô dụng, và cách này không phụ thuộc vào Power Blade.

## Kích thước và khả năng đọc
| Tầng | Kích thước so với máy |
| :---- | :---- |
| 1 | Cụm thấp |
| 2 | Khối vừa, thấy rõ cạnh máy |
| 3 | Lớn hơn thân máy |
| 4 | Rất lớn, nhìn thấy từ xa như đích đến |

Các tầng phải khác nhau bằng silhouette và tỷ lệ, không chỉ bằng màu. Cây cao không được che máy, HUD quota hay đường tiếp cận; vùng gốc phải luôn nhìn rõ.

## Thu hoạch quả và cây ăn quả
* **Quả nằm trên sân:** máy cắt trực tiếp, quả biến mất khi thu hoạch xong; không cần nhặt riêng.

* **Quả trên cây:** cắt xong cây thì nhận trọn số quả đã báo trước, không phải chờ hay nhặt quả rơi. Ví dụ cây táo báo ×5: cắt xong cộng 5 táo một lần.

* Quota cây và quota quả là hai kiểu nhiệm vụ khác nhau. Trong baseline, một cây chỉ đóng góp cho một loại quota.

## Cây lớn và vùng cắt
Cây lớn chỉ cắt được ở vùng gốc, không cắt được bằng cách chạm tán lá từ xa. Thân cây chưa đủ cấp không gây thua và không chặn máy, chỉ hiện tín hiệu khóa. Kích thước thân và gốc cây không được bít con đường duy nhất. Khi cắt xong, cây đổ ngắn hoặc tan thành mảnh cách điệu, để lại mặt sân sạch; không gây sát thương và không chặn đường.

## Hoa bảo vệ
Hoa bảo vệ phải khác rõ với hoa thu hoạch bằng luống, silhouette và icon, không chỉ bằng màu.

**Định nghĩa lỗi:** một **luống** hoa bảo vệ là một đơn vị tính lỗi, dù luống có bao nhiêu bông. Mỗi luống được viền rõ trên sân.

* Lưỡi chạm một luống thì tính một lỗi.

* Tiếp xúc liên tục với cùng luống chỉ tính một lần. Rời luống một khoảng thời gian rồi quay lại mới tính lỗi mới. **\[Cân bằng\]**

* Một lần đi qua chạm nhiều luống thì mỗi luống tính một lỗi. Level design phải tránh đặt các luống sát nhau tới mức phạm vi cắt lớn nhất chạm nhiều luống trên lối đi chính.

* Lưỡi không xuyên qua đá và hàng rào, nên luống có rào bao quanh được an toàn.

Hoa bảo vệ có hai chế độ: **chỉ cảnh báo** (mặc định bản test D1: lỗi làm mất sao 2\) và **gây thua** (vượt giới hạn lỗi thì thua; giới hạn là **\[Cân bằng\]**). Chế độ được chọn bằng remote config. Khi chạm luống, hiện cảnh báo ngắn tại chỗ chạm và số lỗi hiện tại. Hoa bảo vệ không cho XP hay quota. Power Blade cũng không cắt được hoa bảo vệ.

# 6 Cấp máy và hai lựa chọn nâng cấp
XP chỉ tồn tại trong lượt, reset khi chơi lại hoặc sang màn mới. Cả ba máy đều bắt đầu ở cấp 1\. Mỗi lần lên cấp, người chơi chọn một trong hai nâng cấp; chọn hướng nào thì cũng mở cùng nhóm cây.

| Cấp | Nhóm cây mở | Quyền chọn |
| :---- | :---- | :---- |
| 1 | Cỏ thường, hoa thu hoạch | Máy gốc |
| 2 | Cỏ dày, bụi thấp, rau/quả vừa | Một trong hai nâng |
| 3 | Bụi lớn, dưa hấu, bí ngô | Một trong hai nâng |
| 4 | Cây ăn quả lớn, quả khổng lồ | Một trong hai nâng |

Mốc XP của từng cấp là **\[Cân bằng\]**.

| Nâng cấp | Tác dụng | Đánh đổi |
| :---- | :---- | :---- |
| Lưỡi rộng | Tăng bán kính cắt | Dọn diện tích tốt hơn; dễ chạm luống bảo vệ hơn |
| Động cơ khỏe | Tăng sức cắt và tốc độ | Cắt cây bền nhanh hơn, vệt cắt xong rộng hơn khi đi nhanh; phạm vi lưỡi không đổi |

Người chơi có thể chọn lặp một hướng hoặc phối hợp hai hướng. Lưỡi rộng không làm thân máy to ra. Chỉ số nâng cấp cộng vào chỉ số gốc của máy, sau đó mới áp dụng booster. Không có nâng cấp vĩnh viễn trong baseline.

Nếu người chơi nhận đủ XP vượt nhiều mốc cùng lúc, game xử lý lần lượt từng cấp và từng lựa chọn; lượt chơi dừng trong suốt chuỗi này và người chơi không mất quyền chọn nào.

# 7 Ba máy và Loadout
| Máy | Bán kính cắt | Tốc độ | Sức cắt | Phù hợp |
| :---- | :---- | :---- | :---- | :---- |
| Standard | Trung bình | Trung bình | Trung bình | Cân bằng; có sẵn từ đầu |
| Wide | Rộng | Chậm hơn | Trung bình | Sân rộng; chuyển vùng chậm, dễ chạm luống bảo vệ |
| Heavy | Hẹp hơn | Trung bình | Cao | Sân nhiều cây bền; phủ diện tích hẹp hơn |

Chỉ số cụ thể là **\[Cân bằng\]**. Ba máy dùng chung kích thước thân, nên không được mô tả máy nào đi được lối mà máy khác không đi được. Wide và Heavy mở dần theo tiến trình màn chơi; cách sở hữu thuộc tài liệu economy.

Không đổi máy giữa lượt. Loadout hiển thị chỉ số dạng so sánh, đánh đổi của từng máy và kho booster.

**Sân thử:** cho dùng cả máy chưa sở hữu trên cùng một bố cục để so sánh. Sân có cỏ, bụi và luống hoa bảo vệ, có tùy chọn đặt cấp để thử cây khó. Sân thử không tính tiến trình và không tiêu booster.

# 8 Booster và lượt Assisted
| Booster | Hiệu ứng | Vai trò |
| :---- | :---- | :---- |
| Turbo | Tạm thời tăng tốc độ và sức cắt; không vượt cấp | Dọn nhanh hoặc chuyển vùng |
| Power Blade | Tạm thời tăng sức cắt và cấp cắt thêm một cấp | Cắt một nhóm cây cao hơn cấp thực |
| Extra Time | Cộng thêm thời gian ngay lập tức | Cứu phần quota còn thiếu |

Thời lượng và mức tăng là **\[Cân bằng\]**.

**Luật dùng booster:**

* Ở Loadout, mỗi loại booster mang tối đa một cái. Mang theo không bị tiêu; chỉ tiêu khi kích hoạt. Mỗi loại dùng tối đa một lần trong lượt.

* Turbo và Power Blade không chạy cùng lúc. Khi một loại đang chạy, loại kia bị khóa tạm; bấm vào lúc bị khóa không tiêu booster.

* Extra Time dùng được cả khi Turbo hoặc Power Blade đang chạy.

* Chỉ kích hoạt được trong lúc Playing, trước khi có kết quả. Không dùng để hồi sinh sau khi thua.

* Pause và màn chọn nâng cấp làm thời lượng booster dừng lại. Kết thúc lượt, chơi lại hoặc vào Cleanup thì xóa hiệu ứng đang chạy.

* Khi Power Blade hết, cấp cắt trở về cấp thực. XP đã kiếm vẫn giữ; nếu XP giúp lên cấp thực trong lúc booster chạy thì cấp đó được giữ.

* Không dùng booster trong Zen, Cleanup và sân thử.

**Assisted:** kích hoạt bất kỳ booster nào sẽ đánh dấu lượt là Assisted. Lượt Assisted vẫn mở màn tiếp nhưng chỉ nhận sao 1\. Dùng máy Wide hoặc Heavy không làm lượt bị tính Assisted. Không tăng độ khó màn chơi để buộc người chơi dùng booster.

# 9 Thiết kế màn và onboarding
Mỗi Contract có từ một tới vài quota chính, tối đa một quota phụ, và một quyết định nổi bật. Màn 1–3 là màn hướng dẫn, không có timer và không thể thua. Thời lượng màn là **\[Cân bằng\]**; chỉ chỉnh timer sau khi đường thắng và cảm giác điều khiển đã ổn, không dùng việc thiếu thời gian để che bố cục yếu.

## Thứ tự giới thiệu cơ chế
| Bước | Giới thiệu | Điều người chơi cần hiểu |
| :---- | :---- | :---- |
| 1 | Lái và cắt cỏ | Di chuyển làm sân sạch và quota tăng |
| 2 | Hoa thu hoạch | Phải tìm đúng loại mục tiêu |
| 3 | Cấp 2 | Lấy XP trước rồi quay lại cắt bụi; mở Extra Time |
| 4 | Hai đường đi | Đi vòng có ích; mở Turbo |
| 5 | Hoa bảo vệ | Lưỡi chạm luống thì bị lỗi |
| 6 | Sân rộng, luống hẹp | Lưỡi rộng dọn nhanh hơn nhưng dễ chạm luống bảo vệ; mở Wide |
| 7 | Củng cố | Thứ tự vùng, kiểm soát tốc độ, chọn nâng cấp |
| 8 | Cấp 3 và quả lớn | Chuẩn bị XP cho dưa hấu/bí ngô; mở Power Blade (sau bản D1) |
| 9 | Tổng hợp và giới thiệu cấp 4 | Cây rất lớn là đích cuối; mở Heavy (sau bản D1) |

Hoa bảo vệ được đặt trước bài học Lưỡi rộng, để rủi ro của Lưỡi rộng có thật khi người chơi học nó. Mỗi bước có thể kéo dài nhiều màn; số màn mỗi bước là **\[Cân bằng\]**.

**Tutorial theo hành động:** người chơi tự làm đúng thì bỏ qua hướng dẫn tương ứng. Màn đầu có một chỉ dẫn kéo để lái; không khóa input để hiện nhiều đoạn chữ. Nếu người chơi đứng yên một lúc thì hiện gợi ý ngắn.

## Điều kiện để một màn hợp lệ
* Mọi quota có dư số cây so với yêu cầu trên sân (mức dư là **\[Cân bằng\]**).

* Cấp tối đa mà màn cho phép đạt được phải khớp với bước onboarding. Màn chưa giới thiệu cấp 3 thì tổng XP trên sân không được đủ để lên cấp 3\.

* XP để lên cấp 2 có trong vùng cấp 1 đi tới được; XP lên cấp 3 lấy được từ vùng cấp 1 và 2\.

* XP để lên cấp 4 phải lấy đủ từ cây cấp 1–3. Không đặt nguồn XP bắt buộc trong cây cấp 4\. Luôn có đường lên cấp mà không cần booster.

* Lối chính đủ rộng cho thân máy đi qua thoải mái. Không giấu cây bắt buộc ở chỗ lưỡi không chạm tới.

* Ở lối chính, phạm vi cắt lớn nhất có thể đạt trong màn không được buộc người chơi chạm luống bảo vệ.

* Mọi màn chính có đường thắng bằng Standard không booster; phải thử với nâng Lưỡi rộng, nâng Động cơ khỏe và phối hợp.

* Có map rộng thể hiện lợi ích của Wide, và map nhiều cây bền thể hiện lợi ích của Heavy khi đã đủ cấp.

* Mỗi cơ chế mới có màn củng cố trước khi sang cơ chế tiếp theo. Không có vật cản bất ngờ trên con đường duy nhất.

## Sau onboarding: ba chương
Sau bước 9 không thêm cơ chế mới. Các màn tiếp theo ghép lại những cơ chế đã học theo ba chương, mỗi chương có một chủ đề bố cục riêng:

| Chương | Chủ đề | Điểm nhấn gameplay |
| :---- | :---- | :---- |
| A | Vườn rau | Rau xếp luống dài, dưa và bí; sân rộng hợp với Wide |
| B | Vườn hoa | Nhiều luống bảo vệ, lối hẹp; cân nhắc Lưỡi rộng và chọn máy |
| C | Vườn cây ăn quả | Cây táo và quả khổng lồ ở xa; đường dài, cần tích XP lên cấp 4 |

Không bắt buộc màn nào cũng phải lên được cấp 4; màn ngắn cấp 1–3 vẫn được dùng để đổi nhịp.

## Loại màn
| Loại | Hành vi |
| :---- | :---- |
| Hướng dẫn | Màn 1–3. Không timer, không thể thua |
| Thường | Luật Contract đầy đủ |
| Khó | Timer chặt hơn (hệ số là **\[Cân bằng\]**), thường có ba quota. Đặt ở cuối mỗi nhịp năm màn |
| Thư giãn | Không timer, không thể thua, bố cục ngắn. Xen giữa các màn khó để người chơi nghỉ |

Màn Khó và Thư giãn được báo ở Preview. Màn Thư giãn không có timer nên sao 2 chỉ xét điều kiện không có lỗi bảo vệ.

## Mẫu bố cục: màn "Hai đường đi"
Sân hình chữ nhật, xuất phát ở góc tây nam.

* **Vùng A (tây):** cỏ thường, là nguồn XP lên cấp 2\.

* **Vùng B (đông):** hoa đỏ, quota chính.

* **Vùng C (bắc):** bụi thấp, quota chính, cần cấp 2\.

* **Đường nối phía bắc:** cỏ dày, quota phụ.

Hai tuyến hợp lệ: **A → C → B** (lên cấp 2 trước khi tới bụi) và **B → A → C** (lấy hoa trước, rồi lấy XP cho đủ cấp). Cả hai tuyến đều phải có đường đi được và đủ thời gian. Tổng XP trên sân không đủ để lên cấp 3\. Số lượng cây và timer là **\[Cân bằng\]**; designer đo bằng chơi thực tế trước khi chốt.

# 10 Sao và chế độ phụ
| Sao | Điều kiện | Lượt Assisted |
| :---- | :---- | :---- |
| Sao 1 | Hoàn thành quota chính | Được nhận |
| Sao 2 | Thắng, không có lỗi bảo vệ, và còn dư thời gian theo ngưỡng màn | Không nhận |
| Sao 3 | Thắng và hoàn thành quota phụ đã báo ở Preview | Không nhận |

Ngưỡng thời gian dư là **\[Cân bằng\]**. Màn không có hoa bảo vệ thì mặc định đạt điều kiện không lỗi. Màn không có quota phụ thì sao 3 dùng một mục tiêu phụ riêng, ghi rõ ở Preview; không dùng tiêu chí ẩn. Mỗi sao được tính độc lập, không cần có sao 2 mới lấy được sao 3\. Mở màn tiếp chỉ cần sao 1\.

## Cleanup
Cleanup mở sau khi thắng, cho dọn tiếp chính sân vừa thắng. Chế độ này giữ nguyên sân và các nâng cấp đã chọn, bỏ timer, không tính tiến trình hay sao. Cấp cắt được nâng lên **cấp cao nhất mà người chơi đã được giới thiệu trong onboarding**, không tự động lên cấp 4, để không cắt trước những cây mà người chơi chưa "giành được". Cây cao hơn mức này vẫn hiện tín hiệu khóa. Khi vào chế độ, có giải thích ngắn. Không có lựa chọn nâng cấp thêm.

## Zen
Zen mở sau khi người chơi đã học xong cấp 2\. Zen dùng các sân tự do đã mở trong tiến trình, với máy đã chọn và cấp cắt tối đa theo cùng luật như Cleanup. Không có XP, quota, timer hay thất bại.

## Dọn nốt
Mục tiêu dọn là diện tích cây cắt được, không tính luống bảo vệ hay nền đá. Khi phần còn lại đủ nhỏ và rải rác (ngưỡng là **\[Cân bằng\]**), hiện nút "dọn nốt" tùy chọn. Nút này không ảnh hưởng tới sao Contract.

# 11 HUD và phản hồi gameplay
**Tài liệu tham khảo UI/UX:** [https\://claude.ai/artifact/NXex2acfyUmZ4RPE7Li77a](https://claude.ai/artifact/NXex2acfyUmZ4RPE7Li77a)

Sân chơi chiếm toàn màn hình, không chia sân thành khung nhỏ giữa các panel. HUD nhỏ, nằm sát mép; không hiện tiêu đề level hay bảng chỉ số thường trực. Mô tả quota phụ và luật chi tiết nằm ở Preview và Pause.

| Thành phần | Hiển thị |
| :---- | :---- |
| Quota chính | Icon loại cây và số còn thiếu ở góc trên trái; xong thì đổi thành dấu tick |
| Timer | Nhỏ, ở giữa phía trên; cảnh báo nhẹ khi sắp hết giờ |
| XP và cấp | Thanh mảnh kèm nhãn cấp, không có panel riêng |
| Pause | Góc trên phải, ngoài vùng lái |
| Booster | Ba icon ở dưới phải; phân biệt rõ trạng thái sẵn sàng, đang chạy, khóa tạm, đã dùng |
| Joystick | Chỉ hiện khi chạm vùng lái; ẩn khi thả |
| Lỗi bảo vệ | Cảnh báo ngắn tại luống và số lỗi hiện tại |

HUD che càng ít sân càng tốt; phải kiểm tra trên màn hình nhỏ. Icon nhỏ nhưng vùng chạm đủ rộng; chữ và mục tiêu đọc được trên mọi nền. Có tùy chọn chuyển cụm booster sang trái. Zen và Cleanup không hiện những thông tin không dùng tới như timer hay booster.

## Phản hồi
* Cỏ cắt xong đổi ngay sang nền sạch. Mảnh cỏ bay thấp, không che đường.

* Cây cắt dở hiện tiến độ rõ ràng; có tín hiệu khi máy đi quá nhanh để cắt xong cây bền (xem mục 5).

* Âm cắt thay đổi nhẹ theo mật độ cây; không có âm gắt cho từng cây.

* Đạt một quota: âm xác nhận và icon bật nhẹ một lần.

* Nâng cấp: cho thấy phạm vi hoặc khả năng đã thay đổi.

* Turbo và Power Blade có hiệu ứng khác nhau. Extra Time hiện "+thời gian" ngay tại timer rồi biến mất, không có popup.

* Cây lớn cắt xong có hiệu ứng hoàn thành rõ hơn, nhưng không che đường hay khóa điều khiển.

Không dùng màu làm tín hiệu duy nhất cho cây khóa, hoa bảo vệ hay booster. Có cài đặt tắt rung, giảm hiệu ứng và chỉnh nhạc/SFX riêng. Các yêu cầu về khả năng đọc ở trên áp dụng cho mọi art style.

# 12 Prototype và các câu hỏi cần chốt
## Phạm vi bản test D1
Bản test D1 chỉ build những gì cần để đo core gameplay và giữ chân ngày đầu:

| Có trong bản D1 | Để sau |
| :---- | :---- |
| Máy Standard và Wide (Wide mở miễn phí sau màn 10\) | Máy Heavy |
| Hai hướng nâng cấp, cấp 1–4 | — |
| Turbo và Extra Time (tặng khi mở, không có shop) | Power Blade, shop, coin |
| Sao, Cleanup | Zen, sân thử |
| 50 màn: 24 màn onboarding \+ 26 màn ba chương | Meta dài hạn, sự kiện, màn sau 50 |
| Remote config cho hoa bảo vệ, timer, tự giảm tốc | Quảng cáo, IAP |

Danh sách màn ở file Danh sách màn D1; kế hoạch đo và ngưỡng quyết định ở file Kế hoạch test D1.

## Phạm vi prototype
Prototype thử các hệ thống trong tài liệu này với 5–10 màn và một sân tự do. Có ít nhất một màn rút gọn đi từ cấp 1 lên cấp 4 để kiểm chứng cảm giác chinh phục cây rất lớn. Chỉ dùng số loại cây tối thiểu: cỏ, hoa, bụi, rau/quả vừa, quả lớn và cây ăn quả lớn.

| Giả thuyết | Quan sát | Nếu yếu thì sửa |
| :---- | :---- | :---- |
| Điều khiển dễ hiểu | Người mới tự lái và cắt được ngay đầu màn 1 | Input và tín hiệu |
| Cấp cây rõ ràng | Người chơi hiểu cây bị khóa và hiểu cấp 4 mở cây rất lớn | Onboarding và bố trí XP |
| Cắt cây bền đọc được | Người chơi tự đi chậm hoặc quay lại khi cây chưa cắt xong | Phản hồi cắt dở, thử tự giảm tốc |
| Chơi tiếp tự nguyện | Người chơi muốn chơi thêm sau vài màn | Cảm giác cắt và nhịp mục tiêu |
| Máy và nâng cấp có ý nghĩa | Người chơi giải thích được vì sao đổi máy, đổi tuyến | Bố cục, trước khi thêm hệ thống |
| Thất bại công bằng | Người thua nói được nguyên nhân và muốn thử lại | Camera, timer hoặc luật bảo vệ |

Ngưỡng đạt cho mỗi giả thuyết được đặt ở kế hoạch playtest. Ghi lại: thời gian, quota còn thiếu, lỗi bảo vệ, máy, nâng cấp, booster, số lần chơi lại và lý do dừng chơi.

**Thử riêng từng biến:** cùng một map, thử có và không có timer; thử hoa bảo vệ gây thua và chỉ cảnh báo. Không thay đổi hai yếu tố cùng lúc. Nếu chọn hướng không timer làm campaign chính thì phải thiết kế lại sao và booster (Extra Time mất vai trò), không chỉ ẩn timer.

## Các câu hỏi cần chốt
Đã chốt cho bản test D1: hoa bảo vệ chỉ cảnh báo, cây cấp 4 không cho XP, tự giảm tốc tắt mặc định. Còn mở:

1. Lời hứa chính của game là cắt đã tay, chọn máy/tuyến, hay khám phá sân vườn? Nên chốt câu này trước khi mở rộng hệ thống.

2. Timer giữ làm điều kiện thắng/thua hay chuyển thành thử thách tùy chọn? Đo bằng A/B sau khi D1 đạt.

3. Hoa bảo vệ tạo ra quyết định thú vị, hay chỉ làm mất vui? So sánh hai chế độ sau khi D1 đạt.

4. Ba máy và hai hướng nâng cấp có làm hành vi chơi thay đổi đủ rõ không?

5. Máy có nên tự giảm tốc khi đang cắt cây bền không? Thử trong playtest nội bộ.

## Ghi chú thay đổi bản 2.2
* Bản test D1 tăng từ 24 lên 50 màn: thêm 26 màn sau onboarding chia ba chương (Vườn rau, Vườn hoa, Vườn cây ăn quả), không thêm cơ chế mới.

* Thêm bốn loại màn: Hướng dẫn, Thường, Khó, Thư giãn. Màn Thư giãn không timer; sao 2 chỉ xét lỗi bảo vệ.

## Ghi chú thay đổi bản 2.1
* Chốt quyết định cho bản test D1: hoa bảo vệ chỉ cảnh báo (chế độ thua để trong remote config), cây cấp 4 không cho XP, màn 1–3 không timer.

* Thêm phạm vi bản test D1: bỏ Heavy, Power Blade, Zen, sân thử, shop và coin khỏi bản build đầu.

* Thêm danh sách tài liệu liên quan: Cân bằng, Danh sách màn, Tracking events, Kế hoạch test D1.

## Ghi chú thay đổi bản 2.0
* Thu gọn phạm vi: chỉ giữ gameplay. Bỏ coin, giá, phần thưởng, quảng cáo, IAP và skin (chuyển sang tài liệu economy). Bỏ toàn bộ số liệu cân bằng, thay bằng mô tả tương đối và đánh dấu **\[Cân bằng\]**.

* Hoa bảo vệ: định nghĩa một luống là một đơn vị tính lỗi; lưỡi không xuyên qua đá và hàng rào; thêm điều kiện màn để lối chính không buộc chạm luống.

* Luật cắt: nói rõ vệt cắt thực tế hẹp hơn phạm vi lưỡi khi đi nhanh với cây bền; thêm yêu cầu phản hồi và câu hỏi về tự giảm tốc. Giới hạn cam kết "vệt sạch liên tục" chỉ áp dụng cho cỏ thường.

* XP cây cấp 4: nêu rõ ngoại lệ Power Blade và hai hướng cần chốt.

* Bỏ cột giới hạn nâng cấp (vì không bao giờ có tác dụng). Sửa mô tả đánh đổi của Động cơ khỏe.

* Cleanup và Zen: cấp cắt chỉ lên tới cấp đã được giới thiệu, không tự động lên cấp 4\.

* Onboarding: đưa hoa bảo vệ lên trước bài học Lưỡi rộng; thêm điều kiện màn không cho lên vượt cấp đã giới thiệu. Bảng màn đổi thành thứ tự bước.

* Mẫu màn 04 chuyển thành mẫu bố cục không có số.
