# AI Usage Deck — bản thiết kế đề xuất

Ngày: 15/11/2026. Trạng thái: đề xuất để người dùng duyệt trước triển khai.

Đã qua một lượt kiểm tra độc lập về khả năng lập kế hoạch triển khai. Sau lượt kiểm tra, người dùng xác nhận thiết bị Elgato x Discord, bố cục 3 cột × 2 hàng; đề xuất bố cục sáu phím bên dưới vẫn chờ duyệt cùng thiết kế. Các mốc kiểm chứng kỹ thuật được ghi rõ dưới đây.

## Mục đích và quyết định đã có

Xây công cụ riêng cho một người dùng Windows, tập trung hạn mức AI còn lại và thời điểm reset. Stream Deck là nơi quan sát chính. Cấu trúc cho phép thêm dịch vụ AI về sau. Người dùng chọn tự xây vì muốn chủ động bảo mật và phát triển, thay vì dùng hoặc fork ứng dụng theo dõi có sẵn.

Tài khoản ban đầu: Claude Team, Codex Plus, Antigravity Pro; mỗi dịch vụ một tài khoản hiện tại. Claude có thể đổi sang tài khoản khác khi hết hạn mức. Công cụ phải theo đúng tài khoản thực tế, không tự chuyển tài khoản hay thay đổi đăng nhập của các CLI.

## Hiện trạng đã kiểm tra

- agy đã được cập nhật bằng trình cập nhật tích hợp: 1.0.16 → 1.5.9; chạy tiến trình mới xác nhận phiên bản và hỗ trợ xuất JSON.
- `agy -p /usage --output-format json --print-timeout 30s` trả SUCCESS, command.name=usage, num_turns=0 và mọi bộ đếm token đều bằng 0.
- Báo cáo agy có command.data.groups[].buckets[]; bucket có id, name, window, remaining_fraction, reset_time. Có nhiều nhóm quota, không chỉ một tổng phần trăm cho toàn dịch vụ.
- Codex CLI cài đặt: 0.146.0, có app-server. Chưa thử đọc rateLimits của tài khoản thực tế.
- Claude Code cài đặt: 2.1.296. Cấu hình có metadata tài khoản/tổ chức; chưa có cachedUsageUtilization tại thời điểm kiểm tra. Chưa kiểm chứng cách lấy quota Team hiện tại.
- Stream Deck software đang chạy: 7.6.1.23033. Người dùng xác nhận thiết bị Elgato x Discord, kích thước 3 cột × 2 hàng (sáu phím). Người dùng chưa chỉ định số phím muốn dành; bố cục đề xuất có thể dùng trọn một trang sáu phím.
- Chưa cài plugin, sửa profile Stream Deck, sửa cấu hình CLI, hay thay đổi quyền khởi động cùng Windows.

## Phạm vi bản đầu

1. Bộ thu thập chạy dưới tài khoản Windows hiện tại; ba adapter Claude, Codex, Antigravity.
2. Plugin Stream Deck cài cục bộ, có action Usage và Refresh. Mỗi action Usage cấu hình được dịch vụ, tài khoản đang hoạt động, nhóm quota và cửa sổ thời gian.
3. Nhận diện tài khoản Claude thay đổi; chống gắn quota cũ vào tài khoản mới.
4. Refresh tự động, trạng thái freshness, báo lỗi và đếm ngược reset.
5. Chỉ lưu cấu hình và snapshot quota cần thiết trên máy; không thu thập nội dung chat hay mã nguồn.

Cửa sổ sticky, tray đầy đủ, lịch sử dài hạn và dự báo hết quota thuộc giai đoạn sau, sau khi ba nguồn usage và Stream Deck hoạt động đúng. Bản đầu có trang cấu hình action trong Stream Deck và lệnh chẩn đoán tối thiểu cho bộ thu thập.

## Các cách đã xem xét

| Cách | Đánh đổi | Quyết định |
| --- | --- | --- |
| Dùng hoặc mở rộng công cụ sẵn có | Nhanh nhưng phụ thuộc mã nguồn và hành vi lấy dữ liệu của dự án khác | Người dùng chọn tự xây |
| Đặt toàn bộ collector trong plugin Stream Deck | Ít tiến trình nhưng gắn việc thu thập với vòng đời Stream Deck và khó dùng chung cho sticky/tray | Không chọn cho kiến trúc đề xuất |
| Collector độc lập + plugin hiển thị | Thêm một thành phần nhỏ, đổi lại dễ mở rộng, kiểm thử và dùng chung dữ liệu | Lựa chọn kỹ thuật đề xuất của người xây, chờ duyệt |

## Kiến trúc đề xuất

