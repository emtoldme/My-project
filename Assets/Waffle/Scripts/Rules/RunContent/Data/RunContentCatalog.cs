using System;
using System.Collections.Generic;

namespace Waffle.RunContent
{
    /// <summary>
    /// RC01 首轮完整目录（6 攻击 / 18 被动 / 6 进化 / 6 拾取），纯 C# 只读定义，无序列化包。
    /// 数值为草稿初值，说明与理由见 Docs/Design/RC01/Growth/CATALOG.md。1号唯一维护。
    /// </summary>
    public sealed class RunContentCatalog
    {
        public const string HealFallbackId = "HEAL";
        public readonly IReadOnlyList<ContentDefinition> All;
        private readonly Dictionary<string, ContentDefinition> byId = new Dictionary<string, ContentDefinition>();

        public RunContentCatalog(IEnumerable<ContentDefinition> defs)
        {
            var list = new List<ContentDefinition>(defs);
            foreach (var d in list)
            {
                if (byId.ContainsKey(d.Id)) throw new ArgumentException("重复ID " + d.Id);
                byId.Add(d.Id, d);
            }
            All = list.AsReadOnly();
        }

        public bool TryGet(string id, out ContentDefinition def) => byId.TryGetValue(id ?? "", out def);
        public ContentDefinition Get(string id) => byId.TryGetValue(id ?? "", out var d) ? d : null;

        public int Count(ContentKind kind, ImplementationStatus atLeast = ImplementationStatus.Planned)
        {
            int n = 0;
            foreach (var d in All) if (d.Kind == kind && d.Status >= atLeast) n++;
            return n;
        }

        // ---------- 默认目录 ----------
        private static readonly Lazy<RunContentCatalog> defaultCatalog = new Lazy<RunContentCatalog>(Build);
        public static RunContentCatalog Default => defaultCatalog.Value;

        private const ImplementationStatus RR = ImplementationStatus.RulesReady;
        private const ImplementationStatus PL = ImplementationStatus.Planned;
        private static ContentParam P(string k, float v) => new ContentParam(k, v);
        private static ContentRank R(int r, string text, params ContentParam[] ps) => new ContentRank(r, text, ps);

        private static ContentDefinition W(string id, string name, ImplementationStatus st, string desc, string[] tags, params ContentRank[] r)
            => new ContentDefinition(id, ContentKind.Weapon, name, tags, r, 30, st, desc);
        private static ContentDefinition Pa(string id, string name, ImplementationStatus st, string desc, string[] tags, params ContentRank[] r)
            => new ContentDefinition(id, ContentKind.Passive, name, tags, r, 35, st, desc);
        private static ContentDefinition E(string id, string name, ImplementationStatus st, string desc, string w, string p, params ContentParam[] ps)
            => new ContentDefinition(id, ContentKind.Evolution, name, new[] { "evolution" }, new[] { R(1, desc, ps) }, 0, st, desc,
                new EvolutionRequirement(w, 5, p, 2, 240f));
        private static ContentDefinition D(string id, string name, ImplementationStatus st, string desc, params ContentParam[] ps)
            => new ContentDefinition(id, ContentKind.Pickup, name, new[] { "pickup" }, new[] { R(1, desc, ps) }, 0, st, desc);

