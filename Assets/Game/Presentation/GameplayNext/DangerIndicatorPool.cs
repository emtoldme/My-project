using System.Collections.Generic;
using UnityEngine;

namespace Reverb.Drafts.RC01.Presentation.GameplayNext
{
    /// <summary>
    /// 共享危险预警几何渲染器池（Circle / Capsule / Annulus）。
    /// 遵守固定 12 个对象的预算限制，预警、激活、结束三态清晰区分，不使用新程序集或包。
    /// </summary>
    public sealed class DangerIndicatorPool : MonoBehaviour
    {
        [Header("预算上限")]
        public const int MaxDangerIndicators = 12;
        public const int MaxSpawnIndicators = 8;

        [Header("材质引用")]
        public Material lineMaterial;
        public Material fillMaterial;

        // 颜色设计：使用高对比边界 + 填充扫描 + 几何方向，避免单一依靠红黄颜色
        static readonly Color TelegraphBorderColor = new Color(1.0f, 0.42f, 0.15f, 0.95f); // 警示橙红高亮边界
        static readonly Color TelegraphFillColor = new Color(1.0f, 0.35f, 0.10f, 0.28f);   // 半透充能扫频填充
        static readonly Color ActiveBorderColor = new Color(1.0f, 0.95f, 0.30f, 1.0f);     // 生效瞬间亮黄白极性高光
        static readonly Color ActiveFillColor = new Color(1.0f, 0.85f, 0.20f, 0.55f);      // 生效瞬间高能量冲击面
        static readonly Color ResolvedBorderColor = new Color(0.6f, 0.6f, 0.6f, 0.25f);    // 消散灰白残响

        sealed class IndicatorSlot
        {
            public GameObject Root;
            public LineRenderer OuterLine;
            public LineRenderer InnerLine;
            public LineRenderer DirectionArrow;
            public MeshFilter FillFilter;
            public MeshRenderer FillRenderer;
            public Mesh FillMesh;

            public int CurrentAttackId = -1;
            public DangerShapeType Shape;
            public DangerVisualState State;
            public bool InUse;

            public void Reset()
            {
                InUse = false;
                CurrentAttackId = -1;
                if (Root != null) Root.SetActive(false);
                if (InnerLine != null) InnerLine.enabled = false;
                if (DirectionArrow != null) DirectionArrow.enabled = false;
                if (FillMesh != null) FillMesh.Clear();
            }
        }

        readonly List<IndicatorSlot> slots = new List<IndicatorSlot>(MaxDangerIndicators);
        Material runtimeLineMat;
        Material runtimeFillMat;

        void Awake()
        {
            InitMaterials();
            BuildPool();
        }

        void InitMaterials()
        {
            // 默认回退使用 URP/Particles/Unlit 或内置材质，避免任何粉红材质丢失
            var shader = Shader.Find("Reverb/GameplayIndicator");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            runtimeLineMat = lineMaterial != null ? new Material(lineMaterial) : new Material(shader);
            runtimeFillMat = fillMaterial != null ? new Material(fillMaterial) : new Material(shader);

            // 确保双面半透混合
            if (runtimeFillMat.HasProperty("_Surface")) runtimeFillMat.SetFloat("_Surface", 1);
            if (runtimeFillMat.HasProperty("_ZWrite")) runtimeFillMat.SetInt("_ZWrite", 0);
        }

        void BuildPool()
        {
            for (int i = 0; i < MaxDangerIndicators + MaxSpawnIndicators; i++)
            {
                var root = new GameObject($"DangerIndicator_{i:00}");
                root.transform.SetParent(transform, false);

                // 外轮廓 LineRenderer
                var outerGo = new GameObject("OuterLine");
                outerGo.transform.SetParent(root.transform, false);
                var outerLine = outerGo.AddComponent<LineRenderer>();
                ConfigureLine(outerLine, 48, 0.12f);

                // 内轮廓（环形使用）
                var innerGo = new GameObject("InnerLine");
                innerGo.transform.SetParent(root.transform, false);
                var innerLine = innerGo.AddComponent<LineRenderer>();
                ConfigureLine(innerLine, 48, 0.08f);
                innerLine.enabled = false;

                // 方向箭头 / 冲锋轴线
                var arrowGo = new GameObject("DirectionArrow");
                arrowGo.transform.SetParent(root.transform, false);
                var arrowLine = arrowGo.AddComponent<LineRenderer>();
                ConfigureLine(arrowLine, 5, 0.15f);
                arrowLine.enabled = false;

                // 填充网格
                var fillGo = new GameObject("FillMesh");
                fillGo.transform.SetParent(root.transform, false);
                var filter = fillGo.AddComponent<MeshFilter>();
                var renderer = fillGo.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = runtimeFillMat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                var mesh = new Mesh { name = $"DangerMesh_{i}" };
                filter.mesh = mesh;

                var slot = new IndicatorSlot
                {
                    Root = root,
                    OuterLine = outerLine,
                    InnerLine = innerLine,
                    DirectionArrow = arrowLine,
                    FillFilter = filter,
                    FillRenderer = renderer,
                    FillMesh = mesh,
                    InUse = false
                };
                root.SetActive(false);
                slots.Add(slot);
            }
        }

