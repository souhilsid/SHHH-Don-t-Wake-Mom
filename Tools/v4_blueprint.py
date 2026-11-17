from pathlib import Path
import json,html,csv
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'SHHH_Astra_3D_World_Production_V4';D=OUT/'01_Canonical_Architecture'
# Unity axes: +Y up, +Z north. Current room dimensions and task coordinates are retained.
rooms=[{'id':'LivingRoom','center':[0,0,0],'size':[7.2,2.7,6.2]}, {'id':'Kitchen','center':[6.9,0,0],'size':[6.6,2.7,6]}, {'id':'Hall','center':[0,0,4.15],'size':[7.2,2.7,2.1]}, {'id':'Laundry','center':[6.4,0,6.0],'size':[5.6,2.7,5.4]}, {'id':'Stairs','center':[-2.3,1.5,4.55],'size':[2.5,3,3.3]}, {'id':'Landing','center':[0,3,4.0],'size':[7.2,2.7,1.6]}, {'id':'MomBedroom','center':[0,3,6.9],'size':[4.8,2.7,4.2]}]
links=[{'from':'LivingRoom','to':'Kitchen','center':[3.6,0,2.15],'width':1.6,'type':'arch'}, {'from':'LivingRoom','to':'Hall','center':[1.8,0,3.1],'width':1.4,'type':'arch'}, {'from':'Hall','to':'Laundry','center':[3.6,0,4.15],'width':1.2,'type':'door'}, {'from':'Hall','to':'Stairs','center':[-2.95,0,3.2],'width':1.2,'type':'open'}, {'from':'Stairs','to':'Landing','center':[-1.65,3,3.2],'width':1.2,'type':'return_landing'}, {'from':'Landing','to':'MomBedroom','center':[0,3,4.8],'width':1.1,'type':'door'}]
(D/'HOUSE_BLUEPRINT.json').write_text(json.dumps({'status':'CANONICAL_LAYOUT_FOR_IMPLEMENTATION; navigation verification pending','units':'metres','axes':'Unity Y-up, +Z north','rooms':rooms,'connections':links,'stairs':{'rise':3,'steps':18,'riser':1/6,'tread':.3,'width':1.2,'layout':'Two 9-step flights with a 180-degree return and intermediate landing; northbound first, southbound second'},'gameplay_rule':'Keep existing 3-round room flow, player pose and task IDs. Shared house geometry must remain consistent through scene transitions. No parallel scoring or input system.'},indent=2))
svg=['<svg xmlns="http://www.w3.org/2000/svg" width="1240" height="840" viewBox="0 0 1240 840"><rect width="1240" height="840" fill="#211530"/><g font-family="Arial" fill="#FFF0BA"><text x="40" y="48" font-size="28">SHHH! • CANONICAL HOUSE / V4</text><text x="40" y="78" font-size="16">Dimensioned planning view — metres • fixed room relationships • implementation verification pending</text>']
for level,ox in [(0,60),(3,790)]:
 for r in rooms:
  if r['center'][1]!=level:continue
  x,y,z=r['center'];w,h,d=r['size'];scale=38 if level==0 else 45
  sx=ox+(x+3.6)*scale;sy=510-z*scale
  svg.append(f'<rect x="{sx-w*scale/2}" y="{sy-d*scale/2}" width="{w*scale}" height="{d*scale}" rx="6" fill="#70409F" stroke="#FFCF2E" stroke-width="2"/><text x="{sx}" y="{sy-8}" font-size="16" text-anchor="middle">{html.escape(r["id"])}</text><text x="{sx}" y="{sy+16}" font-size="14" text-anchor="middle">{w:g} × {d:g} m</text>')
 svg.append(f'<text x="{ox}" y="720" font-size="21">'+('GROUND FLOOR / y = 0 m' if level==0 else 'UPSTAIRS / y = 3 m')+'</text>')
