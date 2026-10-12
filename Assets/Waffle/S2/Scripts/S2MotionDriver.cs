using System.Collections.Generic;
using UnityEngine;
using Waffle.Combat;

namespace Waffle.S2
{
    /// <summary>
    /// 只动 MotionRoot 下的 Core / Segment_A / Segment_B。
    /// 不缩放玩家根或 CombatViews 的槽位根，不延迟攻击判定。
    /// 执行顺序早于 GameLoop，先记下本帧开始时仍存活的精英序号。
    /// </summary>
    [DefaultExecutionOrder(-250)]
    public class S2MotionDriver : MonoBehaviour
    {
        public GameLoop loop;
        public S2FxProfile profile;

        const int Shot = 1, Hit = 2, Dodge = 3, Hurt = 4;

        sealed class Pose
        {
            public Transform motion;
            public Transform[] parts;
            public Vector3[] rest;
            public Quaternion[] restRot;
            public float age = -1f;
            public int kind;
        }

        readonly List<Pose> poses = new List<Pose>();
        readonly HashSet<int> eliteSerials = new HashSet<int>();
        Transform playerMotion;

        public bool WasElite(int serial) { return eliteSerials.Contains(serial); }

        void OnEnable()
        {
            if (loop == null) loop = GetComponent<GameLoop>();
            if (loop != null) loop.PresentationDispatched += OnEvents;
        }

        void OnDisable()
        {
            if (loop != null) loop.PresentationDispatched -= OnEvents;
            ResetAll();
        }

        void Update()
        {
            SnapshotElites();
            if (!Playing()) return;
            float dt = loop.Sim.LastDt;
            if (dt <= 0f) return;
            for (int i = 0; i < poses.Count; i++)
            {
                Pose pose = poses[i];
                if (pose.age >= 0f) pose.age += dt;
            }
        }

        void LateUpdate()
        {
            Bind();
            for (int i = 0; i < poses.Count; i++)
            {
                Pose pose = poses[i];
                if (pose.motion == null) continue;
                // 回池后 CombatViews 会关掉槽位。先写回静止姿态，避免下次出池带着偏移。
                if (!pose.motion.gameObject.activeInHierarchy) pose.age = -1f;
                Apply(pose);
            }
        }

        void OnEvents(IReadOnlyList<PresentationEvent> events)
        {
            Bind();
            for (int i = 0; i < events.Count; i++)
            {
                PresentationEvent e = events[i];
                switch (e.Type)
                {
                    case PresentationEventType.Shot: Play(playerMotion, Shot); break;
                    case PresentationEventType.Dodge: Play(playerMotion, Dodge); break;
                    case PresentationEventType.PlayerHurt: Play(playerMotion, Hurt); break;
                    case PresentationEventType.EnemyHit:
                    case PresentationEventType.EnemyKilled:
                        Play(NearestEnemy(e.X, e.Z), Hit);
                        break;
                    case PresentationEventType.Restart:
                        ResetAll();
                        break;
                }
            }
        }

        void SnapshotElites()
        {
            eliteSerials.Clear();
            CombatModule combat = loop != null && loop.Sim != null ? loop.Sim.Combat : null;
            if (combat == null) return;
            IReadOnlyList<EnemyState> enemies = combat.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyState enemy = enemies[i];
                if (enemy.IsAlive && enemy.IsElite) eliteSerials.Add(enemy.Serial);
            }
        }

        void Bind()
        {
            if (loop == null) return;
            playerMotion = null;
            if (loop.playerView != null)
            {
                Transform visual = loop.playerView.Find("Visual");
                if (visual != null) playerMotion = FindMotion(visual);
            }
            var views = loop.GetComponent<CombatViews>();
            if (views != null) Collect(views.transform);
            if (playerMotion != null) Ensure(playerMotion);
        }

