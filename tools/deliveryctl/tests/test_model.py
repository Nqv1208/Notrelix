import unittest,shutil,tempfile
from pathlib import Path
from tools.deliveryctl.model import normalize_path,validate_authorities,matches
from tools.deliveryctl.runtime import ROOT
class T(unittest.TestCase):
 def _scratch(self,replace,new):
  tmp=Path(tempfile.mkdtemp());(tmp/'delivery').mkdir(parents=True)
  for f in ('catalog.toml','policy.toml','environments.toml','images.lock.toml'):
   shutil.copy(ROOT/'delivery'/f,tmp/'delivery'/f)
  path=tmp/'delivery'/'policy.toml'
  text=path.read_text()
  assert replace in text,replace
  path.write_text(text.replace(replace,new,1))
  return tmp
 def _reject(self,replace,new,needle):
  with self.assertRaises(ValueError) as ctx:self._scratch_replace(replace,new)
  self.assertIn(needle,str(ctx.exception))
 def _scratch_replace(self,replace,new):
  tmp=self._scratch(replace,new);validate_authorities(tmp)
 def test_authorities(self):validate_authorities(ROOT)
 def test_delivery_platform_profile(self):
  p=self._scratch('.','')
  from tools.deliveryctl.model import load_toml
  self.assertEqual(load_toml(p/'delivery'/'policy.toml')['proof_profiles']['delivery-platform']['required'],['delivery:platform'])
 def test_unknown_component_rule_rejected(self):
  self._reject('delivery_platform = true','delivery_platform = true\ncomponents = ["ghost"]','unknown component')
 def test_unknown_infra_mode_rejected(self):
  self._reject('delivery_platform = true','delivery_platform = true\ninfra_modes = ["magic"]','unknown infra mode')
 def test_unknown_proof_binding_rejected(self):
  self._reject('backend = "security-backend"','backend = "missing-profile"','unknown profile')
 def test_unknown_capability_rejected(self):
  self._reject('capabilities = ["contract", "web-tests"]','capabilities = ["definitely-not-a-cap"]','unknown capability')
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
