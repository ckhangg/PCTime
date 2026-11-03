# PC Time

Ứng dụng Windows theo dõi thời gian sử dụng máy tính theo từng ứng dụng/game. C# / WPF, giao diện tối hồng–cyan, dữ liệu lưu cục bộ.

## Tải và sử dụng

1. Vào **Releases** của repo, tải `PC-Time-v0.1.0-win-x64.zip`.
2. Giải nén toàn bộ vào thư mục cố định, mở `PcTime.exe`.
3. Bản ZIP dành cho Windows 11 x64, có kèm .NET nên không cần cài runtime riêng.

Nhấn X để thu xuống khay hệ thống. Nhấp đúp biểu tượng khay để mở lại; nhấp phải → Thoát để dừng hoàn toàn. Thoát bản cũ trước khi chạy bản mới. Khi nâng cấp, giải nén bản mới vào thư mục riêng; lịch sử vẫn được giữ.

## Tính năng

- Tổng thời gian sử dụng và ứng dụng dùng nhiều nhất theo ngày.
- Chi tiết từng ứng dụng, tên tiến trình, thời gian và tỷ lệ.
- Tự chọn nhóm **Work / Game / Giải trí / Khác**, tự lưu lựa chọn.
- Tổng thời gian từng nhóm theo ngày. Đổi nhóm áp dụng cho toàn bộ lịch sử của ứng dụng; không sửa thời gian gốc.
- Lịch sử 14 ngày gần nhất; chọn ngày bất kỳ bằng lịch để xem cũ hơn.
- Tự nhận biết nghỉ, dừng khi khóa máy hoặc sleep.
- Tùy chọn vẫn tính khi xem phim/chơi tay cầm.
- Tùy chọn khởi động dưới khay hệ thống khi đăng nhập Windows.
- Xuất CSV với các cột Application, Group, Seconds.

## Cách tính và giới hạn

App lấy mẫu cửa sổ ở phía trước mỗi giây, không cộng app chạy nền. Mặc định nhận biết nghỉ sau 5 phút không thao tác chuột/phím, rồi trừ ngược thời gian kể từ lần thao tác cuối. Khóa máy/sleep dừng tính; khoảng gián đoạn lấy mẫu từ 5 giây trở lên được bỏ qua. Sai số khoảng 1 giây mỗi lần đổi cửa sổ.

Bật “Vẫn tính…” khi xem phim hoặc dùng tay cầm; chưa tự nhận diện media/tay cầm. Ngày theo múi giờ Windows, phiên qua nửa đêm chia vào hai ngày. Nhóm mặc định là Khác, chưa tự phân loại, chưa phân biệt từng website trong trình duyệt. Đây là bản thử nghiệm v0.1.0.

## Quyền riêng tư và dữ liệu

Không cần tài khoản. Không gửi lịch sử lên mạng. Không ghi phím bấm, ảnh màn hình hay tiêu đề cửa sổ.

- `%LOCALAPPDATA%\PcTime\usage.json`: tên tiến trình và thời điểm bắt đầu/kết thúc.
- `%LOCALAPPDATA%\PcTime\settings.json`: cài đặt và nhóm ứng dụng.

Lưu lịch sử mỗi 15 giây; nếu dừng đột ngột có thể mất khoảng chưa lưu. Muốn sao lưu, thoát app rồi sao chép hai file trên. Không gửi lịch sử cá nhân khi báo lỗi nếu không muốn chia sẻ.

Tự chạy Windows mặc định tắt. Nếu di chuyển thư mục app, tắt rồi bật lại tùy chọn để cập nhật đường dẫn.

## Phát triển

Cần .NET SDK 10 trên Windows.

```powershell
dotnet build PcTime/PcTime.csproj -c Release -o Grouped
dotnet run --project PcTime.Checks/PcTime.Checks.csproj -c Release
```

Đóng gói bản độc lập:

```powershell
dotnet publish PcTime/PcTime.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -o artifacts/PC-Time-v0.1.0-win-x64
```

Tham số `--preview`: xem giao diện và lịch sử, không ghi thời gian/cài đặt, X thoát hoàn toàn. `--tray`: mở ẩn dưới khay hệ thống.

Bộ kiểm tra gồm 16 trường hợp về chia ngày, thời gian nghỉ, lưu dữ liệu, cài đặt, phân nhóm và cập nhật dòng giao diện. Khởi động qua đăng nhập Windows cần kiểm tra thực tế sau khi bật.

## Font

Chakra Petch và Be Vietnam Pro theo SIL Open Font License. Bản quyền và giấy phép được giữ trong `PcTime/Fonts/` và thư mục Fonts của bản phát hành.

## Góp ý

Mở Issue, mô tả thao tác tái hiện, kết quả mong đợi, kết quả thực tế và phiên bản Windows/app.
