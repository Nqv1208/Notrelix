---
document_id: INFRA-ORACLE-VM-RUNBOOK
document_type: infrastructure-runbook
status: active
owner: infrastructure
applies_to:
  - staging
  - deployment
  - single-host-runtime
evidence:
  - infra/oracle/bootstrap.sh
  - infra/oracle/Caddyfile
  - docker-compose.staging.yml
  - .github/workflows/deploy.yml
  - .github/workflows/release.yml
  - .github/workflows/ci.yml
  - delivery/environments.toml
review_on:
  - staging-host-change
  - deployment-adapter-change
---

# Runbook: staging demo trên một VM Oracle Always Free (ARM A1)

Mục tiêu: chạy bản staging của Notrelix trên một VM Ubuntu 24.04 aarch64 (Singapore, khoảng 2 OCPU và 12 GB RAM), voice/video chạy qua LiveKit Cloud (không có media server trên VM). Phần dưới liệt kê việc owner phải tự làm; phần còn lại do repo lo (workflow, overlay, script bootstrap).

## 1. Luồng tổng thể

1. Cắt nhánh `release/X.Y` từ `main` rồi push. Push vào `main` chỉ chạy CI, không deploy. `Notrelix CI` trên nhánh release chạy xong; nếu thay đổi có mã triển khai được thì các job `release-*` build image amd64 và arm64, chạy stack thử trên đúng các digest đó và ký manifest ứng viên.
2. `release.yml` tải ứng viên, kiểm chữ ký rồi gọi `deploy.yml` với Environment `staging`.
3. `deploy.yml` đăng nhập SSH bằng user `deploy`, kéo image theo digest, sao lưu Postgres vào `backups/pre-migrate/`, chạy `--migrate` và `--rls-apply`, bật stack, kiểm `/health/live`. Lỗi trước khi đổi schema thì tự rollback về digest trước. Mỗi lần deploy ghi một dòng vào `deploy.log`.
4. Caddy trên VM giữ TLS (Let's Encrypt) và proxy vào nginx của stack ở `127.0.0.1:8080`.

Production không nằm trong runbook này: dùng `promote-release.yml` bằng tay, sau khi Environment `production` có người duyệt bắt buộc.

## 2. Việc owner làm bằng tay (theo thứ tự)

### 2.1. Oracle Console và DNS

1. Tạo VM `VM.Standard.A1.Flex` (Ubuntu 24.04, vùng Singapore, 2 OCPU, 12 GB). Nếu báo hết capacity thì thử lại hoặc đổi availability domain.
2. Đĩa boot 100 đến 150 GB. Gán reserved public IP để IP không đổi khi dựng lại VM.
3. Security List (hoặc NSG) cho phép ingress TCP 22 (nên giới hạn theo IP của bạn), 80 và 443. Không mở cổng nào khác.
4. Trỏ bản ghi A `demo.<domain>` về IP đó (thêm AAAA nếu dùng IPv6).
5. Tạo một cặp khóa ed25519 riêng cho deploy (không dùng lại khóa cá nhân): `ssh-keygen -t ed25519 -f notrelix-deploy -C notrelix-deploy`. Khóa công khai đưa vào bootstrap, khóa bí mật vào GitHub (mục 2.3).

### 2.2. Chạy bootstrap trên VM

SSH vào VM bằng tài khoản mặc định, rồi:

```bash
git clone https://github.com/Nqv1208/Notrelix.git /tmp/notrelix-bootstrap
cd /tmp/notrelix-bootstrap
sudo NOTRELIX_DOMAIN=demo.<domain> \
     DEPLOY_SSH_PUBKEY="$(cat /đường/dẫn/notrelix-deploy.pub)" \
     ACME_EMAIL=<email> \
     bash infra/oracle/bootstrap.sh
```

Script chạy lại được nhiều lần. Nó cài Docker Engine và compose plugin, tạo user `deploy` (không phải root, nhóm `docker`), khóa SSH chỉ cho key (không mật khẩu, không root), mở 22/80/443 trong `iptables`, bật fail2ban, unattended-upgrades (có reboot lúc 04:00), swap 4 GB, giới hạn journald 500 MB, clone repo vào `/opt/notrelix`, tạo file env rỗng `/opt/notrelix/.env.staging` (chmod 600) và cài Caddy với Let's Encrypt. Cuối script in checklist PASS/FAIL; chạy lại riêng checklist bằng `sudo bash infra/oracle/bootstrap.sh --verify`.

Lưu ý: ảnh Ubuntu của Oracle có sẵn rule `iptables` chỉ cho phép cổng 22 rồi từ chối phần còn lại, nên mở Security List chưa đủ. Script chèn rule cho 80/443 trước rule REJECT và lưu bằng `netfilter-persistent`. Script không dùng `ufw` để khỏi xung đột với bộ rule này.

Trước khi đóng phiên SSH cũ, mở một phiên mới bằng `ssh -i notrelix-deploy deploy@<ip>` để chắc chắn không bị khóa ngoài.

### 2.3. File env trên VM (không đưa vào GitHub)

Điền `/opt/notrelix/.env.staging` (owner `deploy`, chmod 600). Chỉ ghi tên biến ở đây; giá trị bí mật sinh ngẫu nhiên trên VM, tuyệt đối không commit hay dán vào log.

- Bí mật: `POSTGRES_PASSWORD`, `NOTRELIX_APP_DB_PASSWORD`, `NOTRELIX_WORKER_DB_PASSWORD`, `REDIS_PASSWORD`, `RABBITMQ_USER`, `RABBITMQ_PASSWORD`, `JWT_SECRET`, và tùy chọn `OPERATOR_JWT_SIGNING_KEY`, `OPERATOR_SEED_PASSWORD`. Mật khẩu DB không được chứa `;`, `=` hoặc `"`.
- Không bí mật, đặt theo domain thật: `JWT_ISSUER`, `JWT_AUDIENCE`, `CORS_ORIGIN`, `FRONTEND_APP_BASE_URL` (đều là URL `https://demo.<domain>`).
- Không bí mật, đặt cho kiểu một VM sau Caddy: `HTTP_PORT=127.0.0.1:8080` (cổng nginx chỉ nghe loopback), `NGINX_CONF=nginx.prod.conf` (cấu hình nginx cho TLS ở edge, chuyển tiếp scheme công khai là HTTPS).

### 2.4. GitHub

Environment `staging` (không cần người duyệt):

| Loại | Tên |
|---|---|
| Secret | `DEPLOY_SSH_PRIVATE_KEY`, `DEPLOY_SSH_KNOWN_HOSTS`, `DEPLOY_SSH_USER` |
| Variable | `DEPLOY_HOST`, `DEPLOY_SSH_PORT`, `DEPLOY_PATH`, `DEPLOY_ENV_FILE`, `DEPLOY_HEALTH_URL`, `DEPLOY_SMOKE_URL`, `DEPLOY_ALLOW_STATEFUL_IMAGE_CHANGE` |

Gợi ý giá trị không bí mật: `DEPLOY_SSH_USER=deploy`, `DEPLOY_PATH=/opt/notrelix`, `DEPLOY_ENV_FILE=/opt/notrelix/.env.staging`, `DEPLOY_HEALTH_URL=http://127.0.0.1:8080/health/live`, `DEPLOY_SMOKE_URL=http://127.0.0.1:8080/`, `DEPLOY_ALLOW_STATEFUL_IMAGE_CHANGE=false`. Health và smoke chạy trên VM qua SSH nên dùng địa chỉ loopback. `DEPLOY_SSH_KNOWN_HOSTS` lấy bằng `ssh-keyscan -t ed25519 <ip>` rồi đối chiếu fingerprint trong Console.

Cấu hình email và đăng nhập GitHub do GitHub quản lý (không SSH vào VM để sửa). Mỗi lần deploy ghi các khóa dưới đây vào `$DEPLOY_PATH/.env.secrets` (chmod 600, giá trị đi qua stdin của SSH) và compose đọc file này sau `DEPLOY_ENV_FILE`, trước `images.env`. Khóa nào trống ở GitHub thì bị bỏ qua, nên không đè giá trị sẵn có trong `.env.staging`; khóa có giá trị ở GitHub thì thắng `.env.staging`. Giá trị chứa xuống dòng làm bước deploy thất bại.

| Loại | Tên |
|---|---|
| Secret | `RESEND_API_KEY`, `SMTP_PASSWORD` (tùy chọn), `OAUTH_GITHUB_CLIENT_SECRET`, `LIVEKIT_API_KEY`, `LIVEKIT_API_SECRET` |
| Variable | `EMAIL_ENABLED`, `EMAIL_FROM`, `EMAIL_FROM_NAME`, `SMTP_ENABLED`, `SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_FROM_EMAIL`, `SMTP_FROM_NAME`, `SMTP_ENABLE_SSL`, `OAUTH_GITHUB_ENABLED`, `OAUTH_GITHUB_CLIENT_ID`, `OAUTH_GITHUB_REDIRECT_URI`, `CHAT_CALLS_MEDIA_PROVIDER`, `LIVEKIT_API_URL`, `LIVEKIT_CLIENT_URL` |

Xoay khóa: sửa giá trị trong Environment trên GitHub, rồi chạy lại `release.yml` (`workflow_dispatch`) với `ci_run_id` của run `Notrelix CI` đã xanh. File `.env.secrets` được ghi lại toàn bộ mỗi lần deploy; xóa một khóa ở GitHub thì khóa đó biến mất khỏi file và compose quay về giá trị trong `.env.staging`. Danh sách khóa nằm một chỗ: mảng `keys` ở bước `Write app secrets from GitHub Environment` trong `deploy.yml`.

Đăng nhập GitHub: tạo OAuth App trên GitHub với Homepage URL `https://demo.<domain>` và Authorization callback URL `https://demo.<domain>/api/v1/auth/oauth/github/callback`; `OAUTH_GITHUB_REDIRECT_URI` phải trùng đúng URL đó. Đặt `OAUTH_GITHUB_ENABLED=true` chỉ khi đã có đủ client id, secret và redirect URI, vì API từ chối khởi động nếu thiếu.

Gọi thoại/video: media chạy trên LiveKit Cloud, không mở thêm cổng nào trên VM. Tạo project ở cloud.livekit.io, rồi đặt `CHAT_CALLS_MEDIA_PROVIDER=LiveKit`, `LIVEKIT_CLIENT_URL=wss://<project>.livekit.cloud`, `LIVEKIT_API_URL=https://<project>.livekit.cloud` (Variable) và `LIVEKIT_API_KEY`, `LIVEKIT_API_SECRET` (Secret, secret dài ít nhất 32 ký tự). Thiếu một giá trị khi provider là `LiveKit` thì API từ chối khởi động và deploy tự rollback; bỏ trống `CHAT_CALLS_MEDIA_PROVIDER` thì cuộc gọi dùng bản giả lập không kết nối được.

Environment `production`: bật Required reviewers (owner). Chưa cần secret cho đến khi có host production.

Sau lần publish image đầu tiên, vào GitHub Packages và đặt các package `notrelix-backend`, `notrelix-web`, `notrelix-marketing`, `notrelix-postgres` ở chế độ public (repo public) để VM kéo được mà không cần token. Nếu giữ private thì VM phải `docker login ghcr.io` bằng token chỉ có quyền `read:packages`.

### 2.5. Lần deploy đầu

Cắt `release/0.1` từ `main` và push để kích hoạt staging: `git switch main && git pull && git switch -c release/0.1 && git push -u origin release/0.1`. Theo dõi `Notrelix CI` rồi `Release Candidate`. Nếu cần chạy lại: `release.yml` có `workflow_dispatch` nhận id của run `Notrelix CI` đã thành công trên nhánh `release/**`. Production chỉ qua `promote-release.yml` chạy tay, có người duyệt của Environment `production`.

Sau deploy thành công: `https://demo.<domain>/health/live` trả 200; đăng nhập, tạo workspace và board, chat văn bản. Chạy `docker stats --no-stream` để đo RAM thật và chỉnh `mem_limit` trong `docker-compose.staging.yml` nếu cần (tổng giới hạn hiện khoảng 6 GB, là ước lượng).

## 3. Vận hành

- Nhật ký deploy: `/opt/notrelix/deploy.log` (thời gian, release, sha, người chạy, kết quả, image). Dòng cuối cũng hiện trong summary của job deploy.
- Backup trước migrate: `/opt/notrelix/backups/pre-migrate/pre-<release>.dump` (định dạng custom của `pg_dump`, giữ 5 bản mới nhất). Backup lỗi thì deploy dừng.
- Khôi phục thủ công sau migration đổi schema (tự rollback bị chặn theo policy): dừng backend, `pg_restore --clean --if-exists -U <user> -d <db>` từ file dump vào container `postgres`, đặt lại release trước (`.notrelix-release-previous`), `up -d`.
- Backup định kỳ ra ngoài VM: mục 3.1. Giám sát uptime và cảnh báo đĩa/RAM chưa nằm trong runbook này.

### 3.1. Backup hằng đêm lên Object Storage và diễn tập khôi phục

`bootstrap.sh` cài `restic`, tạo `/opt/notrelix/.backup.env` (rỗng, mode 600) và bật `notrelix-backup.timer` (02:30 UTC, lệch ngẫu nhiên tới 10 phút). Timer chạy `infra/oracle/backup.sh` bằng user `deploy`: `pg_dump -Fc -Z0` từ container `postgres` của stack đang chạy, đẩy lên bucket bằng restic (mã hóa, khử trùng lặp, nén), rồi giữ 7 bản theo ngày và 4 bản theo tuần (`KEEP_DAILY`, `KEEP_WEEKLY`). Dump rỗng hoặc lỗi thì không thành snapshot và service báo lỗi.

Chủ VM làm một lần:

1. Trong Oracle Console tạo bucket riêng (ví dụ `notrelix-staging-backup`, private) và một Customer Secret Key cho user có quyền ghi bucket đó. Lấy namespace ở Object Storage, mục Tenancy.
2. Điền `/opt/notrelix/.backup.env` (mode 600, không bao giờ vào git). Tên biến:
   - `RESTIC_REPOSITORY` dạng `s3:https://<namespace>.compat.objectstorage.<region>.oraclecloud.com/<bucket>`
   - `RESTIC_PASSWORD` mật khẩu mã hóa repo; mất mật khẩu là mất toàn bộ backup, cất ở nơi riêng
   - `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY` khóa Customer Secret Key
   - `AWS_DEFAULT_REGION` ví dụ `ap-singapore-1`
3. Chạy thử: `sudo systemctl start notrelix-backup.service && journalctl -u notrelix-backup.service -n 30`. Lần đầu script tự `restic init`. Xem lịch: `systemctl list-timers notrelix-backup.timer`.

Diễn tập khôi phục (nên chạy sau lần backup đầu và mỗi tháng): `sudo -u deploy bash /opt/notrelix/infra/oracle/restore-drill.sh`. Script kéo snapshot mới nhất, dựng một container Postgres tạm cùng image với container đang chạy, `pg_restore` vào đó, đếm dòng các bảng `accounts users workspaces boards` (đổi bằng `DRILL_TABLES`), in thời gian và dọn container. Kết thúc `drill ok` khi mọi bảng tồn tại và có dữ liệu; nếu không thì thoát lỗi. Ghi thời gian vào `deploy.log` hoặc sổ vận hành. Drill không đụng stack đang chạy; nó cần khoảng 1 GB RAM trống trong vài chục giây.

Khôi phục thật (dữ liệu hỏng hoặc VM mới):

1. Trên VM mới chạy bootstrap, điền lại `.backup.env`, deploy stack để có container `postgres` trống.
2. `restic snapshots --tag staging` để chọn bản; `restic restore <id|latest> --tag staging --target /tmp/restore` (nạp env bằng `set -a; . /opt/notrelix/.backup.env; set +a`).
3. Dừng backend (`api`, `realtime`, `worker`, `pgbouncer`), rồi `docker compose exec -T postgres pg_restore --clean --if-exists -U "$POSTGRES_USER" -d "$POSTGRES_DB" < /tmp/restore/.../notrelix.dump`.
4. `up -d` lại stack, kiểm `/health/live` và đăng nhập. Xóa `/tmp/restore` sau khi xong.

Giới hạn: bản dump là bản cắt lúc backup nên mất tối đa gần 24 giờ dữ liệu; phần mềm trong repo chưa gửi cảnh báo khi timer lỗi (mục giám sát thuộc bước riêng); Data Protection keys và Redis không nằm trong backup này.

## 4. Chính sách quét lỗ hổng (Trivy)

`container-ci.yml` quét từng image ứng dụng và `stack-smoke.yml` quét các image runtime: mức HIGH và CRITICAL, bật `ignore-unfixed`, thất bại trên `main` và khi chạy tay. Nghĩa là chỉ chặn khi đã có bản vá; lỗ hổng chưa có bản vá trong image nền không chặn release. Image postgres phái sinh có cổng riêng, không dùng file ignore. Không nới mức này nếu chưa có quyết định của lead.

## 5. Rủi ro đã biết, cần kiểm ở lần deploy đầu

- Chưa có số đo RAM thật; các giới hạn là ước lượng.
- Hành vi sau Caddy (chuyển hướng HTTPS, header chuyển tiếp, WebSocket realtime, cookie Secure và CSRF) chưa được thử. Nếu đăng nhập lỗi, kiểm `NGINX_CONF`, `HTTPS_REDIRECTION_ENABLED` và `CORS_ORIGIN` trước.
- Oracle có thể thu hồi VM A1 nếu 7 ngày liên tiếp mức dùng CPU, mạng và bộ nhớ đều dưới 20%.
- Image Postgres phái sinh phải được build lại đa kiến trúc và pin lại trong `delivery/images.lock.toml` trước khi VM arm64 chạy được đủ stack; việc này do lead kích hoạt sau khi workflow `Postgres Runtime Image` publish digest index.
