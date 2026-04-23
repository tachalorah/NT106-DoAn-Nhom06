# 🛡️ SecureChat - E2EE Real-time Communication System

> Đồ án Lập trình mạng căn bản - Xây dựng hệ thống Chat bảo mật cao với cơ chế Mã hóa đầu cuối (E2EE), Kiểm tra toàn vẹn dữ liệu và Giao tiếp thời gian thực.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp)
![MariaDB](https://img.shields.io/badge/MariaDB-003545?style=for-the-badge&logo=mariadb)
![SignalR](https://img.shields.io/badge/SignalR-RealTime-blue?style=for-the-badge)

## 📖 Giới thiệu (Overview)
**SecureChat** là ứng dụng nhắn tin Desktop (Client-Server) tập trung vào quyền riêng tư và bảo mật dữ liệu. Hệ thống đảm bảo mọi tin nhắn, tệp tin và âm thanh đều được mã hóa trước khi truyền tải, đồng thời tích hợp các cơ chế chống tấn công đánh cắp tài khoản và kiểm tra toàn vẹn dữ liệu.

## ✨ Tính năng nổi bật (Key Features)

### 🔒 1. Giải pháp Bảo mật & Xác thực
* **Mã hóa đầu cuối (E2EE):** Tin nhắn văn bản, Voice và File được mã hóa bằng **AES-256** ngay tại Client trước khi gửi. Server chỉ lưu trữ dữ liệu dạng Ciphertext.
* **Bảo vệ Mật khẩu:** Chống tấn công Brute-force/Rainbow Tables bằng thuật toán băm **Argon2id** kết hợp Salt (16 bytes).
* **Kiểm tra Toàn vẹn (Integrity Check):** Tự động tính toán và đối chiếu mã băm **SHA-256** khi tải File/Voice để phát hiện dữ liệu bị can thiệp.
* **Xác thực 2 lớp (2FA):** Hỗ trợ gửi mã OTP qua Email cho các tác vụ quan trọng.

### 💬 2. Giao tiếp Thời gian thực (Real-time Communication)
* Nhắn tin cá nhân (1-1) và Nhắn tin nhóm (Group Chat) độ trễ thấp thông qua **SignalR Core**.
* Hiển thị trạng thái Online/Offline, "Đang gõ...".
* Phân quyền Quản trị viên nhóm (Admin): Kick, Mute thành viên.

### 📞 3. Đa phương tiện (Multimedia)
* **Voice Message:** Ghi âm, mã hóa và phát lại trực tiếp trên ứng dụng.
* **Video Call:** Cuộc gọi hình ảnh thời gian thực (P2P Streaming) trong mạng LAN.

## 🛠️ Công nghệ sử dụng (Tech Stack)

**Backend (Server)**
* **Framework:** ASP.NET Core Web API (.NET 8)
* **Real-time:** SignalR Core
* **Database & ORM:** MariaDB + Entity Framework Core (Pomelo)
* **Authentication:** JWT (JSON Web Token)

**Frontend (Client)**
* **Framework:** Windows Forms (.NET 8)
* **Multimedia:** `NAudio` (Xử lý âm thanh), `OpenCvSharp4.Windows` (Xử lý Camera)
* **Cryptography:** `Konscious.Security.Cryptography.Argon2`, `System.Security.Cryptography`

## 🚀 Hướng dẫn Cài đặt (Getting Started)

### Yêu cầu hệ thống (Prerequisites)
* [Visual Studio 2022](https://visualstudio.microsoft.com/) (Có cài đặt workload ASP.NET Web và .NET Desktop)
* .NET 8.0 SDK