svg.append('<text x="40" y="780" font-size="15">Night throughout. Living room and kitchen share an arch. Laundry connects through hall. Mom sleeps upstairs behind a door.</text></g></svg>')
(D/'HOUSE_FLOORPLAN.svg').write_text(''.join(svg),encoding='utf-8')
(D/'CANONICAL_ROOM_DECISIONS.md').write_text('''# Canonical house decisions

The exact submitted master prompt matches the separately attached file byte-for-byte. All 15 supplied reference images and the full 13-page brand guideline were inspected. The current project is Unity 6000.0.62f1 / URP 17.0.4 / Meta Core 201 / OpenXR 1.16.1. The existing gameplay is three room rounds with shared authority and identifiers; preserve it.

## Reconciliation

- Living Room VR Stealth Adventure controls the sofa/TV relationship, chunky furniture, bookcase and warm/cool balance. The other three living views contribute toy clutter, cups, household props and stair character. Their conflicting doors and balconies do not become separate rooms. Mom's landing sleep pose and head-locked overlays are rejected.
- All four kitchen images become one purple/cream kitchen. Duplicate/incremental kitchen views do not authorize another refrigerator or a mirrored layout. The current cooker/sink/anchor coordinates remain authoritative until migration is verified.
- The moonlit laundry controls lighting. Both sunlit laundry references supply cabinet, basket and folded-cloth details only. One washer, one dryer, one sink and a common night window replace the contradictory camera layouts.
- The Family Stage and customization images supply curtains, marquee, podiums and wardrobe presentation. Current Solo/Friends/Customize/Settings flows remain authoritative. Currency and weekly challenge graphics are not new gameplay requirements.
- Character turnarounds guide colors and accessories while existing rig identity is preserved. Siblings keep detached heads, torsos, hands and shoes. Mom remains full-body and sleeps in a bed in her upstairs bedroom.
- The approved brand archive's illustrated logo takes priority over the different logo drawn in the room concepts. Keep its exact aspect ratio and subtitle; reuse the project's existing fonts unless a licensed font import is needed.

## Implementation anchors

HOUSE_BLUEPRINT.json fixes room centers, dimensions, portals, stair rise and room relationships. Ground-floor local room sizes are the actual RoomCatalog values. The first connected living-room slice will retain the shelf/book task IDs and its state validator. An armchair near the kitchen arch may require a documented furniture-position adjustment; do not move chore anchors silently.

Comfort targets: 1.2 m common passage clearance, 1.1 m bedroom doorway, 1.3 m stairs, primary reachable interactions near waist/chest height. Verify against colliders and navigation rather than drawings alone.

## Prioritized reuse/build map

| System | Reuse | Work required |
|---|---|---|
| Input/authority | PlayerRig, OvrHandSource, HandInteractor, UiPointer, PropBody, AnchorFixture, Core ChoreBoard, Photon | Preserve; add art-facing bindings and tests |
| Hands | Installed SimpleHands/Prefabs/WhiteHand.prefab, 16 skin bones, 5248 triangles | Positive-scale left mirror, world-pose retarget, white glove material, confidence visibility, offline evidence |
| Cast | Current V2 reduced meshes and V3 clips in CharacterCatalog | Preserve identity, improve contact/expression wiring; no high-poly blanket replacement |
| House | V2 furniture, decor, reduced models, palette, source blends | Connected shell, actual bedroom/stairs, stable portals, collision and light treatment |
| Props | Existing task-specific meshes and physical state system | Stable art IDs, anchors, meaningful repair visuals, explicit 18-action compatibility map |
| Lobby | Existing functional menu boards and customization state | Family Stage environment and exact brand artwork; preserve reach-tested controls |

## Limits already established

There is no Git checkout on this PC. A timestamped backup of Assets/SHHH, Packages and ProjectSettings precedes changes; original V2/V3 and SimpleHands are hash-inventoried. The supplied SimpleHands readme contains a creator acknowledgement, not a purchase receipt; use is authorized by the owner and raw licensed geometry stays in their project. Meta Interaction SDK is not installed as a package: the working stack uses Meta Core tracking with SHHH's tested custom interactors. Do not replace it with a duplicate SDK stack.

Current runtime ChoreLibrary contains more actions than the earlier 9-group/18-action art manifest, and several destinations differ. Preserve current game predicates and explicitly record exact, partial and missing matches. Hardware is unavailable under the existing offline-development instruction; no Quest FPS or real tracking claim can be made.
''',encoding='utf-8')
print('CANONICAL_BLUEPRINT_WRITTEN')
