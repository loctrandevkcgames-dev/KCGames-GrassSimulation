# Thiết kế game cắt cỏ Grass Route

*Phân tích tham chiếu Grass Master và thiết kế gameplay đề xuất*

Phiên bản 1.0 • Ngày 05 tháng 10 năm 2026 • Ngôn ngữ tiếng Việt

### Định hướng đề xuất

Grass Route là game cắt cỏ góc nhìn từ trên xuống, nơi người chơi chọn đường di chuyển để thu hoạch mục tiêu và nâng máy ngay trong lượt chơi. Điểm cần kiểm chứng là cảm giác cắt có đủ hấp dẫn để chơi lại, và lựa chọn giữa cắt nhanh mục tiêu với tích lũy sức mạnh có tạo ra những đường đi khác nhau hay không.

Tài liệu dành cho Game Designer, Unity Developer, Artist và QA. Bản này mô tả luật chơi, thông số khởi tạo, cấu trúc nội dung, giao diện, phạm vi sản xuất và tiêu chí kiểm chứng. Các con số là giả thuyết thiết kế cần playtest, không phải thông số trích xuất từ game tham chiếu.

### Phạm vi và giả định

- Thiết kế nền cho mobile Android và iOS, màn hình dọc, một người chơi. Người dùng chưa chốt nền tảng cho ý tưởng này; phương án PC được đánh giá riêng ở cuối tài liệu.

- Bối cảnh sân vườn cách điệu. Thu hoạch cây trồng trong khu vực cho phép; bảo vệ hoa hoặc vật trang trí được đánh dấu riêng.

- Không cần story quest. Tiến trình đi qua các khu vườn và bài toán đường cắt.

- Đây là GDD tiền sản xuất đủ để triển khai prototype. Chưa phải cam kết doanh thu, lịch phát hành hay chứng minh nhu cầu thị trường.

### Cách đọc tài liệu

Trang 2 là phân tích có nguồn và giới hạn chứng cứ. Từ trang 3 trở đi là thiết kế mới. Ưu tiên đọc luật cắt, tăng trưởng và mẫu level trước khi làm hệ thống meta hoặc monetization.

| Phần | Nội dung |
| --- | --- |
| 2 | Tham chiếu và đánh giá thiết kế |
| 3–7 | Concept, vòng lặp, điều khiển, luật cắt và tăng trưởng |
| 8–11 | Mục tiêu, level, onboarding, progression và economy |
| 12–15 | UI, cảm giác chơi, kỹ thuật, phạm vi, kiểm chứng và PC |

## 1 Phân tích game tham chiếu

### Thông tin xác minh từ nguồn chính thức

Grass Master – Cutting Game do SayGames Ltd phát hành. Trang Google Play mô tả lượt chơi có danh sách cây cần cắt, thời gian giới hạn và máy cắt tăng kích thước cùng tốc độ khi thu hoạch. Nội dung có nhiều loại thực vật và booster. Trang cửa hàng ghi có quảng cáo và mua hàng trong ứng dụng. Nguồn S1, truy cập ngày 05 tháng 10 năm 2026.

Một số review trên cùng trang phản ánh camera gần khiến né chướng ngại khó, tiến trình giữa màn chưa hấp dẫn và mong muốn chế độ tự do. Đây là ý kiến cá nhân, chưa đại diện cho toàn bộ người chơi. Chưa chơi bản cài đặt nên chưa xác minh economy, công thức tăng trưởng, tần suất quảng cáo hoặc luật chướng ngại hiện hành.

### Đánh giá dưới góc độ thiết kế

Suy luận từ cấu trúc được mô tả: giá trị cốt lõi nằm ở biến đổi mặt sân ngay khi đi qua và sức mạnh tăng trong một lượt chơi. Danh sách mục tiêu tạo lý do chọn vùng cắt; tăng trưởng tạo lý do đi vòng để chuẩn bị. Nếu vùng nào cũng mang giá trị gần giống nhau, lựa chọn đường đi sẽ yếu và gameplay dễ trở thành di chuyển liên tục đến khi thanh tiến độ đầy.

| Yếu tố | Nhận định thiết kế | Hướng xử lý trong bản mới |
| --- | --- | --- |
| Phản hồi cắt | Biến đổi dễ đọc là động lực tức thời | Mặt sân sạch rõ, VFX ngắn, âm thanh theo mật độ |
| Tăng trưởng | Có thể tạo cảm giác vượt giới hạn | Mốc nâng cấp đổi vùng tiếp cận và cách cắt |
| Mục tiêu | Có thể khiến người chơi cân nhắc thứ tự | Mục tiêu chính cộng mục tiêu phụ tùy chọn |
| Timer | Tạo áp lực nhưng dễ xung đột thư giãn | Tách Contract có timer và Zen không timer |
| Nội dung | Chỉ đổi màu cây sẽ thiếu quyết định mới | Thay bố cục, đường hẹp, ngưỡng máy và vùng bảo vệ |

### Những điểm cần kiểm tra nếu có video hoặc bản chơi

Đo thời gian mỗi lượt; kiểm tra điều khiển, camera ở kích thước máy lớn nhất, cách xử lý cây chưa đủ cấp, cách phân bổ mục tiêu và điều kiện thua. Ghi lại sự khác biệt giữa màn đầu và màn sau thay vì suy đoán từ hình quảng cáo.

