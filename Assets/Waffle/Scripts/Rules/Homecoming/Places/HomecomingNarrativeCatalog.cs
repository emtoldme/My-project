using System;
using System.Collections.Generic;

namespace Waffle.Homecoming.Places
{
    /// <summary>
    /// 统一叙事目录：地点/远处地点/遗物/目标(+完成补文 .complete)/永久升级/全部现有RunContent物品（以真实ID为键）。
    /// 只读；不改RunContent名称、数值或代码。新增可交互物品必须在此登记。
    /// </summary>
    public sealed class HomecomingNarrativeCatalog
    {
        public const string CompletionSuffix = ".complete";
        public static string CompletionId(string goalId) => goalId + CompletionSuffix;

        static readonly Lazy<HomecomingNarrativeCatalog> def = new Lazy<HomecomingNarrativeCatalog>(() => new HomecomingNarrativeCatalog());
        public static HomecomingNarrativeCatalog Default => def.Value;

        readonly Dictionary<string, NarrativeEntry> byId = new Dictionary<string, NarrativeEntry>(StringComparer.Ordinal);
        readonly List<NarrativeEntry> all = new List<NarrativeEntry>();
        public IReadOnlyList<NarrativeEntry> All { get; }

        public NarrativeEntry Resolve(string id) => id != null && byId.TryGetValue(id, out var e) ? e : null;
        public bool Contains(string id) => Resolve(id) != null;
        public IEnumerable<NarrativeEntry> OfKind(NarrativeKind k) { foreach (var e in all) if (e.Kind == k) yield return e; }

        void Add(string id, string title, string body, NarrativeKind kind)
        {
            var e = new NarrativeEntry(id, title, body, kind);
            byId.Add(id, e); all.Add(e);
        }

