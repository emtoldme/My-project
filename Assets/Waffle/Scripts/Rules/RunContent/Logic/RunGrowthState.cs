using System;
using System.Collections.Generic;

namespace Waffle.RunContent
{
    /// <summary>成长规则选项（草稿初值，见 Growth/RULES.md）。</summary>
    public sealed class GrowthOptions
    {
        public int WeaponSlots = 4, PassiveSlots = 6;
        public int Rerolls = 3, Banishes = 2, Skips = 2;
        public int NewWeaponWeight = 30, WeaponUpgradeWeight = 35, PassiveWeight = 35;
        /// <summary>前 N 次出牌内若武器槽未满且有合法新武器，至少出现一次新武器。</summary>
        public int EarlyNewWeaponOffers = 5;
        public int NewWeaponOfferInterval = 3;
        public float SkipHealFraction = 0.05f, FallbackHealFraction = 0.10f;
        public string StartingWeapon = "W01";
        /// <summary>进入正常池的最低状态。Planned 永远不进池（构造时强制）。</summary>
        public ImplementationStatus MinDrawStatus = ImplementationStatus.RulesReady;
    }

    /// <summary>
    /// RC01 局内成长核心（纯 C#）。只管装备/选择/资源/进化记账；不存 XP、敌人HP、战斗时钟。
    /// 每局 new 一个实例即干净状态。实现已发布的 IRunContentViewSource / IRunContentCommands。
    /// 宿主（Codex 适配器）职责：在现有升级队列每消费一次 PendingLevelUp 调 EnqueueLevelUpReward；
    /// 精英掉箱拾取时调 TryEnqueueEliteReward(箱子唯一token, 当前局内秒数)；终局调 CloseForRunEnd；
    /// 每次命令被接受后 DrainResults 应用效果。
    /// </summary>
    public sealed class RunGrowthState : IRunContentViewSource, IRunContentCommands
    {
        public sealed class Owned
        {
            public readonly ContentDefinition Def; public int Rank;
            public Owned(ContentDefinition def, int rank) { Def = def; Rank = rank; }
        }

        private readonly struct Reward
        {
            public readonly RewardSource Source; public readonly float RunSeconds;
            public Reward(RewardSource s, float t) { Source = s; RunSeconds = t; }
        }

        public readonly RunContentCatalog Catalog;
        public readonly GrowthOptions Options;
        private readonly GrowthRng rng;
        private readonly Owned[] weapons, passives;
        private readonly HashSet<string> banished = new HashSet<string>();
        private readonly HashSet<string> drawable = new HashSet<string>();
        private readonly HashSet<string> consumedEvolutions = new HashSet<string>();
        private readonly HashSet<long> usedEliteTokens = new HashSet<long>();
        private readonly Queue<Reward> rewards = new Queue<Reward>();
        private readonly List<GrowthResult> results = new List<GrowthResult>();
        private readonly List<string> choices = new List<string>(3);
        private bool offerIsEvolution, closed, offeredNewWeaponEarly;
        private Reward currentReward;
        private long token;
        private int offersMade;
        private RunContentView cachedView;
        private float viewRunSeconds;
        public void SetViewRunSeconds(float seconds)
        { bool crossed = (viewRunSeconds >= 240) != (seconds >= 240); viewRunSeconds = seconds; if (crossed) Dirty(); }

        public int Rerolls { get; private set; }
        public int Banishes { get; private set; }
        public int Skips { get; private set; }
        public long OfferToken => HasOffer ? token : 0;
        public bool HasOffer => choices.Count > 0;
        public int PendingRewards => rewards.Count + (HasOffer ? 1 : 0);
        public bool IsEvolutionOffer => HasOffer && offerIsEvolution;
        public IReadOnlyList<string> CurrentChoiceIds => choices;

        public RunGrowthState(int seed, RunContentCatalog catalog = null, GrowthOptions options = null, IEnumerable<string> drawAllowList = null)
        {
            Catalog = catalog ?? RunContentCatalog.Default;
            Options = options ?? new GrowthOptions();
            rng = new GrowthRng(seed);
            weapons = new Owned[Math.Max(1, Options.WeaponSlots)];
            passives = new Owned[Math.Max(0, Options.PassiveSlots)];
            Rerolls = Options.Rerolls; Banishes = Options.Banishes; Skips = Options.Skips;
            var allow = drawAllowList != null ? new HashSet<string>(drawAllowList) : null;
            foreach (var d in Catalog.All)
            {
                if (d.Status == ImplementationStatus.Planned || d.Status < Options.MinDrawStatus) continue;
                if (allow != null && !allow.Contains(d.Id)) continue;
                drawable.Add(d.Id);
            }
            var start = Catalog.Get(Options.StartingWeapon);
            if (start != null && start.Kind == ContentKind.Weapon) weapons[0] = new Owned(start, 1);
        }

