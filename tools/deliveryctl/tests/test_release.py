import tempfile,unittest
from pathlib import Path
from tools.deliveryctl.release import validate_manifest,staging_verified
from tools.deliveryctl.bundle import materialize
class T(unittest.TestCase):
 def m(self):return {'api_version':'delivery.notrelix.dev/v1','kind':'ReleaseCandidate','source_sha':'a'*40,'schema_change':False,'release_contract':{'migration_commands':['--migrate'],'migration_service':'backend'},'images':[{'id':'web','kind':'application','ref':'ghcr.io/x/web@sha256:'+'1'*64,'compose_service':'web','deploy_env_var':'WEB_IMAGE','stateful':False}]}
 def test_manifest(self):validate_manifest(self.m());self.assertTrue(staging_verified(self.m(),'1')['staging_verified'])
 def test_bundle(self):
  env={'name':'staging','deployment_adapter':'compose-ssh','rollback_after_schema_change':'manual','stateful_image_change_policy':'explicit-override','smoke_profile':'critical','compose_overlay':'docker-compose.staging.yml','promotion_mode':'automatic-after-main-ci','run_migrations':True,'rollout_strategy':'rolling','concurrency_group':'x'}
  with tempfile.TemporaryDirectory() as d:materialize(self.m(),env,Path(d));self.assertTrue((Path(d)/'release.generated.yml').exists())
if __name__=='__main__':unittest.main()