        private static RunContentCatalog Build()
        {
            var d = new List<ContentDefinition>
            {
                // ===== 攻击家族（5阶）。dmg=单次伤害 cd=间隔s，预算 budget=每次攻击最多命中数 =====
                W("W01", "定向脉冲", RR, "最近目标直线弹；单体/精英。", new[] { "projectile", "single" },
                    R(1, "伤害10，间隔0.60s，穿透1", P("dmg", 10), P("cd", 0.60f), P("pierce", 1), P("speed", 14), P("count", 1), P("budget", 1)),
                    R(2, "伤害10→13", P("dmg", 13), P("cd", 0.60f), P("pierce", 1), P("speed", 14), P("count", 1), P("budget", 1)),
                    R(3, "穿透1→2", P("dmg", 13), P("cd", 0.60f), P("pierce", 2), P("speed", 14), P("count", 1), P("budget", 2)),
                    R(4, "间隔0.60→0.50s", P("dmg", 13), P("cd", 0.50f), P("pierce", 2), P("speed", 14), P("count", 1), P("budget", 2)),
                    R(5, "双段脉冲：每次2发×9伤（总18）", P("dmg", 9), P("cd", 0.50f), P("pierce", 2), P("speed", 14), P("count", 2), P("budget", 4))),
                W("W02", "扇形散射", RR, "面向最近目标的短距扇形；贴近清群。", new[] { "projectile", "area", "close" },
                    R(1, "3弹×6伤，45°，射程5m，间隔0.90s", P("dmg", 6), P("cd", 0.90f), P("count", 3), P("arc", 45), P("range", 5), P("sameTargetHits", 1)),
                    R(2, "伤害6→8", P("dmg", 8), P("cd", 0.90f), P("count", 3), P("arc", 45), P("range", 5), P("sameTargetHits", 1)),
                    R(3, "弹数3→5，扇角60°", P("dmg", 8), P("cd", 0.90f), P("count", 5), P("arc", 60), P("range", 5), P("sameTargetHits", 2)),
                    R(4, "间隔0.90→0.75s", P("dmg", 8), P("cd", 0.75f), P("count", 5), P("arc", 60), P("range", 5.5f), P("sameTargetHits", 2)),
                    R(5, "外侧2弹到射程后回收再判一次（同目标最多3次）", P("dmg", 8), P("cd", 0.75f), P("count", 5), P("arc", 60), P("range", 5.5f), P("sameTargetHits", 3), P("returnShots", 2))),
                W("W03", "环绕护卫", RR, "绕身轨道体；接敌与走位。", new[] { "orbit", "contact", "close" },
                    R(1, "2轨道体×5伤，半径1.8m，同目标冷却0.5s", P("dmg", 5), P("count", 2), P("radius", 1.8f), P("angSpeed", 180), P("hitCd", 0.5f)),
                    R(2, "伤害5→7", P("dmg", 7), P("count", 2), P("radius", 1.8f), P("angSpeed", 180), P("hitCd", 0.5f)),
                    R(3, "轨道体2→3", P("dmg", 7), P("count", 3), P("radius", 1.8f), P("angSpeed", 180), P("hitCd", 0.5f)),
                    R(4, "转速180→220°/s，半径2.0m", P("dmg", 7), P("count", 3), P("radius", 2.0f), P("angSpeed", 220), P("hitCd", 0.5f)),
                    R(5, "每4s外扩至3.2m持续1s", P("dmg", 8), P("count", 3), P("radius", 2.0f), P("angSpeed", 220), P("hitCd", 0.5f), P("pulseEvery", 4), P("pulseRadius", 3.2f), P("pulseDur", 1))),
                W("W04", "定点共振", RR, "在目标位置延迟0.6s落区，区域持续伤害。", new[] { "area", "zone", "delayed" },
                    R(1, "1区，半径1.5m，持续2s，每0.5s 4伤，间隔3s", P("dmg", 4), P("cd", 3), P("count", 1), P("radius", 1.5f), P("duration", 2), P("tick", 0.5f), P("delay", 0.6f)),
                    R(2, "伤害4→5，半径1.7m", P("dmg", 5), P("cd", 3), P("count", 1), P("radius", 1.7f), P("duration", 2), P("tick", 0.5f), P("delay", 0.6f)),
                    R(3, "区数1→2", P("dmg", 5), P("cd", 3), P("count", 2), P("radius", 1.7f), P("duration", 2), P("tick", 0.5f), P("delay", 0.6f)),
                    R(4, "持续2→2.5s，间隔2.6s", P("dmg", 5), P("cd", 2.6f), P("count", 2), P("radius", 1.7f), P("duration", 2.5f), P("tick", 0.5f), P("delay", 0.6f)),
                    R(5, "区域结束时爆发一次12伤", P("dmg", 6), P("cd", 2.6f), P("count", 2), P("radius", 1.8f), P("duration", 2.5f), P("tick", 0.5f), P("delay", 0.6f), P("burst", 12))),
                W("W05", "弹跳电弧", PL, "链击，有跳数，不重复目标。", new[] { "chain" },
                    R(1, "8伤，3跳，跳距4m，间隔1.2s", P("dmg", 8), P("cd", 1.2f), P("bounces", 3), P("jump", 4), P("falloff", 0.9f)),
                    R(2, "伤害8→10", P("dmg", 10), P("cd", 1.2f), P("bounces", 3), P("jump", 4), P("falloff", 0.9f)),
                    R(3, "跳数3→4", P("dmg", 10), P("cd", 1.2f), P("bounces", 4), P("jump", 4), P("falloff", 0.9f)),
                    R(4, "间隔1.2→1.0s", P("dmg", 10), P("cd", 1.0f), P("bounces", 4), P("jump", 4.5f), P("falloff", 0.9f)),
                    R(5, "第2跳分叉为2支，总目标预算6不变", P("dmg", 11), P("cd", 1.0f), P("bounces", 4), P("jump", 4.5f), P("falloff", 0.9f), P("fork", 2), P("budget", 6))),
                W("W06", "移动残响", PL, "移动累计距离布置短寿命残留；闪避穿越。", new[] { "trail", "mobility" },
                    R(1, "每移动2.5m留1点，存3s，接触6伤（同目标0.6s）", P("dmg", 6), P("spacing", 2.5f), P("life", 3), P("max", 6), P("hitCd", 0.6f)),
                    R(2, "伤害6→8", P("dmg", 8), P("spacing", 2.5f), P("life", 3), P("max", 6), P("hitCd", 0.6f)),
                    R(3, "相邻残留连线（线宽0.4m）", P("dmg", 8), P("spacing", 2.5f), P("life", 3), P("max", 6), P("hitCd", 0.6f), P("link", 1)),
                    R(4, "寿命3→4s，上限8点", P("dmg", 8), P("spacing", 2.2f), P("life", 4), P("max", 8), P("hitCd", 0.6f), P("link", 1)),
                    R(5, "实际闪避时引爆最近3点各15伤", P("dmg", 9), P("spacing", 2.2f), P("life", 4), P("max", 8), P("hitCd", 0.6f), P("link", 1), P("detonate", 3), P("detDmg", 15))),

                // ===== 被动（3阶）。键为属性聚合键，见 GrowthStats =====
                Pa("P01", "输出增幅", RR, "直接基础伤害每阶+12%（同组加算）", new[] { "stat" }, R(1, "伤害+12%", P("damagePct", 12)), R(2, "伤害+24%", P("damagePct", 24)), R(3, "伤害+36%", P("damagePct", 36))),
                Pa("P02", "节拍加速", RR, "攻击频率每阶+10%，冷却下限0.15s", new[] { "stat" }, R(1, "频率+10%", P("ratePct", 10)), R(2, "频率+20%", P("ratePct", 20)), R(3, "频率+30%", P("ratePct", 30))),
                Pa("P03", "脉冲推进", PL, "弹速每阶+15%，仅弹体武器", new[] { "stat", "projectile" }, R(1, "弹速+15%", P("projSpeedPct", 15)), R(2, "弹速+30%", P("projSpeedPct", 30)), R(3, "弹速+45%", P("projSpeedPct", 45))),
                Pa("P04", "扩域", RR, "范围（半径）每阶+12%", new[] { "stat", "area" }, R(1, "范围+12%", P("areaPct", 12)), R(2, "范围+24%", P("areaPct", 24)), R(3, "范围+36%", P("areaPct", 36))),
                Pa("P05", "延音", PL, "有效持续时间每阶+15%；不延长无敌/回声延迟", new[] { "stat" }, R(1, "持续+15%", P("durationPct", 15)), R(2, "持续+30%", P("durationPct", 30)), R(3, "持续+45%", P("durationPct", 45))),
                Pa("P06", "轻步", RR, "移速每阶+6%，不改闪避", new[] { "stat", "mobility" }, R(1, "移速+6%", P("moveSpeedPct", 6)), R(2, "移速+12%", P("moveSpeedPct", 12)), R(3, "移速+18%", P("moveSpeedPct", 18))),
                Pa("P07", "生命储备", RR, "最大HP每阶+15%，取得时当前HP只增加差值", new[] { "stat", "defense" }, R(1, "最大HP+15%", P("maxHpPct", 15)), R(2, "最大HP+30%", P("maxHpPct", 30)), R(3, "最大HP+45%", P("maxHpPct", 45))),
                Pa("P08", "减伤垫片", PL, "受伤减免每阶+6%，总≤50%，最终伤害≥1", new[] { "stat", "defense" }, R(1, "减伤6%", P("damageReductionPct", 6)), R(2, "减伤12%", P("damageReductionPct", 12)), R(3, "减伤18%", P("damageReductionPct", 18))),
                Pa("P09", "采集磁芯", RR, "拾取半径每阶+25%，不增加XP", new[] { "stat", "economy" }, R(1, "拾取半径+25%", P("pickupPct", 25)), R(2, "拾取半径+50%", P("pickupPct", 50)), R(3, "拾取半径+75%", P("pickupPct", 75))),
                Pa("P10", "缓释恢复", PL, "Playing时钟每5s恢复N点HP，不复活", new[] { "sustain" }, R(1, "每5s回1", P("regenPer5s", 1)), R(2, "每5s回2", P("regenPer5s", 2)), R(3, "每5s回3", P("regenPer5s", 3))),
                Pa("P11", "精准谐振", PL, "直击暴击率每阶+5%，倍率1.5，派生不暴击", new[] { "crit" }, R(1, "暴击5%", P("critPct", 5), P("critMul", 1.5f)), R(2, "暴击10%", P("critPct", 10), P("critMul", 1.5f)), R(3, "暴击15%", P("critPct", 15), P("critMul", 1.5f))),
                Pa("P12", "回声模块", RR, "局内取得：直接命中0.5s后原目标回声；不是AOE", new[] { "mechanism", "echo" },
                    R(1, "启用回声：0.5s后原目标×0.5", P("echoCoef", 0.5f), P("echoDelay", 0.5f)), R(2, "回声系数0.5→0.65", P("echoCoef", 0.65f), P("echoDelay", 0.5f)), R(3, "回声系数0.65→0.8", P("echoCoef", 0.8f), P("echoDelay", 0.5f))),
                Pa("P13", "传播模块", RR, "局内取得：0.1s后传给5m内最近未命中目标；预算封顶", new[] { "mechanism", "propagation" },
                    R(1, "启用传播：5m，×0.4，每命中最多1跳", P("propCoef", 0.4f), P("propRadius", 5), P("propDelay", 0.1f), P("propBudget", 1)),
                    R(2, "传播半径5→6m，×0.5", P("propCoef", 0.5f), P("propRadius", 6), P("propDelay", 0.1f), P("propBudget", 1)),
                    R(3, "×0.6，每命中最多2跳", P("propCoef", 0.6f), P("propRadius", 6), P("propDelay", 0.1f), P("propBudget", 2))),
                Pa("P14", "滞留模块", PL, "直接命中减速，刷新不叠加，精英折半", new[] { "control" }, R(1, "减速15%/1s", P("slowPct", 15), P("slowDur", 1)), R(2, "减速20%/1.2s", P("slowPct", 20), P("slowDur", 1.2f)), R(3, "减速25%/1.5s", P("slowPct", 25), P("slowDur", 1.5f))),
                Pa("P15", "推波模块", RR, "直接命中有限击退，重敌/精英折减", new[] { "control" }, R(1, "击退0.6m", P("knockback", 0.6f), P("knockCd", 0.5f)), R(2, "击退0.9m", P("knockback", 0.9f), P("knockCd", 0.5f)), R(3, "击退1.2m", P("knockback", 1.2f), P("knockCd", 0.5f))),
                Pa("P16", "击破余振", PL, "击杀时小范围伤害，不再触发自身", new[] { "onkill" }, R(1, "击杀爆1.5m 6伤", P("killDmg", 6), P("killRadius", 1.5f), P("killBudget", 4)), R(2, "8伤", P("killDmg", 8), P("killRadius", 1.5f), P("killBudget", 4)), R(3, "10伤，2m", P("killDmg", 10), P("killRadius", 2f), P("killBudget", 5))),
                Pa("P17", "闪避放电", PL, "实际闪避后脉冲一次，独立节流", new[] { "mobility", "ondodge" }, R(1, "闪避脉冲2.5m 12伤", P("dodgeDmg", 12), P("dodgeRadius", 2.5f), P("dodgeCd", 1.5f)), R(2, "16伤", P("dodgeDmg", 16), P("dodgeRadius", 2.5f), P("dodgeCd", 1.5f)), R(3, "20伤，3m", P("dodgeDmg", 20), P("dodgeRadius", 3f), P("dodgeCd", 1.5f))),
                Pa("P18", "间歇护盾", RR, "Playing时钟周期给吸收1次护盾，不与无敌叠成永久", new[] { "defense", "periodic" }, R(1, "每12s护盾1次", P("shieldEvery", 12), P("shieldCharges", 1)), R(2, "每10s", P("shieldEvery", 10), P("shieldCharges", 1)), R(3, "每8s", P("shieldEvery", 8), P("shieldCharges", 1))),

                // ===== 进化（替换原武器槽，1阶）。前置：武器5阶 + 被动≥2阶 + 局内≥240s，精英奖励触发 =====
                E("E01", "延迟重奏", RR, "W01直击目标额外0.5s回声×0.8（每命中1次，派生不递归）", "W01", "P12", P("dmg", 12), P("cd", 0.45f), P("pierce", 2), P("count", 2), P("echoBonus", 0.8f)),
                E("E02", "回折扇面", RR, "W02外侧弹回收+击退开缺口", "W02", "P15", P("dmg", 10), P("cd", 0.7f), P("count", 7), P("arc", 70), P("range", 6), P("knockback", 1.0f), P("sameTargetHits", 3)),
                E("E03", "行走屏障", RR, "W03轨道体4个；护盾触发时外扩脉冲", "W03", "P18", P("dmg", 10), P("count", 4), P("radius", 2.2f), P("angSpeed", 240), P("hitCd", 0.45f), P("shieldPulseDmg", 20)),
                E("E04", "共振网络", PL, "W04区域间有限传播（每区最多2条）", "W04", "P13", P("dmg", 7), P("cd", 2.4f), P("count", 3), P("radius", 2), P("linkBudget", 2)),
                E("E05", "连锁过载", PL, "W05直击暴击时分叉+1，派生不暴击", "W05", "P11", P("dmg", 13), P("cd", 0.9f), P("bounces", 5), P("budget", 8)),
                E("E06", "疾行回路", PL, "W06闪避时整条残留引爆，冷却3s", "W06", "P17", P("dmg", 11), P("spacing", 2f), P("life", 4), P("max", 10), P("detCd", 3)),

                // ===== 场内拾取 =====
                D("D01", "经验", RR, "复用真实XP；小/大面额同一种", P("small", 1), P("large", 5), P("lifetime", 60), P("cap", 200)),
                D("D02", "回复", RR, "回复20%最大HP，不改最大HP", P("healPct", 20), P("dropChance", 0.02f), P("lifetime", 20), P("cap", 3)),
                D("D03", "全场磁吸", RR, "现存经验送入拾取路径，总量守恒", P("dropChance", 0.004f), P("lifetime", 20), P("cap", 1), P("cooldown", 45)),
                D("D04", "清场脉冲", RR, "一次规则伤害30（精英×0.3），最多40目标", P("dmg", 30), P("eliteMul", 0.3f), P("budget", 40), P("dropChance", 0.003f), P("lifetime", 20), P("cap", 1), P("cooldown", 60)),
                D("D05", "临时屏障", PL, "5s内吸收1次伤害，重复只刷新", P("duration", 5), P("charges", 1), P("dropChance", 0.006f), P("lifetime", 20), P("cap", 1)),
                D("D06", "闪避补给", PL, "立即重置闪避冷却，不加输入缓冲", P("dropChance", 0.008f), P("lifetime", 20), P("cap", 2)),
            };
            return new RunContentCatalog(d);
        }
    }
}
