using System.Collections.Generic;
using UnityEngine;

namespace Waffle
{
    /// <summary>
    /// S1 最小特效入口（Docs/Art/02 第三节）。只读取 GameLoop 分发的表现事件，不改玩法。
    /// - Shot：炮口菱形闪光 0.04 s（预留，等 C-01 武器发事件）
    /// - EnemyHit：3 粒碎片 0.18 s（预留）；敌人材质闪白要等敌人表现节点存在后接入
    /// - EnemyKilled：3–5 块碎片外弹后 0.5 s 内缩小消失（预留）
    /// - Dodge：2 个半透明残影，alpha 0.5→0，0.15 s
    /// - PlayerHurt：玩家渲染器用 MaterialPropertyBlock 闪白 0.06 s
    /// 全部对象预先建好放在池里，运行中不 Instantiate。
    /// 只在 Playing 时推进计时（暂停/升级/终局时冻结），重开时全部清空。
    /// </summary>
    public class VfxDirector : MonoBehaviour
    {
        public GameLoop loop;
        [Tooltip("玩家的表现根节点；为空时用 GameLoop.playerView")]
        public Transform playerVisual;

        [Header("材质（由 Waffle/Setup S1 Presentation 创建并赋值）")]
        public Material muzzleMaterial;
        public Material fragmentMaterial;
        public Material afterimageMaterial;

        [Header("参数（Docs/Art/02 设计值）")]
        public float muzzleDuration = 0.04f;
        public float muzzleScale = 0.3f;
        public float hitFragmentDuration = 0.18f;
        public float deathDuration = 0.5f;
        public float afterimageDuration = 0.15f;
        public float afterimageAlpha = 0.5f;
        public float afterimageSecondDelay = 0.07f;
        public float hurtFlashDuration = 0.06f;
        public int fragmentPoolSize = 120;
        public int afterimagePoolSize = 6;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private class Fx
        {
            public GameObject go;
            public Renderer renderer;
            public float age, life;
            public Vector3 velocity;
            public Vector3 baseScale;
            public bool fadeAlpha;
            public bool shrink;
            public bool inUse;
            public Mesh bakedMesh;
        }

        private readonly List<Fx> muzzlePool = new List<Fx>();
        private readonly List<Fx> fragmentPool = new List<Fx>();
        private readonly List<Fx> afterimagePool = new List<Fx>();
        private readonly List<Fx> active = new List<Fx>();
        private MaterialPropertyBlock block;
        private Renderer[] playerRenderers;
        private SkinnedMeshRenderer playerSkin;
        private MeshFilter[] playerMeshFilters;
        private CombineInstance[] playerCombines;
        private float hurtFlashLeft;
        private bool secondAfterimagePending;
        private float secondAfterimageLeft;
        private GameLoop subscribed;

        private void Awake()
        {
            if (loop == null) loop = GetComponent<GameLoop>();
            if (playerVisual == null && loop != null) playerVisual = loop.playerView;
            if (playerVisual != null && playerVisual.Find("Visual") != null)
                playerVisual = playerVisual.Find("Visual");
            block = new MaterialPropertyBlock();

            for (int i = 0; i < 8; i++) muzzlePool.Add(CreateFx(PrimitiveType.Cube, muzzleMaterial, "FX_Muzzle"));
            for (int i = 0; i < fragmentPoolSize; i++) fragmentPool.Add(CreateFx(PrimitiveType.Cube, fragmentMaterial, "FX_Fragment"));
            for (int i = 0; i < afterimagePoolSize; i++) afterimagePool.Add(CreateAfterimage());

            if (playerVisual != null)
            {
                playerRenderers = playerVisual.GetComponentsInChildren<Renderer>(true);
                playerSkin = playerVisual.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (playerSkin == null)
                {
                    var filters = new List<MeshFilter>();
                    var combines = new List<CombineInstance>();
                    foreach (var filter in playerVisual.GetComponentsInChildren<MeshFilter>())
                    {
                        var renderer = filter.GetComponent<MeshRenderer>();
                        if (renderer == null || !renderer.enabled || filter.sharedMesh == null) continue;
                        for (int sub = 0; sub < filter.sharedMesh.subMeshCount; sub++)
                        {
                            filters.Add(filter);
                            combines.Add(new CombineInstance { mesh = filter.sharedMesh, subMeshIndex = sub });
                        }
                    }
                    playerMeshFilters = filters.ToArray();
                    playerCombines = combines.ToArray();
                }
            }
        }