S1 • Google Play • https://play.google.com/store/apps/details?id=com.grass.cut.game&hl=en

## 2 Concept và trải nghiệm mục tiêu

### Ý tưởng trong một câu

Điều khiển máy cắt qua một khu vườn để hoàn thành đơn thu hoạch; chọn giữa đường ngắn đến mục tiêu và đường vòng lấy nguyên liệu nâng máy, rồi tận dụng nâng cấp để cắt các vùng trước đó chưa xử lý được.

### Đối tượng và nhịp chơi

Giả định đối tượng là người thích thao tác đơn giản, thấy tiến độ trực quan và tối ưu nhẹ. Một màn Contract dự kiến 90–150 giây; một phiên 5–10 phút. Các con số này hướng dẫn bố trí nội dung ban đầu, chưa phải dữ liệu hành vi thực tế.

| Trụ cột | Biểu hiện phải thấy khi chơi | Tiêu chí kiểm chứng |
| --- | --- | --- |
| Cắt có cảm giác tốt | Vệt sạch theo đúng vùng lưỡi cắt, mật độ âm thanh tương ứng | Người chơi muốn tiếp tục cắt khi mục tiêu gần xong |
| Đường đi có ý nghĩa | Đường vòng đổi lấy XP; máy rộng hơn bị hạn chế ở lối hẹp | Cùng một level có ít nhất hai cách hoàn thành khả thi |
| Tăng trưởng dễ đọc | Cây chưa cắt được có biểu tượng cấp, mốc nâng hiện trước | Người mới giải thích được tại sao cần nâng máy |
| Thử lại nhanh | Thông báo nguyên nhân thất bại và nút chơi lại rõ | Vào lại màn trong khoảng 3 giây sau khi bấm |

### Điểm khác biệt được đề xuất

Khi lên cấp, người chơi chọn lưỡi rộng hoặc động cơ khỏe. Lưỡi rộng tăng khả năng dọn diện tích, nhưng cần tránh đường hẹp và vùng bảo vệ. Động cơ khỏe tăng tốc độ cắt cây cứng, phù hợp đường mục tiêu có mật độ cao. Máy không phình toàn bộ thân để tránh va chạm khó đoán; nâng cấp thay đổi vùng lưỡi cắt và hiệu suất.

Chế độ Zen mở sau onboarding cho phép dọn sạch một khu vườn với cùng hệ thống cắt, không timer và không thưởng tiền lặp vô hạn. Nó đáp ứng nhu cầu thư giãn mà không buộc mọi level Contract phải mất áp lực.

### Giới hạn phạm vi

Prototype không có đối thủ, combat, multiplayer, nhiên liệu, kho chứa cỏ, bán hàng theo chuyến hoặc xây dựng nông trại. Các hệ thống đó làm lệch trọng tâm kiểm chứng đường cắt. Tên Grass Route là tên làm việc, chưa kiểm tra thương hiệu.

## 3 Vòng lặp chơi và trạng thái màn

### Vòng lặp trong một màn

Xem mục tiêu và bố cục → chọn đường xuất phát → cắt cây đủ cấp → nhận XP và tăng tiến độ → chọn nâng cấp ở mốc XP → mở khả năng xử lý vùng mới → hoàn thành mục tiêu hoặc hết giờ → nhận kết quả và thử đường tốt hơn.

### Luồng bắt đầu và kết thúc

| Trạng thái | Luật | Điều kiện chuyển |
| --- | --- | --- |
| Preview | Hiển thị toàn map, mục tiêu, timer; chưa có input di chuyển | Bấm bắt đầu |
| Playing | Timer chạy, cắt tự động khi lưỡi chạm cây | Đủ XP hoặc điều kiện kết thúc |
| UpgradeChoice | Dừng timer và simulation; hai lựa chọn kèm tác dụng | Chọn một nâng cấp |
| Success | Khóa thưởng một lần; timer dừng, cho thấy mục tiêu hoàn thành | Bấm nhận thưởng hoặc dọn tiếp |
| Failure | Dừng simulation; ghi mục tiêu còn thiếu và nguyên nhân | Chơi lại hoặc về chọn màn |
| Cleanup | Sau thắng, dọn tự do; không phát sinh tiền hoặc XP meta | Bấm kết thúc |

### Quy tắc phân xử

Một tick simulation xử lý di chuyển, cắt, cập nhật mục tiêu, rồi kiểm tra thắng trước kiểm tra hết giờ. Nếu cùng tick hoàn thành mục tiêu và timer về 0, kết quả là thắng. Khi mở nâng cấp, toàn bộ thời gian gameplay dừng; quay lại ứng dụng sau background cũng mở pause. Màn kết quả không cho tiếp tục nhận cỏ hoặc cộng thưởng lần hai.

### Một tình huống chơi mẫu

Người chơi cần 70 đơn vị hoa đỏ và 25 bụi cây. Máy cấp 1 chưa cắt được bụi. Đường trực tiếp dẫn đến hoa đỏ, nhưng XP ở đó chưa đủ lên cấp. Một nhánh cỏ thường ở bên trái cung cấp XP an toàn. Người chơi cắt nhánh này, lên cấp 2 rồi quay sang bụi cây, hoặc cắt hoa trước và bổ sung XP trên đường nối. Lựa chọn tốt phụ thuộc khoảng cách và độ rộng lưỡi đã chọn.