        void ConfigureLine(LineRenderer line, int count, float width)
        {
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = count;
            line.startWidth = width;
            line.endWidth = width;
            line.sharedMaterial = runtimeLineMat;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
        }

        /// <summary>
        /// 每帧或按需同步只读危险描述符列表。超出 12 个预算的丢弃。
        /// </summary>
        public void SyncIndicators(IReadOnlyList<DangerAreaDescriptor> descriptors)
        {
            int attackCount = 0, spawnCount = 0;
            if (descriptors != null)
            {
                int count = descriptors.Count;
                for (int i = 0; i < count; i++)
                {
                    var d = descriptors[i];
                    if (d.IsSpawnWarning ? spawnCount >= MaxSpawnIndicators : attackCount >= MaxDangerIndicators) continue;
                    var slot = slots[d.IsSpawnWarning ? MaxDangerIndicators + spawnCount++ : attackCount++];
                    slot.InUse = true;
                    slot.CurrentAttackId = d.AttackId;
                    slot.Shape = d.Shape;
                    slot.State = d.State;
                    slot.Root.SetActive(true);

                    UpdateIndicatorVisual(slot, d);
                }
            }

            // 清理并隐藏未使用的槽位
            for (int i = 0; i < slots.Count; i++)
            {
                if (i < MaxDangerIndicators ? i < attackCount : i < MaxDangerIndicators + spawnCount) continue;
                if (slots[i].InUse)
                {
                    slots[i].Reset();
                }
            }
        }

        void UpdateIndicatorVisual(IndicatorSlot slot, DangerAreaDescriptor d)
        {
            Color borderColor;
            Color fillColor;

            switch (d.State)
            {
                case DangerVisualState.Active:
                    borderColor = ActiveBorderColor;
                    fillColor = ActiveFillColor;
                    break;
                case DangerVisualState.Resolved:
                    borderColor = ResolvedBorderColor;
                    fillColor = new Color(ResolvedBorderColor.r, ResolvedBorderColor.g, ResolvedBorderColor.b, 0.08f);
                    break;
                case DangerVisualState.Telegraphing:
                default:
                    // 随着进度增加，透明度和饱和度呼吸增强
                    float pulse = 0.85f + 0.15f * Mathf.Sin(d.Progress * Mathf.PI * 4f);
                    borderColor = new Color(TelegraphBorderColor.r, TelegraphBorderColor.g, TelegraphBorderColor.b, TelegraphBorderColor.a * pulse);
                    fillColor = new Color(TelegraphFillColor.r, TelegraphFillColor.g, TelegraphFillColor.b, TelegraphFillColor.a * (0.5f + 0.5f * d.Progress));
                    break;
            }

            slot.OuterLine.startColor = borderColor;
            slot.OuterLine.endColor = borderColor;

            switch (d.Shape)
            {
                case DangerShapeType.Circle:
                    RenderCircle(slot, d, borderColor, fillColor);
                    break;
                case DangerShapeType.Capsule:
                    RenderCapsule(slot, d, borderColor, fillColor);
                    break;
                case DangerShapeType.Annulus:
                    RenderAnnulus(slot, d, borderColor, fillColor);
                    break;
            }
        }