        private void OnEnable()
        {
            if (loop == null) loop = GetComponent<GameLoop>();
            if (loop != null) { loop.PresentationDispatched += OnEvents; subscribed = loop; }
        }

        private void OnDisable()
        {
            if (subscribed != null) subscribed.PresentationDispatched -= OnEvents;
            subscribed = null;
            ClearAll();
        }

        private Fx CreateFx(PrimitiveType type, Material mat, string name)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Destroy(go.GetComponent<Collider>()); // 表现对象不参与任何物理
            go.transform.SetParent(transform, false);
            var r = go.GetComponent<Renderer>();
            if (mat != null) r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.SetActive(false);
            return new Fx { go = go, renderer = r };
        }

        private Fx CreateAfterimage()
        {
            var go = new GameObject("FX_Afterimage");
            go.transform.SetParent(transform, false);
            var mf = go.AddComponent<MeshFilter>();
            var bakedMesh = new Mesh { name = "AfterimageBake" };
            mf.sharedMesh = bakedMesh;
            var r = go.AddComponent<MeshRenderer>();
            if (afterimageMaterial != null) r.sharedMaterial = afterimageMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.SetActive(false);
            return new Fx { go = go, renderer = r, fadeAlpha = true, bakedMesh = bakedMesh };
        }

        // =====================================================================

        private void OnEvents(IReadOnlyList<PresentationEvent> events)
        {
            for (int i = 0; i < events.Count; i++)
            {
                PresentationEvent e = events[i];
                Vector3 p = new Vector3(e.X, 0f, e.Z);
                switch (e.Type)
                {
                    case PresentationEventType.Shot: SpawnMuzzle(p + Vector3.up * 1.2f); break;
                    case PresentationEventType.EnemyHit: SpawnFragments(p + Vector3.up, 3, hitFragmentDuration, 2f, 0.12f); break;
                    case PresentationEventType.EnemyKilled: SpawnFragments(p + Vector3.up * 0.8f, Random.Range(3, 6), deathDuration, 3f, 0.35f); break;
                    case PresentationEventType.Dodge:
                        SpawnAfterimage();
                        secondAfterimagePending = true;
                        secondAfterimageLeft = afterimageSecondDelay;
                        break;
                    case PresentationEventType.PlayerHurt: hurtFlashLeft = hurtFlashDuration; SetPlayerFlash(true); break;
                    case PresentationEventType.Victory:
                    case PresentationEventType.Defeat:
                        secondAfterimagePending = false;
                        hurtFlashLeft = 0f; SetPlayerFlash(false); break;
                    case PresentationEventType.Restart: ClearAll(); break;
                }
            }
        }

        private Fx Take(List<Fx> pool)
        {
            // A slot remains leased until Release; never overwrite a live snapshot.
            for (int i = 0; i < pool.Count; i++) if (!pool[i].inUse) return pool[i];
            return null; // 池满就放弃这次表现（不 new）
        }

        private void Activate(Fx fx, Vector3 pos, Vector3 scale, float life)
        {
            fx.go.transform.position = pos;
            fx.go.transform.localScale = scale;
            fx.baseScale = scale;
            fx.age = 0f;
            fx.life = life;
            fx.inUse = true;
            fx.go.SetActive(true);
            active.Add(fx);
        }

        private void SpawnMuzzle(Vector3 pos)
        {
            Fx fx = Take(muzzlePool);
            if (fx == null) return;
            fx.velocity = Vector3.zero; fx.shrink = false;
            fx.go.transform.rotation = Quaternion.Euler(45f, 45f, 0f); // 菱形
            Activate(fx, pos, Vector3.one * muzzleScale, muzzleDuration);
        }

        private void SpawnFragments(Vector3 pos, int count, float life, float speed, float size)
        {
            for (int i = 0; i < count; i++)
            {
                Fx fx = Take(fragmentPool);
                if (fx == null) return;
                Vector3 dir = Random.insideUnitSphere; dir.y = Mathf.Abs(dir.y);
                fx.velocity = dir.normalized * speed;
                fx.shrink = true;
                fx.go.transform.rotation = Random.rotation;
                Activate(fx, pos, Vector3.one * size, life);
            }
        }

