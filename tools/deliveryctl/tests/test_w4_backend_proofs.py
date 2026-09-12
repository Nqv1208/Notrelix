import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
REGISTRY = ROOT / 'backend/tests/ci-proofs.json'
VERIFIER = ROOT / 'scripts/ci/verify-required-proofs-trx.py'
DISCIPLINE = ROOT / 'scripts/ci/check-migration-discipline.py'
API_VERSION = 'ci.notrelix.dev/backend-proofs/v1'
TRX_HEAD = '<?xml version="1.0" encoding="utf-8"?><TestRun><Results>'
TRX_TAIL = '</Results></TestRun>'


def trx(*entries):
  rows = ''.join(f'<UnitTestResult testName="{name}" outcome="{outcome}" />' for name, outcome in entries)
  return TRX_HEAD + rows + TRX_TAIL


class RegistryTests(unittest.TestCase):
  def test_bep_001_registry_schema_valid(self):
    registry = json.loads(REGISTRY.read_text())
    self.assertEqual(registry['api_version'], API_VERSION)
    self.assertIsInstance(registry['profiles'], dict)
    self.assertIsInstance(registry['proofs'], dict)

  def test_bep_002_duplicate_proof_id_rejected(self):
    with tempfile.TemporaryDirectory() as tmp:
      path = Path(tmp) / 'ci-proofs.json'
      path.write_text('{"api_version":"%s","profiles":{"api":["p"]},"proofs":{"p":{"tests":[]},"p":{"tests":["A.B"]}}}' % API_VERSION)
      result = subprocess.run([sys.executable, str(VERIFIER), '--registry', str(path), '--profile', 'api', '--trx', str(path)], capture_output=True, text=True)
      self.assertNotEqual(result.returncode, 0)
      self.assertIn('duplicate registry key', result.stderr)

  def test_bep_003_unknown_profile_reference_rejected(self):
    with tempfile.TemporaryDirectory() as tmp:
      path = Path(tmp) / 'ci-proofs.json'
      path.write_text('{"api_version":"%s","profiles":{"api":["ghost"]},"proofs":{"p":{"tests":["A.B"]}}}' % API_VERSION)
      result = subprocess.run([sys.executable, str(VERIFIER), '--registry', str(path), '--profile', 'api', '--trx', str(path)], capture_output=True, text=True)
      self.assertNotEqual(result.returncode, 0)
      self.assertIn('unknown proof ghost', result.stderr)

  def test_bep_004_empty_mapping_rejected(self):
    with tempfile.TemporaryDirectory() as tmp:
      path = Path(tmp) / 'ci-proofs.json'
      path.write_text('{"api_version":"%s","profiles":{"api":["p"]},"proofs":{"p":{"tests":[]}}}' % API_VERSION)
      result = subprocess.run([sys.executable, str(VERIFIER), '--registry', str(path), '--profile', 'api', '--trx', str(path)], capture_output=True, text=True)
      self.assertNotEqual(result.returncode, 0)
      self.assertIn('empty tests mapping', result.stderr)

  def test_bep_005_malformed_selector_rejected(self):
    with tempfile.TemporaryDirectory() as tmp:
      path = Path(tmp) / 'ci-proofs.json'
      path.write_text('{"api_version":"%s","profiles":{"api":["p"]},"proofs":{"p":{"tests":[".A.B"]}}}' % API_VERSION)
      result = subprocess.run([sys.executable, str(VERIFIER), '--registry', str(path), '--profile', 'api', '--trx', str(path)], capture_output=True, text=True)
      self.assertNotEqual(result.returncode, 0)
      self.assertIn('malformed selector', result.stderr)


class VerifierTests(unittest.TestCase):
  def setUp(self):
    self.tmp = tempfile.TemporaryDirectory()
    self.addCleanup(self.tmp.cleanup)
    self.registry_path = Path(self.tmp.name) / 'registry.json'
    self.registry_path.write_text(json.dumps({
      'api_version': API_VERSION,
      'profiles': {'api': ['p']},
      'proofs': {'p': {'tests': ['Notrelix.API.Tests.Idempotency.IdempotencyEndpointContractTests']}},
    }))
    self.trx_path = Path(self.tmp.name) / 'api.trx'

  def _run(self):
    return subprocess.run([sys.executable, str(VERIFIER), '--registry', str(self.registry_path), '--profile', 'api', '--trx', str(self.trx_path)], capture_output=True, text=True)

  def test_bep_006_required_proof_executed_passes(self):
    self.trx_path.write_text(trx(('Notrelix.API.Tests.Idempotency.IdempotencyEndpointContractTests.Execute_Conflict', 'Passed')))
    result = self._run()
    self.assertEqual(result.returncode, 0, result.stderr)
    summary = json.loads(result.stdout)
    self.assertTrue(summary['ok'])
    self.assertEqual(summary['proofs']['p']['executed'], 1)

  def test_bep_006_declared_without_execution_fails(self):
    self.trx_path.write_text(trx(('Notrelix.API.Tests.Other.Thing', 'Passed')))
    result = self._run()
    self.assertNotEqual(result.returncode, 0)
    self.assertIn('did not execute', result.stderr)

  def test_bep_008_deleted_mapped_test_fails(self):
    self.trx_path.write_text(trx())
    result = self._run()
    self.assertNotEqual(result.returncode, 0)
    self.assertIn('executed zero required proofs', result.stderr)

  def test_bep_009_conflicting_outcomes_rejected(self):
    self.trx_path.write_text(trx(('Notrelix.API.Tests.Idempotency.IdempotencyEndpointContractTests.A', 'Passed'), ('Notrelix.API.Tests.Idempotency.IdempotencyEndpointContractTests.A', 'Failed')))
    result = self._run()
    self.assertNotEqual(result.returncode, 0)
    self.assertIn('ambiguous TRX result', result.stderr)

  def test_bep_010_malformed_trx_rejected(self):
    self.trx_path.write_text('<TestRun><Results>')
    result = self._run()
    self.assertNotEqual(result.returncode, 0)
    self.assertIn('malformed TRX', result.stderr)

  def test_bep_011_empty_trx_rejected_when_profile_requires_proof(self):
    self.trx_path.write_text(trx())
    result = self._run()
    self.assertNotEqual(result.returncode, 0)

  def test_bep_012_summary_deterministic(self):
    self.trx_path.write_text(trx(('Notrelix.API.Tests.Idempotency.IdempotencyEndpointContractTests.Execute', 'Passed')))
    first = self._run().stdout
    second = self._run().stdout
    self.assertEqual(first, second)
    self.assertEqual(json.loads(first)['proofs'], dict(sorted(json.loads(first)['proofs'].items())))

  def test_failed_outcome_fails(self):
    self.trx_path.write_text(trx(('Notrelix.API.Tests.Idempotency.IdempotencyEndpointContractTests.Execute', 'Failed')))
    result = self._run()
    self.assertNotEqual(result.returncode, 0)
    self.assertIn('selector failed', result.stderr)