        // ================= 查询 =================
        public bool IsDrawable(string id) => drawable.Contains(id);
        public bool IsBanished(string id) => banished.Contains(id);
        public int RankOf(string id)
        {
            var o = Find(id, out _, out _); return o?.Rank ?? 0;
        }
        public IReadOnlyList<Owned> WeaponSlots => Array.AsReadOnly(weapons);
        public IReadOnlyList<Owned> PassiveSlots => Array.AsReadOnly(passives);

        private Owned Find(string id, out bool isWeapon, out int slot)
        {
            for (int i = 0; i < weapons.Length; i++) if (weapons[i] != null && weapons[i].Def.Id == id) { isWeapon = true; slot = i; return weapons[i]; }
            for (int i = 0; i < passives.Length; i++) if (passives[i] != null && passives[i].Def.Id == id) { isWeapon = false; slot = i; return passives[i]; }
            isWeapon = false; slot = -1; return null;
        }
        private static int FreeSlot(Owned[] slots) { for (int i = 0; i < slots.Length; i++) if (slots[i] == null) return i; return -1; }

        /// <summary>进化资格（不看精英奖励，仅判断条件）。runSeconds 由宿主给出，本核心不存时钟。</summary>
        public bool IsEvolutionEligible(ContentDefinition evo, float runSeconds, out string missing)
        {
            missing = "";
            var req = evo.Evolution;
            if (req == null) { missing = "非进化"; return false; }
            if (consumedEvolutions.Contains(evo.Id)) { missing = "已进化"; return false; }
            if (!drawable.Contains(evo.Id)) { missing = "未实装"; return false; }
            var parts = new List<string>();
            int wr = 0; Find(req.WeaponId, out bool isW, out _); if (isW) wr = RankOf(req.WeaponId);
            if (wr < req.WeaponRank) parts.Add(req.WeaponId + " " + wr + "/" + req.WeaponRank + "阶");
            int pr = RankOf(req.PassiveId);
            if (pr < req.PassiveRank) parts.Add(req.PassiveId + " " + pr + "/" + req.PassiveRank + "阶");
            if (runSeconds < req.MinRunSeconds) parts.Add("局内≥" + (int)(req.MinRunSeconds / 60) + "分钟");
            missing = string.Join("，", parts);
            return parts.Count == 0;
        }

        // ================= 宿主入口 =================
        /// <summary>现有升级队列每出现一个待升级调用一次。终局后拒绝。</summary>
        public bool EnqueueLevelUpReward()
        {
            if (closed) return false;
            rewards.Enqueue(new Reward(RewardSource.LevelUp, 0f));
            TryOpenNext(); return true;
        }

        /// <summary>精英宝箱：同一 token 只记一次；条件满足时出进化卡，否则一次普通成长，不吞箱。</summary>
        public bool TryEnqueueEliteReward(long eliteToken, float runSeconds)
        {
            if (closed || !usedEliteTokens.Add(eliteToken)) return false;
            rewards.Enqueue(new Reward(RewardSource.Elite, runSeconds));
            TryOpenNext(); return true;
        }

        /// <summary>胜利/失败：丢弃剩余奖励与当前选项，此后不再出牌。</summary>
        public void CloseForRunEnd()
        {
            closed = true; rewards.Clear(); choices.Clear(); Dirty();
        }

        /// <summary>取走自上次以来已接受命令的结果。</summary>
        public void DrainResults(List<GrowthResult> into)
        {
            into.AddRange(results); results.Clear();
        }

        // ================= 命令 =================
        public bool TryChoose(long offerToken, string definitionId)
        {
            if (!Valid(offerToken) || !choices.Contains(definitionId)) return false;
            var src = currentReward.Source;
            if (definitionId == RunContentCatalog.HealFallbackId)
                results.Add(new GrowthResult(GrowthResultKind.FallbackHeal, src, definitionId, 0, -1, null, Options.FallbackHealFraction));
            else if (!Apply(Catalog.Get(definitionId), src)) return false;
            Consume(); return true;
        }

        public bool TryReroll(long offerToken)
        {
            if (!Valid(offerToken) || offerIsEvolution || Rerolls <= 0) return false;
            Rerolls--; DrawNormal(); return true;
        }

        public bool TryBanish(long offerToken, string definitionId)
        {
            if (!Valid(offerToken) || offerIsEvolution || Banishes <= 0) return false;
            int idx = choices.IndexOf(definitionId);
            if (idx < 0 || definitionId == RunContentCatalog.HealFallbackId) return false;
            Banishes--; banished.Add(definitionId);
            choices.RemoveAt(idx);
            choices.Remove(RunContentCatalog.HealFallbackId);
            var extra = DrawCards(3 - choices.Count, choices);
            choices.InsertRange(Math.Min(idx, choices.Count), extra);
            if (choices.Count < 3 && !choices.Contains(RunContentCatalog.HealFallbackId)) choices.Add(RunContentCatalog.HealFallbackId);
            NewToken(); return true;
        }

