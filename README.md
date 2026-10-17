# TOEIC Reading Exam App

Ứng dụng gồm website quản lý đề cho giảng viên được xây trên template ASP.NET Boilerplate ASP.NET Core MVC, API và ứng dụng Flutter cho sinh viên. Cả hai database dùng SQL Server Express `.\SQLEXPRESS`: `TOEIC` lưu dữ liệu nền của Boilerplate; `ToeicReading` lưu đề thi và bài làm.

## Yêu cầu môi trường

- .NET SDK 9
- SQL Server Express đang chạy trên máy, instance `SQLEXPRESS`
- Flutter SDK tương thích Dart `^3.13.2`

## Khởi tạo template và database

Mở PowerShell tại thư mục gốc. Chạy migrator một lần để tạo/cập nhật database nền:

```powershell
Push-Location TOEIC\TOEIC\aspnet-core\src\2380600189.Migrator
dotnet run -- -q
Pop-Location
```

Template dùng ASP.NET Boilerplate MVC, EF Core và SQL Server; các migration cho database đề thi được áp dụng khi website khởi động. Nếu SQL Server Express không dùng `.\SQLEXPRESS`, cập nhật connection strings trong `TOEIC\TOEIC\aspnet-core\src\2380600189.Web.Mvc\appsettings.json` và `TOEIC\TOEIC\aspnet-core\src\2380600189.Migrator\appsettings.json`. Nếu chạy MVC bằng Docker Compose, phải đặt biến môi trường `TOEIC_DB_CONNECTION_STRING` trước khi khởi động.

## Chạy backend

```powershell
Push-Location TOEIC\TOEIC\aspnet-core\src\2380600189.Web.Mvc
dotnet run --launch-profile TOEIC.Web
Pop-Location
```

Website giảng viên: `http://localhost:5000/`  
API danh sách đề đã xuất bản: `http://localhost:5000/api/exams`

## Chạy ứng dụng Flutter

```powershell
cd Frontend
flutter pub get
flutter run
```

Ứng dụng Android Emulator mặc định dùng `http://10.0.2.2:5000/api`. Khi chạy Flutter Web, chỉ định địa chỉ backend trên máy:

```powershell
flutter run -d chrome --dart-define=API_BASE_URL=http://localhost:5000/api
```

## Import và chỉnh sửa đề

Giảng viên chọn **Import a Word exam** và tải lên DOCX theo cấu trúc thẻ mà `TaggedExamParser` kiểm tra. File phải có đủ 100 câu đánh số liên tục từ 101 đến 200, chia Part 5/6/7, đủ lựa chọn A-D, đáp án và hint. Các thuộc tính `shuffleQuestions`, `shuffleAnswers` ở cấp Part/Passage và `shuffleChoices` ở cấp câu hỏi được áp dụng khi tạo đề cho sinh viên. Sau khi import, dùng **Preview** để xem hoặc **Edit** để chỉnh tiêu đề, thời lượng, bài đọc, câu hỏi, đáp án và hint. Có thể xuất bản đề để ứng dụng sinh viên nhìn thấy.

Importer đọc văn bản và ảnh PNG/JPEG/GIF/WebP nhúng trong nội dung DOCX. Ảnh được giữ cùng nội dung câu hỏi/lựa chọn/đoạn đọc, hiển thị trong preview của giảng viên và ứng dụng sinh viên; định dạng ảnh không hỗ trợ sẽ được báo lỗi khi import. Theo phạm vi bài tập, website và endpoint quản lý đề không yêu cầu đăng nhập; chỉ chạy trong môi trường phát triển tin cậy, không đưa lên môi trường công khai.

Tài liệu đề thi Word/PDF được cung cấp riêng và không nằm trong kho mã nguồn công khai. Có thể tạo dữ liệu đề minh họa với nội dung placeholder bằng `python scripts\generate_demo_exam.py`; các file DOCX/PDF đầu vào hoặc đề đã nhập không được đưa lên repository.

## Kiểm tra

```powershell
dotnet test Backend.Tests\Backend.Tests.csproj
cd Frontend
flutter analyze
flutter build web
```

## Thiết kế

Figma: [TOEIC Reading Exam App](https://www.figma.com/design/Sb8SFBHVFlzdWDqeyI7Tnn)

Sơ đồ cơ sở dữ liệu: [docs/database-schema.svg](docs/database-schema.svg)

> Lưu ý: project template ASP.NET Boilerplate được cung cấp trong `TOEIC\TOEIC\aspnet-core`. Tên project mẫu ban đầu là chuỗi số, đã đổi các namespace/assembly references thành `TOEIC` hợp lệ để solution build được. Phần đăng nhập mặc định của template không được yêu cầu để dùng dashboard giảng viên. Trước khi triển khai, cấu hình khóa ký JWT riêng bằng biến môi trường `Authentication__JwtBearer__SecurityKey`; không dùng khóa mẫu.
