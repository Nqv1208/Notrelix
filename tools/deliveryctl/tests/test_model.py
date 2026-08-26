import unittest
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
if __name__=='__main__':unittest.main()
