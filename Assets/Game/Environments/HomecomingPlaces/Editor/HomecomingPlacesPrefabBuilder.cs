using System.IO;
using UnityEditor;
using UnityEngine;
using Waffle.Homecoming.Places;

namespace Reverb.Homecoming.Places.EditorTools
{
    /// <summary>
    /// 手动菜单：用Unity API把三处地点Prefab生成到 Environments/HomecomingPlaces/Prefabs。
    /// 不自动运行、不打开/保存/重建Main场景；只用原始几何与既有R3材质。所有碰撞体被移除（无空气墙）。
    /// 状态：作者未在Unity中运行过本工具，生成结果未验证。
    /// </summary>
    public static class HomecomingPlacesPrefabBuilder
    {
        const string Folder = "Assets/Game/Environments/HomecomingPlaces/Prefabs";
        const string R3 = "Assets/Game/Art/VisualIdentity/HomecomingR3/";
        static readonly Color WarmLamp = new Color(1f, 0.72f, 0.42f);

        [MenuItem("Reverb/Homecoming/生成三处地点Prefab（HC-A）")]
        public static void BuildAll()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Game/Environments/HomecomingPlaces", "Prefabs");
            Save(BuildOldHome(), "P_HC_OldHome");
            Save(BuildSilo(), "P_HC_OldSilo");
            Save(BuildGuitarCorner(), "P_HC_OldGuitar");
            AssetDatabase.SaveAssets();
            Debug.Log("HC-A: 三处地点Prefab已生成到 " + Folder + "（需人工检视并在Main中由Codex绑定）");
        }