### Vòng lặp giữa các màn

Hoàn thành đơn → mở khu vườn kế tiếp → nhận tiền mua ngoại hình và tiến độ bộ sưu tập → thử cơ chế mới → quay lại màn để đạt mục tiêu phụ. Phần meta không tăng sức mạnh bắt buộc; độ khó màn dựa trên công cụ trong lượt để designer có thể cân bằng đường đi.

## 4 Điều khiển camera và luật di chuyển

### Input nền tảng mobile

Dùng joystick nổi: chạm vào vùng trống ở nửa dưới màn hình để tạo tâm joystick, kéo theo hướng muốn đi. Biên độ nhỏ điều khiển tốc độ chậm; biên độ tối đa cho tốc độ đầy đủ. Thả tay thì máy giảm tốc về đứng yên. Cắt là tự động, không cần nút thao tác thứ hai.

| Thông số prototype | Giá trị khởi tạo | Ý nghĩa |
| --- | --- | --- |
| Tốc độ tối đa | 4,0 m/s | Tăng sau khi kiểm tra khả năng kiểm soát |
| Gia tốc và giảm tốc | 12 và 18 m/s² | Dừng nhanh hơn khởi động |
| Tốc độ xoay hình máy | 540 độ/s | Hướng di chuyển theo input; model xoay theo sau |
| Joystick dead zone | 10% bán kính | Giảm trôi do ngón tay |
| Bán kính thân máy | 0,30 m | Collision cố định suốt màn |
| Bán kính cắt ban đầu | 0,65 m | Vùng cắt tách khỏi vùng collision |

### Camera

Góc nhìn 3D nghiêng khoảng 55–65 độ, orthographic, máy gần trung tâm nhưng ưu tiên nhìn phía trước hướng di chuyển. Bản prototype dùng framing cố định theo level, không zoom sát hơn khi nâng máy. Lưỡi tối đa và vùng cảnh báo phía trước phải cùng nằm trong khung nhìn. Preview cho thấy toàn map, còn mini-map chỉ thêm nếu playtest cho thấy người chơi mất phương hướng.

### Va chạm và khả năng đọc đường

Thân máy va vào đá, hàng rào và rìa map thì trượt dọc bề mặt, không bật lùi. Cây chưa đủ cấp không chặn thân; lưỡi chạy qua nhưng không cắt. Những cây này xuất hiện biểu tượng khóa ngắn khi tiếp xúc. Khi nâng lưỡi, model và vòng phạm vi chuyển kích thước trong khoảng 0,25 giây, timer vẫn dừng đến khi đóng lựa chọn.

### Quy tắc chống khó chịu

- Không lấy màu làm tín hiệu duy nhất; loại cây có silhouette và icon riêng.

- Không đặt mục tiêu sau một hành lang hẹp hơn đường kính thân máy cộng 0,25 m.

- Không cho VFX cỏ che vùng bảo vệ hoặc mục tiêu còn thiếu.

- Nút pause và nâng cấp không nằm trong vùng joystick. Có tùy chọn tắt rung và giảm hiệu ứng.

## 5 Luật cắt mục tiêu và dữ liệu cây

### Đơn vị gameplay

Map được chia thành các ô logic 0,25 × 0,25 m. Một ô chứa tối đa một đơn vị cây có loại, cấp yêu cầu, độ bền và XP. Số cây hiển thị có thể nhiều hơn số ô logic; toàn bộ cụm trong ô biến mất khi ô bị cắt. Mục tiêu đếm ô logic, không đếm từng lá hoặc từng instance trang trí.

### Điều kiện cắt

Ô nằm trong swept area của lưỡi giữa vị trí trước và hiện tại, nằm trong khu được phép cắt và có RequiredTier ≤ MachineTier thì nhận tiến độ cắt. Swept area giúp máy đi nhanh vẫn cắt liên tục. Mỗi ô nhận thời gian tiếp xúc thực tế trong tick, không nhận toàn bộ tick nếu chỉ lướt qua cạnh.

CutProgress tăng theo Δt × CuttingPower ÷ Toughness. Khi đạt 1, ô được đánh dấu Cut trước khi phát event. Mỗi ô chỉ cộng mục tiêu và XP một lần. Cây chưa cắt xong giữ tiến độ trong màn và đổi hình nhẹ để người chơi biết đã xử lý; restart khôi phục toàn bộ dữ liệu ban đầu.

| Loại | Cấp | Độ bền | XP mỗi ô | Vai trò |
| --- | --- | --- | --- | --- |
| Cỏ thường | 1 | 0,12 | 1 | Nguồn tăng trưởng dễ lấy |
| Hoa thu hoạch | 1 | 0,18 | 1 | Mục tiêu định tuyến |
| Cỏ dày | 2 | 0,30 | 2 | Tăng trưởng hiệu quả sau cấp 2 |
| Bụi thấp | 2 | 0,45 | 3 | Mục tiêu buộc chuẩn bị |
| Bụi cứng | 3 | 0,65 | 4 | Đích cuối của màn nâng cao |
| Hoa bảo vệ | Không cắt | — | 0 | Ràng buộc đường cắt |

### Phạm vi mẫu và kiểm soát số liệu

