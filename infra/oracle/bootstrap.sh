#!/usr/bin/env bash
# Idempotent bootstrap of a single Ubuntu 24.04 VM (built for Oracle Always Free A1, aarch64) that runs the
# Notrelix staging stack behind Caddy. Safe to re-run: every step checks before it changes anything.
#
#   sudo NOTRELIX_DOMAIN=demo.example.com DEPLOY_SSH_PUBKEY="ssh-ed25519 AAAA... deploy-key" \
#        [ACME_EMAIL=you@example.com] bash infra/oracle/bootstrap.sh
#   sudo bash infra/oracle/bootstrap.sh --verify     # only print the checklist
#
# It never writes a secret: the application env file is created empty (mode 600) for you to fill in.
# shellcheck source=/dev/null
set -euo pipefail

DEPLOY_USER="${DEPLOY_USER:-deploy}"
DEPLOY_PATH="${DEPLOY_PATH:-/opt/notrelix}"
REPO_URL="${REPO_URL:-https://github.com/Nqv1208/Notrelix.git}"
SWAP_GB="${SWAP_GB:-4}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

log() { printf '==> %s\n' "$*"; }
need_root() { [[ "$(id -u)" -eq 0 ]] || { echo "run with sudo" >&2; exit 1; }; }

# Insert an ACCEPT for a TCP port before the first REJECT/DROP rule. Oracle's Ubuntu image ships
# iptables rules (netfilter-persistent) that accept 22 and then reject everything else, so opening the
# port in the OCI Security List alone is not enough.
open_port() {
  local port="$1" pos
  iptables -C INPUT -p tcp --dport "$port" -m conntrack --ctstate NEW -j ACCEPT 2>/dev/null && return 0
  pos="$(iptables -L INPUT --line-numbers -n | awk '$2=="REJECT"||$2=="DROP"{print $1; exit}')"
  if [[ -n "$pos" ]]; then
    iptables -I INPUT "$pos" -p tcp --dport "$port" -m conntrack --ctstate NEW -j ACCEPT
  else
    iptables -A INPUT -p tcp --dport "$port" -m conntrack --ctstate NEW -j ACCEPT
  fi
}

write_if_changed() { # path, content on stdin; returns 0 when the file changed
  local path="$1" tmp
  tmp="$(mktemp)"; cat > "$tmp"
  if [[ -f "$path" ]] && cmp -s "$tmp" "$path"; then rm -f "$tmp"; return 1; fi
  install -m 644 "$tmp" "$path"; rm -f "$tmp"; return 0
}

verify() {
  local fail=0
  install -d -m 0755 /run/sshd # Ubuntu 24.04 socket-activated ssh may not have created it yet; sshd -T needs it
  check() { if eval "$2" >/dev/null 2>&1; then printf 'PASS  %s\n' "$1"; else printf 'FAIL  %s\n' "$1"; fail=1; fi; }
  check "docker engine and compose plugin" "docker compose version"
  check "deploy user exists, is not root, is in the docker group" "id -nG $DEPLOY_USER | grep -qw docker"
  check "deploy user has an authorized key" "test -s /home/$DEPLOY_USER/.ssh/authorized_keys"
  check "sshd: password login off" "sshd -T | grep -qi '^passwordauthentication no'"
  check "sshd: root login off" "sshd -T | grep -qi '^permitrootlogin no'"
  check "fail2ban sshd jail active" "fail2ban-client status sshd"
  check "unattended-upgrades enabled" "grep -q 'Unattended-Upgrade \"1\"' /etc/apt/apt.conf.d/20auto-upgrades"
  check "swap active" "swapon --show | grep -q /swapfile"
  check "firewall accepts 22, 80 and 443" "iptables -C INPUT -p tcp --dport 22 -m conntrack --ctstate NEW -j ACCEPT && iptables -C INPUT -p tcp --dport 80 -m conntrack --ctstate NEW -j ACCEPT && iptables -C INPUT -p tcp --dport 443 -m conntrack --ctstate NEW -j ACCEPT"
  check "caddy running with a valid config" "systemctl is-active caddy && caddy validate --config /etc/caddy/Caddyfile"
  check "deploy path owned by the deploy user" "test \"\$(stat -c %U $DEPLOY_PATH)\" = $DEPLOY_USER"
  check "backup env file exists with mode 600 and the timer is enabled" "test \"\$(stat -c %a $DEPLOY_PATH/.backup.env)\" = 600 && systemctl is-enabled notrelix-backup.timer && command -v restic"
  check "env file exists with mode 600" "test \"\$(stat -c %a $DEPLOY_PATH/.env.staging)\" = 600"
  echo "Not checked here: OCI Security List ingress 22/80/443, DNS A record, GitHub Environment secrets."
  return "$fail"
}

