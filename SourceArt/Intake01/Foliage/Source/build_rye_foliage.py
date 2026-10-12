"""Generate original low-poly rye/wheat-field foliage for Reverb in the Rye.
Pure Python mesh authoring (no Blender required). OBJ + texture atlas + GLB when possible.
"""
from pathlib import Path
import math, random, json, zipfile, shutil
import numpy as np
from PIL import Image, ImageFilter

ROOT=Path('/mnt/data/ReverbInTheRye_Foliage_v01')
BASE=ROOT/'Assets'/'ReverbInTheRye'/'Foliage'
MODELS=BASE/'Models'; TEX=BASE/'Textures'; SHADERS=BASE/'Shaders'; EDITOR=BASE/'Editor'; SCENE=BASE/'Runtime'; PREVIEW=ROOT/'Previews'
for p in (MODELS, TEX, SHADERS, EDITOR, SCENE, PREVIEW): p.mkdir(parents=True, exist_ok=True)
np.random.seed(20261012)

# Texture atlas from authored mathematical procedural colors, not sampled photographs.
size=1024
rng=np.random.default_rng(20261012)
noise=rng.standard_normal((size,size)).astype('float32')
from scipy.ndimage import gaussian_filter
micro=gaussian_filter(noise,0.7)
broad=gaussian_filter(noise,12)
vertical=np.cumsum(rng.normal(0,0.25,size).astype('float32'))
vertical=gaussian_filter(vertical,10);vertical=(vertical-vertical.mean())/(vertical.std()+1e-8)
# PIL top-row upper is grain, upper-right is dry straw, lower-left green leaf, lower-right stems
palettes=[([195,145,68],[235,191,99]),([153,113,56],[215,173,94]),([79,91,45],[148,151,72]),([109,101,56],[191,153,79])]
a=np.zeros((size,size,3),dtype=np.uint8)
for q,(c0,c1) in enumerate(palettes):
    x0=(q%2)*(size//2); y0=(q//2)*(size//2)
    ny,nx=size//2,size//2
    yy,xx=np.mgrid[:ny,:nx]
    fib=0.11*np.sin(xx*.11 + yy*.035)+0.07*np.sin(xx*.045)
    texture=np.clip(0.5+0.17*micro[y0:y0+ny,x0:x0+nx]+0.08*broad[y0:y0+ny,x0:x0+nx]+fib,0,1)
    if q==0: texture += .07*np.sin(yy*.07)
    if q==2: texture += .08*np.sin(xx*.029+yy*.01)
    texture=np.clip(texture,0,1)
    colors=np.array(c0)[None,None,:]*(1-texture[:,:,None])+np.array(c1)[None,None,:]*texture[:,:,None]
    a[y0:y0+ny,x0:x0+nx]=np.clip(colors,0,255).astype(np.uint8)
img=Image.fromarray(a,'RGB'); img.save(TEX/'Rye_Foliage_Atlas.png', optimize=True)

# UVs: OpenGL UV origin bottom left, region locations follow display atlas.
regions={'grain':(.03,.47,.53,.97),'straw':(.53,.97,.53,.97),'green':(.03,.47,.03,.47),'stem':(.53,.97,.03,.47)}

def vec(x): return np.array(x,dtype=float)
def normalize(v):
    v=vec(v); n=np.linalg.norm(v)
    return v/n if n>1e-10 else v

class Mesh:
 def __init__(self,name): self.name=name; self.v=[];self.uv=[];self.f=[];self.tags=[]
 def tri(self,p,q,r,kind='straw',rnum=0):
    lo,hi,bot,top=regions[kind]
    # small UV variations but consistent same patch; limits avoiding bleed outside region
    sh=(rnum%7)*0.009
    uv=[(lo+.05+sh,bot+.05),(hi-.05,bot+.07+sh),(lo+.10+sh,top-.05)]
    start=len(self.v)
    for pt,t in zip((p,q,r),uv): self.v.append(tuple(vec(pt))); self.uv.append(t)
    self.f.append((start+1,start+2,start+3));self.tags.append(kind)
 def quad(self,a,b,c,d,kind):self.tri(a,b,c,kind);self.tri(a,c,d,kind)
 def tapered_tube(self,points,radii,kind='stem',sides=5):
    # Parallel-ish orientation, tube along curving axis.
    for i in range(len(points)-1):
     base,tip=vec(points[i]),vec(points[i+1]);axis=normalize(tip-base)
     u=normalize(np.cross(axis,[0,0,1]));
     if np.linalg.norm(u)<.01: u=normalize(np.cross(axis,[1,0,0]))
     v=np.cross(axis,u)
     for k in range(sides):
      theta=k*2*math.pi/sides;next_theta=(k+1)*2*math.pi/sides
      rp=u*math.cos(theta)+v*math.sin(theta)
      rq=u*math.cos(next_theta)+v*math.sin(next_theta)
      a=base+rp*radii[i];b=base+rq*radii[i]
      c=tip+rq*radii[i+1];d=tip+rp*radii[i+1]
      self.quad(a,b,c,d,kind)
 def leaf(self,base,direction,length,width,kind='green',rise=0.05,droop=0.10):
    base=vec(base); direction=normalize(vec(direction))
    horizontal=vec((direction[0],0,direction[2])); horizontal=normalize(horizontal)
    if np.linalg.norm(horizontal)<.01: horizontal=vec((1,0,0))
    across=normalize(np.cross([0,1,0],horizontal))
    # leaf is a ribbon with rise then droop and pointed tip
    centers=[]
    for t in (0.,.25,.55,.8,1.):
     c=base+horizontal*length*t+vec((0,rise*math.sin(math.pi*t)-droop*t*t,0))
     c[1] = max(c[1], .008)  # no leaves dipping below terrain y=0
     centers.append(c)
    widths=[.30,1.,.72,.43,.01]
    for i in range(4):
     p=centers[i];q=centers[i+1];w0=width*widths[i];w1=width*widths[i+1]
     self.quad(p-across*w0/2,q-across*w1/2,q+across*w1/2,p+across*w0/2,kind)
 def write_obj(self,folder):
    path=folder/(self.name+'.obj')
    with open(path,'w',encoding='utf-8') as f:
      f.write('# Reverb in the Rye original authored low-poly foliage (meters, Y-up)\n')
      f.write('mtllib Rye_Foliage.mtl\no '+self.name+'\nusemtl Rye_Foliage\ns off\n')
      for p in self.v:f.write('v %.6f %.6f %.6f\n'%p)
      for uv in self.uv:f.write('vt %.6f %.6f\n'%uv)
      for inds in self.f:f.write('f %d/%d %d/%d %d/%d\n'%(inds[0],inds[0],inds[1],inds[1],inds[2],inds[2]))
    return path
 def info(self):
    a=np.asarray(self.v)
    return {'model':self.name,'triangles':len(self.f),'vertices_no_index_reuse':len(self.v),'bounds_m':{'minimum':a.min(axis=0).round(3).tolist(),'maximum':a.max(axis=0).round(3).tolist()}}

material='''# Atlas-based original rye foliage material\nnewmtl Rye_Foliage\nKa 0.18 0.18 0.18\nKd 1.0 1.0 1.0\nKs 0.02 0.02 0.02\nNs 8.0\nd 1.0\nillum 2\nmap_Kd ../Textures/Rye_Foliage_Atlas.png\n'''
(MODELS/'Rye_Foliage.mtl').write_text(material,encoding='utf8')

def wheat(mesh,seed,base,height=1.65,lean=0.2,heading=0.0,lean_side=None,lod=False):
 r=random.Random(seed)
 base=vec(base)
 if lean_side is None:lean_side=r.uniform(0,2*math.pi)
 tilt=vec((math.cos(lean_side),0,math.sin(lean_side)))
 def center(t):return base+vec((0,height*t,0))+tilt*(lean*t*t)
 shaft_end=height-(.20 if lod else .25)
 nseg=3 if lod else 5
 pts=[center((shaft_end/height)*i/nseg) for i in range(nseg+1)]
 mesh.tapered_tube(pts,np.linspace(.011,.0035,nseg+1),'stem',4 if lod else 5)
 for i in range(1 if lod else 2):
    yy=(.19 if i==0 else .47)*height
    p=center(yy/height)
    angle=lean_side+(1 if i==0 else -1)*1.4+r.uniform(-.5,.5)
    d=(math.cos(angle),0,math.sin(angle))
    mesh.leaf(p,d,r.uniform(.30,.49),r.uniform(.022,.042),'green' if i==0 else 'straw',rise=.07,droop=.14)
 # Head centreline follows stem lean; axis moderately tilted
 headroot=center(shaft_end/height)
 a=normalize(vec((tilt[0]*lean/height*.9,1.,tilt[2]*lean/height*.9)))
 ear_len=.21 if lod else .26
 mesh.tapered_tube([headroot,headroot+a*ear_len*.45,headroot+a*ear_len], [.022,.035,.007],'grain',4 if lod else 6)
 # grain kernels tetrahedra around spine, arranged rings
 spokes=3 if lod else 4
 rings=3 if lod else 4
 u=normalize(np.cross(a,vec((0,0,1))))
 if np.linalg.norm(u)<.01:u=vec((1,0,0))
 v=normalize(np.cross(a,u))
 for z in range(rings):
    t=.16+z*(.64/(rings-1)); pos=headroot+a*ear_len*t
    for k in range(spokes):
       ang=2*math.pi*(k+.42*(z%2))/spokes
       radial=normalize(u*math.cos(ang)+v*math.sin(ang))
       side=normalize(np.cross(a,radial))
       scale=1.-.32*abs(t-.49)/.5
       p0=pos-radial*.006-a*.034
       p1=pos+radial*(.040*scale)+a*.016
       p2=pos+radial*(.019*scale)-side*.017*scale
       p3=pos+radial*(.019*scale)+side*.017*scale
       mesh.tri(p0,p1,p2,'grain');mesh.tri(p0,p3,p1,'grain')
       mesh.tri(p0,p2,p3,'grain');mesh.tri(p1,p3,p2,'grain')
       if not lod:
        awnstart=p1+a*.012;awntip=awnstart+a*r.uniform(.12,.22)+radial*r.uniform(.04,.085)
        mesh.tri(awnstart-side*.003,awnstart+side*.003,awntip,'straw')
 if not lod:
    tip=headroot+a*ear_len
    for k in range(4):
        ang=k*math.pi/2; radial=u*math.cos(ang)+v*math.sin(ang)
        mesh.tri(tip+radial*.005,tip-radial*.005,tip+a*r.uniform(.18,.28)+radial*.02,'straw')

def wheat_cluster(name,seed,stalk_count,variant,lod=False):
 r=random.Random(seed);m=Mesh(name)
 for i in range(stalk_count):
    phi=i*2.39996+r.uniform(-.2,.2)
    rad=(i/max(1,stalk_count))**.55 * (0.23 if variant=='A' else .29)
    h=1.55 + r.uniform(-.15,.22)
    lean=r.uniform(.06,.20) if variant=='A' else r.uniform(.13,.37)
    wheat(m,seed+i*73,(rad*math.cos(phi),0,rad*math.sin(phi)),h,lean,lean_side=phi+.42,lod=lod)
 return m

def stubble(name,seed=18):
 r=random.Random(seed);m=Mesh(name)
 for i in range(23):
  rad=.23*math.sqrt(r.random());ang=r.uniform(0,math.tau)
  x,z=rad*math.cos(ang),rad*math.sin(ang)
  h=r.uniform(.16,.52);side=r.uniform(0,math.tau);bend=r.uniform(.02,.14)
  start=vec((x,0,z));mid=start+vec((.32*bend*math.cos(side),h*.45,.32*bend*math.sin(side)));end=start+vec((bend*math.cos(side),h,bend*math.sin(side)))
  m.tapered_tube([start,mid,end],[.009,.006,.001],'straw',3)
  if i%3==0: m.leaf(start+vec((0,h*.3,0)),(math.cos(ang),0,math.sin(ang)),r.uniform(.12,.23),.015,'straw',rise=.012,droop=.1)
 return m

def tuft(name,seed=42):
 r=random.Random(seed);m=Mesh(name)
 for i in range(28):
  ang=2.39996*i+r.uniform(-.3,.3);dist=r.uniform(0,.11)
  bas=vec((dist*math.cos(ang),.01,dist*math.sin(ang)))
  length=r.uniform(.16,.43)
  kind='green' if i%4 else 'straw'
  m.leaf(bas,(math.cos(ang),0,math.sin(ang)),length,r.uniform(.016,.035),kind,rise=.27,droop=r.uniform(.08,.17))
 return m

models=[wheat_cluster('Wheat_Cluster_A',90,5,'A'),wheat_cluster('Wheat_Cluster_B',116,4,'B'),wheat_cluster('Wheat_Cluster_A_LOD1',90,4,'A',True),wheat_cluster('Wheat_Cluster_B_LOD1',116,3,'B',True),stubble('Cut_Stubble_A'),tuft('Wild_Grass_A')]
infos=[]
for mesh in models:
    mesh.write_obj(MODELS); infos.append(mesh.info())

# Optional GLBs for Blender, authored with the same UVs (trimesh can include PNG as texture).
try:
 import trimesh
 material=trimesh.visual.material.PBRMaterial(name='Rye_Foliage',baseColorTexture=img,roughnessFactor=.88,metallicFactor=0)
 for m in models:
  tm=trimesh.Trimesh(vertices=np.array(m.v,dtype=np.float32),faces=np.array(m.f,dtype=np.int32)-1,process=False)
  tm.visual=trimesh.visual.TextureVisuals(uv=np.asarray(m.uv),material=material)
  tm.export(MODELS/(m.name+'.glb'),file_type='glb')
except Exception as e:
 print('optional GLB generation failed:',e)

(ROOT/'mesh_stats.json').write_text(json.dumps(infos,ensure_ascii=False,indent=2),encoding='utf8')
print('\n'.join(f"{x['model']}: {x['triangles']} tris, height {x['bounds_m']['maximum'][1]}m" for x in infos))
