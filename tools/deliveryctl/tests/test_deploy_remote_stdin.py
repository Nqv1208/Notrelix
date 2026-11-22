import re
import subprocess
import tempfile
import unittest
from pathlib import Path

from tools.deliveryctl.runtime import ROOT

MARKER = 'NOTRELIX_DEPLOY_COMPLETE'


def run_piped(script: str, fix: bool) -> str:
    """Pipe a script into `bash -s` like ssh does; the fake docker drains stdin like the real client."""
    with tempfile.TemporaryDirectory() as d:
        docker = Path(d) / 'docker'
        docker.write_text('#!/bin/sh\ncat >/dev/null\n')
        docker.chmod(0o755)
        redirect = ' </dev/null' if fix else ''
        body = f'compose(){{ docker compose "$@"{redirect}; }}\ncompose exec -T x\n{script}\n'
        done = subprocess.run(['bash', '-s'], input=body, capture_output=True, text=True, env={'PATH': f'{d}:/usr/bin:/bin'})
        assert done.returncode == 0
        return done.stdout


class DeployRemoteStdinTest(unittest.TestCase):
    def test_child_reading_stdin_truncates_script_without_the_fix(self):
        self.assertNotIn(MARKER, run_piped(f'echo {MARKER}', fix=False))

    def test_stdin_from_dev_null_lets_the_script_finish(self):
        self.assertIn(MARKER, run_piped(f'echo {MARKER}', fix=True))

    def test_deploy_workflow_guards(self):
        wf = (ROOT / '.github/workflows/deploy.yml').read_text()
        self.assertRegex(wf, r'compose\(\)\{ docker compose .*"\$@" </dev/null; \}')
        calls = re.findall(r'\bdocker compose "\$\{envs\[@\]\}"[^;]*?(?:pull|up -d --no-build --remove-orphans)[^;]*;', wf)
        self.assertEqual(len(calls), 2)
        for call in calls:
            self.assertIn('</dev/null', call)
        self.assertIn('compose run --rm -T ', wf)
        self.assertRegex(wf, r'log success\n\s+echo ' + MARKER + r'\n\s+REMOTE\n(?:.*\n)*?\s+grep -qx ' + MARKER + r' "\$out"')
        self.assertIn('set -o pipefail', wf)


def secrets_step() -> str:
    wf = (ROOT / '.github/workflows/deploy.yml').read_text()
    return wf.split('- name: Write app secrets from GitHub Environment\n')[1].split('      - name: Promote same bytes and verify')[0]


class DeploySecretsStepTest(unittest.TestCase):
    def test_static_guards(self):
        step = secrets_step()
        self.assertEqual(step.count('ssh -p'), 1)
        self.assertIn("printf '%s' \"$content\" | ssh", step)
        self.assertIn('umask 077', step)
        self.assertIn('.env.secrets.tmp', step)
        self.assertIn("$'\\n'", step)
        self.assertIn('exit 1', step)
        self.assertIn('[[ -n "$v" ]] || continue', step)
        self.assertNotIn('set -x', step)
        wf = (ROOT / '.github/workflows/deploy.yml').read_text()
        self.assertRegex(wf, r'\[\[ -f "\$path/\.env\.secrets" \]\] && envs\+=\(--env-file "\$path/\.env\.secrets"\)')
        self.assertLess(wf.index('envs=(--env-file "$env_file")'), wf.index('--env-file "$path/.env.secrets"'))
        self.assertLess(wf.index('--env-file "$path/.env.secrets"'), wf.index('--env-file "$current/images.env"'))
        self.assertNotIn('docker compose --env-file', wf)
        self.assertGreaterEqual(wf.count('docker compose "${envs[@]}"'), 3)

    def run_writer(self, extra_env: dict):
        run = re.search(r'run: \|\n(.*)', secrets_step(), re.S).group(1)
        with tempfile.TemporaryDirectory() as d:
            ssh = Path(d) / 'ssh'
            ssh.write_text('#!/bin/bash\nfor a; do last="$a"; done\nexec bash -c "$last"\n')
            ssh.chmod(0o755)
            dest = Path(d) / 'deploy'
            dest.mkdir()
            env = {'PATH': f'{d}:/usr/bin:/bin', 'USER': 'u', 'HOST': 'h', 'DEPLOY_PATH': str(dest), **extra_env}
            done = subprocess.run(['bash', '-e', '-c', run], capture_output=True, text=True, env=env)
            target = dest / '.env.secrets'
            return done, target, target.exists() and target.read_text(), target.exists() and target.stat().st_mode & 0o777

    def test_writer_writes_only_non_empty_keys_with_mode_600(self):
        done, _, text, mode = self.run_writer({'RESEND_API_KEY': 'k1', 'EMAIL_FROM_NAME': 'Notrelix Team', 'SMTP_HOST': ''})
        self.assertEqual(done.returncode, 0, done.stderr)
        self.assertEqual(text, 'RESEND_API_KEY=k1\nEMAIL_FROM_NAME=Notrelix Team\n')
        self.assertEqual(mode, 0o600)

    def test_writer_rejects_newline_and_writes_nothing(self):
        done, target, _, _ = self.run_writer({'RESEND_API_KEY': 'a\nINJECT=1'})
        self.assertNotEqual(done.returncode, 0)
        self.assertFalse(target.exists())
        self.assertNotIn('INJECT', done.stdout + done.stderr)


if __name__ == '__main__':
    unittest.main()
