"""Unit tests for validate-workflow-permissions.py."""
from __future__ import annotations
import sys, tempfile, unittest
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from validate_workflow_permissions import (
    validate_repository,
    _parse_permissions,
    _parse_job_permissions,
    _callee_required_permissions,
    LEVELS,
)


class TestPermissionParsing(unittest.TestCase):
    def test_inline_map(self):
        text = "permissions: {contents: read, packages: read}\n"
        self.assertEqual(_parse_permissions(text), {"contents": "read", "packages": "read"})

    def test_block_map(self):
        text = "permissions:\n  contents: read\n  packages: write\n"
        self.assertEqual(_parse_permissions(text), {"contents": "read", "packages": "write"})

    def test_no_permissions(self):
        self.assertEqual(_parse_permissions("name: test\njobs:\n  a:\n    runs-on: ubuntu\n"), {})

    def test_job_level_permissions(self):
        text = "jobs:\n  build:\n    permissions:\n      contents: read\n      packages: write\n    runs-on: ubuntu\n"
        self.assertEqual(_parse_job_permissions(text, "build"), {"contents": "read", "packages": "write"})

    def test_job_level_not_found(self):
        text = "jobs:\n  build:\n    runs-on: ubuntu\n"
        self.assertEqual(_parse_job_permissions(text, "missing"), {})


class TestCalleeRequired(unittest.TestCase):
    def test_workflow_level_only(self):
        text = "permissions:\n  contents: read\n  packages: read\njobs:\n  a:\n    runs-on: ubuntu\n"
        result = _callee_required_permissions(text)
        self.assertEqual(result, {"contents": "read", "packages": "read"})

    def test_job_level_elevates(self):
        text = "permissions:\n  contents: read\njobs:\n  a:\n    permissions:\n      packages: write\n    runs-on: ubuntu\n"
        result = _callee_required_permissions(text)
        self.assertEqual(result["contents"], "read")
        self.assertEqual(result["packages"], "write")


class TestRepositoryValidation(unittest.TestCase):
    def _make_repo(self, files: dict[str, str]) -> Path:
        d = Path(tempfile.mkdtemp())
        (d / ".github" / "workflows").mkdir(parents=True)
        for name, content in files.items():
            (d / ".github" / "workflows" / name).write_text(content)
        return d

    def test_workflow_none_callee_read_fail(self):
        """Caller has no permissions -> callee needs contents:read -> fail."""
        callee = "permissions:\n  contents: read\njobs:\n  a:\n    runs-on: ubuntu\n"
        caller = "name: caller\njobs:\n  a:\n    uses: ./.github/workflows/callee.yml\n"
        d = self._make_repo({"callee.yml": callee, "caller.yml": caller})
        errors = validate_repository(d)
        self.assertTrue(any("contents" in e for e in errors))

    def test_workflow_read_callee_read_pass(self):
        callee = "permissions:\n  contents: read\njobs:\n  a:\n    runs-on: ubuntu\n"
        caller = "name: caller\npermissions:\n  contents: read\njobs:\n  a:\n    uses: ./.github/workflows/callee.yml\n"
        d = self._make_repo({"callee.yml": callee, "caller.yml": caller})
        errors = validate_repository(d)
        self.assertEqual(errors, [])

    def test_caller_job_write_callee_read_pass(self):
        callee = "permissions:\n  contents: read\njobs:\n  a:\n    runs-on: ubuntu\n"
        caller = "name: caller\npermissions:\n  contents: none\njobs:\n  a:\n    permissions:\n      contents: write\n    uses: ./.github/workflows/callee.yml\n"
        d = self._make_repo({"callee.yml": callee, "caller.yml": caller})
        errors = validate_repository(d)
        self.assertEqual(errors, [])

    def test_caller_job_map_missing_required_scope_fail(self):
        callee = "permissions:\n  packages: read\njobs:\n  a:\n    runs-on: ubuntu\n"
        caller = "name: caller\npermissions:\n  contents: read\njobs:\n  a:\n    permissions:\n      contents: read\n    uses: ./.github/workflows/callee.yml\n"
        d = self._make_repo({"callee.yml": callee, "caller.yml": caller})
        errors = validate_repository(d)
        self.assertTrue(any("packages" in e for e in errors))

    def test_unknown_value_fail(self):
        text = "permissions:\n  contents: borked\njobs:\n  a:\n    runs-on: ubuntu\n"
        d = self._make_repo({"w.yml": text})
        errors = validate_repository(d)
        self.assertTrue(any("borked" in e for e in errors))

    def test_scalar_read_all_fail(self):
        text = "permissions: read-all\n"
        d = self._make_repo({"w.yml": text})
        errors = validate_repository(d)
        self.assertTrue(any("read-all" in e for e in errors))

    def test_scalar_write_all_fail(self):
        text = "permissions: write-all\n"
        d = self._make_repo({"w.yml": text})
        errors = validate_repository(d)
        self.assertTrue(any("write-all" in e for e in errors))

    def test_empty_map_means_none(self):
        callee = "permissions:\n  contents: read\njobs:\n  a:\n    runs-on: ubuntu\n"
        caller = "name: caller\npermissions: {}\njobs:\n  a:\n    uses: ./.github/workflows/callee.yml\n"
        d = self._make_repo({"callee.yml": callee, "caller.yml": caller})
        errors = validate_repository(d)
        self.assertTrue(any("contents" in e for e in errors))

    def test_inline_map_pass(self):
        callee = "permissions: {contents: read}\njobs:\n  a:\n    runs-on: ubuntu\n"
        caller = "name: caller\npermissions:\n  contents: read\njobs:\n  a:\n    uses: ./.github/workflows/callee.yml\n"
        d = self._make_repo({"callee.yml": callee, "caller.yml": caller})
        errors = validate_repository(d)
        self.assertEqual(errors, [])

    def test_block_map_pass(self):
        callee = "permissions:\n  contents: read\n  packages: read\njobs:\n  a:\n    runs-on: ubuntu\n"
        caller = "name: caller\npermissions:\n  contents: read\n  packages: read\njobs:\n  a:\n    uses: ./.github/workflows/callee.yml\n"
        d = self._make_repo({"callee.yml": callee, "caller.yml": caller})
        errors = validate_repository(d)
        self.assertEqual(errors, [])


if __name__ == "__main__":
    unittest.main()