Collector: C# trên phiên bản .NET LTS phù hợp với môi trường, ưu tiên .NET 10 sau khi kiểm tra SDK. Chạy như tiến trình người dùng, không phải Windows service chạy quyền hệ thống. Plugin: TypeScript, SDK chính thức @elgato/streamdeck, chọn phiên bản SDK/runtime tương thích Stream Deck 7.6.1 đang cài.

Collector quản lý vòng đời adapter, lịch refresh, identity và snapshot. Plugin nhận snapshot đã lọc, vẽ ảnh/chữ trên phím và gửi yêu cầu refresh. Thêm dịch vụ mới bằng adapter có mã nguồn trong dự án và đăng ký rõ ràng; bản đầu không tải và chạy plugin/adapter từ Internet hay cho phép nhập shell command tùy ý trong cấu hình.

Giao tiếp qua Windows named pipe giới hạn cho người dùng Windows hiện tại. Giao thức có version, giới hạn kích thước thông điệp và danh sách lệnh cho phép: lấy snapshot, theo dõi thay đổi, yêu cầu refresh. Không mở cổng LAN/Internet. Nhiều phím cùng theo dõi một nguồn chỉ tạo một lịch thu thập. Yêu cầu refresh từ plugin được giới hạn tần suất.

Không yêu cầu quyền administrator. Điều này giảm quyền và đường truyền credential, không tạo ra ranh giới chống một tiến trình độc hại khác đã chạy dưới cùng tài khoản Windows.

## Mô hình dữ liệu

Snapshot được định danh bằng provider + account ID + organization/workspace ID khi nguồn cung cấp + quota group ID + window ID. Không dùng email hay tên model làm khóa duy nhất.

Mỗi bucket lưu: tên hiển thị, remainingPercent nếu biết, thời lượng cửa sổ nếu biết, resetAt nếu biết, observedAt, lastAttemptAt, source, trạng thái và mã lỗi đã lọc. Giữ nguyên độ chính xác nguồn trả về và chỉ làm tròn khi hiển thị. Một cửa sổ không được trả về là unavailable; không tự coi là 0%, 100% hay suy ra từ bộ đếm token. Không cộng phần trăm giữa những nhóm quota độc lập.

Trạng thái tối thiểu: fresh, stale, needsLogin, rateLimited, unavailable, accountChanged/unverified. Lần refresh thất bại không cập nhật observedAt của snapshot cũ.

## Nguồn dữ liệu theo dịch vụ

### Antigravity

Ưu tiên lệnh /usage JSON vừa kiểm chứng, chạy từ đường dẫn CLI cố định và truyền arguments trực tiếp, không qua shell. Đọc command.data.groups[].buckets[] và chuẩn hóa theo window thực tế. Đặt timeout, giới hạn output, chỉ dừng subprocess do collector tạo. Tắt auto-update cho subprocess probe nếu CLI hỗ trợ; không thay đổi thiết lập CLI người dùng.

Báo cáo hiện kiểm tra không có identity rõ ràng: chỉ gắn với nguồn CLI đang hoạt động, không hứa theo dõi nhiều tài khoản Google riêng biệt. Khi phát hiện đăng nhập thay đổi mà không xác minh được identity, xóa liên kết snapshot đang hiển thị và đọc lại. Muốn hỗ trợ nhiều tài khoản Google trong tương lai cần một cơ chế identity được kiểm chứng riêng.

### Codex

Dùng app-server và giao thức account/read, account/rateLimits/read; tận dụng account/rateLimits/updated khi có. Không gửi prompt hay tạo thread AI để đo usage. Đọc độ dài cửa sổ thực tế, không hardcode primary=5 giờ hoặc secondary=tuần. Hỗ trợ nhiều limit_id nếu server trả về. Sự kiện chỉ bổ sung cho lịch đọc định kỳ, không được giả định nhận mọi thay đổi phát sinh ở ứng dụng khác.

Tái sử dụng đăng nhập do Codex quản lý; collector không tự thực hiện OAuth refresh song song hoặc đăng xuất tài khoản. Chỉ kết nối/khởi chạy app-server với transport cục bộ cần thiết; kiểm tra và giảm hành vi khởi động không liên quan.

### Claude Team — mốc kiểm chứng bắt buộc

Không dựa vào rate_limits của statusline cho Team. Trước tiên kiểm chứng đường đọc quota chính CLI /usage hỗ trợ ở phiên bản đang cài, gồm định dạng, identity và khả năng chạy không phát sinh lượt model. Không dùng prompt giả hoặc đọc token log để ước lượng quota.

Ưu tiên nguồn do CLI cung cấp và xác minh cache thực tế. Nếu không có đường đọc máy ổn định, thử có giới hạn endpoint usage chỉ đọc mà CLI đang sử dụng, bằng access token hiện có trong credential store, giữ token trong bộ nhớ, không sao chép refresh token hoặc tự xoay token. Endpoint này không phải API công khai được bảo đảm ổn định; adapter phải cô lập thay đổi và dừng với trạng thái unavailable khi không kiểm chứng được. Không dùng cookie trình duyệt làm fallback âm thầm.

