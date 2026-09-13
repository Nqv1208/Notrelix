import unittest
from tools.deliveryctl.architecture import check, jobs_missing_local_action_checkout
from tools.deliveryctl.runtime import ROOT
class T(unittest.TestCase):
 def test_architecture(self):check(ROOT)
 def test_wf_001_job_with_local_action_requires_earlier_checkout(self):
  text='''jobs:
  gate:
    steps:
      - uses: ./.github/actions/emit-evidence
'''
  self.assertEqual(jobs_missing_local_action_checkout(text),['gate'])
 def test_wf_001_job_with_earlier_checkout_passes(self):
  text='''jobs:
  gate:
    steps:
      - if: inputs.source_sha != ''
        uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
      - uses: ./.github/actions/emit-evidence
  other:
    steps:
      - run: echo hi
'''
  self.assertEqual(jobs_missing_local_action_checkout(text),[])
 def test_wf_001_checkout_after_local_action_does_not_count(self):
  text='''jobs:
  gate:
    steps:
      - uses: ./.github/actions/emit-evidence
      - uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
'''
  self.assertEqual(jobs_missing_local_action_checkout(text),['gate'])
 def test_wf_001_only_offending_job_reported(self):
  text='''jobs:
  good:
    steps:
      - uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
      - uses: ./.github/actions/setup-frontend
  bad:
    steps:
      - uses: ./.github/actions/setup-backend
'''
  self.assertEqual(jobs_missing_local_action_checkout(text),['bad'])
if __name__=='__main__':unittest.main()
