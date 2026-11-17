from pathlib import Path
import zipfile,json,hashlib,datetime
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'SHHH_Astra_3D_World_Production_V4'
for name in ['00_Audit_And_Project_Backup_Notes','01_Canonical_Architecture','02_Blender_Source/Scripts','03_Unity_Game_Assets','04_Hand_Tracking_Integration','05_Task_And_Sabotage_Library','06_Rendered_And_InEngine_Reviews','07_Performance_And_QA','08_Handoff_For_Claude_And_Unity','Reference_Inputs']:(OUT/name).mkdir(parents=True,exist_ok=True)
for filename,folder in [('SHHH_Astra_Full_3D_Production_Submission_v1.zip','Submission'),('SHHH_Complete_Brand_Kit_v1.zip','Brand')]:
 src=Path('C:/Users/aisci/Downloads')/filename;dest=OUT/'Reference_Inputs'/folder
 with zipfile.ZipFile(src) as z:
  for info in z.infolist():
   target=(dest/info.filename).resolve()
   if not target.is_relative_to(dest.resolve()):raise ValueError('Archive path leaves reference folder')
  z.extractall(dest)
prompt=OUT/'Reference_Inputs/Submission/00_Master_Prompt/SHHH_Astra_Full_3D_World_Production_Prompt_v1.md'
assert prompt.read_bytes()==Path('C:/Users/aisci/Downloads/SHHH_Astra_Full_3D_World_Production_Prompt_v1.md').read_bytes()
stamp=datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ');backup=OUT/'00_Audit_And_Project_Backup_Notes'/('Project_Before_V4_'+stamp+'.zip')
roots=[ROOT/'UnityProject/Assets/SHHH',ROOT/'UnityProject/ProjectSettings',ROOT/'UnityProject/Packages']
files=sorted(p for base in roots for p in base.rglob('*') if p.is_file())
with zipfile.ZipFile(backup,'w',zipfile.ZIP_DEFLATED,compresslevel=3) as z:
 for p in files:z.write(p,p.relative_to(ROOT))
hashes=[]
for base in [ROOT/'UnityProject/Assets/SHHH',ROOT/'UnityProject/Assets/SimpleHands',ROOT/'UnityProject/Assets/SHHHArtV2',ROOT/'UnityProject/Assets/SHHHArtV3']:
 for p in base.rglob('*'):
  if p.is_file() and p.suffix in ['.cs','.unity','.prefab','.meta','.asset','.fbx']:
   hashes.append({'path':p.relative_to(ROOT).as_posix(),'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
(OUT/'00_Audit_And_Project_Backup_Notes/BASELINE_FILE_HASHES.json').write_text(json.dumps(hashes,indent=2))
(OUT/'00_Audit_And_Project_Backup_Notes/BACKUP_RECEIPT.json').write_text(json.dumps({'backup':backup.name,'files':len(files),'bytes':backup.stat().st_size,'scope':'Existing SHHH assets, scripts, scenes, Packages and ProjectSettings. SimpleHands and V2/V3 are hash-inventoried and preserved in place. No Secrets folder copied.','git_checkout_present':(ROOT/'.git').exists(),'master_prompt_matches_submission':True},indent=2))
print('V4_PREPARATION_COMPLETE',backup.name,len(files),'files',len(hashes),'baseline hashes')
