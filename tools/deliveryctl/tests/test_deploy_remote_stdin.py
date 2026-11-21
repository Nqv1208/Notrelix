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
        calls = re.findall(r'\bdocker compose --env-file[^;]*?(?:pull|up -d --no-build --remove-orphans)[^;]*;', wf)
        self.assertEqual(len(calls), 2)
        for call in calls:
            self.assertIn('</dev/null', call)
        self.assertIn('compose run --rm -T ', wf)
        self.assertRegex(wf, r'log success\n\s+echo ' + MARKER + r'\n\s+REMOTE\n(?:.*\n)*?\s+grep -qx ' + MARKER + r' "\$out"')
        self.assertIn('set -o pipefail', wf)


if __name__ == '__main__':
    unittest.main()