        void RenderCircle(IndicatorSlot slot, DangerAreaDescriptor d, Color borderColor, Color fillColor)
        {
            slot.InnerLine.enabled = false;
            slot.DirectionArrow.enabled = false;

            int segs = 40;
            slot.OuterLine.positionCount = segs;
            slot.OuterLine.loop = true;

            Vector3[] positions = new Vector3[segs];
            Vector3 center = d.Position;
            center.y += 0.05f; // 轻微浮起避免麦田地表穿模 Z-fighting

            for (int i = 0; i < segs; i++)
            {
                float angle = i * Mathf.PI * 2f / segs;
                positions[i] = center + new Vector3(Mathf.Cos(angle) * d.Radius, 0, Mathf.Sin(angle) * d.Radius);
            }
            SetGroundPositions(slot.OuterLine, positions);

            // 构建圆形扇面填充网格，半径随 Progress 递增（充满视效）
            float currentFillRadius = d.State == DangerVisualState.Active ? d.Radius : Mathf.Max(0.1f, d.Radius * Mathf.Clamp01(d.Progress));
            BuildDiskMesh(slot.FillMesh, center, currentFillRadius, segs, fillColor);
        }

        void RenderCapsule(IndicatorSlot slot, DangerAreaDescriptor d, Color borderColor, Color fillColor)
        {
            slot.InnerLine.enabled = false;

            Vector3 dir = d.Direction.normalized;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
            Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;

            Vector3 start = d.Position;
            start.y += 0.05f;
            Vector3 end = start + dir * d.Length;

            // 绘制胶囊体外边框
            int halfSegs = 16;
            int totalSegs = halfSegs * 2 + 2;
            slot.OuterLine.positionCount = totalSegs;
            slot.OuterLine.loop = true;

            List<Vector3> pts = new List<Vector3>(totalSegs);

            // 前半圆 (围绕 end)
            for (int i = 0; i <= halfSegs; i++)
            {
                float angle = -Mathf.PI * 0.5f + i * Mathf.PI / halfSegs;
                Vector3 p = end + (dir * Mathf.Cos(angle) + side * Mathf.Sin(angle)) * d.Radius;
                pts.Add(p);
            }
            // 后半圆 (围绕 start)
            for (int i = 0; i <= halfSegs; i++)
            {
                float angle = Mathf.PI * 0.5f + i * Mathf.PI / halfSegs;
                Vector3 p = start + (dir * Mathf.Cos(angle) + side * Mathf.Sin(angle)) * d.Radius;
                pts.Add(p);
            }
            SetGroundPositions(slot.OuterLine, pts.ToArray());

            // 冲锋方向中轴指示箭头
            slot.DirectionArrow.enabled = true;
            slot.DirectionArrow.loop = false;
            slot.DirectionArrow.positionCount = 3;
            slot.DirectionArrow.startColor = borderColor;
            slot.DirectionArrow.endColor = borderColor;

            Vector3 arrowTip = start + dir * (d.Length * Mathf.Clamp01(d.Progress));
            Vector3 arrowLeft = arrowTip - dir * 0.6f + side * 0.35f;
            Vector3 arrowRight = arrowTip - dir * 0.6f - side * 0.35f;
            SetGroundPositions(slot.DirectionArrow, new[] { arrowLeft, arrowTip, arrowRight });

            // 填充胶囊体矩形 + 半圆
            BuildCapsuleMesh(slot.FillMesh, start, end, dir, side, d.Radius, d.Progress, fillColor);
        }

        void RenderAnnulus(IndicatorSlot slot, DangerAreaDescriptor d, Color borderColor, Color fillColor)
        {
            slot.DirectionArrow.enabled = false;
            slot.InnerLine.enabled = true;

            int segs = 40;
            Vector3 center = d.Position;
            center.y += 0.05f;

            // 外圆
            slot.OuterLine.positionCount = segs;
            slot.OuterLine.loop = true;
            Vector3[] outerPts = new Vector3[segs];
            for (int i = 0; i < segs; i++)
            {
                float angle = i * Mathf.PI * 2f / segs;
                outerPts[i] = center + new Vector3(Mathf.Cos(angle) * d.Radius, 0, Mathf.Sin(angle) * d.Radius);
            }
            SetGroundPositions(slot.OuterLine, outerPts);

            // 内圆
            slot.InnerLine.positionCount = segs;
            slot.InnerLine.loop = true;
            slot.InnerLine.startColor = borderColor;
            slot.InnerLine.endColor = borderColor;
            Vector3[] innerPts = new Vector3[segs];
            for (int i = 0; i < segs; i++)
            {
                float angle = i * Mathf.PI * 2f / segs;
                innerPts[i] = center + new Vector3(Mathf.Cos(angle) * d.InnerRadius, 0, Mathf.Sin(angle) * d.InnerRadius);
            }
            SetGroundPositions(slot.InnerLine, innerPts);

            // 构建圆环填充网格
            BuildRingMesh(slot.FillMesh, center, d.InnerRadius, d.Radius, segs, fillColor);
        }

