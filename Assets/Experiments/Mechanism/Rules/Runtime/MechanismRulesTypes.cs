// TEST_ONLY：M01 机制规则适配的数据结构。纯 C#，不引用 UnityEngine。
namespace Reverb.Experiments.Mechanism.Rules
{
    /// <summary>XZ 平面坐标（米）。宿主把世界坐标的 x/z 填进来。</summary>
    public readonly struct MechanismVec2
    {
        public readonly float X, Z;
        public MechanismVec2(float x, float z) { X = x; Z = z; }
        public float SqrDistance(MechanismVec2 o) { float dx = X - o.X, dz = Z - o.Z; return dx * dx + dz * dz; }
        public override string ToString() => $"({X:0.##},{Z:0.##})";
    }

    /// <summary>目标句柄：Serial + Generation，对应 CombatModule 的 EnemyState。</summary>
    public readonly struct MechanismTarget
    {
        public readonly int Serial, Generation;
        public MechanismTarget(int serial, int generation) { Serial = serial; Generation = generation; }
        public bool Same(MechanismTarget o) => Serial == o.Serial && Generation == o.Generation;
        public override string ToString() => $"#{Serial}g{Generation}";
    }

    /// <summary>宿主报告的活目标。</summary>
    public readonly struct MechanismLiveTarget
    {
        public readonly MechanismTarget Target;
        public readonly MechanismVec2 Position;
        public MechanismLiveTarget(MechanismTarget t, MechanismVec2 p) { Target = t; Position = p; }
    }

    /// <summary>已经发生（已扣血）的真实直接命中。本模块不会再次扣这笔伤害。</summary>
    public readonly struct DirectHitRecord
    {
        public readonly double Time;
        public readonly MechanismTarget Target;
        public readonly MechanismVec2 Position;
        public readonly int Damage;
        public DirectHitRecord(double time, MechanismTarget target, MechanismVec2 position, int damage)
        { Time = time; Target = target; Position = position; Damage = damage; }
    }

    public enum DerivedKind { Echo = 0, Propagation = 1, EchoPropagation = 2 }

    /// <summary>宿主结算派生伤害的结果。Missed/Invalid 时本分支不再产生后续派生。</summary>
    public enum DerivedDamageResult { Applied = 0, Killed = 1, Missed = 2, Invalid = 3 }

    /// <summary>向宿主申请的一次派生伤害。</summary>
    public readonly struct DerivedDamageRequest
    {
        public readonly DerivedKind Kind;
        public readonly MechanismTarget Target;
        public readonly int Damage;
        public readonly double PlannedTime;
        /// <summary>传播起点（回声为目标自身位置）。供表现画连线。</summary>
        public readonly MechanismVec2 From;
        public readonly MechanismTarget Origin;
        public DerivedDamageRequest(DerivedKind kind, MechanismTarget target, int damage, double planned, MechanismVec2 from, MechanismTarget origin)
        { Kind = kind; Target = target; Damage = damage; PlannedTime = planned; From = from; Origin = origin; }
    }

    /// <summary>本 Tick 已结算的派生事件（输出缓冲，每次 Tick 开头清空）。</summary>
    public readonly struct DerivedDamageEvent
    {
        public readonly DerivedDamageRequest Request;
        public readonly DerivedDamageResult Result;
        public readonly MechanismVec2 TargetPosition;
        public DerivedDamageEvent(DerivedDamageRequest r, DerivedDamageResult res, MechanismVec2 pos) { Request = r; Result = res; TargetPosition = pos; }
        public override string ToString() => $"{Request.PlannedTime:0.###} {Request.Kind} {Request.Target} {Request.Damage} {Result}";
    }

    /// <summary>
    /// 宿主最小接口。由 Codex 桥接唯一 CombatModule；必须在规则步骤内同步调用（T6 之后、T7 之前）。
    /// </summary>
    public interface IMechanismHost
    {
        /// <summary>目标仍活且代号一致时返回 true 并给出当前位置。</summary>
        bool TryGetLiveTarget(MechanismTarget target, out MechanismVec2 position);
        /// <summary>把当前所有活目标追加到 buffer（调用前本模块已清空）。</summary>
        void CollectLiveTargets(System.Collections.Generic.List<MechanismLiveTarget> buffer);
        /// <summary>同步扣派生伤害。目标失效返回 Invalid；击杀返回 Killed（死亡/掉落仍由 T7 处理）。</summary>
        DerivedDamageResult ApplyDerivedDamage(in DerivedDamageRequest request);
    }

    /// <summary>参数。开局时拷贝，局内修改原对象不影响本局。</summary>
    public sealed class MechanismRulesConfig
    {
        public float EchoMultiplier = 0.5f;
        public double EchoDelay = 0.5;
        public float PropagationMultiplier = 0.4f;
        public double PropagationDelay = 0.1;
        public float PropagationRadius = 5f;

        public MechanismRulesConfig Clone() => (MechanismRulesConfig)MemberwiseClone();
    }
}
