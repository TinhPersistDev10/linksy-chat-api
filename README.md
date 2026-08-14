# Linksy Backend API (`linksy_backend_api`)

ASP.NET Core 8 Web API for **Linksy** — realtime chat, friends, groups, calls, notifications, privacy, and system admin.

| Language | Section |
|----------|---------|
| English | [English](#english) |
| Tiếng Việt | [Tiếng Việt](#tiếng-việt) |

Parent folder overview: [`../README.md`](../README.md)

---

## English

### What is Linksy?

**Linksy** is a realtime messaging and social communication platform. People use it to stay connected with friends and groups: send messages (text, media, files, voice), react and poll in chats, make voice/video calls, manage friend requests, control who can message or call them, and get live notifications. System administrators use the same platform to manage users and monitor the service.

This repository is the **server side** that powers those features for the [Linksy frontend](../../linksy_frontend).

### Overview

This project is the runnable Linksy API:

- REST under `/api/v1/*`
- SignalR hub at `/hubs/chat`
- JWT in httpOnly cookies (`accessToken`, `refreshToken`) with DB revocation
- PostgreSQL + EF Core; optional Redis; Cloudinary media; email OTP (SMTP / Brevo)

### Tech stack

| Area | Technology |
|------|------------|
| Runtime | .NET 8 / ASP.NET Core |
| Data | EF Core 8, PostgreSQL (Npgsql) |
| Auth | JWT Bearer, BCrypt, cookie + refresh rotation |
| Realtime | SignalR |
| Cache | Redis (optional; in-memory fallback) |
| Media | Cloudinary, ImageSharp |
| Docs | Swagger / OpenAPI |
| Health | `/health` |

### Features

- Auth: register, email OTP, login/logout, refresh, forgot/reset/change password
- Users & avatars; friends (request / accept / reject / remove)
- Direct & group chatrooms; roles, permissions, invitations
- Messaging: text/media/file/voice, reply, edit, delete, pins, mentions, reactions, polls, delivery/read
- WebRTC call signaling + call logs
- Notifications (DB + SignalR), including friend avatar change
- Privacy: `WhoCanMessageMe` (`everyone` \| `friends`) for stranger DMs and 1-1 calls
- Block users; content moderation
- Admin API (`/api/v1/admin/*`, role `Admin`)

### Project structure

```
.
├── API/
│   ├── Controllers/     # Auth, Users, Friends, Messages, Chatrooms, Admin, …
│   └── Hubs/            # ChatHub (messages, typing, calls)
├── Domain/
│   ├── Entities/Models/
│   ├── DTOs/
│   └── Interfaces/
├── Infrastructure/
│   ├── Data/            # LinksyDbContext + configurations
│   ├── Migrations/
│   ├── Repositories/
│   └── Services/
├── Program.cs
├── Dockerfile
├── compose.yaml
└── appsettings*.json
```

Tests live in sibling project: [`../linksy_backend_api.Tests`](../linksy_backend_api.Tests)

### Prerequisites

| Component | Requirement |
|-----------|-------------|
| Backend | [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (see repo `global.json`) |
| Database | PostgreSQL 14+ running locally or remotely |
| Frontend | [Node.js 20+](https://nodejs.org/) and npm |
| Optional | Redis, Cloudinary, SMTP/Gmail or Brevo (email OTP), TURN server (calls behind NAT) |

Also install the EF Core CLI once (for migrations):

```bash
dotnet tool install --global dotnet-ef
```

### Configuration

Configure via `appsettings.Development.json`, [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets), or environment variables.

| Key | Purpose |
|-----|---------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL |
| `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` | JWT |
| `Jwt:AccessTokenExpirationMinutes` | Access TTL (default 60) |
| `Jwt:RefreshTokenExpirationDays` | Refresh TTL (default 7) |
| `Cors:AllowedOrigins` | Frontend origins (include `http://localhost:3000`) |
| `Email:*` or `Brevo:*` | OTP / notification mail |
| `Cloudinary:*` | Uploads |

Docker Compose env mapping: see [`compose.yaml`](./compose.yaml).  
**Do not commit real secrets.**

#### 1. Backend API

1. Create a PostgreSQL database (example name: `linksy`).
2. Open `appsettings.Development.json` in this folder and set at least:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=linksy;Username=postgres;Password=YOUR_PASSWORD"
  },
  "Jwt": {
    "Key": "a-long-secret-key-at-least-32-characters",
    "Issuer": "linksy",
    "Audience": "linksy"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://localhost:3000"
    ]
  }
}
```

3. (Recommended for local login/OTP) fill `Email:*` or `Brevo:*`, and `Cloudinary:*` if you need media uploads.
4. From this directory (`linksy_backend_api`):

```bash
dotnet restore
dotnet ef database update --context LinksyDbContext
dotnet run --launch-profile http
```

| Endpoint | URL |
|----------|-----|
| API | `http://localhost:5253` |
| Swagger | `http://localhost:5253/swagger` |
| Health | `http://localhost:5253/health` |
| SignalR | `ws://localhost:5253/hubs/chat` |

Leave this terminal running. CORS must allow the frontend origin with credentials.

#### 2. Frontend (Next.js)

Open a **second** terminal:

```bash
cd ../../linksy_frontend
cp .env.example .env.local
npm install
npm run dev
```

Edit `.env.local` so the API URL matches the backend:

```env
NEXT_PUBLIC_API_URL=http://localhost:5253/api/v1
NEXT_PUBLIC_STUN_URLS=stun:stun.l.google.com:19302,stun:stun1.l.google.com:19302
# Optional — needed for some WebRTC/NAT cases
NEXT_PUBLIC_TURN_URL=
NEXT_PUBLIC_TURN_USERNAME=
NEXT_PUBLIC_TURN_CREDENTIAL=
```

| App | URL |
|-----|-----|
| Frontend | `http://localhost:3000` |
| Login / register | `http://localhost:3000/login` |

#### 3. Quick check

1. Backend health: open `http://localhost:5253/health`
2. Swagger: `http://localhost:5253/swagger`
3. Frontend: open `http://localhost:3000`, register/login, open a chat

If the browser blocks cookies or API calls, confirm `Cors:AllowedOrigins` includes `http://localhost:3000` and `NEXT_PUBLIC_API_URL` points to `http://localhost:5253/api/v1`.

### Docker

```bash
# From this directory — set ASPNETCORE_ENVIRONMENT, DB_CONNECTION, JWT_*, EMAIL_*
docker compose up --build
```

Host port `5253` → container `8080`.

### Main API areas

| Prefix | Description |
|--------|-------------|
| `/api/v1/auth` | Register, login, OTP, refresh, password |
| `/api/v1/users` | Profile, search |
| `/api/v1/friends` | Friends & requests |
| `/api/v1/chatrooms` | Direct / group rooms |
| `/api/v1/messages` | Messages, attachments, polls, … |
| `/api/v1/notifications` | Notification inbox |
| `/api/v1/settings` | General / notifications / privacy / status |
| `/api/v1/avatar` | User & group avatars |
| `/api/v1/blockeduser` | Block list |
| `/api/v1/admin` | Admin user management |
| `/hubs/chat` | SignalR |

### Tests

```bash
cd ..
dotnet test linksy_backend_api.Tests/linksy_backend_api.Tests.csproj
```
## Tiếng Việt

### Linksy là gì?

**Linksy** là nền tảng nhắn tin và giao tiếp xã hội theo thời gian thực. Người dùng dùng Linksy để kết nối với bạn bè và nhóm: gửi tin nhắn (chữ, ảnh/media, file, thoại), thả reaction và bình chọn trong chat, gọi thoại/video, quản lý lời mời kết bạn, kiểm soát ai được nhắn tin hay gọi mình, và nhận thông báo realtime. Quản trị viên hệ thống dùng cùng nền tảng để quản lý người dùng và theo dõi dịch vụ.

Repo này là **phía máy chủ (API)** cung cấp các tính năng trên cho [frontend Linksy](../../linksy_frontend).

### Tổng quan

Project API chạy được của **Linksy**:

- REST tại `/api/v1/*`
- SignalR tại `/hubs/chat`
- JWT cookie httpOnly + thu hồi token trên DB
- PostgreSQL + EF Core; Redis tuỳ chọn; Cloudinary; OTP email

Frontend: [`../../linksy_frontend`](../../linksy_frontend)

### Công nghệ

| Thành phần | Công nghệ |
|------------|-----------|
| Runtime | .NET 8 / ASP.NET Core |
| Dữ liệu | EF Core 8, PostgreSQL (Npgsql) |
| Auth | JWT Bearer, BCrypt, cookie + refresh |
| Realtime | SignalR |
| Cache | Redis (tuỳ chọn) |
| Media | Cloudinary, ImageSharp |
| Docs | Swagger / OpenAPI |

### Tính năng chính

- Auth: đăng ký, OTP, đăng nhập/đăng xuất, refresh, quên/đổi mật khẩu
- User & avatar; bạn bè
- Chat 1-1 & nhóm; quyền thành viên; lời mời
- Tin nhắn đa loại; reaction; poll; đã gửi/đã đọc
- Signaling gọi thoại/video
- Thông báo realtime (kể cả bạn bè đổi avatar)
- Riêng tư người lạ: `WhoCanMessageMe`
- Chặn user; kiểm duyệt nội dung
- Admin hệ thống

### Cấu trúc

```
.
├── API/Controllers & Hubs
├── Domain/Entities, DTOs, Interfaces
├── Infrastructure/Data, Migrations, Repositories, Services
├── Program.cs
├── Dockerfile
└── compose.yaml
```

Unit test: [`../linksy_backend_api.Tests`](../linksy_backend_api.Tests)

### Yêu cầu

| Thành phần | Yêu cầu |
|------------|---------|
| Backend | [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (xem `global.json` ở root) |
| Database | PostgreSQL 14+ |
| Frontend | [Node.js 20+](https://nodejs.org/) và npm |
| Tuỳ chọn | Redis, Cloudinary, SMTP/Gmail hoặc Brevo (OTP email), TURN (gọi qua NAT) |

Cài EF Core CLI (một lần):

```bash
dotnet tool install --global dotnet-ef
```

### Cấu hình

Dùng `appsettings.Development.json`, User Secrets, hoặc biến môi trường. Xem [`compose.yaml`](./compose.yaml) khi chạy Docker.

| Key | Mục đích |
|-----|----------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL |
| `Jwt:*` | JWT |
| `Cors:AllowedOrigins` | Origin frontend (thêm `http://localhost:3000`) |
| `Email:*` / `Brevo:*` | Email OTP |
| `Cloudinary:*` | Upload media |

**Không** commit secret.

### Hướng dẫn cài đặt (backend + frontend)

Cấu trúc monorepo (từ thư mục gốc `chat_realtime`):

```
chat_realtime/
├── backend_api/linksy_backend_api/   ← API này
└── linksy_frontend/                 ← giao diện Next.js
```

#### 1. Backend API

1. Tạo database PostgreSQL (ví dụ: `linksy`).
2. Mở `appsettings.Development.json` trong thư mục này và cấu hình tối thiểu:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=linksy;Username=postgres;Password=YOUR_PASSWORD"
  },
  "Jwt": {
    "Key": "a-long-secret-key-at-least-32-characters",
    "Issuer": "linksy",
    "Audience": "linksy"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://localhost:3000"
    ]
  }
}
```

3. (Khuyến nghị khi chạy local) điền `Email:*` hoặc `Brevo:*` để nhận OTP; điền `Cloudinary:*` nếu cần upload ảnh/file.
4. Trong thư mục này (`linksy_backend_api`):

```bash
dotnet restore
dotnet ef database update --context LinksyDbContext
dotnet run --launch-profile http
```

| Endpoint | URL |
|----------|-----|
| API | `http://localhost:5253` |
| Swagger | `http://localhost:5253/swagger` |
| Health | `http://localhost:5253/health` |
| SignalR | `ws://localhost:5253/hubs/chat` |

