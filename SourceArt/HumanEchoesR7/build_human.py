"""Create an original clothed/rigged derivative of the user's CC0 Blender basemesh.
Run in Blender 5.x, headless. Never writes the input .blend.
"""
import bpy, bmesh, math, os
from mathutils import Vector
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SOURCE = os.environ.get('REVERB_HUMAN_BASEMESH', os.path.abspath(os.path.join(ROOT,'../../..','Protagonist_v05_Basemesh.blend')))
OUT = os.path.join(ROOT, 'Assets/Game/Art/VisualIdentity/HumanEchoesR7')
os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=SOURCE)
keep = ['Protagonist_Body_Male_Stylized', 'Protagonist_Eye_L', 'Protagonist_Eye_R']
world = {n:bpy.data.objects[n].matrix_world.copy() for n in keep}
for o in list(bpy.data.objects):
    if o.name not in keep: bpy.data.objects.remove(o, do_unlink=True)
for col in bpy.data.collections:
    col.hide_viewport = col.hide_render = False
for n in keep:
    o=bpy.data.objects[n];o.parent=None;o.matrix_world=world[n];o.hide_set(False);o.hide_render=False
    bpy.context.view_layer.objects.active=o;o.select_set(True)
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);o.select_set(False)
body=bpy.data.objects[keep[0]];body.name='Human_Skin'
def material(name, color, rough=.85):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough
    return m
skin=material('Skin',(.54,.29,.18),.9)
canvas=material('Canvas',(.75,.70,.56),.94)
denim=material('Denim',(.075,.13,.17),.94)
leather=material('Leather',(.11,.055,.025),.9)
hair=material('Hair',(.038,.021,.012),.96)
scarf=material('Scarf',(.19,.47,.49),.9)
eye=material('Eye',(.10,.07,.045),.7)
body.data.materials.clear();body.data.materials.append(skin)
for n in keep[1:]:
    o=bpy.data.objects[n];o.data.materials.clear();o.data.materials.append(eye)

# Select complete faces by region, create a slightly loose cloth shell, then remove
# covered skin. Shells keep an anatomical silhouette without clipping nude surfaces.
def zone(c):
    x,y,z=c; a=abs(x)
    if z<.19: return 'Boots'
    if z<1.015 and a<.235: return 'Trousers'
    if .995<z<1.455 and a<.245: return 'Jacket'
    if 1.115<z<1.43 and a>=.20: return 'Sleeves'
    return None
regions={n:[] for n in ['Boots','Trousers','Jacket','Sleeves']}
for f in body.data.polygons:
    z=zone(f.center)
    if z: regions[z].append(f.index)
def shell(name, indices, mat, thickness):
    used=sorted({v for i in indices for v in body.data.polygons[i].vertices});lookup={v:i for i,v in enumerate(used)}
    verts=[]
    for i in used:
        v=body.data.vertices[i];p=v.co+v.normal*thickness
        # Small irregular cloth creases, not shiny, inflated armor panels.
        p += v.normal*(.0025*math.sin(p.z*65+abs(p.x)*9))
        verts.append(p)
    faces=[tuple(lookup[v] for v in body.data.polygons[i].vertices) for i in indices]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.new(name,mesh);bpy.context.scene.collection.objects.link(o);mesh.materials.append(mat)
    solid=o.modifiers.new('Cloth hems','SOLIDIFY');solid.thickness=.004
    bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=solid.name)
    return o
for n,fs in regions.items(): shell(n,fs,leather if n=='Boots' else denim if n=='Trousers' else canvas,.009 if n=='Boots' else .018)
bm=bmesh.new();bm.from_mesh(body.data);bm.faces.ensure_lookup_table()
covered={i for fs in regions.values() for i in fs}
bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.index in covered],context='FACES');bm.to_mesh(body.data);bm.free()

def ellipsoid(name,loc,scale,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,location=loc)
    o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    o.data.materials.append(mat);return o
def box(name,loc,scale,mat,bevel=.018):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.scale=scale
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);o.data.materials.append(mat)
    m=o.modifiers.new('Worn edges','BEVEL');m.width=bevel;m.segments=2;bpy.ops.object.modifier_apply(modifier=m.name);return o
def ribbon(name,points,width,mat):
    verts=[]
    for p in points: verts.extend([(p[0]-width/2,p[1],p[2]),(p[0]+width/2,p[1],p[2])])
    faces=[(i*2,i*2+1,i*2+3,i*2+2) for i in range(len(points)-1)]
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.materials.append(mat)
    o=bpy.data.objects.new(name,me);bpy.context.scene.collection.objects.link(o)
    so=o.modifiers.new('Fabric thickness','SOLIDIFY');so.thickness=.006
    bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=so.name);return o

# Sculpt a hair cap from the head region, preserving the forehead and ears.
hairverts=[];hairfaces=[];mapping={}
for f in body.data.polygons:
    c=f.center
    if c.z < 1.635 or (c.y<-.075 and c.z<1.70):continue
    ids=[]
    for vi in f.vertices:
        if vi not in mapping:
            v=body.data.vertices[vi];mapping[vi]=len(hairverts);hairverts.append(v.co+v.normal*.008)
        ids.append(mapping[vi])
    hairfaces.append(ids)