Mốc này chỉ đạt khi một lần đọc mới khớp trang Usage hoặc /usage của đúng tài khoản/tổ chức Team. Nếu chưa đạt, báo kết quả và giới hạn cụ thể trước khi hứa hỗ trợ đủ ba dịch vụ. Có thể phát triển phần còn lại bằng fixture được ghi rõ là dữ liệu thử, nhưng không đưa fixture vào đường hiển thị usage thật.

Khi accountUuid hoặc organizationUuid thay đổi: tăng generation của adapter, hủy/loại kết quả request cũ đang chạy, bỏ snapshot đang liên kết tài khoản cũ và lấy snapshot mới. Không gộp lịch sử giữa tài khoản hoặc tổ chức. Bản đầu theo dõi tài khoản CLI hiện tại; không tự đăng nhập hay swap thay người dùng.

## Hành vi Stream Deck

Thiết kế độc lập số phím: người dùng kéo thả action vào vị trí muốn dùng. Không ghi đè profile hiện tại. Với thiết bị Elgato x Discord sáu phím đã được xác nhận, đề xuất dùng một trang theo bố cục ba cột, hai hàng:

| Vị trí | Cột 1 | Cột 2 | Cột 3 |
| --- | --- | --- | --- |
| Hàng trên | Claude — 5 giờ | Codex — 5 giờ | Antigravity — 5 giờ |
| Hàng dưới | Claude — tuần | Codex — tuần | Antigravity — tuần |

Mỗi phím hiện tên dịch vụ/nhóm và % còn lại cho cửa sổ được chọn. Nhãn 5 giờ/tuần chỉ áp dụng khi nguồn thực sự trả về cửa sổ tương ứng. Đối với nguồn không có cửa sổ đó, hiện không có dữ liệu thay vì ánh xạ tùy tiện. Đây là bố cục đề xuất của người xây; không mặc định người dùng đã đồng ý dành cả sáu phím. Người dùng vẫn có thể đặt ba action dạng chuyển cửa sổ trên trang hiện có.

Phím Usage hiển thị tên dịch vụ/nhóm, % còn lại với nhãn cửa sổ rõ ràng. Với hai cửa sổ 5 giờ/tuần và đủ không gian, hiển thị cả hai; nếu không đủ, cấu hình mỗi phím cho một cửa sổ. Antigravity có nhiều nhóm: chọn nhóm trong cấu hình action hoặc chuyển giữa các nhóm khi bấm. Không hiện phần trăm tổng không có nghĩa.

Bố cục sáu phím không cần phím Refresh riêng: bấm ngắn tạm hiển thị thời gian còn lại đến reset trên phím đó; giữ phím ít nhất 700 ms yêu cầu refresh dịch vụ tương ứng, chỉ một lần cho mỗi lần giữ, qua bộ giới hạn tần suất chung. Hai phím Antigravity dùng cùng nhóm quota đã chọn trong cấu hình, không cộng các nhóm. Bản đầu đổi nhóm trong phần cấu hình Stream Deck để tránh thêm thao tác ẩn trên sáu phím; hỗ trợ chuyển nhóm bằng phím có thể bổ sung sau. Chế độ ba phím tùy chọn cho phép bấm ngắn chuyển cửa sổ 5 giờ/tuần; mỗi action chỉ có một chế độ bấm được định nghĩa rõ trong cấu hình. Action Refresh riêng vẫn có thể kéo thả nếu người dùng muốn dùng trang khác. Có thể thêm mở trang Usage bằng action riêng về sau.

Màu khởi đầu theo phần trăm còn lại: trên 30% bình thường, 10–30% cảnh báo, dưới 10% nghiêm trọng; có chữ/ký hiệu bên cạnh màu. Dữ liệu cũ có ký hiệu stale; mất collector hiện offline; hết đăng nhập hiện login; không dùng 0% để thay thế lỗi. Không đưa email đầy đủ, token hay organization ID lên phím hoặc vào settings của plugin.

Bố cục sáu phím ở trên phù hợp kích thước phần cứng người dùng cung cấp. Việc dành toàn bộ trang cho usage hoặc chỉ đặt ba phím trên trang hiện tại là tùy chọn bố trí, không thay đổi kiến trúc thu thập.

## Refresh và reset

Giá trị khởi đầu: mỗi nguồn tối đa một request đang chạy, polling 120 giây khi có phím hiển thị; giảm xuống 5 phút khi không có consumer đang dùng, hoặc dừng nếu người dùng chọn. Claude có thể cần lịch chậm hơn tùy rate limit thực tế; cấu hình riêng từng adapter. Sự kiện CLI được gom và có khoảng chờ tối thiểu giữa các lần gọi backend.