CuttingPower khởi tạo là 1,0. Ví dụ cỏ thường cần khoảng 0,12 giây tiếp xúc, bụi thấp cần 0,45 giây ở cấp đủ điều kiện. Đây là thời gian mẫu; phải cân với vận tốc và footprint để đường đi bình thường không để lại cỏ vụn khó chịu. Designer xem heatmap phần cắt dở trước khi tăng tốc máy.

### Hoàn thành và phần cỏ sót

Mục tiêu thắng là số lượng thu hoạch cụ thể, không yêu cầu xóa mọi pixel. Các ô sát biên được bố trí trong phạm vi lưỡi có thể chạm. Cleanup hiển thị tỷ lệ diện tích đã cắt; khi còn dưới 1% ô hợp lệ và mỗi cụm còn lại không quá 4 ô, có nút dọn nốt tùy chọn, không tính vào điểm kỹ năng.

## 6 Tăng trưởng và lựa chọn nâng cấp

### Mốc cấp trong lượt chơi

XP chỉ dùng trong màn và reset khi vào màn mới. Cấp máy quyết định nhóm cây được phép cắt. Khi đạt mốc, cấp tăng trước khi mở bảng lựa chọn; mọi phương án đều mở cùng nhóm cây để không tạo tình huống chọn sai và kẹt mục tiêu.

| Cấp đạt được | XP tích lũy | Khả năng mở | Lựa chọn |
| --- | --- | --- | --- |
| 1 | 0 | Cỏ và hoa thu hoạch | Máy nền |
| 2 | 100 | Cỏ dày và bụi thấp | Lưỡi rộng hoặc động cơ khỏe |
| 3 | 260 | Bụi cứng | Lưỡi rộng hoặc động cơ khỏe |
| 4 | 480 | Tối ưu tốc độ dọn | Lưỡi rộng hoặc động cơ khỏe |

### Hai hướng nâng cấp

Lưỡi rộng tăng bán kính cắt thêm 0,15 m, tối đa 1,10 m. Động cơ khỏe tăng CuttingPower thêm 0,30 và tốc độ tối đa thêm 0,25 m/s, tối đa 4,75 m/s. Có thể chọn lặp lại cùng hướng hoặc phối hợp. Không nâng ngẫu nhiên và không giới thiệu ba chỉ số mới cùng lúc.

Lưỡi rộng phù hợp thảm cỏ thoáng, nhưng dễ chạm hoa bảo vệ. Động cơ phù hợp bụi cứng hoặc tuyến vòng dài. Không ép lựa chọn chỉ bằng số lớn hơn: level phải có bố cục khiến cả hai hướng đạt lợi ích thực tế. Nếu playtest luôn chọn một hướng, điều chỉnh geometry và mật độ trước khi thêm nhiều nâng cấp.

### Ràng buộc để màn luôn giải được

- XP cần đạt cấp 2 phải có trong vùng cấp 1 tiếp cận được từ điểm xuất phát.

- XP cần đạt cấp 3 phải có trong tổng vùng cấp 1 và 2; không đặt toàn bộ XP cần thiết trong vùng cấp 3.

- Mỗi loại mục tiêu có ít nhất 110% quota trong map để người chơi có lựa chọn vị trí.

- Kiểm tra đường hợp lệ với bán kính lưỡi lớn nhất, đặc biệt ở màn có hoa bảo vệ.

- Validator kiểm tra ngưỡng XP và số lượng cây; designer vẫn phải chạy cả hai build để xác nhận timer.

### Booster trong prototype

Chưa có booster trong vòng lặp mặc định. Một công cụ debug có thể bật CuttingPower ×1,5 trong 8 giây để kiểm tra tác động. Chỉ đưa booster thành nội dung sau khi nhịp nền đã chơi tốt; booster không được che lỗi level hoặc thay thế yêu cầu lựa chọn đường đi.

## 7 Mục tiêu chế độ và mức thử thách

### Chế độ Contract

Mỗi màn có 1–3 quota thu hoạch. Thắng khi toàn bộ quota đạt và số lỗi bảo vệ không vượt giới hạn. Thua khi hết giờ hoặc vượt số lỗi. Màn 1–3 không có thua do bảo vệ; từ màn giới thiệu hoa bảo vệ mới áp dụng luật này.

### Hoa bảo vệ và xử lý lỗi

Hoa bảo vệ được đặt trong các luống riêng có viền và icon. Khi vùng lưỡi chạm một ô bảo vệ, tính một lỗi, phát feedback đỏ và hiển thị số lỗi còn được phép. Một cụm tiếp xúc liên tục chỉ tính một lỗi; cụm đó chỉ được tính lỗi lại sau khi lưỡi rời hoàn toàn ít nhất 1 giây. Hoa không cho XP và không biến mất. Giới hạn mặc định là 3 lỗi; lỗi thứ 4 kết thúc màn.

Để tránh cảm giác bị phạt bất ngờ, phạm vi lưỡi phải đọc được ở vùng bảo vệ. Màn đầu của cơ chế có khoảng trống đủ cho máy cấp 1 và giải thích cách đi chậm. Nếu player không nhận ra nguyên nhân lỗi, bỏ cơ chế khỏi bản đầu thay vì tăng số lỗi cho phép.