        public bool TrySkip(long offerToken)
        {
            if (!Valid(offerToken) || Skips <= 0) return false;
            Skips--;
            results.Add(new GrowthResult(GrowthResultKind.Skipped, currentReward.Source, null, 0, -1, null, Options.SkipHealFraction));
            Consume(); return true;
        }

        private bool Valid(long t) => !closed && HasOffer && t == token;

        private bool Apply(ContentDefinition def, RewardSource src)
        {
            if (def == null) return false;
            var owned = Find(def.Id, out bool isW, out int slot);
            switch (def.Kind)
            {
                case ContentKind.Weapon:
                case ContentKind.Passive:
                    if (owned != null)
                    {
                        if (owned.Rank >= def.MaxRank) return false;
                        owned.Rank++;
                        results.Add(new GrowthResult(isW ? GrowthResultKind.WeaponRankUp : GrowthResultKind.PassiveRankUp, src, def.Id, owned.Rank, slot));
                    }
                    else
                    {
                        var arr = def.Kind == ContentKind.Weapon ? weapons : passives;
                        int free = FreeSlot(arr); if (free < 0) return false;
                        arr[free] = new Owned(def, 1);
                        results.Add(new GrowthResult(def.Kind == ContentKind.Weapon ? GrowthResultKind.WeaponAdded : GrowthResultKind.PassiveAdded, src, def.Id, 1, free));
                    }
                    return true;
                case ContentKind.Evolution:
                    if (!offerIsEvolution || !IsEvolutionEligible(def, currentReward.RunSeconds, out _)) return false;
                    Find(def.EvolvesFromWeapon, out _, out int ws);
                    weapons[ws] = new Owned(def, 1);
                    consumedEvolutions.Add(def.Id);
                    results.Add(new GrowthResult(GrowthResultKind.Evolved, src, def.Id, 1, ws, def.EvolvesFromWeapon));
                    return true;
            }
            return false;
        }

        private void Consume() { choices.Clear(); Dirty(); TryOpenNext(); }

        private void TryOpenNext()
        {
            if (closed || HasOffer || rewards.Count == 0) { Dirty(); return; }
            currentReward = rewards.Dequeue();
            offerIsEvolution = false;
            if (currentReward.Source == RewardSource.Elite)
            {
                foreach (var d in Catalog.All)
                    if (d.Kind == ContentKind.Evolution && choices.Count < 3 && IsEvolutionEligible(d, currentReward.RunSeconds, out _))
                        choices.Add(d.Id);
                if (choices.Count > 0) { offerIsEvolution = true; NewToken(); return; }
            }
            offersMade++;
            DrawNormal();
        }

        // ================= 出牌 =================
        private enum Cat { NewWeapon, WeaponUp, Passive }

        private void DrawNormal()
        {
            choices.Clear();
            var forced = new List<string>(2);
            // 早期新武器机会
            if ((!offeredNewWeaponEarly && offersMade <= Options.EarlyNewWeaponOffers)
                || (Options.NewWeaponOfferInterval > 0 && offersMade % Options.NewWeaponOfferInterval == 0))
            {
                var nw = Legal(Cat.NewWeapon, forced);
                if (nw.Count > 0) forced.Add(PickWeighted(nw));
            }
            // 已有升级保底
            var ownedUp = Legal(Cat.WeaponUp, forced); ownedUp.AddRange(LegalOwnedPassiveUp(forced));
            if (ownedUp.Count > 0) forced.Add(PickWeighted(ownedUp));
            choices.AddRange(forced);
            choices.AddRange(DrawCards(3 - choices.Count, choices));
            if (choices.Count < 3) choices.Add(RunContentCatalog.HealFallbackId);
            foreach (var id in choices) if (Catalog.Get(id)?.Kind == ContentKind.Weapon && RankOf(id) == 0) offeredNewWeaponEarly = true;
            NewToken();
        }

        private List<string> DrawCards(int n, List<string> exclude)
        {
            var picked = new List<string>();
            var ex = new List<string>(exclude);
            for (int k = 0; k < n; k++)
            {
                var pools = new[] { Legal(Cat.NewWeapon, ex), Legal(Cat.WeaponUp, ex), Legal(Cat.Passive, ex) };
                int[] w = { Options.NewWeaponWeight, Options.WeaponUpgradeWeight, Options.PassiveWeight };
                int total = 0;
                for (int i = 0; i < 3; i++) if (pools[i].Count > 0) total += Math.Max(0, w[i]);
                if (total == 0)
                {
                    // 权重全为0但仍有合法项：按顺序兜底
                    string any = null; for (int i = 0; i < 3 && any == null; i++) if (pools[i].Count > 0) any = pools[i][0];
                    if (any == null) break; picked.Add(any); ex.Add(any); continue;
                }
                int r = rng.Next(total), c = 0;
                for (; c < 3; c++) { if (pools[c].Count == 0) continue; int wc = Math.Max(0, w[c]); if (r < wc) break; r -= wc; }
                string id = PickWeighted(pools[c]);
                picked.Add(id); ex.Add(id);
            }
            return picked;
        }