class MigrationDisciplineTests(unittest.TestCase):
  def setUp(self):
    self.tmp = tempfile.TemporaryDirectory()
    self.addCleanup(self.tmp.cleanup)
    self.repo = Path(self.tmp.name)
    self._git('init', '-q', str(self.repo))
    self._git('config', 'user.email', 'ci@example.invalid')
    self._git('config', 'user.name', 'CI')

  def _git(self, *args):
    return subprocess.run(['git', *args], cwd=self.repo, capture_output=True, text=True)

  def _commit(self, message):
    self._git('add', '.')
    result = self._git('commit', '-qm', message)
    self.assertEqual(result.returncode, 0, result.stderr)
    return self._git('rev-parse', 'HEAD').stdout.strip()

  def _migration(self, name, content='CREATE TABLE t ();'):
    path = self.repo / 'backend/src/Notrelix.Infrastructure/Migrations' / name
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content)
    return path

  def _run(self, *extra):
    return subprocess.run([sys.executable, str(DISCIPLINE), *extra], cwd=self.repo, capture_output=True, text=True)

  def test_mig_001_append_only_new_migration_passes(self):
    self._migration('20260101000000_Base.cs')
    base = self._commit('base')
    self._migration('20260102000000_AddBoard.cs')
    head = self._commit('add migration')
    result = self._run('--base-sha', base, '--head-sha', head)
    self.assertEqual(result.returncode, 0, result.stderr)
    self.assertTrue(json.loads(result.stdout)['compared'])

  def test_mig_002_modified_historical_migration_fails(self):
    self._migration('20260101000000_Base.cs')
    base = self._commit('base')
    self._migration('20260101000000_Base.cs', 'CREATE TABLE changed ();')
    head = self._commit('modify migration')
    result = self._run('--base-sha', base, '--head-sha', head)
    self.assertNotEqual(result.returncode, 0)
    self.assertIn('append-only', result.stderr)

  def test_mig_003_deleted_historical_migration_fails(self):
    path = self._migration('20260101000000_Base.cs')
    base = self._commit('base')
    path.unlink()
    head = self._commit('delete migration')
    result = self._run('--base-sha', base, '--head-sha', head)
    self.assertNotEqual(result.returncode, 0)
    self.assertIn('append-only', result.stderr)

  def test_mig_004_range_comparison_uses_given_shas(self):
    self._migration('20260101000000_Base.cs')
    base = self._commit('base')
    self._migration('20260102000000_AddBoard.cs')
    head = self._commit('add')
    untouched = self._migration('20260103000000_Unrelated.cs')
    self._git('reset', '-q', '--hard', head)
    untouched.parent.joinpath(untouched.name).write_text('CREATE TABLE t ();')
    result = self._run('--base-sha', base, '--head-sha', head)
    self.assertEqual(result.returncode, 0, result.stderr)
    self.assertEqual(json.loads(result.stdout)['added'], ['backend/src/Notrelix.Infrastructure/Migrations/20260102000000_AddBoard.cs'])

  def test_mig_005_push_before_head_range_compares(self):
    self._migration('20260101000000_Base.cs')
    before = self._commit('before')
    self._migration('20260102000000_AddBoard.cs')
    head = self._commit('head')
    result = self._run('--base-sha', before, '--head-sha', head)
    self.assertEqual(result.returncode, 0, result.stderr)

  def test_mig_006_unknown_range_with_schema_change_hard_fails(self):
    self._migration('20260101000000_Base.cs')
    self._commit('base')
    result = self._run('--base-sha', '', '--head-sha', 'HEAD', '--full-range', '--schema-change')
    self.assertNotEqual(result.returncode, 0)
    self.assertIn('cannot verify append-only', result.stderr)

  def test_mig_006_unknown_range_without_schema_change_conservative(self):
    self._migration('20260101000000_Base.cs')
    self._commit('base')
    result = self._run('--base-sha', '', '--head-sha', 'HEAD', '--full-range')
    self.assertEqual(result.returncode, 0, result.stderr)
    summary = json.loads(result.stdout)
    self.assertFalse(summary['compared'])
    self.assertTrue(summary['conservative'])

  def test_mig_007_helper_does_not_interpret_github_events(self):
    text = DISCIPLINE.read_text()
    for token in ('GITHUB_EVENT', 'EVENT_NAME', 'github.event'):
      self.assertNotIn(token, text)