Refresh khi có mạng lại, máy thức dậy, nhận diện tài khoản thay đổi, người dùng bấm Refresh, và sau thời điểm reset nguồn đã báo. Countdown cập nhật tại chỗ, không gọi backend mỗi giây. Đến resetAt phải đọc lại; chưa xác nhận thì hiện đang xác minh reset, không tự đặt 100%.

Tôn trọng Retry-After, exponential backoff có jitter và giới hạn tối đa. Cấu hình mặc định stale sau hai chu kỳ refresh thường lệ không có dữ liệu mới; trạng thái rateLimited/needsLogin vẫn được hiển thị cùng tuổi snapshot. Lỗi xác thực không được tự mở browser hay gửi prompt; chỉ hướng dẫn người dùng đăng nhập lại bằng CLI.

## Lưu trữ và bảo mật

Chỉ lưu cấu hình action, nhãn người dùng đặt và snapshot quota đã lọc. File snapshot có ACL giới hạn người dùng, ghi atomic, giữ observedAt gốc và mặc định chỉ bản gần nhất, chưa có lịch sử dài hạn. Account ID có thể lưu dạng định danh hash cục bộ khi không cần giá trị gốc. Không lưu token, cookie, transcript hay mã nguồn trong cache, log, gói plugin và repository.

Nếu về sau cần lưu credential riêng, phải dùng Windows Credential Manager/DPAPI và có thiết kế bổ sung; bản đầu dùng phiên đăng nhập do CLI quản lý. Logger chỉ ghi danh sách trường cho phép, không ghi nguyên stdout/stderr của subprocess, request header hay JSON credential. Không có telemetry, upload dữ liệu hay dịch vụ cloud của công cụ.

Tự xây cho phép kiểm tra và kiểm soát mã nguồn; không tự nó bảo đảm tuyệt đối bảo mật. Chọn dependency tối thiểu, khóa phiên bản, kiểm tra dependency khi xây bản phát hành. Không cần phát hành lên Elgato Marketplace cho bản dùng cá nhân.

## Kiểm chứng trước nghiệm thu

- Đọc quota thật của từng dịch vụ, đối chiếu nguồn Usage tại thời điểm gần nhau và cùng tài khoản/tổ chức, giải thích sai khác do làm tròn hoặc độ trễ backend.
- Antigravity /usage tiếp tục báo không có lượt model/token; Codex chỉ gọi account methods. Claude chỉ chấp nhận đường thu thập đã xác minh không gửi lượt model.
- Kiểm thử chuẩn hóa remaining_fraction/usedPercent, trường thiếu, window không phải 5 giờ, ngày giờ reset và giá trị ngoài miền hợp lệ.
- Kiểm thử đổi Claude account trong khi request cũ chưa xong; kết quả cũ không được xuất hiện trên tài khoản mới.
- Kiểm thử snapshot cũ, mất mạng, 429, hết đăng nhập, sleep/resume và reset chưa được server xác nhận.
- Cắm/rút Stream Deck, chuyển trang, khởi động lại collector/plugin: hiển thị đúng trạng thái; nhiều phím không nhân số request.
- Kiểm tra gói phát hành/cache/log không chứa credential; named pipe từ chối người dùng Windows khác.
- Cài action vào vị trí người dùng chọn, giữ nguyên các profile/action hiện có.

## Thứ tự thực hiện sau khi thiết kế được duyệt

1. Kiểm chứng nguồn Claude Team, Codex account methods và cách nhận diện tài khoản. Lưu fixture đã loại bỏ dữ liệu nhạy cảm cho kiểm thử.
2. Xây collector và hợp đồng snapshot/IPC, kiểm thử các tình huống ảnh hưởng độ chính xác.
3. Xây plugin Stream Deck, cấu hình action, phần trăm/reset và trạng thái lỗi.
4. Đóng gói cài cục bộ, thử trên thiết bị thật, tài liệu chạy/dừng/gỡ và cách thêm adapter.
5. Bổ sung sticky/tray nếu người dùng vẫn cần sau khi dùng Stream Deck.

## Các lựa chọn còn lại do người xây quyết định

Tên dự án/UUID plugin tạm thời, cấu trúc repository, framework test, SVG vẽ phím, schema IPC cụ thể và vị trí thư mục dữ liệu theo chuẩn Windows sẽ được chốt trong kế hoạch triển khai. Không yêu cầu người dùng chọn những chi tiết này.

## Tài liệu đối chiếu

- https://learn.chatgpt.com/docs/app-server
- https://code.claude.com/docs/en/statusline#rate-limit-usage
- https://antigravity.google/docs/cli/commands/usage/
- https://antigravity.google/docs/changelog
- https://docs.elgato.com/streamdeck/sdk/introduction/plugin-environment/
- https://docs.elgato.com/streamdeck/sdk/guides/devices/