        static Material M(string name)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(R3 + name + ".mat");
            if (m == null) Debug.LogWarning("HC-A: 缺少R3材质 " + name);
            return m;
        }

        static Material Brass() => AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Art/VisualIdentity/Materials/Brushed_Brass.mat");

        static void Save(GameObject go, string name)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            PrefabUtility.SaveAsPrefabAsset(go, Path.Combine(Folder, name + ".prefab").Replace('\\', '/'));
            Object.DestroyImmediate(go);
        }

        static GameObject Part(Transform parent, PrimitiveType t, string name, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default)
        {
            var g = GameObject.CreatePrimitive(t);
            g.name = name; g.transform.SetParent(parent, false);
            g.transform.localPosition = pos; g.transform.localEulerAngles = euler; g.transform.localScale = scale;
            var c = g.GetComponent<Collider>(); if (c != null) Object.DestroyImmediate(c);
            if (mat != null) g.GetComponent<Renderer>().sharedMaterial = mat;
            return g;
        }

        // 有灯罩/玻璃实体的暖光：灯罩(铁) + 玻璃球 + 点光源
        static Light Lamp(Transform parent, string name, Vector3 pos, float range)
        {
            var holder = new GameObject(name).transform; holder.SetParent(parent, false); holder.localPosition = pos;
            Part(holder, PrimitiveType.Cylinder, "Shade", new Vector3(0, .12f, 0), new Vector3(.34f, .04f, .34f), M("Old iron"));
            Part(holder, PrimitiveType.Sphere, "Glass", Vector3.zero, Vector3.one * .22f, M("Warm practical lamp glass"));
            var l = new GameObject("Light").AddComponent<Light>();
            l.transform.SetParent(holder, false); l.transform.localPosition = new Vector3(0, -.1f, 0);
            l.type = LightType.Point; l.color = WarmLamp; l.range = range; l.intensity = .15f; l.shadows = LightShadows.Soft;
            return l;
        }

        static HomecomingPlaceView Root(string name, string template, out Transform t)
        {
            var go = new GameObject(name); t = go.transform;
            var v = go.AddComponent<HomecomingPlaceView>(); v.templateId = template; return v;
        }

        // 旧家：外部可接近的单层木屋、门廊、门框身高刻痕、窗台针线盒；门前留约8米空地（-Z朝路）。
        static GameObject BuildOldHome()
        {
            var v = Root("P_HC_OldHome", HomecomingPlaceCatalog.OldHome, out var t);
            Material wood = M("Sun-bleached timber"), cream = M("Faded cream enamel"), iron = M("Old iron"), stone = M("Field limestone");
            Part(t, PrimitiveType.Cube, "Foundation", new Vector3(0, .15f, 4f), new Vector3(7.2f, .3f, 5.6f), stone);
            Part(t, PrimitiveType.Cube, "Walls", new Vector3(0, 1.75f, 4f), new Vector3(7f, 2.9f, 5.4f), cream);
            Part(t, PrimitiveType.Cube, "RoofL", new Vector3(-1.85f, 3.85f, 4f), new Vector3(4.2f, .18f, 6.2f), wood, new Vector3(0, 0, 32));
            Part(t, PrimitiveType.Cube, "RoofR", new Vector3(1.85f, 3.85f, 4f), new Vector3(4.2f, .18f, 6.2f), wood, new Vector3(0, 0, -32));
            Part(t, PrimitiveType.Cube, "Chimney", new Vector3(2.2f, 4.4f, 5f), new Vector3(.6f, 1.6f, .6f), stone);
            Part(t, PrimitiveType.Cube, "PorchDeck", new Vector3(0, .2f, .7f), new Vector3(4.4f, .12f, 1.6f), wood);
            Part(t, PrimitiveType.Cube, "PorchRoof", new Vector3(0, 2.6f, .7f), new Vector3(4.6f, .1f, 1.8f), wood, new Vector3(-8, 0, 0));
            Part(t, PrimitiveType.Cube, "PostL", new Vector3(-2.1f, 1.4f, 0f), new Vector3(.14f, 2.4f, .14f), wood);
            Part(t, PrimitiveType.Cube, "PostR", new Vector3(2.1f, 1.4f, 0f), new Vector3(.14f, 2.4f, .14f), wood);
            Part(t, PrimitiveType.Cube, "DoorFrame", new Vector3(0, 1.3f, 1.27f), new Vector3(1.2f, 2.2f, .06f), wood);
            Part(t, PrimitiveType.Cube, "Door", new Vector3(0, 1.25f, 1.25f), new Vector3(.95f, 2.05f, .06f), M("Faded tractor vermilion"));
            for (int i = 0; i < 6; i++) // 身高刻痕，最后一道最高
                Part(t, PrimitiveType.Cube, "HeightMark" + i, new Vector3(.6f, .7f + i * .17f, 1.22f), new Vector3(.16f, .015f, .02f), iron);
            Part(t, PrimitiveType.Cube, "KitchenWindow", new Vector3(-2.3f, 1.7f, 1.27f), new Vector3(1.1f, .9f, .05f), M("Dormant broadcast glass"));
            Part(t, PrimitiveType.Cube, "Sill", new Vector3(-2.3f, 1.22f, 1.18f), new Vector3(1.3f, .06f, .22f), wood);
            v.relicProp = Part(t, PrimitiveType.Cylinder, "SewingTin", new Vector3(-2.3f, 1.29f, 1.15f), new Vector3(.22f, .04f, .22f), Brass() ?? iron);
            v.warmLamps = new[] { Lamp(t, "PorchLamp", new Vector3(-.85f, 2.2f, 1.1f), 7f), Lamp(t, "WindowLamp", new Vector3(-2.3f, 1.9f, 2.0f), 4f) };
            return t.gameObject;
        }

        // 老筒仓：高圆柱+锥顶的清晰剪影，侧门粉笔线，门边一盏暖灯；四周开阔可绕行。
        static GameObject BuildSilo()
        {
            var v = Root("P_HC_OldSilo", HomecomingPlaceCatalog.OldSilo, out var t);
            Material iron = M("Old iron"), stone = M("Field limestone"), cream = M("Faded cream enamel"), wood = M("Sun-bleached timber");
            Part(t, PrimitiveType.Cylinder, "Base", new Vector3(0, .25f, 0), new Vector3(4.6f, .25f, 4.6f), stone);
            Part(t, PrimitiveType.Cylinder, "Body", new Vector3(0, 4.5f, 0), new Vector3(4.2f, 4.2f, 4.2f), cream);
            for (int i = 0; i < 4; i++) Part(t, PrimitiveType.Cylinder, "Band" + i, new Vector3(0, 1.6f + i * 2f, 0), new Vector3(4.28f, .05f, 4.28f), iron);
            Part(t, PrimitiveType.Sphere, "Dome", new Vector3(0, 8.7f, 0), new Vector3(4.3f, 1.6f, 4.3f), iron);
            Part(t, PrimitiveType.Cube, "Door", new Vector3(0, 1.2f, -2.08f), new Vector3(1.1f, 2f, .08f), wood);
            for (int i = 0; i < 5; i++)
                Part(t, PrimitiveType.Cube, "ChalkLine" + i, new Vector3(.85f, .6f + i * .32f, -2.1f), new Vector3(.3f, .02f, .02f), cream);
            Part(t, PrimitiveType.Cube, "Ladder", new Vector3(1.6f, 4f, -1.4f), new Vector3(.08f, 7.5f, .5f), iron, new Vector3(0, 45, 0));
            Part(t, PrimitiveType.Cube, "Crate", new Vector3(-1.4f, .3f, -2.6f), new Vector3(.7f, .6f, .7f), wood);
            v.relicProp = Part(t, PrimitiveType.Cube, "WorkGloves", new Vector3(-1.4f, .66f, -2.6f), new Vector3(.32f, .06f, .22f), M("Old tyre rubber"));
            v.warmLamps = new[] { Lamp(t, "DoorLamp", new Vector3(-.9f, 2.6f, -2.3f), 8f) };
            return t.gameObject;
        }

        // 倒下的苹果树+旧吉他：横倒树干、翻起的根、残枝；挂在残枝上的马灯。
        static GameObject BuildGuitarCorner()
        {
            var v = Root("P_HC_OldGuitar", HomecomingPlaceCatalog.OldGuitar, out var t);
            Material wood = M("Sun-bleached timber"), straw = M("Harvested ochre straw"), rut = M("Dark wheel rut"), iron = M("Old iron");
            Part(t, PrimitiveType.Cylinder, "Trunk", new Vector3(1.2f, .45f, 1.5f), new Vector3(.7f, 2.6f, .7f), wood, new Vector3(0, 20, 84));
            Part(t, PrimitiveType.Sphere, "RootPlate", new Vector3(3.8f, .9f, 2.4f), new Vector3(.6f, 2f, 2.2f), rut, new Vector3(0, 20, 0));
            Part(t, PrimitiveType.Cylinder, "Branch1", new Vector3(-1.6f, 1f, .6f), new Vector3(.18f, 1.2f, .18f), wood, new Vector3(30, 0, 40));
            Part(t, PrimitiveType.Cylinder, "Branch2", new Vector3(-1.1f, 1.5f, 1.4f), new Vector3(.12f, .9f, .12f), wood, new Vector3(-20, 0, -15));
            Part(t, PrimitiveType.Sphere, "DryLeaves", new Vector3(-2.2f, .7f, 1.1f), new Vector3(2.2f, .9f, 1.8f), straw);
            var guitar = new GameObject("OldGuitar").transform; guitar.SetParent(t, false);
            guitar.localPosition = new Vector3(.4f, .55f, .55f); guitar.localEulerAngles = new Vector3(-70, 15, 0);
            Part(guitar, PrimitiveType.Sphere, "BodyLow", new Vector3(0, -.18f, 0), new Vector3(.42f, .36f, .12f), wood);
            Part(guitar, PrimitiveType.Sphere, "BodyHigh", new Vector3(0, .1f, 0), new Vector3(.32f, .28f, .12f), wood);
            Part(guitar, PrimitiveType.Cube, "Neck", new Vector3(0, .55f, 0), new Vector3(.06f, .65f, .04f), rut);
            Part(guitar, PrimitiveType.Cylinder, "SoundHole", new Vector3(0, -.02f, -.061f), new Vector3(.1f, .003f, .1f), rut, new Vector3(90, 0, 0));
            v.relicProp = guitar.gameObject;
            v.warmLamps = new[] { Lamp(t, "Lantern", new Vector3(-1.3f, 1.9f, 1.5f), 6f) };
            Part(t, PrimitiveType.Cylinder, "LanternHook", new Vector3(-1.3f, 2.1f, 1.5f), new Vector3(.02f, .1f, .02f), iron);
            return t.gameObject;
        }
    }
}