me=bpy.data.meshes.new('Hair');me.from_pydata(hairverts,[],hairfaces);me.materials.append(hair)
o=bpy.data.objects.new('Hair',me);bpy.context.scene.collection.objects.link(o)
ellipsoid('Scarf_Collar',(0,-.005,1.445),(.112,.086,.038),scarf)
ribbon('Scarf_Tail',[(.055,.085,1.44),(.065,.11,1.32),(.085,.14,1.20)],.075,scarf)
box('Canvas_Pocket_L',(.105,-.134,1.29),(.11,.026,.12),canvas,.008)
box('Canvas_Pocket_R',(-.105,-.134,1.29),(.11,.026,.12),canvas,.008)
ribbon('Jacket_Opening',[(0,-.104,1.44),(0,-.153,1.25),(0,-.139,1.03)],.012,leather)
box('Old_Satchel',(.245,.025,.94),(.145,.14,.20),leather,.025)
box('Satchel_Flap',(.245,-.053,.96),(.147,.025,.105),canvas,.012)
ribbon('Satchel_Strap',[(-.14,.08,1.43),(-.15,-.08,1.43),(.03,-.17,1.20),(.245,-.068,.97),(.25,.09,.96),(.03,.125,1.20),(-.14,.08,1.43)],.027,leather)
for side in [-1,1]:
    box('Work_boot_toecap', (side*.105,-.065,.085),(.17,.32,.14),leather,.045)
    box('Work_boot_sole', (side*.105,-.065,.018),(.174,.326,.026),leather,.01)

# Original generic rig. Soft nearest-segment skin weights are deterministic and
# avoid a dependency on a proprietary animation pack or automatic heat failure.
bpy.ops.object.select_all(action='DESELECT')
arm=bpy.data.armatures.new('HumanRig');rig=bpy.data.objects.new('HumanRig',arm);bpy.context.scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
spec=[('Hips',(0,0,.91),(0,0,1.06),None),('Spine',(0,0,1.06),(0,0,1.27),'Hips'),('Chest',(0,0,1.27),(0,0,1.43),'Spine'),('Head',(0,0,1.43),(0,0,1.73),'Chest')]
for side,s in [('L',1),('R',-1)]:
    spec += [(side+'_UpperArm',(s*.205,0,1.39),(s*.315,0,1.19),'Chest'),(side+'_Forearm',(s*.315,0,1.19),(s*.40,-.01,1.02),side+'_UpperArm'),(side+'_Hand',(s*.40,-.01,1.02),(s*.42,-.03,.93),side+'_Forearm'),(side+'_Thigh',(s*.095,0,.94),(s*.105,.015,.51),'Hips'),(side+'_Shin',(s*.105,.015,.51),(s*.105,0,.13),side+'_Thigh'),(side+'_Foot',(s*.105,0,.13),(s*.105,-.15,.06),side+'_Shin')]
for n,h,t,parent in spec:
    b=arm.edit_bones.new(n);b.head=h;b.tail=t
    if parent:b.parent=arm.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT');rig.select_set(False)
def dist(p,a,b):
    d=b-a;t=max(0,min(1,(p-a).dot(d)/d.length_squared));return (p-(a+d*t)).length
segments={n:(Vector(h),Vector(t)) for n,h,t,_ in spec}
for o in [x for x in bpy.context.scene.objects if x.type=='MESH']:
    groups={n:o.vertex_groups.new(name=n) for n,_,_,_ in spec}
    for v in o.data.vertices:
        p=o.matrix_world@v.co;x,y,z=p
        if 'Eye' in o.name or o.name=='Hair':weights=[('Head',1)]
        elif 'Satchel' in o.name or 'Scarf' in o.name or 'Pocket' in o.name or o.name=='Jacket_Opening':weights=[('Chest' if z>1.15 else 'Spine',1)]
        else:
            side='L' if x>0 else 'R'
            names=[side+'_Thigh',side+'_Shin',side+'_Foot','Hips'] if z<1.02 and abs(x)<.24 else [side+'_UpperArm',side+'_Forearm',side+'_Hand','Chest'] if abs(x)>.21 and z<1.46 else ['Hips','Spine','Chest','Head']
            ds=sorted([(dist(p,*segments[n]),n) for n in names])[:2]
            vals=[(n,1/max(.018,d)**4) for d,n in ds];total=sum(v for _,v in vals);weights=[(n,v/total) for n,v in vals]
        for n,w in weights:groups[n].add([v.index],w,'REPLACE')
    mod=o.modifiers.new('Human skinning','ARMATURE');mod.object=rig;o.parent=rig
    for f in o.data.polygons:f.use_smooth=True

# Store source derivative and FBX, with no hidden source meshes or BlenderKit parts.
bpy.ops.object.select_all(action='SELECT')
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT,'SourceArt/HumanEchoesR7/Human_Fieldwalker.blend'))
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,'Human_Fieldwalker.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True)
print('R7_HUMAN_EXPORTED',len([x for x in bpy.context.scene.objects if x.type=='MESH']))
