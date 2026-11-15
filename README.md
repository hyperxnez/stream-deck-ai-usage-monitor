# AI Usage Deck

Công cụ riêng trên Windows x64: theo dõi hạn mức Claude, Codex và Antigravity trên Stream Deck. Phần trăm trên phím là **còn lại**, không phải đã dùng. Toàn bộ collector và plugin chạy trên máy; không telemetry hay backend cloud.

## Cài và sử dụng

1. Cài/đăng nhập Claude Code, Codex CLI và Antigravity CLI (`agy`). Phiên bản đã kiểm tra: Claude 2.1.296, Codex 0.146.0, agy 1.5.9; Stream Deck 7.6.1.
2. Mở `com.local.aiusagedeck.streamDeckPlugin` bằng Stream Deck để cài. Bản dùng cá nhân không có chữ ký nhà phát hành. Hoặc chạy `install.ps1` trong thư mục release rồi mở lại Stream Deck.
3. Import `AI Usage Deck.streamDeckProfile` cho Stream Deck Mini Discord Edition. Đây là profile riêng; profile Discord cũ được giữ nguyên. Hoặc kéo sáu action trong nhóm **AI Usage Deck** lên trang mới.
4. Bố cục: cột Claude / Codex / Antigravity; hàng trên 5 giờ, hàng dưới tuần.
5. Bấm ngắn để xem thời gian còn đến reset trong 5 giây. Giữ ít nhất 0,7 giây rồi thả để refresh dịch vụ. Giờ phía dưới phím bình thường là giờ reset theo giờ máy.
6. Trong phần cấu hình phím, có thể đổi dịch vụ/cửa sổ/nhóm quota. Đặt hai phím Antigravity cùng nhóm (mặc định Gemini Models). Các nhóm Antigravity có quota riêng, không được cộng lại.

Plugin tự khởi chạy collector ẩn khi cần kết nối. Không tạo tác vụ startup Windows riêng. Có consumer: kiểm tra mỗi 120 giây; khi Stream Deck không hiển thị action: mỗi 5 phút. Countdown cập nhật tại chỗ mỗi giây. Server có thể cập nhật usage trễ hơn hoạt động thực tế.

## Trạng thái trên phím

- `OFFLINE`: chưa kết nối collector.
- `LOGIN`: cần đăng nhập hoặc làm mới phiên bằng CLI của dịch vụ; công cụ không tự đăng nhập.
- `ACCOUNT`: identity thay đổi/chưa khớp; chưa hiển thị quota cho tài khoản mới.
- `OLD`: dữ liệu cũ hoặc lần đọc mới thất bại. Giá trị giữ lại không phải dữ liệu vừa được xác nhận.
- `WAIT`: đang chờ giới hạn truy vấn của dịch vụ; giữ phím cũng không bỏ qua thời gian chờ.
- `RESET`: đã đến thời điểm reset, đang chờ server xác nhận; không tự giả định quota đã đầy.
- `N/A`: nguồn không cung cấp cửa sổ/nhóm quota tương ứng.

## Bảo mật và giới hạn

Collector tái sử dụng thông tin đăng nhập của CLI, chỉ trong bộ nhớ; không sao chép refresh token và không đổi tài khoản. Plugin chỉ nhận snapshot quota đã lọc qua named pipe `AIUsageDeck.v1`, giới hạn cùng người dùng Windows. Không mở cổng TCP/LAN. Một phần mềm khác đã chạy dưới cùng tài khoản Windows không bị cách ly bởi thiết kế này.

Claude: đọc endpoint OAuth usage/profile, đối chiếu account và organization với CLI trước khi nhận quota. Đây là endpoint không có cam kết API công khai; có thể phải cập nhật adapter nếu nhà cung cấp thay đổi. Đổi tài khoản hoặc token sẽ xóa liên kết số liệu cũ. Công cụ không tự gia hạn credential: dùng CLI để đăng nhập/làm mới khi hiện LOGIN.

Codex: chỉ gọi app-server initialize và account methods, không tạo thread/model prompt. Antigravity: gọi `/usage` JSON, từ chối báo cáo có lượt model/token khác 0. Bản đầu theo dõi phiên agy hiện tại với nhãn CLI; chưa xác minh được identity tài khoản Google trong báo cáo này và chưa hỗ trợ nhiều tài khoản Google riêng biệt.

Dữ liệu máy: `%LOCALAPPDATA%\AIUsageDeck\snapshot.json` (chỉ snapshot gần nhất); directory ACL giới hạn người dùng hiện tại. Mã không ghi token, cookie, email, ID tổ chức hay stdout/stderr nguyên bản vào log. Lỗi chỉ là mã chẩn đoán đã lọc. Cache không được dùng để tự coi quota là mới sau khi khởi động lại.

Đường dẫn CLI chuẩn được nhận diện ở vị trí cài phổ biến trên máy này; `CODEX_HOME` và `CLAUDE_CONFIG_DIR` được tôn trọng. Nếu cài CLI vào vị trí đặc biệt, bổ sung đường dẫn trong `IdentityTools.Executable` rồi build lại. Không nhập shell command tùy ý vào plugin.

## Dừng, kiểm tra và gỡ

- Dừng: đóng Stream Deck và dừng tiến trình `AIUsageDeck.Collector` trong Task Manager. Nếu plugin vẫn chạy, nó có thể khởi động collector lại.
- Kiểm tra nguồn một lần: chạy `com.local.aiusagedeck.sdPlugin\collector\AIUsageDeck.Collector.exe --once`; chỉ in snapshot đã lọc.
- Gỡ: đóng Stream Deck trước, chạy `uninstall.ps1`. Script chuyển đúng thư mục plugin này vào backup có thể khôi phục; giữ nguyên profile và cache. Có thể gỡ qua giao diện Stream Deck nếu plugin được cài bằng package.

## Build và kiểm thử

Yêu cầu .NET 10 SDK, Node.js 22+ và npm. Dependency JavaScript được khóa trong package-lock.json; C# chỉ dùng thư viện nền tảng .NET.

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

`build.ps1` tạo collector self-contained, plugin package, profile sáu phím và SHA-256 trong `artifacts/release`. Smoke test gọi nguồn quota thật, không gửi model prompt; tự dừng collector mà chính test khởi chạy. Không chạy smoke test khi đang có collector sản xuất vì phép kiểm tra singleton có thể dùng tiến trình đã chạy sẵn.

## Thêm dịch vụ AI

1. Tạo adapter thực hiện `IUsageAdapter`: `Name`, `ReadIdentityAsync`, `FetchAsync`.
2. Trả `ProviderRead` với identity đã xác minh và các `QuotaBucket`; không trả token/email. Giữ null nếu không biết quota.
3. Đăng ký adapter trong collector Program, bổ sung provider vào danh sách lệnh IPC và cấu hình plugin.
4. Thêm kiểm thử parser/identity/failure, action và icon. Không thay đổi giao thức cho một provider mới nếu schema hiện tại đủ dùng.
5. Build, kiểm tra quota với trang Usage cùng tài khoản, rồi cập nhật plugin cục bộ.

Các tình huống đã có kiểm thử: đổi account giữa request, dữ liệu cũ, reset chờ xác nhận, Retry-After, dedup refresh, credential mismatch, IPC quá dài/JSON lỗi, trạng thái phím và thao tác giữ phím.

Sticky/tray và lịch sử dài hạn được để cho giai đoạn sau; collector/IPC hiện tại là nền dùng chung cho các giao diện đó.
