from pathlib import Path
import vtk, math
import numpy as np
from PIL import Image,ImageDraw,ImageFont,ImageFilter
ROOT=Path('/mnt/data/ReverbInTheRye_Foliage_v01')
MODELS=ROOT/'Assets/ReverbInTheRye/Foliage/Models'
ATLAS=np.array(Image.open(ROOT/'Assets/ReverbInTheRye/Foliage/Textures/Rye_Foliage_Atlas.png').convert('RGB'))
P=ROOT/'Previews'

font='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
font_b=ImageFont.truetype(font,28)
font_sm=ImageFont.truetype(font,19)

def load_obj(name):
 vertices=[];uvs=[];faces=[]
 with open(MODELS/(name+'.obj')) as f:
  for line in f:
   if line.startswith('v '): vertices.append(list(map(float,line.strip().split()[1:])))
   if line.startswith('vt '): uvs.append(list(map(float,line.strip().split()[1:])))
   if line.startswith('f '): faces.append([int(x.split('/')[0])-1 for x in line.strip().split()[1:]])
 verts=np.asarray(vertices)
 return verts,np.asarray(uvs),np.asarray(faces)

def polydata(names, offsets):
 pd=vtk.vtkPolyData();pts=vtk.vtkPoints(); polys=vtk.vtkCellArray(); colors=vtk.vtkUnsignedCharArray();colors.SetNumberOfComponents(3);colors.SetName('color')
 for name,offset in zip(names,offsets):
  verts,uvs,faces=load_obj(name)
  for v,uv in zip(verts,uvs):
   pts.InsertNextPoint(*((v+np.asarray(offset)).tolist()))
   u,vv=uv
   xx=int(np.clip(u*1023,0,1023));yy=int(np.clip((1-vv)*1023,0,1023))
   color=ATLAS[yy,xx]
   colors.InsertNextTuple3(int(color[0]),int(color[1]),int(color[2]))
  shift=pts.GetNumberOfPoints()-len(verts)
  for f in faces:
    tri=vtk.vtkTriangle()
    for i,j in enumerate(f):tri.GetPointIds().SetId(i,int(j+shift))
    polys.InsertNextCell(tri)
 pd.SetPoints(pts);pd.SetPolys(polys);pd.GetPointData().SetScalars(colors)
 return pd

def render(names, offsets, size=(700,700), camera=(3.0,2.75,4.6), target=(0,.88,0), scale=2.2, floor=True):
 pd=polydata(names,offsets)
 normals=vtk.vtkPolyDataNormals();normals.SetInputData(pd);normals.ComputePointNormalsOn();normals.ConsistencyOff();normals.AutoOrientNormalsOff();normals.SplittingOff();normals.Update()
 mapper=vtk.vtkPolyDataMapper();mapper.SetInputConnection(normals.GetOutputPort())
 actor=vtk.vtkActor();actor.SetMapper(mapper);actor.GetProperty().SetAmbient(.5);actor.GetProperty().SetDiffuse(.75);actor.GetProperty().SetSpecular(.02);actor.GetProperty().BackfaceCullingOff()
 ren=vtk.vtkRenderer();ren.SetBackground(.115,.126,.137)
 ren.AddActor(actor)
 if floor:
  plane=vtk.vtkPlaneSource();plane.SetOrigin(-10,-.025,-10);plane.SetPoint1(10,-.025,-10);plane.SetPoint2(-10,-.025,10)
  plmapper=vtk.vtkPolyDataMapper();plmapper.SetInputConnection(plane.GetOutputPort())
  plactor=vtk.vtkActor();plactor.SetMapper(plmapper);plactor.GetProperty().SetColor(1,1,1);plactor.GetProperty().SetAmbient(.65);plactor.GetProperty().SetDiffuse(.35)
  ground_texture=Path('/mnt/data/ReverbInTheRye_Ground_v02/Assets/ReverbInTheRye/Ground/Textures/Ground_DrySoil_BaseColor.png')
  if len(names)>10 and ground_texture.exists():
   from vtk.util.numpy_support import vtk_to_numpy
   plane.Update()
   uv=vtk_to_numpy(plane.GetOutput().GetPointData().GetTCoords())
   uv[:]*=5.0
   reader=vtk.vtkPNGReader();reader.SetFileName(str(ground_texture));reader.Update()
   texture=vtk.vtkTexture();texture.SetInputConnection(reader.GetOutputPort());texture.RepeatOn();texture.InterpolateOn()
   plactor.SetTexture(texture)
  ren.AddActor(plactor)
 light=vtk.vtkLight();light.SetPosition(3,7,4);light.SetFocalPoint(0,0,0);light.SetColor(1,.91,.77);light.SetIntensity(1.0);ren.AddLight(light)
 light2=vtk.vtkLight();light2.SetPosition(-2,4,-3);light2.SetIntensity(.36);ren.AddLight(light2)
 cam=ren.GetActiveCamera();cam.SetPosition(*camera);cam.SetFocalPoint(*target);cam.SetViewUp(0,1,0);cam.ParallelProjectionOn();cam.SetParallelScale(scale)
 rw=vtk.vtkRenderWindow();rw.SetOffScreenRendering(1);rw.SetSize(*size);rw.AddRenderer(ren);rw.SetMultiSamples(4);rw.Render()
 wt=vtk.vtkWindowToImageFilter();wt.SetInput(rw);wt.SetInputBufferTypeToRGB();wt.ReadFrontBufferOff();wt.Update()
 writer=vtk.vtkPNGWriter();p=P/'_tmp.png';writer.SetFileName(str(p));writer.SetInputConnection(wt.GetOutputPort());writer.Write()
 im=Image.open(p).convert('RGB').copy(); p.unlink(missing_ok=True)
 return im

