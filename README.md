# Notrelix

Notrelix là nền tảng SaaS quản lý công việc kết hợp:
- Notion-like: pages, block editor, nested content.
- Trello-like: board, list, card, drag & drop.
- Collaboration: workspace, member roles, comments, notifications.

## Kiến trúc tổng quan

- Frontend: Next.js App Router, TypeScript, Tailwind CSS, shadcn/ui, TanStack Query.
- Backend: ASP.NET Core 8, Clean Architecture (Domain / Application / Infrastructure / Web), MediatR, FluentValidation, EF Core.
- Database: PostgreSQL.
- Cache / security support: Redis (OTP, rate limit, JWT blacklist, cache primitives).
- Email: Resend API (gửi OTP quên mật khẩu, thông báo đổi mật khẩu).
- Reverse proxy: Nginx (qua Docker Compose).

## Cấu trúc thư mục

```txt
.
├── backend/
│   ├── TodoApp.Domain/
│   ├── TodoApp.Application/
│   ├── TodoApp.Infrastructure/
│   ├── TodoApp.Web/
│   └── TodoApp.Tests/
├── frontend/
│   ├── app/
│   ├── features/
│   ├── lib/
│   └── registry/
├── docker-compose.yml
├── docker-compose.dev.yml
├── docker-compose.prod.yml
└── nginx/
```

## Yêu cầu môi trường

- Docker + Docker Compose (khuyến nghị để chạy full stack).
- Hoặc chạy local:
  - .NET SDK 8.0+
  - Bun 1.2+ (frontend)
  - PostgreSQL 16
  - Redis 7

## Chạy nhanh bằng Docker (Development)

### 1) Chuẩn bị biến môi trường

Tạo `.env` từ `.env.dev` (hoặc dùng trực tiếp `--env-file .env.dev`):

```bash
cp .env.dev .env
```

Khuyến nghị cập nhật:
- `REDIS_PASSWORD`
- `POSTGRES_PASSWORD`
- `RESEND_API_KEY` (nếu muốn gửi email thật)

### 2) Khởi động services

```bash
docker compose --env-file .env.dev -f docker-compose.yml -f docker-compose.dev.yml up -d --build
```

### 3) Truy cập

- Frontend: `http://localhost:3000`
- Backend API: `http://localhost:8000`
- Nginx gateway (nếu bật): `http://localhost`
- pgAdmin (optional profile tools): `http://localhost:5050`

## Chạy local không Docker

### Backend

```bash
cd backend
dotnet restore TodoApp.Web/TodoApp.Web.csproj
dotnet run --project TodoApp.Web/TodoApp.Web.csproj
```

### Frontend

```bash
cd frontend
bun install
bun dev
```

## Cấu hình quan trọng

### Backend (`backend/TodoApp.Web/appsettings.json`)

Các section chính:
- `ConnectionStrings:TodoAppDb`
- `ConnectionStrings:Redis`
- `JwtSettings`
- `Email` (Resend)
- `Cors`

### Frontend (`frontend/.env.local`)

```env
NEXT_PUBLIC_API_URL=http://localhost:8000/api
NEXT_PUBLIC_LOCALE=en
```

## Auth & Forgot Password (OTP)

Flow hiện tại:
1. User nhập email tại `/forgot-password`.
2. Backend rate-limit request, tạo OTP 6 số trong Redis (TTL 10 phút), gửi email qua Resend.
3. User nhập OTP + mật khẩu mới.
4. Backend verify OTP, đổi mật khẩu, revoke toàn bộ session cũ.

API endpoints:
- `POST /api/auth/forgot-password`
- `POST /api/auth/reset-password`
- `POST /api/auth/login`
- `POST /api/auth/register`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/auth/me`

## Redis đang được dùng cho gì?

- OTP forgot password:
  - `TodoApp_otp:forgot-password:{email}`
- OTP attempts:
  - `TodoApp_otp:attempts:forgot-password:{email}`
- Rate limiting:
  - `TodoApp_ratelimit:forgot-password:{email}`
- JWT blacklist:
  - `TodoApp_jwt:blacklist:{jti}`
- Generic cache primitives qua `IRedisCacheService`.

## Email (Resend)

Notrelix dùng Resend SDK (REST API), không cần SMTP host/port.

Cần set `Email:ApiKey` hợp lệ:
- local appsettings
- hoặc env `Email__ApiKey`
- hoặc Docker env `RESEND_API_KEY` (được map vào `Email__ApiKey`).

Lưu ý:
- `FromEmail` cần là sender/domain hợp lệ theo Resend.
- Nếu API key không hợp lệ, backend sẽ lỗi gửi email.

## Scripts hữu ích

### Frontend

```bash
cd frontend
bun dev
bun build
bun lint
```

### Backend

```bash
cd backend
dotnet build TodoApp.Web/TodoApp.Web.csproj
dotnet run --project TodoApp.Web/TodoApp.Web.csproj
```

## Troubleshooting nhanh

### 401 từ Resend (API key invalid)

Kiểm tra thứ tự ưu tiên config:
1. Docker env `Email__ApiKey` (qua `RESEND_API_KEY`) có thể override appsettings.
2. `Email:ApiKey` trong appsettings.

Kiểm tra trong container:

```bash
docker compose exec backend printenv | rg Email__ApiKey
docker compose exec backend printenv | rg RESEND_API_KEY
```

### Frontend gọi sai API URL

Đảm bảo:
- `NEXT_PUBLIC_API_URL` trỏ đúng backend.
- Nếu dùng Docker gateway qua Nginx, cập nhật URL phù hợp.

## Bảo mật

- Không commit secrets thật vào git (`API keys`, `JWT secret`, `DB password`).
- Dùng `.env` và secret manager cho production.
- Rotate các credentials nếu đã từng lộ trong config.

## Roadmap ngắn hạn

- Realtime collaboration.
- More advanced automation/workflow.
- Audit/logging & observability tốt hơn.
- Hardening production config (secret store, health checks, tracing).