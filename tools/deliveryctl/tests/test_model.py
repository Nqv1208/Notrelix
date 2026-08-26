import unittest
from tools.deliveryctl.model import validate_authorities,matches
from tools.deliveryctl.runtime import ROOT
class T(unittest.TestCase):
 def test_authorities(self):validate_authorities(ROOT)
 def test_glob(self):self.assertTrue(matches('frontend/apps/web/a.ts','frontend/apps/web/**'));self.assertFalse(matches('backend/a.cs','frontend/**'))
if __name__=='__main__':unittest.main()