def card(name, label, height=''):
 im=render([name],[(0,0,0)])
 d=ImageDraw.Draw(im)
 d.rectangle((0,590,700,700),fill=(22,25,28))
 d.text((30,600),label,font=font_b,fill=(240,235,211))
 d.text((30,645),height,font=font_sm,fill=(193,193,185))
 im.save(P/(name+'_Preview.jpg'),quality=91)
 return im

cards=[]
for x in [('Wheat_Cluster_A','Wheat / A','Mature standing ear | LOD0'),('Wheat_Cluster_B','Wheat / B','Leaning ear | LOD0'),('Cut_Stubble_A','Cut stubble','Harvested stalks | 0.50m'),('Wild_Grass_A','Wild grass','Low weeds and dry blades')]:cards.append(card(*x))
board=Image.new('RGB',(1440,1480),(13,16,19))
for idx,im in enumerate(cards):board.paste(im,((idx%2)*720+10,(idx//2)*720+10))
ImageDraw.Draw(board).text((20,1442),'REVERB IN THE RYE  |  ACTUAL GENERATED OBJ MODEL PREVIEWS  |  UNITY URP',font=font_sm,fill=(226,218,193))
board.save(P/'Foliage_Models_Overview.jpg',quality=92)

# Actual geometry scene arrangement to preview visual density. Terrain deliberately simplified.
random=np.random.default_rng(22)
names=[];offs=[]
for idx in range(170):
 x=float(random.uniform(-4.8,4.8));z=float(random.uniform(-4.8,4.8))
 if abs(x)<.85:continue
 name='Wheat_Cluster_A' if idx%3 else 'Wheat_Cluster_B'
 names.append(name);offs.append((x,0,z))
for idx in range(14):
 x=float(random.uniform(-4.8,4.8));z=float(random.uniform(-4.8,4.8))
 names.append('Wild_Grass_A' if idx%2 else 'Cut_Stubble_A');offs.append((x,0,z))
scene=render(names,offs,size=(1440,820),camera=(10,11,15),target=(0,.65,0),scale=6.6)
ImageDraw.Draw(scene).rectangle((0,750,1440,820),fill=(24,24,24))
ImageDraw.Draw(scene).text((30,762),'Foliage patch preview | actual geometry | 170 candidate wheat clumps, central walking lane | NOT a Unity screenshot',font=font_sm,fill=(240,235,216))
scene.save(P/'Wheat_Field_Geometry_Preview.jpg',quality=92)
print('previews created')