Giữ terminal này chạy. CORS phải cho phép origin frontend kèm credentials.

#### 2. Frontend (Next.js)

Mở **terminal thứ hai**:

```bash
cd ../../linksy_frontend
cp .env.example .env.local
npm install
npm run dev
```

Chỉnh `.env.local` cho khớp backend:

```env
NEXT_PUBLIC_API_URL=http://localhost:5253/api/v1
NEXT_PUBLIC_STUN_URLS=stun:stun.l.google.com:19302,stun:stun1.l.google.com:19302
# Tuỳ chọn — dùng khi WebRTC/NAT cần TURN
NEXT_PUBLIC_TURN_URL=
NEXT_PUBLIC_TURN_USERNAME=
NEXT_PUBLIC_TURN_CREDENTIAL=
```

| Ứng dụng | URL |
|----------|-----|
| Frontend | `http://localhost:3000` |
| Đăng nhập / đăng ký | `http://localhost:3000/login` |

#### 3. Kiểm tra nhanh

1. Health backend: `http://localhost:5253/health`
2. Swagger: `http://localhost:5253/swagger`
3. Frontend: mở `http://localhost:3000`, đăng ký/đăng nhập, mở chat

Nếu trình duyệt chặn cookie hoặc gọi API lỗi, kiểm tra `Cors:AllowedOrigins` có `http://localhost:3000` và `NEXT_PUBLIC_API_URL` là `http://localhost:5253/api/v1`.

### Docker

```bash
docker compose up --build
```

Port `5253` → `8080`.

### Nhóm API

| Prefix | Mô tả |
|--------|--------|
| `/api/v1/auth` | Auth |
| `/api/v1/users` | User |
| `/api/v1/friends` | Bạn bè |
| `/api/v1/chatrooms` | Phòng chat |
| `/api/v1/messages` | Tin nhắn |
| `/api/v1/notifications` | Thông báo |
| `/api/v1/settings` | Cài đặt |
| `/api/v1/avatar` | Avatar |
| `/api/v1/blockeduser` | Chặn |
| `/api/v1/admin` | Admin |
| `/hubs/chat` | SignalR |

### Kiểm thử

```bash
cd ..
dotnet test linksy_backend_api.Tests/linksy_backend_api.Tests.csproj
```

### Tài liệu

- [`../AGENTS.md`](../AGENTS.md)
- [`../docs/admin-prd.md`](../docs/admin-prd.md)
- [`../docs/usecase-diagram.md`](../docs/usecase-diagram.md)
