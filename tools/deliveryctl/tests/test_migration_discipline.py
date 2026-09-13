import json,subprocess,tempfile,unittest
from pathlib import Path
from tools.deliveryctl.runtime import ROOT

SCRIPT=ROOT/'scripts'/'ci'/'check-migration-discipline.py'
MIG='backend/src/Notrelix.Infrastructure/Data/Migrations'

class MigrationDisciplineTests(unittest.TestCase):
 def _repo(self):
  tmp=Path(tempfile.mkdtemp())
  def git(*args):
   r=subprocess.run(['git',*args],cwd=tmp,capture_output=True,text=True)
   self.assertEqual(r.returncode,0,(args,r.stderr))
   return r.stdout.strip()
  git('init','-q','-b','main');git('config','user.email','a@b.c');git('config','user.name','t')
  return tmp,git
 def _base(self,tmp,git):
  mig=tmp/MIG;mig.mkdir(parents=True)
  (mig/'20260101000000_Init.cs').write_text('class Init { v1 }\n')
  (mig/'20260101000000_Init.Designer.cs').write_text('class InitDesigner { v1 }\n')
  (mig/'ApplicationDbContextModelSnapshot.cs').write_text('class Snapshot { v1 }\n')
  git('add','-A');git('commit','-q','-m','base')
  return git('rev-parse','HEAD')
 def _commit_head(self,tmp,git):
  git('add','-A');git('commit','-q','--allow-empty','-m','head')
  return git('rev-parse','HEAD')
 def _run(self,tmp,*flags):
  r=subprocess.run(['python3',str(SCRIPT),*flags],cwd=tmp,capture_output=True,text=True)
  payload=None
  for line in reversed(r.stdout.splitlines()):
   if line.startswith('{'):
    payload=json.loads(line);break
  return r.returncode,payload
 def _append_migration(self,mig,mutation=None):
  (mig/'20260202000000_AddThing.cs').write_text('class AddThing { v2 }\n')
  (mig/'20260202000000_AddThing.Designer.cs').write_text('class AddThingDesigner { v2 }\n')
  if mutation=='snapshot':
   (mig/'ApplicationDbContextModelSnapshot.cs').write_text('class Snapshot { v2 with AddThing }\n')

 def test_append_with_snapshot_change_passes(self):
  tmp,git=self._repo();base=self._base(tmp,git)
  self._append_migration(tmp/MIG,'snapshot');head=self._commit_head(tmp,git)
  rc,payload=self._run(tmp,'--base-sha',base,'--head-sha',head)
  self.assertEqual(rc,0,payload)
  self.assertTrue(payload['compared'] and payload['ok'])
  self.assertEqual(len(payload['added_migrations']),2)
  self.assertEqual(len(payload['snapshot_modified']),1)
  self.assertEqual(payload['violations'],[])
 def test_append_without_snapshot_change_passes(self):
  tmp,git=self._repo();base=self._base(tmp,git)
  self._append_migration(tmp/MIG);head=self._commit_head(tmp,git)
  rc,payload=self._run(tmp,'--base-sha',base,'--head-sha',head)
  self.assertEqual(rc,0,payload);self.assertTrue(payload['ok']);self.assertEqual(payload['snapshot_modified'],[])
 def test_snapshot_only_modification_fails(self):
  tmp,git=self._repo();base=self._base(tmp,git)
  (tmp/MIG/'ApplicationDbContextModelSnapshot.cs').write_text('class Snapshot { rogue edit }\n')
  head=self._commit_head(tmp,git)
  rc,payload=self._run(tmp,'--base-sha',base,'--head-sha',head)
  self.assertEqual(rc,1);self.assertTrue(payload['compared'] and not payload['ok'])
  self.assertTrue(any('without appending a migration' in v for v in payload['violations']))
 def test_snapshot_deletion_fails(self):
  tmp,git=self._repo();base=self._base(tmp,git)
  (tmp/MIG/'ApplicationDbContextModelSnapshot.cs').unlink();head=self._commit_head(tmp,git)
  rc,payload=self._run(tmp,'--base-sha',base,'--head-sha',head)
  self.assertEqual(rc,1);self.assertTrue(any('deleted, renamed or type-changed' in v for v in payload['violations']))
 def test_snapshot_rename_fails(self):
  tmp,git=self._repo();base=self._base(tmp,git)
  subprocess.run(['git','mv',f'{MIG}/ApplicationDbContextModelSnapshot.cs',f'{MIG}/MySnapshot.cs'],cwd=tmp,check=True)
  head=self._commit_head(tmp,git)
  rc,payload=self._run(tmp,'--base-sha',base,'--head-sha',head)
  self.assertEqual(rc,1);self.assertTrue(any('deleted, renamed or type-changed' in v for v in payload['violations']))
 def test_migration_definition_modification_fails(self):
  tmp,git=self._repo();base=self._base(tmp,git)
  (tmp/MIG/'20260101000000_Init.cs').write_text('class Init { hand edited history }\n')
  head=self._commit_head(tmp,git)
  rc,payload=self._run(tmp,'--base-sha',base,'--head-sha',head)
  self.assertEqual(rc,1);self.assertTrue(any('append-only; changed' in v for v in payload['violations']))
 def test_designer_modification_fails(self):
  tmp,git=self._repo();base=self._base(tmp,git)
  (tmp/MIG/'20260101000000_Init.Designer.cs').write_text('class InitDesigner { hand edited }\n')
  head=self._commit_head(tmp,git)
  rc,payload=self._run(tmp,'--base-sha',base,'--head-sha',head)
  self.assertEqual(rc,1);self.assertTrue(payload['violations'])
 def test_migration_deletion_fails(self):
  tmp,git=self._repo();base=self._base(tmp,git)
  (tmp/MIG/'20260101000000_Init.cs').unlink();head=self._commit_head(tmp,git)
  rc,payload=self._run(tmp,'--base-sha',base,'--head-sha',head)
  self.assertEqual(rc,1);self.assertTrue(any('append-only; deleted' in v for v in payload['violations']))
 def test_migration_rename_fails(self):
  tmp,git=self._repo();base=self._base(tmp,git)
  subprocess.run(['git','mv',f'{MIG}/20260101000000_Init.cs',f'{MIG}/20260303000000_InitMoved.cs'],cwd=tmp,check=True)
  head=self._commit_head(tmp,git)
  rc,payload=self._run(tmp,'--base-sha',base,'--head-sha',head)
  self.assertEqual(rc,1);self.assertTrue(any('append-only; changed' in v for v in payload['violations']))
 def test_unknown_range_with_schema_change_fails(self):
  tmp,git=self._repo();self._base(tmp,git)
  rc,payload=self._run(tmp,'--full-range','--schema-change')
  self.assertEqual(rc,1);self.assertFalse(payload['compared']);self.assertFalse(payload['ok'])
 def test_unknown_range_without_schema_change_conservative_pass(self):
  tmp,git=self._repo();self._base(tmp,git)
  rc,payload=self._run(tmp,'--full-range')
  self.assertEqual(rc,0);self.assertFalse(payload['compared']);self.assertTrue(payload['ok'])
 def test_duplicate_chain_index_fails(self):
  tmp,git=self._repo();base=self._base(tmp,git)
  (tmp/MIG/'20260101000000_Duplicate.cs').write_text('class Duplicate {}\n')
  head=self._commit_head(tmp,git)
  rc,payload=self._run(tmp,'--base-sha',base,'--head-sha',head)
  self.assertEqual(rc,1);self.assertFalse(payload['ok']);self.assertIn('duplicates',payload['duplicates'][0])
if __name__=='__main__':unittest.main()