        private void SpawnAfterimage()
        {
            if (playerVisual == null) return;
            Fx fx = Take(afterimagePool);
            if (fx == null) return;
            var mf = fx.go.GetComponent<MeshFilter>();
            if (playerSkin != null)
            {
                mf.sharedMesh = fx.bakedMesh;
                // Compensate the FBX/bone transform scale before applying it to the
                // snapshot transform. The default overload baked it twice (~23x).
                playerSkin.BakeMesh(fx.bakedMesh, true);
            }
            else if (playerMeshFilters != null && playerMeshFilters.Length > 0)
            {
                // Capture all sculpture parts in the visual root's coordinate space.
                // Arrays and destination meshes are preheated; only the pose changes.
                Matrix4x4 toLocal = playerVisual.worldToLocalMatrix;
                for (int i = 0; i < playerMeshFilters.Length; i++)
                    playerCombines[i].transform = toLocal * playerMeshFilters[i].transform.localToWorldMatrix;
                fx.bakedMesh.Clear();
                fx.bakedMesh.CombineMeshes(playerCombines, true, true);
                mf.sharedMesh = fx.bakedMesh;
            }
            else return;
            Transform src = playerSkin != null ? playerSkin.transform : playerVisual;
            fx.go.transform.rotation = src.rotation;
            fx.velocity = Vector3.zero; fx.shrink = false;
            Activate(fx, src.position, src.lossyScale, afterimageDuration);
        }

        // =====================================================================

        private void LateUpdate()
        {
            // 冻结：只有 Playing 才推进（暂停/升级/终局时特效停在原地）
            if (loop == null || loop.Sim == null || loop.Sim.State.Phase != RunPhase.Playing) return;
            float dt = loop.Sim.LastDt;
            if (dt <= 0f) return;

            if (hurtFlashLeft > 0f)
            {
                hurtFlashLeft -= dt;
                if (hurtFlashLeft <= 0f) SetPlayerFlash(false);
            }

            for (int i = active.Count - 1; i >= 0; i--)
            {
                Fx fx = active[i];
                fx.age += dt;
                float t = fx.life > 0f ? fx.age / fx.life : 1f;
                if (t >= 1f) { Release(fx); active.RemoveAt(i); continue; }

                if (fx.velocity != Vector3.zero)
                {
                    fx.go.transform.position += fx.velocity * dt;
                    fx.velocity += Vector3.down * 9.8f * dt;
                }
                if (fx.shrink) fx.go.transform.localScale = fx.baseScale * (1f - t);
                if (fx.fadeAlpha)
                {
                    Color c = afterimageMaterial != null && afterimageMaterial.HasProperty(BaseColorId) ? afterimageMaterial.GetColor(BaseColorId) : Color.cyan;
                    c.a = afterimageAlpha * (1f - t);
                    block.Clear();
                    block.SetColor(BaseColorId, c);
                    fx.renderer.SetPropertyBlock(block);
                }
            }
            // Sample a fresh pose at the later world position, rather than showing
            // a second copy of the initial pose after a visibility delay.
            if (secondAfterimagePending)
            {
                secondAfterimageLeft -= dt;
                if (secondAfterimageLeft <= 0f)
                {
                    secondAfterimagePending = false;
                    SpawnAfterimage();
                }
            }
        }

        private void Release(Fx fx)
        {
            fx.go.SetActive(false);
            fx.renderer.SetPropertyBlock(null);
            fx.inUse = false;
        }

        private void SetPlayerFlash(bool on)
        {
            if (playerRenderers == null) return;
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                Renderer r = playerRenderers[i];
                if (r == null) continue;
                if (on)
                {
                    block.Clear();
                    block.SetColor(EmissionColorId, Color.white);
                    r.SetPropertyBlock(block);
                }
                else
                {
                    r.SetPropertyBlock(null); // 还原到材质原来的自发光
                }
            }
        }

        /// <summary>重开：所有特效立即回池，玩家闪白还原。</summary>
        private void ClearAll()
        {
            for (int i = 0; i < active.Count; i++) Release(active[i]);
            active.Clear();
            secondAfterimagePending = false;
            hurtFlashLeft = 0f;
            SetPlayerFlash(false);
        }

        private void OnDestroy()
        {
            // Runtime-created meshes are not released with their MeshFilter.
            // Never destroy a model asset used by the static-mesh fallback.
            for (int i = 0; i < afterimagePool.Count; i++)
                if (afterimagePool[i].bakedMesh != null) Destroy(afterimagePool[i].bakedMesh);
        }
    }
}
