using System.Collections.Generic;
using UnityEngine;
using Waffle.Combat;

namespace Waffle
{
    /// <summary>只读规则状态，按PoolIndex同步预热视图；不参与命中和奖励判定。</summary>
    public class CombatViews : MonoBehaviour
    {
        public GameLoop loop;
        public GameObject enemyVisualPrefab;
        public Material projectileMaterial, gemMaterial;
        private sealed class Slot
        {
            public Transform root;
            public Renderer[] renderers;
            public int serial;
            public float flash;
            public bool seen;
        }
        private Slot[] enemies, projectiles, gems;
        private MaterialPropertyBlock block;
        private Transform playerVisual;
        private Vector3 playerBaseScale, playerBasePosition;
        private Vector3 previousPlayer;
        private Transform playerMarker;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        public int VisibleEnemies { get; private set; }
        public int VisibleProjectiles { get; private set; }
        public int VisibleGems { get; private set; }

        private void Start()
        {
            if (loop == null) loop = GetComponent<GameLoop>();
            if (loop == null || loop.Sim.Combat == null) { enabled = false; return; }
            block = new MaterialPropertyBlock();
            var c = loop.Sim.Combat.Config;
            enemies = CreateSlots(Mathf.Max(1, c.enemyPoolSize), true, null, "Enemy");
            projectiles = CreateSlots(Mathf.Max(1, c.projectilePoolSize), false, projectileMaterial, "Projectile");
            gems = CreateSlots(Mathf.Max(1, c.gemPoolSize), false, gemMaterial, "Gem");
            playerMarker = new GameObject("PlayerFootprint").transform;
            playerMarker.SetParent(transform, false);
            var ring = playerMarker.gameObject.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.sharedMaterial = projectileMaterial;
            ring.widthMultiplier = 0.035f;
            ring.positionCount = 33;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            for (int i = 0; i <= 32; i++)
            {
                float angle = i * Mathf.PI * 2f / 32f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * c.playerRadius, 0f, Mathf.Sin(angle) * c.playerRadius));
            }
            if (loop.playerView != null)
            {
                playerVisual = loop.playerView.Find("Visual");
                previousPlayer = loop.playerView.position;
                if (playerVisual != null)
                {
                    playerBaseScale = playerVisual.localScale;
                    playerBasePosition = playerVisual.localPosition;
                }
            }
            loop.PresentationDispatched += OnEvents;
            Sync();
        }

        private Slot[] CreateSlots(int count, bool enemy, Material material, string label)
        {
            var slots = new Slot[count];
            for (int i = 0; i < count; i++)
            {
                var root = new GameObject(label + "_" + i).transform;
                root.SetParent(transform, false);
                var body = enemy && enemyVisualPrefab != null ? Instantiate(enemyVisualPrefab, root) : GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.transform.SetParent(root, false);
                foreach (var collider in body.GetComponentsInChildren<Collider>()) Destroy(collider);
                var renderers = body.GetComponentsInChildren<Renderer>();
                if (enemy && enemyVisualPrefab != null && renderers.Length > 0)
                {
                    var bounds = renderers[0].bounds;
                    for (int r = 1; r < renderers.Length; r++) bounds.Encapsulate(renderers[r].bounds);
                    float scale = 1.5f / Mathf.Max(0.01f, bounds.size.y);
                    body.transform.localScale *= scale;
                    body.transform.localPosition = new Vector3(-bounds.center.x * scale, -bounds.min.y * scale, -bounds.center.z * scale);
                }
                if (material != null) foreach (var r in renderers) r.sharedMaterial = material;
                root.gameObject.SetActive(false);
                slots[i] = new Slot { root = root, renderers = renderers };
            }
            return slots;
        }

        private void OnDestroy()
        {
            if (loop != null) loop.PresentationDispatched -= OnEvents;
        }

        private void OnEvents(IReadOnlyList<PresentationEvent> events)
        {
            if (enemies == null) return;
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                if (e.Type != PresentationEventType.EnemyHit) continue;
                for (int j = 0; j < enemies.Length; j++)
                    if (enemies[j].root.gameObject.activeSelf && enemies[j].serial == e.Data) enemies[j].flash = 0.06f;
            }
        }

        public void Sync()
        {
            if (enemies == null || loop == null || loop.Sim.Combat == null) return;
            var sim = loop.Sim;
            var combat = sim.Combat;
            if (playerMarker != null) playerMarker.position = new Vector3(sim.PlayerX, 0.03f, sim.PlayerZ);
            float dt = sim.State.Phase == RunPhase.Playing ? sim.LastDt : 0f;
            Hide(enemies); Hide(projectiles); Hide(gems);
            VisibleEnemies = VisibleProjectiles = VisibleGems = 0;
            foreach (var e in combat.Enemies)
            {
                if (!e.IsAlive) continue;
                Slot slot = enemies[e.PoolIndex];
                slot.seen = true;
                if (slot.serial != e.Serial) { slot.serial = e.Serial; slot.flash = 0f; }
                slot.flash = Mathf.Max(0f, slot.flash - dt);
                slot.root.gameObject.SetActive(true);
                slot.root.position = new Vector3(e.Position.X, 0f, e.Position.Y);
                Vector3 toward = new Vector3(sim.PlayerX - e.Position.X, 0f, sim.PlayerZ - e.Position.Y);
                if (toward.sqrMagnitude > 0.001f) slot.root.rotation = Quaternion.LookRotation(toward);
                slot.root.localScale = Vector3.one * (e.IsElite ? 1.6f : 1f);
                Color tint = e.IsElite ? new Color(1f, 0.58f, 0.22f) : Color.Lerp(new Color(1f, 0.65f, 0.52f), new Color(0.9f, 0.3f, 0.2f), e.Phase / 3f);
                block.Clear(); block.SetColor(BaseColor, slot.flash > 0f ? Color.white : tint);
                block.SetColor(Emission, slot.flash > 0f ? Color.white * 2f : Color.black);
                foreach (var r in slot.renderers) r.SetPropertyBlock(block);
                VisibleEnemies++;
            }
            foreach (var p in combat.Projectiles)
            {
                if (p.Finished) continue;
                Transform root = projectiles[p.PoolIndex].root;
                projectiles[p.PoolIndex].seen = true;
                root.gameObject.SetActive(true);
                root.position = new Vector3(p.Position.X, 0.8f, p.Position.Y);
                root.rotation = Quaternion.LookRotation(new Vector3(p.Direction.X, 0f, p.Direction.Y));
                root.localScale = new Vector3(0.14f, 0.14f, 0.55f);
                VisibleProjectiles++;
            }
            foreach (var g in combat.Gems)
            {
                if (g.Life != GemLife.Free && g.Life != GemLife.Magnet) continue;
                if (g.IsBlinking(combat.Config.gemLifetime) && (int)(sim.State.Now * 8f) % 2 == 0) continue;
                Transform root = gems[g.PoolIndex].root;
                gems[g.PoolIndex].seen = true;
                root.gameObject.SetActive(true);
                root.position = new Vector3(g.Position.X, 0.32f + Mathf.Sin(sim.State.Now * 3f + g.Serial) * 0.06f, g.Position.Y);
                root.rotation = Quaternion.Euler(35f, sim.State.Now * 45f, 45f);
                root.localScale = Vector3.one * (g.IsBig ? 0.55f : 0.25f);
                VisibleGems++;
            }
            HideUnused(enemies); HideUnused(projectiles); HideUnused(gems);
            if (playerVisual != null && dt > 0f)
            {
                Vector3 movement = loop.playerView.position - previousPlayer;
                movement.y = 0f;
                var target = combat.FindNearestTarget();
                Vector3 aim = target != null ? new Vector3(target.Position.X - sim.PlayerX, 0f, target.Position.Y - sim.PlayerZ) : movement;
                if (aim.sqrMagnitude > 0.0001f) playerVisual.rotation = Quaternion.Slerp(playerVisual.rotation, Quaternion.LookRotation(aim), Mathf.Min(1f, dt * 18f));
                float bob = movement.sqrMagnitude > 0.00001f ? Mathf.Sin(sim.State.Now * 16f) * 0.035f : 0f;
                playerVisual.localPosition = playerBasePosition + Vector3.up * bob;
                playerVisual.localScale = Vector3.Scale(playerBaseScale, sim.IsDodging ? new Vector3(1.05f, 0.85f, 1.08f) : Vector3.one);
            }
            if (loop.playerView != null) previousPlayer = loop.playerView.position;
        }

        private static void Hide(Slot[] slots)
        {
            for (int i = 0; i < slots.Length; i++) slots[i].seen = false;
        }

        private static void HideUnused(Slot[] slots)
        {
            for (int i = 0; i < slots.Length; i++)
                if (!slots[i].seen && slots[i].root.gameObject.activeSelf) slots[i].root.gameObject.SetActive(false);
        }
    }
}