| Biến thể | Thắng | Áp lực | Thời điểm dùng |
| --- | --- | --- | --- |
| Thu hoạch cơ bản | Đạt quota | Timer rộng | Onboarding |
| Chọn tuyến | Quota nằm ở nhiều nhánh | Khoảng cách và XP | Màn 4 trở đi |
| Bảo vệ luống hoa | Quota cộng số lỗi trong giới hạn | Độ rộng lưỡi và tốc độ | Sau khi hiểu tăng trưởng |
| Zen | Dọn diện tích tự chọn | Không timer hoặc lỗi thất bại | Mở sau màn 3 |

### Đánh giá kết quả

Một sao cho hoàn thành mục tiêu chính. Sao thứ hai khi còn ít nhất 20% thời gian khởi tạo. Sao thứ ba khi không mắc lỗi và thu đủ một quota phụ do designer đặt. Quota phụ có thể là một loại cây ngoài đường chính; phải hiển thị từ preview. Sao không cộng dồn sức mạnh, chỉ dùng mở ngoại hình hoặc bộ sưu tập.

### Chơi lại và khả năng hoàn thiện

Sau thắng có thể vào Cleanup, hoặc quay lại để tối ưu sao. Thất bại cho thấy loại cây còn thiếu và vị trí vùng còn mục tiêu trên preview. Retry giữ seed và bố cục để người chơi học được từ lần trước. Không đổi quota ngẫu nhiên khi bấm chơi lại.

## 8 Level design và mẫu màn có thể triển khai

### Quy trình thiết kế level

Chốt quyết định muốn kiểm tra → bố trí điểm xuất phát và vùng mục tiêu → đặt nguồn XP trước vùng khóa → thêm một đường thay thế → chạy build rộng và build khỏe → đặt timer dựa trên đường hoàn thành có sai số → kiểm tra quota, camera và đường tiếp cận. Không dùng số lượng cỏ làm thước đo độ khó duy nhất.

| Màn | Cơ chế mới | Quyết định chính | Timer mẫu |
| --- | --- | --- | --- |
| 1 | Di chuyển và cắt | Theo một dải cỏ rõ | 90 s |
| 2 | Quota hoa | Đến đúng vùng cần thu hoạch | 100 s |
| 3 | Cấp 2 | Lấy XP trước khi cắt bụi | 110 s |
| 4 | Hai tuyến | Đi vòng tăng máy hay đi thẳng | 120 s |
| 5 | Áp dụng lưỡi rộng | Dọn thảm lớn hoặc tuyến hẹp | 120 s |
| 6 | Hoa bảo vệ | Đi chậm và tránh chạm lưỡi | 130 s |
| 7–8 | Phối hợp đã học | Thứ tự vùng mục tiêu | 130–140 s |
| 9–10 | Cấp 3 và quota phụ | Chuẩn bị trước vùng cứng | 140–150 s |

### Level mẫu 04 Sân vườn hai nhánh

Map 18 × 14 m, ô logic 0,25 m. Điểm xuất phát (2; 2). Vùng A ở phía tây có 120 ô cỏ thường; vùng B ở phía đông có 90 ô hoa đỏ; vùng C ở phía bắc có 40 ô bụi thấp. Mục tiêu chính là 70 hoa đỏ và 25 bụi thấp. Mốc cấp 2 là 100 XP; timer 120 giây. Quota phụ là 30 trong số 34 ô cỏ dày ở đường nối phía bắc.

Tuyến A → C → B cung cấp cấp 2 trước khi tới bụi. Tuyến B → A → C xử lý hoa sớm và có thể lên cấp trên đoạn A trước C. Mỗi vùng có lối vào đủ rộng; không cần cấp 3 để thắng. Đây là layout khởi tạo, chưa chứng minh hai tuyến tương đương cho đến khi graybox và đo thời gian.

### Điều chỉnh độ khó

Giảm timer chỉ sau khi đường đi và phạm vi cắt đã rõ. Tăng khoảng cách, phân tán mục tiêu hoặc thêm vùng bảo vệ từng yếu tố một. Màn sau một cơ chế mới là màn củng cố, không thêm luật khác ngay. Không spawn vật cản ngẫu nhiên vào đường đã được xác nhận là tuyến hoàn thành duy nhất.

## 9 Onboarding và tiến trình nội dung

### Ba màn đầu

| Mốc | Hướng dẫn | Hành vi cần quan sát |
| --- | --- | --- |
| 0–10 giây đầu | Một chỉ dẫn kéo để lái, dải cỏ trước máy | Người chơi tự di chuyển và thấy cỏ biến mất |
| Màn 1 | Một mục tiêu cỏ và thanh đếm | Hiểu rằng cắt làm tiến độ tăng |
| Màn 2 | Hoa có icon tương ứng quota | Tìm đúng loại thay vì cắt toàn map |
| Màn 3 | Bụi khóa và XP ở nhánh gần | Lên cấp rồi quay lại vùng đã khóa |
| Nâng lần đầu | Hai lựa chọn có preview phạm vi hoặc tốc độ | Hiểu lựa chọn tác động ngay trong màn |

Hướng dẫn xuất hiện theo sự kiện, không theo timer cố định. Nếu người chơi đã tự làm đúng thì bỏ qua chỉ dẫn tương ứng. Không khóa input để trình bày nhiều đoạn văn. Khi đứng yên 8 giây ở màn đầu, hiện gợi ý nhỏ; người chơi vẫn có thể bỏ qua.