        HomecomingNarrativeCatalog()
        {
            const NarrativeKind P = NarrativeKind.Place, I = NarrativeKind.Item, G = NarrativeKind.Goal, U = NarrativeKind.Upgrade;
            Add("combo.backstitch", "回针 · 把来路缝进歌里", "母亲收针时总要回一针。把琴弦系在针线盒上，你也能把离开的地方留住：首次共鸣闪避留针，下一次花1碎音返回旧位置并发出残响。线长24米，24秒后散开；回来之前，先想清楚那边还有谁。", NarrativeKind.Combination);
            Add("combo.thread-lure", "牵弦 · 顺着声音收拢", "父亲的手套认得麦秆，琴弦认得风。花1碎音，让闪避留下的旧声源牵走附近普通追击者2.5秒。把声音留在残留区域边上，自己从另一侧绕开；特殊敌人和首领仍认得你的脚步。", NarrativeKind.Combination);
            Add("combo.hearth-gather", "归巢 · 将散落的带回家", "手套兜住田间散落的声，针线盒让它们不再漏出去。花1碎音随闪避牵回8米内至多12份已有经验；真正收进手里才算所得。是继续用掉碎音赶路，还是留满三份回旧地返灯，由你决定。", NarrativeKind.Combination);
            Add("poi.old-home.rekindled", "又一次亮起的门廊", "你把田里带回的碎音倒在窗台上。铁盒轻轻一响，门廊的灯亮了。这次离开时，衣襟里多了一针，能替你挡一次冷风。", G);
            Add("poi.old-silo.rekindled", "粮高线以下的回声", "空仓里没有粮食，你却还是把手放在那道粉笔线上。三份碎音滚向仓底，回来的声音终于与你的呼吸合上了。", G);
            Add("poi.old-guitar.rekindled", "为断弦添一小节", "你没有换掉第三根弦，只把结重新系紧。带回来的碎音接上了那一小节；弹完以后，路还在麦田里等着。", G);
            Add("poi.field-swing.rekindled", "替陌生人收起座板", "你扶正了被风翻过来的秋千，留下一点田间碎音。这里不是你家，也能歇一会儿再走。", G);
            Add("poi.field-tractor.rekindled", "借一盏地头的灯", "散热格栅里的麦秆轻轻一抖，旧灯在碎音里亮了一瞬。你理好衣襟，把路重新认了一遍。", G);
            Add("poi.field-radio.rekindled", "天气预报之后", "你将收来的碎音调进广播箱。天气预报的尾声里，忽然有人说：回去的时候，记得带上外套。", G);
            // ---- 三处故事地点 ----
            Add("poi.old-home", "主角家的老房子", "门框上最后一道身高线旁，用铅笔写着“等秋收”。门锁换过了，钥匙早不知去了哪里，可厨房窗台上的针线盒还在，盖子上落着一层跟八八年夏天一样细的土。", P);
            Add("poi.old-silo", "老筒仓", "父亲用粉笔在内壁记粮高，最矮的那道是你踮脚够得着的位置。如今仓是空的，风从缝里穿过去，喊一声，回声总要迟一拍才回来。", P);
            Add("poi.old-guitar", "倒下的果树与旧吉他", "那棵苹果树终究没熬过去年的风。树根翻起的土旁边靠着一把旧吉他，第三根弦上还系着你打的结。那首没学完的歌，如今只剩风替你接下一小节。", P);
            // ---- 远处通用地点（无奖励） ----
            Add("poi.field-swing", "别人家的秋千", "两根锈链吊着一块磨亮的座板。不知道是谁家的孩子在这里荡过整个夏天，绳结的打法和你的不一样。", P);
            Add("poi.field-tractor", "停在地头的农机", "褪色的红漆，散热格栅里塞着去年的麦秆。驾驶座上留着一顶草帽，帽檐朝着收音机里天气预报说要下雨的方向。", P);
            Add("poi.field-radio", "田边的旧广播箱", "铁皮箱里的喇叭早该哑了，可偶尔还能听见一段跑调的乡村电台，像有人隔着很多年在调频。", P);
            // ---- 三件遗物 ----
            Add("item.sewing-tin", "针线盒", "饼干铁盒改的，里面的线按颜色绕在旧纽扣卡片上。母亲说补过的衣服比新的更贴身。装备：最大生命提高，取得时不会立刻补满。", I);
            Add("item.work-gloves", "父亲的工作手套", "皮面磨出了父亲的掌纹，左手食指缝过两次。戴上它，手掌认得田里每一种重量。装备：拾取半径变大。", I);
            Add("item.old-guitar", "旧吉他", "琴颈上的漆被拇指磨掉一块，第三根弦系着你打的结。装备后，每次真正躲开攻击，都会把断掉的那一拍接回来，留下一声短促的残响。", I);
            // ---- 三个目标与完成补文 ----
            Add("goal.homeward", "门框上的最后一道线", "回到老房子，读完门框上的字，把母亲的针线盒带走。", G);
            Add("goal.homeward.complete", "门框上的最后一道线·完成", "这次你没有在门口转身。", G);
            Add("goal.harvest-memory", "空仓也记得秋收", "找到老筒仓，戴上父亲的手套，再从那片空地走出去。", G);
            Add("goal.harvest-memory.complete", "空仓也记得秋收·完成", "粉笔线最矮的那一道旁边，父亲后来又补了一笔：那年你够得着的，是他扛不动的最后一袋粮。", G);
            Add("goal.unfinished-song", "把最后一小节弹完", "背起旧吉他，在一次真正的躲闪里，让它响一声。", G);
            Add("goal.unfinished-song.complete", "把最后一小节弹完·完成", "断弦没有接上，可那一小节终于有人弹完了。风停了一下，像在等你开下一首。", G);
            // ---- 三条永久升级 ----
            Add("meta.mended-coat", "补好的外套", "照着针线盒里的针脚，一针一针把袖口补好。每一级都让下一次出发更扛得住一点：最大生命小幅提高。", U);
            Add("meta.travel-light", "熟路的脚步", "走过的田埂会记得你。每一级都让你在麦田里走得稍快一些。", U);
            Add("meta.sorted-satchel", "整理行囊", "把挎包里的东西重新排好，伸手就能摸到。每一级都让拾取范围稍大一点。", U);

            // ---- 现有RunContent全部物品（真实ID，不改原名称/数值） ----
            Add("W01", "定向脉冲", "镇上无线电修理铺老板教过你：先找最近的那个信号，再把力气全推过去。一条直线，干净利落。", I);
            Add("W02", "扇形散射", "像撒种子那样张开手。离得越近，散开的声浪越密，贴脸的东西一个也跑不掉。", I);
            Add("W03", "环绕护卫", "小时候你绕着谷仓跑圈，影子总追在身后。现在那些影子学会了替你挡在身边。", I);
            Add("W04", "定点共振", "把一个音钉进地里，停一拍，整块地都跟着颤起来。祖父说这叫“让土地记住”。", I);
            Add("W05", "弹跳电弧", "雷雨夜里，电线杆之间跳过的那道光你一直记得。它从不在同一个人身上停两次。", I);
            Add("W06", "移动残响", "你走过的地方会多留一会儿声音，像麦茬上没散的脚印，专等追来的东西踩上去。", I);
            Add("P01", "输出增幅", "把收音机音量旋钮拧到底时，喇叭会微微发烫。每一下都更重。", I);
            Add("P02", "节拍加速", "教堂唱诗班的节拍器被你偷偷调快了一格，从此每首歌都赶着收尾。", I);
            Add("P03", "脉冲推进", "顺风喊出去的声音能传到对面的谷仓。弹出去的东西也更快到达。", I);
            Add("P04", "扩域", "站在空地中央拍一下手，回声比站在墙角时铺得更开。", I);
            Add("P05", "延音", "踩住钢琴的延音踏板，最后一个音迟迟不肯离开。", I);
            Add("P06", "轻步", "光脚踩过晒热的土路，你学会了只让脚尖着地。", I);
            Add("P07", "生命储备", "母亲腌的那一窖菜，熬过了最冷的一个冬天。底子厚了，人就经得住。", I);
            Add("P08", "减伤垫片", "父亲把旧轮胎皮剪成垫片，塞进你的鞋跟和肩带里。磕碰还会疼，只是轻一点。", I);
            Add("P09", "采集磁芯", "口袋里那块从废收音机拆下的磁铁，总能把散落的螺丝吸回来。", I);
            Add("P10", "缓释恢复", "井水很凉，一小口一小口地喝，伤口也就慢慢不疼了。", I);
            Add("P11", "精准谐振", "调准频率的那一刻，杂音全消，只剩一声最清楚的。", I);
            Add("P12", "回声模块", "对着山谷喊一声，半拍之后，它会原样还给刚才那个人。", I);
            Add("P13", "传播模块", "邻里之间的消息传得比风还快，总会找到下一个没听说的人。", I);
            Add("P14", "滞留模块", "雨后的泥地粘住靴子，谁踩进来都得慢下来。", I);
            Add("P15", "推波模块", "推开纱门时那一下反弹，把扑上来的东西推回门外。", I);
            Add("P16", "击破余振", "砸碎一只旧瓶子，碎片还会在地板上响一会儿。", I);
            Add("P17", "闪避放电", "冬天在地毯上蹭过脚，再碰门把手，会噼啪一声。躲开的那一下也是。", I);
            Add("P18", "间歇护盾", "谷仓的木门每隔一阵就被风吹得关上一次，刚好替你挡一下。", I);
            Add("E01", "延迟重奏", "老唱片转到一半会跳针，同一句歌词唱了两遍，第二遍更响。", I);
            Add("E02", "回折扇面", "风车的叶片转过去又转回来，把靠近的麦浪推出一道缺口。", I);
            Add("E03", "行走屏障", "四只看门狗围着你走，只要有谁扑上来，它们就一齐吼出声。", I);
            Add("E04", "共振网络", "一排电线杆在夜里嗡嗡作响，一根接一根，把同一首歌传下去。", I);
            Add("E05", "连锁过载", "保险丝烧断的那一声，整排灯接连炸亮又熄灭。", I);
            Add("E06", "疾行回路", "你一路留下的脚印同时响起来，像整条田埂都在替你喊。", I);
            Add("D01", "经验", "田里捡回来的一点一滴，都会慢慢长成你自己的本事。", I);
            Add("D02", "回复", "谁家窗台上晾着一块玉米面包，还温着。吃一口，力气回来些。", I);
            Add("D03", "全场磁吸", "母亲在门廊上喊一声开饭，满地乱跑的东西都往你这边回来。", I);
            Add("D04", "清场脉冲", "夏天第一声雷落下来，整片田一下子安静了。", I);
            Add("D05", "临时屏障", "一把旧雨伞，只撑得住一阵，但这一阵刚好够。", I);
            Add("D06", "闪避补给", "系紧鞋带，深吸一口气，又能再跑一次。", I);
            Add("HEAL", "歇一口气", "没什么新的可学了，那就坐在田埂上歇一会儿，喝口水，把伤口包好。", I);
            All = all.AsReadOnly();
        }
    }
}
