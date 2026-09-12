import json
import subprocess
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
DOTNET_CHECKER = ROOT / 'scripts/ci/check-dotnet-vulnerabilities.py'
PNPM_CHECKER = ROOT / 'frontend/scripts/ci/check-pnpm-audit.mjs'

VULNERABLE_REPORT = '''Project `Notrelix.API` has the following vulnerability(s)
   net9.0
    > Compromised.Package 1.2.3
      Critical
      -> Sources: https://api.nuget.org/v3/index.json

Project `Notrelix.Platform` has the following vulnerability(s)
   net9.0
    > Another.Bad 0.1.0
      High
'''

CLEAN_REPORT = ''


class DotnetVulnerabilityCheckerTests(unittest.TestCase):
  def _run(self, content):
    with tempfile.TemporaryDirectory() as tmp:
      report = Path(tmp) / 'report.txt'
      report.write_text(content)
      return subprocess.run(
        [__import__('sys').executable, str(DOTNET_CHECKER), '--report', str(report)],
        capture_output=True, text=True)

  def test_clean_report_passes(self):
    result = self._run(CLEAN_REPORT)
    self.assertEqual(result.returncode, 0, result.stderr)
    self.assertTrue(json.loads(result.stdout)['ok'])

  def test_vulnerable_report_fails_with_structured_summary(self):
    result = self._run(VULNERABLE_REPORT)
    self.assertNotEqual(result.returncode, 0)
    summary = json.loads(result.stdout)
    self.assertFalse(summary['ok'])
    self.assertEqual(sorted(summary['vulnerable_projects']), ['Notrelix.API', 'Notrelix.Platform'])
    self.assertIn('Compromised.Package 1.2.3', summary['packages'])
    self.assertIn('Another.Bad 0.1.0', summary['packages'])

  def test_missing_report_fails_closed(self):
    result = subprocess.run(
      [__import__('sys').executable, str(DOTNET_CHECKER), '--report', '/nonexistent/report.txt'],
      capture_output=True, text=True)
    self.assertNotEqual(result.returncode, 0)
    self.assertIn('cannot read report', result.stderr)

  def test_summary_output_written(self):
    with tempfile.TemporaryDirectory() as tmp:
      report = Path(tmp) / 'report.txt'
      report.write_text(CLEAN_REPORT)
      output = Path(tmp) / 'summary.json'
      result = subprocess.run(
        [__import__('sys').executable, str(DOTNET_CHECKER), '--report', str(report), '--output', str(output)],
        capture_output=True, text=True)
      self.assertEqual(result.returncode, 0)
      self.assertTrue(json.loads(output.read_text())['ok'])


class PnpmAuditCheckerTests(unittest.TestCase):
  def _run(self, payload):
    with tempfile.TemporaryDirectory() as tmp:
      report = Path(tmp) / 'audit.json'
      report.write_text(payload if isinstance(payload, str) else json.dumps(payload))
      return subprocess.run(['node', str(PNPM_CHECKER), '--input', str(report)], capture_output=True, text=True)

  def test_clean_audit_passes(self):
    result = self._run({'advisories': {}, 'metadata': {'vulnerabilities': {'high': 0, 'critical': 0}}})
    self.assertEqual(result.returncode, 0, result.stderr)
    self.assertTrue(json.loads(result.stdout)['ok'])

  def test_high_severity_advisory_fails(self):
    result = self._run({'advisories': {'100': {'module_name': 'left-pad', 'severity': 'high', 'title': 'ReDoS'}}})
    self.assertNotEqual(result.returncode, 0)
    summary = json.loads(result.stdout)
    self.assertFalse(summary['ok'])
    self.assertEqual(summary['failing'][0]['module'], 'left-pad')

  def test_critical_severity_advisory_fails(self):
    result = self._run({'advisories': [{'module_name': 'bad-pkg', 'severity': 'critical'}]})
    self.assertNotEqual(result.returncode, 0)

  def test_moderate_only_passes_high_threshold(self):
    result = self._run({'advisories': [{'module_name': 'meh', 'severity': 'moderate'}]})
    self.assertEqual(result.returncode, 0, result.stderr)

  def test_advisories_object_form_supported(self):
    result = self._run({'advisories': {'7': {'module_name': 'x', 'severity': 'high'}}})
    self.assertNotEqual(result.returncode, 0)

  def test_malformed_report_fails_closed(self):
    result = self._run('not-json-at-all')
    self.assertNotEqual(result.returncode, 0)
    self.assertIn('malformed audit report', result.stderr)

  def test_missing_report_fails_closed(self):
    result = subprocess.run(['node', str(PNPM_CHECKER), '--input', '/nonexistent/audit.json'], capture_output=True, text=True)
    self.assertNotEqual(result.returncode, 0)