        void Collect(Transform root)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == "MotionRoot" && all[i] != playerMotion) Ensure(all[i]);
        }

        static Transform FindMotion(Transform root)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == "MotionRoot") return all[i];
            return null;
        }

        void Ensure(Transform motion)
        {
            for (int i = 0; i < poses.Count; i++)
                if (poses[i].motion == motion) return;
            var parts = new List<Transform>(3);
            for (int i = 0; i < motion.childCount; i++)
            {
                Transform child = motion.GetChild(i);
                if (child.name == "Core" || child.name == "Segment_A" || child.name == "Segment_B")
                    parts.Add(child);
            }
            var pose = new Pose
            {
                motion = motion,
                parts = parts.ToArray(),
                rest = new Vector3[parts.Count],
                restRot = new Quaternion[parts.Count]
            };
            for (int i = 0; i < parts.Count; i++)
            {
                pose.rest[i] = parts[i].localPosition;
                pose.restRot[i] = parts[i].localRotation;
            }
            poses.Add(pose);
        }

        void Play(Transform motion, int kind)
        {
            if (motion == null) return;
            for (int i = 0; i < poses.Count; i++)
            {
                if (poses[i].motion != motion) continue;
                poses[i].age = 0f;
                poses[i].kind = kind;
                return;
            }
        }

        Transform NearestEnemy(float x, float z)
        {
            Transform best = null;
            float bestSqr = 4f;
            for (int i = 0; i < poses.Count; i++)
            {
                Transform motion = poses[i].motion;
                if (motion == null || motion == playerMotion) continue;
                Vector3 p = motion.position;
                float dx = p.x - x, dz = p.z - z;
                float sqr = dx * dx + dz * dz;
                if (sqr < bestSqr) { bestSqr = sqr; best = motion; }
            }
            return best;
        }

        void Apply(Pose pose)
        {
            if (pose.motion == null || pose.parts.Length == 0) return;
            float contractT = profile != null ? profile.contractSeconds : 0.04f;
            float unfoldT = profile != null ? profile.unfoldSeconds : 0.06f;
            float recoverT = profile != null ? profile.recoverSeconds : 0.08f;
            float contract = profile != null ? profile.contract : 0.14f;
            float meters = profile != null ? profile.unfoldMeters : 0.05f;
            float yaw = profile != null ? profile.unfoldYaw : 7f;
            if (pose.kind == Dodge) meters *= 1.6f;
            if (pose.kind == Hurt) { contract *= 1.4f; meters *= 0.4f; }
            float total = Mathf.Max(0.01f, contractT + unfoldT + recoverT);
            bool done = pose.age < 0f || pose.age >= total;
            float age = done ? total : pose.age;
            for (int i = 0; i < pose.parts.Length; i++)
            {
                Transform part = pose.parts[i];
                if (part == null) continue;
                Vector3 rest = pose.rest[i];
                Vector3 side = Side(part.name) * meters;
                Vector3 contracted = rest * (1f - contract);
                Vector3 unfolded = rest + side;
                Vector3 local;
                float turn;
                if (done || age <= 0f)
                {
                    local = rest;
                    turn = 0f;
                }
                else if (age < contractT)
                {
                    float k = contractT <= 0.0001f ? 1f : age / contractT;
                    local = Vector3.Lerp(rest, contracted, k);
                    turn = 0f;
                }
                else if (age < contractT + unfoldT)
                {
                    float k = unfoldT <= 0.0001f ? 1f : (age - contractT) / unfoldT;
                    local = Vector3.Lerp(contracted, unfolded, k);
                    turn = Mathf.Lerp(0f, yaw, k);
                }
                else
                {
                    float k = recoverT <= 0.0001f ? 1f : (age - contractT - unfoldT) / recoverT;
                    local = Vector3.Lerp(unfolded, rest, k);
                    turn = Mathf.Lerp(yaw, 0f, k);
                }
                part.localPosition = local;
                part.localRotation = pose.restRot[i] * Quaternion.Euler(0f, turn * Side(part.name).x, 0f);
            }
            if (pose.age >= total) pose.age = -1f;
        }

        static Vector3 Side(string part)
        {
            if (part == "Segment_A") return Vector3.right;
            if (part == "Segment_B") return Vector3.left;
            return new Vector3(0f, 0f, -0.35f);
        }

        void ResetAll()
        {
            for (int i = 0; i < poses.Count; i++)
            {
                poses[i].age = -1f;
                Apply(poses[i]);
            }
        }

        bool Playing()
        {
            return loop != null && loop.Sim != null && loop.Sim.State.Phase == RunPhase.Playing;
        }
    }
}
