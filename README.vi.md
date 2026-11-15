# Stream Deck AI Usage Monitor (AI Usage Deck)

[English README](README.md)

Plugin Elgato Stream Deck và local collector trên Windows x64 *(hiển thị trong app Stream Deck với tên **AI Usage Deck**)* để theo dõi hạn mức **còn lại** (`%`, cửa sổ `5H` & `7D`) và thời điểm reset cho **Claude Code**, **OpenAI Codex (ChatGPT)** và **Antigravity (`agy`)**. Toàn bộ tiến trình chạy cục bộ trên máy; không gửi prompt AI, không tốn token, không telemetry hay backend cloud.

## Cài đặt và sử dụng

1. Cài và đăng nhập sẵn ít nhất một trong các CLI: Claude Code CLI, Codex CLI, hoặc Antigravity CLI (`agy`).
2. **Tải bản release hoặc tự build:**
   - Tự build từ mã nguồn: chạy `./scripts/build.ps1` (yêu cầu .NET 10 SDK và Node.js 20+). Kết quả nằm trong `artifacts/release/`.
3. **Cài plugin:**
   - Mở đúp `com.local.aiusagedeck.streamDeckPlugin` bằng Stream Deck để cài. *(Bản dùng cá nhân không có chữ ký nhà phát hành.)*
   - Hoặc đóng Stream Deck, chạy `install.ps1` trong thư mục release rồi mở lại Stream Deck. Nếu đã có bản cũ, script tự sao lưu bản cũ vào `%LOCALAPPDATA%\AIUsageDeck\Backups\`.
4. **Đặt phím lên thiết bị:**
   - **Stream Deck Mini (3×2, model `20GAI9901`):** Có thể import `AI Usage Deck.streamDeckProfile` để tạo profile 6 phím riêng (profile cũ vẫn được giữ nguyên).
   - **Mọi dòng Stream Deck khác:** Kéo 6 action trong nhóm **AI Usage Deck** lên trang bất kỳ và chọn Dịch vụ / Cửa sổ (`5 giờ` hoặc `Tuần`) / Nhóm quota trong phần cấu hình action.
5. **Bố cục gợi ý (3×2):** Cột Claude / Codex / Antigravity; hàng trên 5 giờ, hàng dưới tuần.
6. **Thao tác phím:**
   - **Bấm ngắn:** Xem thời gian còn lại đến lúc reset trong 5 giây.
   - **Giữ ít nhất `0,7` giây rồi thả:** Yêu cầu refresh dịch vụ tương ứng (tối đa 1 lần mỗi 15 giây, tôn trọng `Retry-After`).
   - Giờ hiển thị phía dưới phím ở trạng thái `LIVE` là giờ reset theo giờ máy.
   - Với Antigravity, nên đặt hai phím `5H` và `7D` cùng một nhóm quota (mặc định `Gemini Models`). Các nhóm Antigravity có hạn mức độc lập, không cộng gộp.

Plugin tự khởi chạy collector ẩn khi cần kết nối và không tạo tác vụ Windows Startup riêng. Khi có consumer kết nối: polling mỗi `120` giây; khi Stream Deck không mở hoặc ẩn action: giảm xuống `300` giây.

## Trạng thái trên phím

- `OFFLINE`: chưa kết nối được tới collector.
- `LOGIN`: cần đăng nhập hoặc làm mới phiên bằng CLI của dịch vụ; công cụ không tự đăng nhập.
- `ACCOUNT`: identity tài khoản/tổ chức thay đổi hoặc chưa khớp; dữ liệu cũ được xóa để tránh hiển thị nhầm tài khoản.
- `OLD`: dữ liệu cũ hoặc lần đọc mới thất bại. Giá trị giữ lại không phải dữ liệu vừa được xác nhận.
- `WAIT`: đang chờ giới hạn truy vấn (`429` / backoff); giữ phím cũng không bỏ qua thời gian chờ.
- `RESET`: đã qua thời điểm reset, đang chờ server xác nhận; không tự giả định đã đầy `100%`.
- `N/A`: nguồn/nhóm quota không cung cấp cửa sổ tương ứng.

## Bảo mật và giới hạn

- Collector tái sử dụng thông tin đăng nhập của CLI chỉ trong bộ nhớ; không sao chép refresh token và không đổi tài khoản.
- Plugin chỉ nhận snapshot quota đã lọc qua named pipe `\\.\pipe\AIUsageDeck.v1` giới hạn cùng người dùng Windows (`CurrentUserOnly`), không mở cổng TCP/LAN.
- **Claude:** Đọc endpoint OAuth usage/profile, đối chiếu account và organization UUID với CLI trước khi nhận quota. Đây là endpoint nội bộ của CLI, không có cam kết API công khai.
- **Codex:** Chỉ gọi `codex app-server --stdio` (`initialize`, `account/read`, `account/rateLimits/read`), không tạo thread hay gửi prompt.
- **Antigravity:** Gọi `agy -p /usage --output-format json`, từ chối báo cáo nếu `num_turns != 0` hoặc `total_tokens != 0`. Bản đầu theo dõi phiên `agy` hiện tại với nhãn `CLI`, chưa phân biệt nhiều tài khoản Google riêng biệt.
- Dữ liệu máy tại `%LOCALAPPDATA%\AIUsageDeck\snapshot.json` (chỉ lưu snapshot gần nhất) được bảo vệ bằng NTFS ACL giới hạn người dùng hiện tại. Log không ghi token, cookie, email, organization UUID hay stdout/stderr nguyên bản.
- Đường dẫn CLI chuẩn được nhận diện ở vị trí cài phổ biến; tôn trọng `CODEX_HOME` và `CLAUDE_CONFIG_DIR`. Nếu cài CLI ở vị trí đặc biệt, bổ sung đường dẫn trong `IdentityTools.Executable` (`src/AIUsageDeck.Collector/AdapterPaths.cs`) rồi build lại.

## Dừng, kiểm tra và gỡ

- **Dừng:** Đóng Stream Deck và dừng tiến trình `AIUsageDeck.Collector` trong Task Manager.
- **Kiểm tra nguồn một lần:** Chạy `com.local.aiusagedeck.sdPlugin\collector\AIUsageDeck.Collector.exe --once` (chỉ in snapshot đã lọc).
- **Gỡ:** Đóng Stream Deck trước rồi chạy `uninstall.ps1`. Script chuyển thư mục plugin sang `%LOCALAPPDATA%\AIUsageDeck\Uninstalled\`, giữ nguyên các profile của bạn.

## Build và kiểm thử

```powershell
dotnet run --project tests/AIUsageDeck.Tests -c Release
cd plugin
npm ci --ignore-scripts --no-fund --no-audit
npm run build
npm test
cd ..
./scripts/build.ps1
node scripts/smoke.mjs artifacts/collector/AIUsageDeck.Collector.exe
```

*(Lưu ý: Không chạy `smoke.mjs` khi collector chính từ Stream Deck đang chạy.)*

## Thêm dịch vụ AI mới

1. Tạo adapter thực hiện `IUsageAdapter` (`Name`, `ReadIdentityAsync`, `FetchAsync`) trong `src/AIUsageDeck.Collector/`.
2. Trả `ProviderRead` với identity đã hash/khử nhạy cảm và các `QuotaBucket`; giữ `null` nếu không biết quota.
3. Đăng ký adapter trong `src/AIUsageDeck.Collector/Program.cs`, thêm provider vào `PipeProtocol.cs`, danh sách provider trong `plugin/src/` và `ui/settings.html`.
4. Thêm unit test cho parser, đổi identity, lỗi xác thực/429 và render phím.