        void BuildDiskMesh(Mesh mesh, Vector3 center, float radius, int segments, Color color)
        {
            mesh.Clear();
            Vector3[] verts = new Vector3[segments + 1];
            Color[] colors = new Color[segments + 1];
            int[] tris = new int[segments * 3];

            verts[0] = center;
            colors[0] = color;

            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                verts[i + 1] = center + new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius);
                colors[i + 1] = color;

                tris[i * 3 + 0] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = (i + 1 >= segments) ? 1 : i + 2;
            }

            ProjectMesh(verts); mesh.vertices = verts;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
        }

        void BuildRingMesh(Mesh mesh, Vector3 center, float innerR, float outerR, int segments, Color color)
        {
            mesh.Clear();
            int vertCount = segments * 2;
            Vector3[] verts = new Vector3[vertCount];
            Color[] colors = new Color[vertCount];
            int[] tris = new int[segments * 6];

            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                float cos = Mathf.Cos(a);
                float sin = Mathf.Sin(a);

                verts[i * 2 + 0] = center + new Vector3(cos * innerR, 0, sin * innerR);
                verts[i * 2 + 1] = center + new Vector3(cos * outerR, 0, sin * outerR);
                colors[i * 2 + 0] = color;
                colors[i * 2 + 1] = color;

                int next = (i + 1) % segments;
                int idx = i * 6;
                tris[idx + 0] = i * 2 + 0;
                tris[idx + 1] = i * 2 + 1;
                tris[idx + 2] = next * 2 + 0;

                tris[idx + 3] = next * 2 + 0;
                tris[idx + 4] = i * 2 + 1;
                tris[idx + 5] = next * 2 + 1;
            }

            ProjectMesh(verts); mesh.vertices = verts;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
        }

        void BuildCapsuleMesh(Mesh mesh, Vector3 start, Vector3 end, Vector3 dir, Vector3 side, float radius, float progress, Color color)
        {
            mesh.Clear();
            // 简单填充：使用四边形表示冲锋蓄力带
            Vector3 currentEnd = Vector3.Lerp(start, end, Mathf.Clamp01(progress));

            Vector3[] verts = new Vector3[4];
            Color[] colors = new Color[4];
            int[] tris = new[] { 0, 1, 2, 0, 2, 3 };

            verts[0] = start - side * radius;
            verts[1] = start + side * radius;
            verts[2] = currentEnd + side * radius;
            verts[3] = currentEnd - side * radius;

            for (int i = 0; i < 4; i++) colors[i] = color;

            ProjectMesh(verts); mesh.vertices = verts;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// 清空所有预警指示器并隐藏（终局、重开、死亡时调用）
        /// </summary>
        public void Clear()
        {
            foreach (var slot in slots)
            {
                slot.Reset();
            }
        }

        static Vector3 Ground(Vector3 point)
        {
            var terrain = Terrain.activeTerrain;
            point.y = (terrain != null ? terrain.SampleHeight(point) + terrain.transform.position.y : 0f) + .07f;
            return point;
        }
        static void SetGroundPositions(LineRenderer line, Vector3[] points)
        {
            for (int i = 0; i < points.Length; i++) points[i] = Ground(points[i]);
            line.SetPositions(points);
        }
        void ProjectMesh(Vector3[] points)
        {
            for (int i = 0; i < points.Length; i++) points[i] = transform.InverseTransformPoint(Ground(points[i]));
        }

        void OnDestroy()
        {
            Clear();
            foreach (var slot in slots) if (slot.FillMesh != null) Destroy(slot.FillMesh);
            if (runtimeLineMat != null) Destroy(runtimeLineMat);
            if (runtimeFillMat != null) Destroy(runtimeFillMat);
        }
    }
}
