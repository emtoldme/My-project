using System;
using System.Collections.Generic;
using System.Globalization;

namespace Waffle.RunContent
{
    // Player-facing explanations use the same rank parameters as combat, rather than a second numeric table.
    public static class RunContentDescriptions
    {
        static string N(float n) => n.ToString("0.##", CultureInfo.InvariantCulture);
        public static string Choice(ContentDefinition def, int currentRank)
        {
            var next=def.RankAt(currentRank+1);
            if(next==null) return def.Description;
            var cur=def.RankAt(currentRank);
            var changes=new List<string>();
            void Add(string key,string label,string unit="")
            {
                float to=next.Get(key), from=cur?.Get(key) ?? 0;
                if(currentRank>0 && Math.Abs(to-from)<.0001f) return;
                changes.Add(label+" "+(currentRank>0 ? N(from)+unit+" → " : "")+N(to)+unit);
            }
            void Ratio(string key,string label)
            {
                float from=(cur?.Get(key) ?? 0)*100, to=next.Get(key)*100;
                changes.Add(label+" "+(currentRank>0 ? N(from)+"% → " : "")+N(to)+"%");
            }
            string effect,limit="";
            switch(def.Id)
            {
                case "W01":
                    effect="自动向最近敌人发射直线弹，用于点杀和穿透。";
                    Add("dmg","每弹伤害"); Add("cd","发射间隔","秒"); Add("count","每次弹数","发"); Add("pierce","每弹最多命中","敌");
                    limit="穿透数包含首个敌人；双发的伤害分别结算。"; break;
                case "W02":
                    effect="朝最近敌人扇形散射，适合贴近敌群开路。";
                    Add("dmg","每弹伤害");Add("cd","间隔","秒");Add("count","弹数","发");Add("arc","扇角","°");Add("range","射程","米");Add("sameTargetHits","同轮对同一敌最多命中","次");
                    if(next.Get("returnShots")>0 && (cur?.Get("returnShots") ?? 0)==0) changes.Add("外侧2发到射程后回返");
                    limit="命中上限含去程和回程，不是所有弹丸都能打中同一敌人。"; break;
                case "W03":
                    effect="护卫球绕身旋转，只伤害实际碰到的敌人；靠走位接敌。";
                    Add("dmg","接触伤害");Add("count","护卫球","个");Add("radius","轨道半径","米");Add("angSpeed","转速","°/秒");
                    if(next.Get("pulseEvery")>0) changes.Add("每"+N(next.Get("pulseEvery"))+"秒外扩至"+N(next.Get("pulseRadius"))+"米，持续"+N(next.Get("pulseDur"))+"秒");
                    limit="同一敌人基础受击间隔0.5秒；轨道内部不是整片伤害区。";break;
                case "W04":
                    effect="在目标位置预警0.6秒后生成固定伤害区，适合封路和聚团。";
                    Add("dmg","每次伤害");Add("tick","伤害间隔","秒");Add("cd","施放间隔","秒");Add("count","区域数","个");Add("radius","半径","米");Add("duration","持续","秒");
                    if(next.Get("burst")>0) changes.Add("区域结束时额外造成"+N(next.Get("burst"))+"伤害");
                    limit="区域不追踪敌人；敌人离开后不继续受伤。";break;
                case "P01": effect="提高所有武器伤害，回声和传播也随原命中变强。";Add("damagePct","伤害加成","%");limit="各阶为累计加成，不是每次乘上一个新倍率。";break;
                case "P02": effect="缩短武器发射、落区伤害和环绕接触的间隔。";Add("ratePct","攻击频率加成","%");limit="频率+10%表示间隔÷1.10；不增加护卫球转速、移速或护盾频率。";break;
                case "P04": effect="扩大散射射程、护卫轨道和共振区域。";Add("areaPct","范围加成","%");limit="不扩大定向脉冲索敌范围，也不增加伤害或弹数。";break;
                case "P06": effect="提高常规移动速度，方便走位和收集。";Add("moveSpeedPct","移速加成","%");limit="不改变闪避距离、速度或冷却。";break;
                case "P07": effect="提高最大生命，并立刻补上新增的生命差值。";Add("maxHpPct","最大生命加成","%");limit="原来缺失的生命不会补满；不是全额回复。";break;
                case "P09": effect="扩大经验宝石及回复、磁吸、清场道具的吸取范围，进入范围后自动飞向玩家。";Add("pickupPct","吸取半径加成","%");limit="不增加经验价值；精英箱仍需靠近开启。各阶为累计加成。";break;
                case "P12": effect="直接命中后0.5秒，同一敌人再受一次回声伤害。";Ratio("echoCoef","额外伤害相当于原命中的");limit="目标提前死亡则取消；与传播同时拥有时，回声可再传给1名邻居一次。";break;
                case "P13": effect="直接命中后0.1秒，把部分伤害传给附近其他敌人。";Ratio("propCoef","传播伤害相当于原命中的");Add("propRadius","搜索半径","米");Add("propBudget","每次最多邻居","名");limit="不是无限连锁；没有邻居时无效，派生不继续递归。";break;
                case "P15": effect="直接命中时把敌人向远离玩家的方向推开。";Add("knockback","推退距离","米");limit="同一敌人每0.5秒最多一次；精英仅获得35%的推退距离。";break;
                case "P18": effect="周期补充一层护盾，抵挡一次有效接触伤害。";Add("shieldEvery","补充间隔","秒");limit="最多1层，首次需等待一个周期；暂停不计时，不延长闪避无敌。";break;
                case "E01": return "替换满阶定向脉冲，不占新武器槽。\n双发，每发12伤，间隔0.45秒，每弹最多命中2敌。\n原目标回声系数在已装备回声的基础上再加0.8；仍只有一次回声，可与传播组合。";
                case "E02": return "替换满阶扇形散射，不占新武器槽。\n7发×10伤，70°扇角，射程6米，间隔0.7秒；外侧2发回返。\n额外推退1米（精英折减），同轮同一敌人最多受击3次。";
                case "E03": return "替换满阶环绕护卫，不占新武器槽。\n4个护卫球，接触10伤，轨道2.2米、240°/秒，同敌基础间隔0.45秒。\n每次护盾补充时发出3.5米、20伤脉冲；不保留旧武器的周期外扩。";
                default:return def.Description+"\n本次："+next.EffectText;
            }
            return effect+"\n本次："+string.Join("；",changes)+"。\n"+limit;
        }
    }
}
