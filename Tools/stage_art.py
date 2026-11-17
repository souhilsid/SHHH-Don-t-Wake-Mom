"""Copies the curated subset of CC0/CC-BY source models into the Unity project (keeps originals untouched)."""
import os, shutil, sys
SRC='/home/claude/assets_raw'; DST='/home/claude/SHHH/UnityProject/Assets/SHHH/Art/ThirdParty'
KF=f'{SRC}/KayKit_Furniture_Bits_1.0_FREE/KayKit_Furniture_Bits_1.0_FREE/Assets'
KR=f'{SRC}/KayKit_Restaurant_Bits_1.0_FREE/KayKit_Restaurant_Bits_1.0_FREE/Assets'
plan = {
 'KayKitFurniture': (f'{KF}/fbx (unity)', ['couch_pillows','armchair_pillows','table_low','table_small','table_medium_long','shelf_B_large','shelf_A_small','lamp_standing','lamp_table','rug_rectangle_stripes_A','rug_oval_A','rug_rectangle_B','pillow_A','pillow_B','book_single','book_set','pictureframe_medium','pictureframe_large_A','cabinet_small','cabinet_medium_decorated','bed_double_A','chair_A_wood','cactus_small_A'], [f'{KF}/texture/furniturebits_texture.png']),
 'KayKitRestaurant': (f'{KR}/fbx (unity)', ['kitchencounter_straight_A','kitchencounter_straight_A_backsplash','kitchencounter_sink_backsplash','stove_single','fridge_A','kitchentable_A','chair_A','plate','pan_A','pot_A','bowl_small','jar_A_large','dishrack','towelrail','kitchencabinet','shelf_papertowel','cuttingboard','lid_A','door_A','wall','wall_window_closed'], [f'{KR}/textures/restaurantbits_texture.png']),
 'KenneyFurniture': (f'{SRC}/kenney_furniture-kit/Models/FBX format', ['washer','dryer','cabinetTelevision','televisionModern','kitchenCabinetUpper','cardboardBoxOpen','rugDoormat','rugRound','pillowBlueLong','bookcaseOpen','coatRackStanding','lampRoundTable','plantSmall2','pottedPlant','trashcan','radio','bear','doorway','sideTableDrawers','bedSingle'], []),
 'KenneyFood': (f'{SRC}/kenney_food-kit/Models/FBX format', ['cup-coffee','mug','utensil-fork','utensil-spoon','soda-bottle','loaf','bowl','frying-pan','pot','plate','bottle-ketchup','carton','apple','banana','cookie'], [f'{SRC}/kenney_food-kit/Models/FBX format/Textures/colormap.png']),
 'KenneyToyCar': (f'{SRC}/kenney_toy-car-kit/Models/FBX format', ['vehicle-racer','vehicle-monster-truck','vehicle-suv'], [f'{SRC}/kenney_toy-car-kit/Models/FBX format/Textures/colormap.png']),
 'KenneyBrick': (f'{SRC}/kenney_brick-kit/Models/FBX format', ['bevel-hq-brick-2x2','bevel-hq-brick-1x2','bevel-hq-brick-2x4'], [f'{SRC}/kenney_brick-kit/Models/FBX format/Textures/colormap.png']),
}
n=0
for pack,(folder,names,extras) in plan.items():
    out=os.path.join(DST,pack); os.makedirs(out,exist_ok=True)
    for nm in names:
        s=os.path.join(folder,nm+'.fbx')
        if not os.path.exists(s): print('MISSING',pack,nm); continue
        shutil.copy2(s,out); n+=1
    tex=os.path.join(out,'Textures') if pack.startswith('Kenney') else out
    os.makedirs(tex,exist_ok=True)
    for e in extras: shutil.copy2(e,tex); n+=1
# Characters
QW=f'{SRC}/Animated_Women_Characters_-_Feb_2019-20261104T114938Z-1-001/Animated Women Characters - Feb 2019'
out=os.path.join(DST,'QuaterniusWomen'); os.makedirs(out,exist_ok=True)
for nm in ['Female_Dress','Female_Casual']:
    shutil.copy2(f'{QW}/FBX/{nm}.fbx',out); n+=1
for f in os.listdir(QW):
    if f.lower().endswith('.png'): shutil.copy2(os.path.join(QW,f),out)
UB=f'{SRC}/Universal_Base_Characters_Standard_/Universal Base Characters[Standard]'
out=os.path.join(DST,'QuaterniusBase'); os.makedirs(out,exist_ok=True)
for nm in ['Superhero_Male_FullBody','Superhero_Female_FullBody']:
    shutil.copy2(f'{UB}/Base Characters/Unity/{nm}.fbx',out); n+=1
hair=f'{UB}/Hairstyles/Rigged to Head Bone/FBX (Unity)'
os.makedirs(out+'/Hair',exist_ok=True)
for f in os.listdir(hair): shutil.copy2(os.path.join(hair,f),out+'/Hair'); n+=1
# textures for base characters
for root,_,files in os.walk(UB):
    for f in files:
        if f.lower().endswith('.png') and 'Preview' not in f and 'Unreal' not in root:
            os.makedirs(out+'/Textures',exist_ok=True); shutil.copy2(os.path.join(root,f),out+'/Textures/'+f)
out=os.path.join(DST,'PolyByGoogle'); os.makedirs(out,exist_ok=True)
for f in os.listdir(f'{SRC}/Sneakers_by_Poly_by_Google_-_2cAXk_gG3Eh'): shutil.copy2(f'{SRC}/Sneakers_by_Poly_by_Google_-_2cAXk_gG3Eh/{f}',out)
print('copied',n)
