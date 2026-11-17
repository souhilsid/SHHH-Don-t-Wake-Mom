"""Inventories the supplied source archives: hashes, dedupes, maps license, writes Docs/ASSET_LICENSE_MANIFEST.csv."""
import csv, hashlib, os, sys, zipfile, collections
ATT = sys.argv[1]; OUT = sys.argv[2]
LIC = {
 'kenney': ('CC0 1.0', 'Kenney (kenney.nl)', 'No attribution required (credit appreciated)'),
 'KayKit': ('CC0 1.0', 'Kay Lousberg (kaylousberg.com)', 'No attribution required'),
 'Animated_Women': ('CC0 1.0', 'Quaternius', 'No attribution required'),
 'Universal_Base': ('CC0 1.0', 'Quaternius (Universal Base Characters, Standard/free)', 'No attribution required'),
 'Plate_by_Kay': ('CC0 1.0 (Poly Pizza listing of KayKit)', 'Kay Lousberg', 'Credit given'),
 'Mat_by_Isa': ('CC0 1.0 (Poly Pizza listing)', 'Isa Lousberg', 'Credit given'),
 'Towel_Rack_by_Quaternius': ('CC0 1.0 (Poly Pizza listing)', 'Quaternius', 'Credit given'),
 'Sneakers_by_Poly_by_Google': ('CC-BY 3.0 (Poly by Google via Poly Pizza)', 'Poly by Google', 'ATTRIBUTION REQUIRED in credits'),
 'Basket_by_Poly_by_Google': ('CC-BY 3.0 (Poly by Google via Poly Pizza)', 'Poly by Google', 'ATTRIBUTION REQUIRED in credits'),
 'SHHH_': ('User-owned project material', 'Project owner', 'Internal design source'),
}
def lic(name):
    for k,v in LIC.items():
        if k in name: return v
    return ('UNKNOWN - verify', '?', 'Do not ship until verified')
rows=[]; seen=collections.defaultdict(list)
for f in sorted(os.listdir(ATT)):
    p=os.path.join(ATT,f)
    if f.startswith('.') or not os.path.isfile(p): continue
    name=f.split('-',1)[1] if '-' in f[:9] else f
    h=hashlib.sha256(open(p,'rb').read()).hexdigest()
    n_inner=''
    if f.endswith('.zip'):
        with zipfile.ZipFile(p) as z:
            infos=[i for i in z.infolist() if not i.is_dir()]
            n_inner=len(infos)
            for i in infos:
                seen[(i.file_size, i.CRC)].append(name+':'+i.filename)
    l=lic(name)
    rows.append([name, os.path.getsize(p), h, n_inner, l[0], l[1], l[2]])
dups=sum(1 for k,v in seen.items() if len(v)>1)
with open(OUT,'w',newline='') as fo:
    w=csv.writer(fo); w.writerow(['source_file','bytes','sha256','files_inside','license','author','attribution','used_in_build'])
    for r in rows: w.writerow(r+['see ASSET_RESEARCH.md'])
print(len(rows),'sources;', dups,'duplicate inner files (same size+CRC) across/within archives')
