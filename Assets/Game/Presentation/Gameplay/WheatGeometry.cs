using System.Collections.Generic;
using UnityEngine;

namespace Reverb.Drafts.RC01.Presentation
{
    /// <summary>Original solid ears and stems, with curved thin leaves. No billboard atlas or alpha silhouettes.</summary>
    public static class WheatGeometry
    {
        public const float Height = 1.78f;
        public static Mesh Create(int detail, int variant)
        {
            var b = new Builder();
            for (int shoot = 0; shoot < (detail == 0 ? 3 : detail == 1 ? 2 : 1); shoot++)
            {
                float angle = shoot * 2.39996f + variant * .71f;
                float scale = shoot == 0 ? 1 : .81f + .04f * ((variant + shoot) % 3);
                Vector3 origin = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (shoot == 0 ? .025f : .16f);
                var lean = new Vector3(Mathf.Cos(angle) * .12f, 0, Mathf.Sin(angle) * .12f);
                b.Shoot(origin, lean, scale, detail, angle);
            }
            var mesh = new Mesh { name = "Wheat volume " + variant + " LOD" + detail };
            mesh.SetVertices(b.v); mesh.SetUVs(0, b.uv);
            var roots=new List<Vector4>(b.v.Count);foreach(var unused in b.v)roots.Add(new Vector4(0,0,Height,variant*.25f));
            mesh.SetUVs(1,roots);mesh.SetTriangles(b.tri, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        sealed class Builder
        {
            public readonly List<Vector3> v = new List<Vector3>();
            public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<int> tri = new List<int>();
            int Add(Vector3 p, float part, float flutter = 0) { int n = v.Count; v.Add(p); uv.Add(new Vector2(part, flutter)); return n; }
            void Face(int a, int b, int c) { tri.Add(a); tri.Add(b); tri.Add(c); }
            Vector3 Stem(Vector3 root, Vector3 lean, float y, float scale) => root + lean * (y * y / Height) + Vector3.up * (y * scale);
            public void Shoot(Vector3 root, Vector3 lean, float scale, int detail, float angle)
            {
                int sides = detail == 0 ? 4 : 3, rings = detail == 0 ? 4 : 2, start = v.Count;
                for (int ring = 0; ring < rings; ring++)
                {
                    float y = ring * 1.36f / (rings - 1);
                    Vector3 center = Stem(root, lean, y, scale);
                    float radius = Mathf.Lerp(.012f, .006f, y / 1.36f);
                    for (int s = 0; s < sides; s++) { float a = s * Mathf.PI * 2 / sides; Add(center + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius, 0); }
                }
                for (int r = 0; r < rings - 1; r++) for (int s = 0; s < sides; s++)
                { int a = start + r * sides + s, b = start + r * sides + (s + 1) % sides; Face(a, a + sides, b); Face(b, a + sides, b + sides); }
                int leaves = detail == 0 ? 3 : detail == 1 ? 2 : 1;
                for (int leaf = 0; leaf < leaves; leaf++)
                {
                    float y = .38f + leaf * .29f, a = angle + leaf * 2.8f;
                    Vector3 direction = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    Vector3 sideways = Vector3.Cross(Vector3.up, direction);
                    Vector3 attach = Stem(root, lean, y, scale);
                    int segments = detail == 0 ? 4 : 2, first = v.Count;
                    for (int j = 0; j <= segments; j++)
                    {
                        float t = j / (float)segments, width = .038f * Mathf.Sin(Mathf.PI * t) + .001f;
                        Vector3 center = attach + direction * (.42f * t) + Vector3.up * (Mathf.Sin(t * 2.8f) * .19f - t * .08f);
                        Add(center - sideways * width, .4f, t); Add(center + Vector3.up * (width * .23f), .4f, t); Add(center + sideways * width, .4f, t);
                    }
                    for (int j = 0; j < segments; j++) for (int edge = 0; edge < 2; edge++)
                    { int a0 = first + j * 3 + edge; Face(a0, a0 + 3, a0 + 1); Face(a0 + 1, a0 + 3, a0 + 4); }
                }
                Vector3 ear = Stem(root, lean, 1.34f, scale);
                if (detail == 0)
                {
                    for (int row = 0; row < 5; row++) for (int side = -1; side <= 1; side += 2)
                    {
                        float a = angle + .28f * row, taper = 1 - row * .065f;
                        Vector3 radial = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * side;
                        Vector3 center = ear + Vector3.up * (row * .057f * scale) + radial * (.024f * taper);
                        Grain(center, radial, .031f * taper, .057f * scale);
                        Vector3 tip = center + radial * .029f + Vector3.up * .04f;
                        Vector3 across = Vector3.Cross(Vector3.up, radial) * .0023f;
                        int awn = Add(tip - across, 1); Add(tip + across, 1); Add(tip + Vector3.up * (.17f * scale) + radial * .056f, 1, 1); Face(awn, awn + 1, awn + 2);
                    }
                }
                else
                {
                    int sCount = detail == 1 ? 5 : 3, first = v.Count;
                    for (int row = 0; row < 4; row++) for (int s = 0; s < sCount; s++)
                    {
                        float a = s * Mathf.PI * 2 / sCount + angle, radius = row == 0 ? .024f : row == 3 ? .007f : .048f;
                        Add(ear + Vector3.up * (row * .115f * scale) + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius, .8f);
                    }
                    for (int row = 0; row < 3; row++) for (int s = 0; s < sCount; s++)
                    { int a = first + row * sCount + s, b = first + row * sCount + (s + 1) % sCount; Face(a, a + sCount, b); Face(b, a + sCount, b + sCount); }
                    int cap = Add(ear + Vector3.up * (.345f * scale), .8f);
                    for (int s = 0; s < sCount; s++) Face(cap, first + 3 * sCount + s, first + 3 * sCount + (s + 1) % sCount);
                }
            }
            void Grain(Vector3 center, Vector3 radial, float radius, float length)
            {
                Vector3 across = Vector3.Cross(Vector3.up, radial);
                int first = Add(center - Vector3.up * length, .8f);
                Add(center + radial * radius, .8f); Add(center + across * (radius * .7f), .8f);
                Add(center - radial * (radius * .6f), .8f); Add(center - across * (radius * .7f), .8f);
                Add(center + Vector3.up * length + radial * .012f, .8f);
                for (int s = 0; s < 4; s++) { int a = first + 1 + s, b = first + 1 + (s + 1) % 4; Face(first, b, a); Face(first + 5, a, b); }
            }
        }
    }
}