need_root
if [[ "${1:-}" == "--verify" ]]; then verify; exit $?; fi

: "${NOTRELIX_DOMAIN:?set NOTRELIX_DOMAIN, the public host name Caddy serves}"
: "${DEPLOY_SSH_PUBKEY:?set DEPLOY_SSH_PUBKEY, the public key GitHub Actions uses to log in as the deploy user}"
[[ "$DEPLOY_SSH_PUBKEY" == ssh-ed25519\ * ]] || { echo "DEPLOY_SSH_PUBKEY must be an ssh-ed25519 public key" >&2; exit 1; }

log "architecture: $(uname -m), os: $(. /etc/os-release && echo "$PRETTY_NAME")"
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq
apt-get install -y -qq ca-certificates curl gnupg git jq fail2ban unattended-upgrades iptables iptables-persistent restic debian-keyring debian-archive-keyring apt-transport-https >/dev/null

log "deploy user"
id "$DEPLOY_USER" >/dev/null 2>&1 || adduser --disabled-password --gecos "" "$DEPLOY_USER"
install -d -m 700 -o "$DEPLOY_USER" -g "$DEPLOY_USER" "/home/$DEPLOY_USER/.ssh"
touch "/home/$DEPLOY_USER/.ssh/authorized_keys"
grep -qxF "$DEPLOY_SSH_PUBKEY" "/home/$DEPLOY_USER/.ssh/authorized_keys" || echo "$DEPLOY_SSH_PUBKEY" >> "/home/$DEPLOY_USER/.ssh/authorized_keys"
chown "$DEPLOY_USER:$DEPLOY_USER" "/home/$DEPLOY_USER/.ssh/authorized_keys"; chmod 600 "/home/$DEPLOY_USER/.ssh/authorized_keys"

log "docker engine from the official apt repository"
if ! command -v docker >/dev/null 2>&1; then
  install -m 0755 -d /etc/apt/keyrings
  curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
  chmod a+r /etc/apt/keyrings/docker.asc
  echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" > /etc/apt/sources.list.d/docker.list
  apt-get update -qq
  apt-get install -y -qq docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin >/dev/null
fi
usermod -aG docker "$DEPLOY_USER"
if write_if_changed /etc/docker/daemon.json <<'JSON'
{"log-driver": "json-file", "log-opts": {"max-size": "20m", "max-file": "5"}}
JSON
then systemctl restart docker; fi
systemctl enable --now docker >/dev/null

log "ssh: key-only, no root login (validated before reload)"
write_if_changed /etc/ssh/sshd_config.d/00-notrelix.conf <<'CONF' || true
PasswordAuthentication no
KbdInteractiveAuthentication no
PermitRootLogin no
PubkeyAuthentication yes
CONF
install -d -m 0755 /run/sshd # privilege separation dir, missing until ssh.service first starts under ssh.socket
sshd -t
if systemctl is-active --quiet ssh.service; then
  systemctl reload ssh.service || systemctl restart ssh.service
else
  echo "ssh.service is not running (socket-activated): new connections already read the new config"
fi

log "firewall: 22, 80, 443 (Oracle default iptables rules reject the rest)"
for p in 22 80 443; do open_port "$p"; done
netfilter-persistent save >/dev/null

log "fail2ban"
write_if_changed /etc/fail2ban/jail.d/notrelix.local <<'CONF' || true
[sshd]
enabled = true
backend = systemd
maxretry = 5
bantime = 1h
CONF
systemctl enable --now fail2ban >/dev/null; systemctl restart fail2ban

