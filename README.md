# 🛡️ SecureChat - E2EE Real-time Communication System

> Đồ án Lập trình mạng căn bản - Hệ thống chat bảo mật cao với cơ chế Mã hóa đầu cuối (E2EE), Kiểm tra toàn vẹn dữ liệu và Giao tiếp thời gian thực.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp)
![MariaDB](https://img.shields.io/badge/MariaDB-003545?style=for-the-badge&logo=mariadb)
![SignalR](https://img.shields.io/badge/SignalR-RealTime-blue?style=for-the-badge)

---

## 📖 Giới thiệu (Overview)

**SecureChat** là ứng dụng nhắn tin Desktop theo mô hình Client-Server, tập trung vào **bảo mật và quyền riêng tư dữ liệu**.

Hệ thống đảm bảo:
- Mọi tin nhắn đều được mã hóa trước khi truyền đi
- Server không thể đọc nội dung gốc
- Có cơ chế kiểm tra toàn vẹn dữ liệu và xác thực người dùng

---

## ✨ Tính năng nổi bật (Key Features)

### 🔒 1. Bảo mật & Xác thực
- **Mã hóa đầu cuối (E2EE):** AES-256 mã hóa tại Client trước khi gửi, Server chỉ lưu ciphertext.
- **Băm mật khẩu an toàn:** Argon2id + Salt (16 bytes).
- **Kiểm tra toàn vẹn dữ liệu:** SHA-256 phát hiện chỉnh sửa file/voice.
- **Xác thực JWT:** Quản lý phiên đăng nhập an toàn.
- **2FA (OTP Email):** Xác thực hai lớp khi đăng nhập hoặc reset mật khẩu.

---

### 💬 2. Giao tiếp thời gian thực
- Nhắn tin 1-1 và nhóm qua **SignalR Core**
- Trạng thái online/offline
- “Đang nhập...” (typing indicator)
- Quản lý nhóm: thêm/xóa thành viên, phân quyền Admin

---

### 📞 3. Đa phương tiện
- 🎤 Voice Message (ghi âm + mã hóa AES-256)
- 📁 File Transfer an toàn (kèm SHA-256 integrity check)
- 📹 Video Call (OpenCvSharp + streaming LAN)

---

## 🛠️ Tech Stack

### 🖥️ Backend (Server)
- ASP.NET Core Web API (.NET 8)
- SignalR Core (Realtime)
- Entity Framework Core
- **Database: MariaDB**
- JWT Authentication

### 💻 Client (WinForms)
- Windows Forms (.NET 8)
- NAudio (Audio processing)
- OpenCvSharp4 (Camera streaming)
- AES-256 Encryption
- SHA-256 Integrity Check

---

## 🗄️ Database

- **MariaDB (MySQL-compatible)**
- ORM: Entity Framework Core (Pomelo Provider)

### Main Tables:
- Users
- Messages
- Conversations
- Friendships
- FriendRequests
- Groups
- GroupMembers

---

## 🚀 Hướng dẫn Cài đặt (Getting Started)

### Yêu cầu hệ thống (Prerequisites)
* [Visual Studio 2022](https://visualstudio.microsoft.com/) (Có cài đặt workload ASP.NET Web và .NET Desktop)
* .NET 8.0 SDK
