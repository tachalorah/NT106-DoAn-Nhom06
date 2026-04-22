# SecureChat Project Instructions (E2EE Real-time Communication - MariaDB)

You are an expert senior .NET developer specializing in Network Programming, Cybersecurity, and WinForms/ASP.NET Core architecture. You are assisting in developing "SecureChat", a high-security real-time messaging system.

---

## 🛠 Tech Stack & Environment
- **Framework:** .NET 8.0 (Client & Server)
- **Backend:** ASP.NET Core Web API
- **Realtime:** SignalR Core for real-time communication
- **Frontend:** Windows Forms (WinForms)
- **Database:** MariaDB (MySQL-compatible)
- **ORM:** Entity Framework Core (Pomelo.EntityFrameworkCore.MySql)
- **Security:**
  - AES-256 (End-to-End Encryption)
  - Argon2id (Password Hashing)
  - SHA-256 (Integrity Check)
  - JWT (Authentication)
- **Multimedia:**
  - NAudio (Voice)
  - OpenCvSharp (Video)

---

## 🏗 Architectural Patterns

### N-Tier Architecture
- Presentation Layer: WinForms + Controllers
- Business Logic Layer: Services
- Data Access Layer: EF Core (MariaDB)

---

### Shared Project (`SecureChat.Shared`)
Used for:
- DTOs
- Constants
- AES encryption helpers
- SHA-256 utilities

👉 Must ensure identical logic between Client and Server.

---

### DTO Rules
- Always use DTOs for API communication
- Never expose Entity models directly

---

## 🔐 Critical Security Rules (NON-NEGOTIABLE)

### 1. End-to-End Encryption (E2EE)
- All messages (text, voice, file) MUST be encrypted on client side
- Server ONLY stores encrypted data (ciphertext)
- Server MUST NEVER access plaintext
- Decryption ONLY on receiving client

---

### 2. Password Security
- Use Argon2id
- Always use 16-byte salt
- Never store plaintext passwords

---

### 3. Data Integrity
- Use SHA-256 hashing for:
  - File transfers
  - Voice messages
- Detect tampering before decrypting

---

### 4. Authentication
- Use JWT tokens
- Store securely on client (`TokenStorage`)
- Attach token:
  `Authorization: Bearer <token>`

---

## 🗄 Database Context (MariaDB)

Database: `securechat`

### Users
- password_hash
- key_salt
- encryption_key (AES encrypted)

### Conversations
- Type:
  - 0 = Private
  - 1 = Group

### Messages
- conversation_id
- sender_id
- encrypted_content
- timestamp

### Friendships
- user_id
- friend_id
- status

### FriendRequests
- sender_id
- receiver_id
- status

### Groups
- group_id
- name
- created_by

### GroupMembers
- group_id
- user_id
- role

---

## ⚙️ EF Core Configuration (MariaDB)

Connection string:
**server=localhost;port=3306;database=securechat;user=root;password=YOUR_PASSWORD;**

Use Pomelo provider:
- Pomelo.EntityFrameworkCore.MySql
- Auto-detect server version

---

## 💻 Coding Standards

### Async/Await
- All I/O operations MUST be async

### WinForms Thread Safety
- Use Invoke / BeginInvoke for UI updates from background threads

### Naming Conventions
- PascalCase: classes, methods
- camelCase: variables
- Interfaces: prefix "I"

### Error Handling
- Server: ExceptionMiddleware
- Client: frmError

### Dependency Injection
- Use constructor injection only

---

## 📂 Project Structure

### Server
- Controllers/
- Hubs/
- Data/AppDbContext.cs
- Services/

### Client
- Security/
- Services/Api/
- Components/

---

## 🔄 System Flow

Client → Encrypt (AES-256) → Send → Server → Store (MariaDB) → Client → Decrypt → Display

---

## 🎯 Core Principles

- Client encrypts everything
- Server never sees plaintext
- MariaDB stores only encrypted data
- JWT handles authentication
- SHA-256 ensures integrity
- Argon2id secures passwords