log "unattended security upgrades (docker stays manual)"
write_if_changed /etc/apt/apt.conf.d/20auto-upgrades <<'CONF' || true
APT::Periodic::Update-Package-Lists "1";
APT::Periodic::Unattended-Upgrade "1";
CONF
write_if_changed /etc/apt/apt.conf.d/51notrelix-upgrades <<'CONF' || true
Unattended-Upgrade::Automatic-Reboot "true";
Unattended-Upgrade::Automatic-Reboot-Time "04:00";
CONF

log "swap ${SWAP_GB} GB and journald cap"
if ! swapon --show | grep -q /swapfile; then
  [[ -f /swapfile ]] || { fallocate -l "${SWAP_GB}G" /swapfile; chmod 600 /swapfile; mkswap /swapfile >/dev/null; }
  swapon /swapfile
fi
grep -q '^/swapfile ' /etc/fstab || echo '/swapfile none swap sw 0 0' >> /etc/fstab
write_if_changed /etc/sysctl.d/99-notrelix.conf <<'CONF' && sysctl --system >/dev/null || true
vm.swappiness=10
CONF
install -d /etc/systemd/journald.conf.d
write_if_changed /etc/systemd/journald.conf.d/notrelix.conf <<'CONF' && systemctl restart systemd-journald || true
[Journal]
SystemMaxUse=500M
CONF

log "deploy path ${DEPLOY_PATH} and an empty env file (mode 600)"
install -d -o "$DEPLOY_USER" -g "$DEPLOY_USER" "$DEPLOY_PATH"
[[ -d "$DEPLOY_PATH/.git" ]] || sudo -u "$DEPLOY_USER" git clone --quiet "$REPO_URL" "$DEPLOY_PATH"
[[ -f "$DEPLOY_PATH/.env.staging" ]] || install -m 600 -o "$DEPLOY_USER" -g "$DEPLOY_USER" /dev/null "$DEPLOY_PATH/.env.staging"

log "nightly backup: restic env file (empty, mode 600) and systemd timer"
[[ -f "$DEPLOY_PATH/.backup.env" ]] || install -m 600 -o "$DEPLOY_USER" -g "$DEPLOY_USER" /dev/null "$DEPLOY_PATH/.backup.env"
write_if_changed /etc/systemd/system/notrelix-backup.service <<CONF || true
[Unit]
Description=Notrelix nightly Postgres backup to Object Storage
[Service]
Type=oneshot
User=$DEPLOY_USER
Environment=DEPLOY_PATH=$DEPLOY_PATH
ExecStart=/usr/bin/env bash $DEPLOY_PATH/infra/oracle/backup.sh
CONF
write_if_changed /etc/systemd/system/notrelix-backup.timer <<'CONF' && systemctl daemon-reload || true
[Unit]
Description=Notrelix nightly Postgres backup
[Timer]
OnCalendar=*-*-* 02:30:00 UTC
RandomizedDelaySec=10min
Persistent=true
[Install]
WantedBy=timers.target
CONF
systemctl enable --now notrelix-backup.timer >/dev/null

log "caddy with automatic Let's Encrypt, proxying to 127.0.0.1:8080"
if ! command -v caddy >/dev/null 2>&1; then
  curl -fsSL https://dl.cloudsmith.io/public/caddy/stable/gpg.key | gpg --dearmor --yes -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
  curl -fsSL https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt -o /etc/apt/sources.list.d/caddy-stable.list
  apt-get update -qq
  apt-get install -y -qq caddy >/dev/null
fi
{
  [[ -z "${ACME_EMAIL:-}" ]] || printf '{\n\temail %s\n}\n\n' "$ACME_EMAIL"
  sed "s/@@DOMAIN@@/${NOTRELIX_DOMAIN}/g" "$SCRIPT_DIR/Caddyfile"
} > /etc/caddy/Caddyfile.new
caddy validate --config /etc/caddy/Caddyfile.new >/dev/null
mv /etc/caddy/Caddyfile.new /etc/caddy/Caddyfile
systemctl enable --now caddy >/dev/null; systemctl reload caddy

log "checklist"
verify