        private string PickWeighted(List<string> ids)
        {
            int total = 0; foreach (var id in ids) total += Math.Max(1, Catalog.Get(id).Weight);
            int r = rng.Next(total);
            foreach (var id in ids) { int w = Math.Max(1, Catalog.Get(id).Weight); if (r < w) return id; r -= w; }
            return ids[ids.Count - 1];
        }

        private bool Base(ContentDefinition d, List<string> ex) => drawable.Contains(d.Id) && !banished.Contains(d.Id) && !ex.Contains(d.Id);

        private List<string> Legal(Cat cat, List<string> ex)
        {
            var list = new List<string>();
            foreach (var d in Catalog.All)
            {
                if (!Base(d, ex)) continue;
                var o = Find(d.Id, out _, out _);
                if (cat == Cat.NewWeapon && d.Kind == ContentKind.Weapon && o == null && FreeSlot(weapons) >= 0 && !IsEvolvedAway(d.Id)) list.Add(d.Id);
                else if (cat == Cat.WeaponUp && d.Kind == ContentKind.Weapon && o != null && o.Rank < d.MaxRank) list.Add(d.Id);
                else if (cat == Cat.Passive && d.Kind == ContentKind.Passive && (o != null ? o.Rank < d.MaxRank : FreeSlot(passives) >= 0)) list.Add(d.Id);
            }
            return list;
        }

        private List<string> LegalOwnedPassiveUp(List<string> ex)
        {
            var list = new List<string>();
            foreach (var p in passives) if (p != null && p.Rank < p.Def.MaxRank && Base(p.Def, ex)) list.Add(p.Def.Id);
            return list;
        }

        private bool IsEvolvedAway(string weaponId)
        {
            foreach (var e in consumedEvolutions) if (Catalog.Get(e)?.EvolvesFromWeapon == weaponId) return true;
            return false;
        }

        private void NewToken() { token++; Dirty(); }
        private void Dirty() { cachedView = null; }

        // ================= 视图（变化时才重建） =================
        public RunContentView ReadView()
        {
            if (cachedView != null) return cachedView;
            var cv = new RunContentChoiceView[HasOffer ? choices.Count : 0];
            for (int i = 0; i < cv.Length; i++)
            {
                string id = choices[i];
                if (id == RunContentCatalog.HealFallbackId) { cv[i] = new RunContentChoiceView(id, "回复", "回复" + (int)(Options.FallbackHealFraction * 100) + "%最大生命（候选已耗尽）", 0, 0); continue; }
                var d = Catalog.Get(id); int cur = d.Kind == ContentKind.Evolution ? 0 : RankOf(id);
                cv[i] = new RunContentChoiceView(id, d.DisplayName, RunContentDescriptions.Choice(d, cur), cur, cur + 1);
            }
            var wv = new List<RunContentOwnedView>();
            foreach (var o in weapons) if (o != null) wv.Add(new RunContentOwnedView(o.Def.Id, o.Def.DisplayName, o.Rank, o.Def.MaxRank));
            var pv = new List<RunContentOwnedView>();
            foreach (var o in passives) if (o != null) pv.Add(new RunContentOwnedView(o.Def.Id, o.Def.DisplayName, o.Rank, o.Def.MaxRank));
            var ev = new List<RunContentEvolutionView>();
            foreach (var d in Catalog.All)
            {
                if (d.Kind != ContentKind.Evolution || !drawable.Contains(d.Id) || consumedEvolutions.Contains(d.Id)) continue;
                // 时间门槛视图里只显示"需≥N分钟"，资格以精英奖励时宿主给的秒数为准
                bool ok = IsEvolutionEligible(d, viewRunSeconds, out string miss);
                string req = ok ? "已满足条件；局内≥" + (int)(d.Evolution.MinRunSeconds / 60) + "分钟后由精英宝箱触发" : "缺：" + miss;
                ev.Add(new RunContentEvolutionView(d.Id, d.EvolvesFromWeapon, d.DisplayName, req, ok));
            }
            cachedView = new RunContentView(OfferToken, Rerolls, Banishes, Skips, cv, wv.ToArray(), pv.ToArray(), ev.ToArray());
            return cachedView;
        }
    }
}
