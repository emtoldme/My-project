using System.IO;
using UnityEditor;
using UnityEngine;

namespace Reverb.Drafts.RC01.Pipeline.EditorTools
{
    public static class GardenFoliageTools
    {
        [MenuItem("Reverb/更新植被遮罩")]
        public static void RefreshMask()
        {
            var garden=GameObject.Find("Acoustic Garden");
            var terrain=garden!=null?garden.GetComponentInChildren<Terrain>():null;
            if(terrain==null){Debug.LogWarning("请先打开声学花园主场景。");return;}
            var data=terrain.terrainData;int size=data.alphamapResolution;
            var layers=data.GetAlphamaps(0,0,size,size);
            var image=new Texture2D(size,size,TextureFormat.RGB24,false,true);
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {float grass=Mathf.Pow(1-layers[y,x,0],2);pixels[y*size+x]=new Color(grass,grass,grass);}
            image.SetPixels(pixels);image.Apply();
            const string path="Assets/Game/Environments/AcousticGarden/Foliage/GroundMask.png";
            File.WriteAllBytes(path,image.EncodeToPNG());Object.DestroyImmediate(image);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.SaveAndReimport();
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Art/VisualIdentity/Materials/Meadow_Gold.mat");
            material.SetTexture("_GroundMask",AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            material.SetVector("_FieldBounds",new Vector4(terrain.transform.position.x,terrain.transform.position.z,data.size.x,data.size.z));
            EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();Debug.Log("GARDEN_MASK_UPDATED");
        }
    }
}
