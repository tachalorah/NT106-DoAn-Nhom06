# GitHub Copilot Instructions for SecureChat Project

## Project Overview
- **Name:** SecureChat (Đồ án Lập trình mạng căn bản NT106)
- [cite_start]**Architecture:** N-tier Architecture (Client-Server-Shared)[cite: 130].
- [cite_start]**Tech Stack:** .NET 8, ASP.NET Core (Server), WinForms (Client), SignalR (Real-time), SQLite (Database)[cite: 130, 149, 158, 170].

## Repository Structure
- [cite_start]`SecureChat.Server`: Backend API và SignalR Hub[cite: 131].
- [cite_start]`SecureChat.Client`: Ứng dụng WinForms cho người dùng[cite: 171].
- [cite_start]`SecureChat.Shared`: Thư viện dùng chung cho các lớp Security và DTO[cite: 211].

## Coding Standards & Rules
1. [cite_start]**Asynchronous Programming:** Luôn sử dụng `async/await` cho các tác vụ I/O, gọi API hoặc truy vấn database[cite: 161].
2. **Security First:**
   - [cite_start]Mã hóa tin nhắn/file bằng **AES-256** (E2EE)[cite: 190].
   - [cite_start]Kiểm tra tính toàn vẹn bằng **SHA-256**[cite: 191, 207].
   - [cite_start]Hash mật khẩu bằng **Argon2**[cite: 161].
   - [cite_start]Xác thực qua **JWT Token**[cite: 145].
3. **Data Transfer:**
   - [cite_start]Luôn sử dụng **DTOs** để trao đổi dữ liệu giữa Client và Server[cite: 153].
   - [cite_start]Đảm bảo thuộc tính trong DTO ở Client và Server phải khớp hoàn toàn để tránh lỗi Serialization[cite: 188].
4. **Error Handling:**
   - [cite_start]Server: Sử dụng `ExceptionMiddleware` để bắt lỗi toàn cục và trả về JSON[cite: 167, 219].
   - [cite_start]Client: Sử dụng `frmError` hoặc `Helper` để hiển thị thông báo lỗi thân thiện[cite: 197].
5. **UI Rules (WinForms):**
   - Tách biệt logic xử lý ra khỏi các file `.Designer.cs`.
   - [cite_start]Sử dụng `Helpers/UIHelper.cs` để quản lý các thay đổi giao diện lặp lại[cite: 247].

## Specific Implementation Notes
- [cite_start]Khi sửa logic mã hóa, luôn tham chiếu đến các file trong `SecureChat.Shared/Security/`[cite: 211].
- [cite_start]SignalR Hub nằm tại `SecureChat.Server/Hubs/ChatHub.cs`[cite: 151].
- [cite_start]Mọi API call từ Client phải thông qua `ApiClient.cs`[cite: 185].