### Cấu trúc nội dung bản đầu

Đề xuất bản đầu có 30 màn thủ công trong 3 chủ đề sân vườn. Mỗi nhóm 10 màn có mở đầu giới thiệu, màn củng cố và màn tổng hợp. Thay chủ đề đi kèm một thay đổi bố cục có ý nghĩa, ví dụ sân nhỏ nhiều lối nối, công viên có vùng rộng, vườn cảnh có luống bảo vệ.

Tiến trình tuyến tính theo thắng màn, không khóa đường chính bằng số sao. Các sao mở mục tiêu phụ về ngoại hình. Zen dùng 3 map tái sử dụng từ Contract nhưng loại quota, timer và thất bại; không cần sản xuất một hệ level riêng ngay từ đầu.

### Meta nhẹ

Người chơi mở skin máy, màu vệt cắt và bộ trang trí album sân vườn. Skin phải giữ footprint gameplay. Album ghi số màn hoàn thành và sao, giúp người chơi nhìn thấy tiến độ dài hạn mà không kéo sang quản lý kinh doanh.

### Nội dung sau bản đầu

Chỉ thêm hệ thống ngày hoặc challenge seed sau khi 30 màn nền có nhịp tốt. Một challenge cố định có thể so thành tích bằng thời gian còn lại trong cùng cấu hình máy; chưa cần bảng xếp hạng trực tuyến. Tránh sinh procedural hoàn toàn trước khi có validator đảm bảo ngưỡng cấp và quota.

## 10 Economy và monetization đề xuất

### Một tiền tệ mềm

Coin dùng mua ngoại hình. Không có phí vào màn, không trừ coin khi thua và không mua chỉ số bắt buộc. Chiến thắng lần đầu cho 100 coin; mỗi sao thưởng thêm 25 coin một lần. Chơi lại không phát coin cơ bản; chỉ phát phần sao mới đạt. Zen không tạo coin để tránh farm vô hạn.

| Nguồn hoặc chi | Giá trị mẫu | Quy tắc |
| --- | --- | --- |
| Thắng lần đầu | 100 coin | Một lần theo LevelId |
| Mỗi sao mới | 25 coin | Tổng tối đa 75 coin mỗi màn |
| Skin nhóm A | 600 coin | Khoảng 4 màn nếu đạt trung bình 2 sao |
| Skin nhóm B | 1.200 coin | Khoảng 8 màn cùng giả định |
| 30 màn đạt 3 sao | 5.250 coin tối đa | Ngân sách tổng để đặt số lượng vật phẩm |

Ví dụ thắng 2 sao lần đầu nhận 150 coin. Lần sau đạt sao thứ ba chỉ nhận thêm 25 coin. Giao dịch thưởng phải có ID duy nhất theo màn và lần nhận; crash sau màn kết quả không được nhân đôi hoặc mất thưởng. Giá skin cần kiểm tra xem người chơi có thấy mục tiêu đủ gần trong buổi chơi đầu hay không.

### Monetization nếu phát triển mobile

Prototype không quảng cáo. Nếu nhịp chơi và retention nội bộ đủ tốt, bản phát hành có thể thử rewarded ad tự chọn để nhận thêm một khoản coin sau chiến thắng hoặc thử skin. Không đưa quảng cáo vào lúc chọn nâng cấp, timer đang chạy hoặc retry ngay sau thất bại. Không dùng revive bắt buộc để bù một màn khó quá mức.

Interstitial là phương án thử nghiệm sau, chưa phải yêu cầu mặc định. Nếu dùng, chỉ đặt giữa các màn với cooldown và bảo vệ onboarding; phải đo số người thoát và tỷ lệ bắt đầu màn kế tiếp. Gói tắt quảng cáo cần nói rõ loại quảng cáo được tắt. Không dự toán doanh thu khi chưa có dữ liệu người chơi và chi phí thu hút.

### Chống lệch thiết kế

Không bán nâng cấp khiến quota chỉ giải được sau mua. Tăng trưởng trong màn phải giữ cùng ngưỡng ở build có và không quảng cáo. Nếu monetization làm người chơi thường xuyên bỏ nhịp cắt hoặc bỏ retry, ưu tiên sửa vị trí xuất hiện thay vì thêm phần thưởng lớn hơn.

## 11 Giao diện âm thanh và cảm giác chơi

### Thông tin theo màn hình

| Màn hình | Nội dung bắt buộc | Hành động chính |
| --- | --- | --- |
| Home | Tiến độ khu vườn, màn tiếp theo, Zen, tùy chọn | Chơi |
| Preview | Quota, timer, quota phụ và toàn map | Bắt đầu |
| Gameplay | Quota kèm icon, timer, XP, cấp và pause | Joystick |
| Nâng cấp | Hai tác dụng có số liệu và preview | Chọn một |
| Kết quả | Thắng hoặc lý do thua, sao, coin mới nhận | Màn tiếp hoặc retry |
| Pause | Tiếp tục, chơi lại, thoát, âm thanh và rung | Tiếp tục |

### Thứ bậc HUD

Quota chính đặt ở đầu màn. Timer cần đọc được nhưng không che vùng đi; chỉ đổi trạng thái cảnh báo ở 15 giây cuối. XP nằm gần dưới HUD với mốc cấp tiếp theo. Không hiển thị coin chạy liên tục trong gameplay vì coin được quyết toán ở cuối. Mục tiêu đã đủ chuyển sang dấu hoàn thành, không biến mất đột ngột.

