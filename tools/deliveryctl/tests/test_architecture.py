import unittest
from tools.deliveryctl.architecture import check
from tools.deliveryctl.runtime import ROOT
class T(unittest.TestCase):
 def test_architecture(self):check(ROOT)
if __name__=='__main__':unittest.main()
