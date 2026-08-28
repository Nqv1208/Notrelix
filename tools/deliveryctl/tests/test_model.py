import unittest
from unittest.mock import patch
from tools.deliveryctl.model import normalize_path,validate_authorities,matches
from tools.deliveryctl.runtime import ROOT
class T(unittest.TestCase):
 def test_authorities(self):validate_authorities(ROOT)
 def test_glob(self):self.assertTrue(matches('frontend/apps/web/a.ts','frontend/apps/web/**'));self.assertFalse(matches('backend/a.cs','frontend/**'))
 def test_normalize_preserves_dotfiles(self):
  # Canonical path identity: leading dotfiles are repository paths, not noise.
  self.assertEqual(normalize_path('.github/workflows/ci.yml'),'.github/workflows/ci.yml')
  self.assertEqual(normalize_path('.python-version'),'.python-version')
  self.assertEqual(normalize_path('frontend/apps/web/a.ts'),'frontend/apps/web/a.ts')
 def test_normalize_strips_literal_dot_slash_prefix(self):
  self.assertEqual(normalize_path('./.github/workflows/ci.yml'),'.github/workflows/ci.yml')
  self.assertEqual(normalize_path('./frontend/apps/web/a.ts'),'frontend/apps/web/a.ts')
  def test_normalize_no_collision_between_dotfile_and_bare(self):
   # Regression: lstrip('./') collapsed '.github/foo' and 'github/foo' into one identity.
   self.assertNotEqual(normalize_path('.github/dependabot.yml'),normalize_path('github/dependabot.yml'))
 def test_rehearsal_string_rejected(self):
   import tempfile, pathlib
   d=pathlib.Path(tempfile.mkdtemp())/'delivery'; d.mkdir()
   for k in ('catalog.toml','environments.toml','images.lock.toml'):
     (d/k).write_text((ROOT/'delivery'/k).read_text())
   pol=(ROOT/'delivery'/'policy.toml').read_text().replace('rehearsal = true','rehearsal = "true"')
   (d/'policy.toml').write_text(pol)
   with self.assertRaises(ValueError):validate_authorities(d.parent)
 def test_unknown_rehearsal_proof_profile_rejected(self):
   import tempfile, pathlib
   d=pathlib.Path(tempfile.mkdtemp())/'delivery'; d.mkdir()
   for k in ('catalog.toml','environments.toml','images.lock.toml'):
     (d/k).write_text((ROOT/'delivery'/k).read_text())
   pol=(ROOT/'delivery'/'policy.toml').read_text().replace(
     '[proof_bindings.rehearsal]\nprofile = "release-rehearsal"',
     '[proof_bindings.rehearsal]\nprofile = "nonexistent-profile"')
   (d/'policy.toml').write_text(pol)
   with self.assertRaises(ValueError):validate_authorities(d.parent)
if __name__=='__main__':unittest.main()