### Chuỗi feedback khi cắt

Lưỡi tiếp xúc → cây rung ngắn → ô đủ tiến độ đổi sang nền sạch trong cùng tick → một số mảnh cỏ bay thấp → âm cắt theo mật độ → quota cập nhật → XP tiến tới mốc. Không tạo popup cho mọi ô. Gom event thành feedback theo cụm trong khoảng 0,1 giây để âm thanh, VFX và rung không quá tải.

| Sự kiện | Feedback mẫu | Giới hạn |
| --- | --- | --- |
| Cắt liên tục | Loop cắt thay đổi pitch nhẹ theo mật độ | Crossfade để không bật tắt gắt |
| Đạt quota | Icon bật nhẹ và âm xác nhận | Chỉ một lần mỗi quota |
| Lên cấp | Máy mở rộng, vòng lưỡi hiện ngắn | Không che vùng mục tiêu |
| Chạm vùng bảo vệ | Đỏ trên cụm và đếm lỗi | Không rung kéo dài |
| Thắng | Toàn sân xuất hiện ngắn và bảng sao | Cho bỏ qua animation |

### Art direction và accessibility

Mặt đất sạch tương phản với cây chưa cắt. Cỏ thường thấp, hoa có đầu bông, bụi có khối rõ. Hoa bảo vệ dùng luống và biểu tượng khiên; màu đỏ đơn lẻ không đủ. Ưu tiên 60 FPS trên máy mục tiêu, cho giảm VFX và rung. Âm nền nhẹ, âm cắt không chói, có thanh chỉnh riêng SFX và music.

## 12 Yêu cầu triển khai Unity và QA

### Hệ thống tối thiểu

Input điều khiển vận tốc; Motor giải quyết collision; CuttingSystem truy vấn ô và xử lý tiến độ; GrowthSystem quản XP và lựa chọn; ObjectiveSystem đếm quota; LevelSession điều phối trạng thái; RewardService quyết toán; SaveService lưu tiến trình. UI quan sát event, không tự quyết định cắt hoặc thưởng.

### Dữ liệu có thể chỉnh trong editor

| Dữ liệu | Trường bắt buộc |
| --- | --- |
| PlantDefinition | Id, RequiredTier, Toughness, XpValue, ObjectiveTag, prefab hoặc render data |
| LevelDefinition | Id, seed, bounds, spawn, timer, quota chính và phụ, errorLimit, map cells |
| MachineConfig | CollisionRadius, CutRadius, Speed, Acceleration, CuttingPower, XP thresholds |
| UpgradeDefinition | Id, radiusDelta, powerDelta, speedDelta, cap |
| ProgressSave | Version, unlockedLevel, bestStars, grantedRewards, coins, ownedSkins, settings |

### Hiệu năng và lưu trữ

Dùng spatial grid để chỉ xét ô gần swept area; không tạo collider riêng cho mọi lá cỏ. Có thể dùng mesh theo chunk hoặc GPU instancing, cập nhật trạng thái cắt theo chunk. Pool VFX, hạn chế event từng ô ra UI. Burst hoặc Job System chỉ thêm sau khi profiler xác định nút thắt; không để kiến trúc tối ưu trì hoãn prototype.

Mục tiêu khởi tạo là 60 FPS trên một thiết bị Android tầm trung được team chọn cụ thể; frame budget 16,7 ms, đo trong map dày nhất và máy rộng nhất. Save tiến trình sau kết quả bằng ghi file tạm rồi thay thế. Bản đầu không cần resume giữa màn; khi app bị đóng, khởi động lại màn nhưng giữ thưởng đã xác nhận.

### Các ca QA bắt buộc

- Đi tốc độ tối đa không bỏ sọc cỏ giữa hai tick; cắt trùng không cộng XP hai lần.

- Đạt quota đúng tick hết giờ được thắng; bảng nâng cấp và pause không trừ timer.

- Mọi quota chính có đủ cây và XP để mở cấp cần thiết; thử cả hai hướng nâng.

- Nhấn nhận thưởng nhiều lần, restart app ở màn kết quả và mở save cũ không nhân đôi coin.

- Hoa bảo vệ tiếp xúc liên tục chỉ ghi một lỗi; quay lại sau cooldown đúng luật.

- UI không chồng joystick trên tỷ lệ màn hình mục tiêu; camera thấy được rìa lưỡi tối đa.

## 13 Phạm vi sản xuất và tiêu chí quyết định

### Các giai đoạn theo kết quả

| Giai đoạn | Deliverable | Điều kiện chuyển |
| --- | --- | --- |
| Prototype | 3 map graybox, joystick, cắt, quota, timer, cấp và hai lựa chọn | Người mới hiểu vòng lặp và muốn thử lại |
| Vertical slice | 10 màn, 1 chủ đề art, HUD, âm thanh, save, 3 skin, Zen | Cảm giác cắt ổn, đường đi đa dạng, hiệu năng đạt |
| Bản đầu | 30 màn, 3 chủ đề, economy ngoại hình, QA thiết bị | Không kẹt level, không lỗi thưởng, nội dung đã playtest |

Ước lượng tham khảo cho team gồm một Unity Developer, một GD kiêm level design và artist hỗ trợ: prototype 1–2 tuần, vertical slice thêm 3–5 tuần, bản đầu thêm 4–8 tuần. Đây là khoảng lập kế hoạch chưa xét năng lực team, asset sẵn có hoặc lịch bán thời gian; cần ước lượng lại sau prototype.

### Playtest và telemetry

Đợt đầu quan sát 8–12 người chưa biết luật; mỗi người chơi ít nhất 3 màn. Hỏi họ định đi đâu và vì sao sau lượt chơi, tránh chỉ hỏi game có vui không. Ghi level_start, first_cut, tier_up, upgrade_selected, objective_complete, protected_hit, level_end, retry và session_end. Level_end kèm duration, result, remainingQuota, chosenBuild và errors; không cần dữ liệu định danh để kiểm tra gameplay.

| Giả thuyết | Ngưỡng nội bộ ban đầu | Nếu không đạt |
| --- | --- | --- |
| Điều khiển dễ hiểu | Ít nhất 8/10 người tự cắt trong 15 s | Sửa input và feedback trước nội dung |
| Tăng trưởng rõ | Ít nhất 7/10 giải thích đúng cây khóa sau màn 3 | Sửa tín hiệu cấp và bố trí XP |
| Lựa chọn có ý nghĩa | Cả hai build được dùng và có đường thắng | Sửa map nếu một build luôn vượt trội |
| Có động lực chơi lại | Ít nhất 6/10 tự chọn thêm một lượt | Xem lại cảm giác cắt và quyết định đường |
| Khó nhưng công bằng | Người thua chỉ được nguyên nhân cụ thể | Sửa camera, quota hoặc luật lỗi |

Các ngưỡng trên là tiêu chí nội bộ trên mẫu nhỏ, không phải benchmark thị trường. Chỉ mở rộng sản xuất nếu vấn đề nằm ở nội dung có thể cải thiện. Nếu sau hai vòng chỉnh sửa người chơi vẫn chỉ di chuyển theo vùng gần nhất và không quan tâm nâng máy, cần thay trọng tâm gameplay trước khi làm 30 màn.

## 14 Rủi ro phương án PC và quyết định còn mở

### Rủi ro ưu tiên

| Rủi ro | Dấu hiệu | Biện pháp |
| --- | --- | --- |
| Cắt trở nên đơn điệu | Player không đổi chiến thuật giữa các map | Đặt tradeoff XP, mục tiêu và footprint |
| Tăng máy làm khó điều khiển | Lỗi bảo vệ tăng vọt sau chọn rộng | Giữ collision cố định, preview lưỡi rõ |
| Cỏ sót gây khó chịu | Lượt cuối chỉ tìm vài ô khó thấy | Quota có dư, cleanup có hỗ trợ |
| Thư giãn xung đột timer | Player muốn cắt tiếp nhưng bị kết thúc | Cho Cleanup sau thắng và Zen riêng |
| Scope tăng quá sớm | Chưa test cắt đã làm farm hoặc shop phức tạp | Khóa phạm vi ở prototype và slice |

### Nếu chọn PC và Steam

Không nên chỉ đổi joystick sang WASD rồi giữ toàn bộ nhịp mobile. Với PC, đề xuất game tối ưu đường cắt theo khu vườn, phiên 15–30 phút, map lớn hơn và loadout trước lượt chơi. Camera cần nhìn rộng, hỗ trợ chuột hoặc gamepad; mức độ sâu đến từ geometry và công cụ như lưỡi rộng, lưỡi chính xác và động cơ xử lý cây cứng.

Mô hình sản phẩm đề xuất là trả tiền một lần, bỏ quảng cáo và economy phục vụ quảng cáo. Trọng tâm có thể là bộ màn thủ công có nhiều huy chương, Zen và chia sẻ thành tích. Chưa đưa procedural hoặc level editor vào bản đầu. Quy mô nội dung, mức giá và định vị thị trường phải được nghiên cứu riêng; tài liệu hiện tại không xác nhận bản mobile đã đủ nội dung để bán trên Steam.

### Các quyết định cần chốt sau prototype

- Nền tảng chính của dự án này là mobile hay PC. Chọn một hướng trước vertical slice.

- Giữ timer làm mục tiêu chính hay chỉ là huy chương tốc độ, dựa trên hành vi playtest.

- Hoa bảo vệ có tạo quyết định tốt hay chỉ gây mất vui; có thể loại khỏi bản đầu.

- Hai nâng cấp có tạo tuyến chơi khác nhau hay cần đổi một lựa chọn.

- Danh sách thiết bị mục tiêu, nguồn asset, nhân lực và ngân sách thực tế.

### Việc nên triển khai đầu tiên

Làm một sân graybox có cỏ thường, hoa mục tiêu và bụi cấp 2. Hoàn thiện input, swept cutting, XP và hai lựa chọn nâng trước. Dùng level mẫu 04 để kiểm tra hai đường thắng. Khi người chơi nhận ra tại sao đi vòng giúp lượt chơi hiệu quả hơn, mới đầu tư art và hệ thống tiến trình.

Toàn bộ luật và thông số từ trang 3 đến trang này là đề xuất thiết kế độc lập. Nguồn tham chiếu S1 chỉ dùng cho phần mô tả game trên trang 2; không dùng để khẳng định khả năng thương mại của Grass Route.
