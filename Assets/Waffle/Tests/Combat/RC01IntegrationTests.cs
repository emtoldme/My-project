using System;
using System.Linq;
using NUnit.Framework;
using Waffle.Combat;
using Waffle.RunContent;

namespace Waffle.Tests.Combat
{
    public class RC01IntegrationTests
    {
        static CombatConfig Quiet() => new CombatConfig { spawningEnabled = false, separationRadius = 0, projectilePoolSize = 128 };
        static CombatInput Input(float now) => new CombatInput { Dt = .05f, RunTime = now, PlayerPos = Vec2.Zero,
            Stats = new CombatPlayerStats { MagnetRadius = 3 } };
        static CombatModule Weapon(string id)
        {
            var growth = new RunGrowthState(1,null,new GrowthOptions { StartingWeapon = id },new[] { id });
            var combat = new CombatModule(Quiet(),1); combat.EnableRunContent(growth,1); return combat;
        }
        [Test]
        public void FanLimitsSameTargetHitsAcrossOneVolley()
        {
            var combat = Weapon("W02"); var enemy = combat.DebugSpawnEnemy(new Vec2(1.5f,0),100,0);
            for (int i = 0;i < 10;i++) { combat.StepCombat(Input(i*.05f)); combat.Release(); }
            Assert.That(enemy.Hp,Is.EqualTo(94).Within(.01f));
        }
        [Test]
        public void OrbitHitsItsPathAndDoesNotDamageTheWholeInnerDisk()
        {
            var combat = Weapon("W03"); var near = combat.DebugSpawnEnemy(new Vec2(1.8f,0),100,0);
            var centre = combat.DebugSpawnEnemy(Vec2.Zero,100,0);
            combat.StepCombat(Input(0));
            Assert.That(near.Hp,Is.EqualTo(95)); Assert.That(centre.Hp,Is.EqualTo(100));
            combat.Release(); combat.StepCombat(Input(.05f)); Assert.That(near.Hp,Is.EqualTo(95));
        }
        [Test]
        public void ZoneWaitsForItsWarningAndTicksAtALimitedRate()
        {
            var combat = Weapon("W04"); var e = combat.DebugSpawnEnemy(new Vec2(3,0),100,0);
            for(int i=0;i<12;i++) { combat.StepCombat(Input(i*.05f)); combat.Release(); }
            Assert.That(e.Hp,Is.EqualTo(100));
            combat.StepCombat(Input(.65f)); Assert.That(e.Hp,Is.EqualTo(96)); combat.Release();
            combat.StepCombat(Input(.7f)); Assert.That(e.Hp,Is.EqualTo(96));
        }
        [Test]
        public void PickupsHealMagnetConserveXpAndPulseOnlyKillsOnce()
        {
            var combat = Weapon("W01");
            combat.DebugSpawnGem(new Vec2(9,0),5); combat.DebugSpawnGem(new Vec2(12,0),7);
            combat.DebugSpawnPickup("D02",Vec2.Zero); combat.DebugSpawnPickup("D03",Vec2.Zero);
            var result=combat.StepGems(Input(0)); Assert.That(result.HealFraction,Is.EqualTo(.2f)); combat.Release();
            int xp=result.XpCollected;
            for(int i=1;i<40;i++) { result=combat.StepGems(Input(i*.05f)); xp+=result.XpCollected; combat.Release(); }
            Assert.That(xp,Is.EqualTo(12)); Assert.That(combat.Counters.TotalXpDropped,Is.EqualTo(12));
            combat.DebugSpawnEnemy(new Vec2(2,0),10,0); combat.DebugSpawnPickup("D04",Vec2.Zero);
            result=combat.StepGems(Input(2)); Assert.That(result.Kills,Is.EqualTo(1));
            Assert.That(result.Events.Count(e=>e.Type==PresentationEventType.EnemyKilled),Is.EqualTo(1)); combat.Release();
            Assert.That(combat.StepGems(Input(2.05f)).Kills,Is.Zero);
        }
        [Test]
        public void AcquiredCombinationHasOneSecondaryHopAndNeverRecurses()
        {
            var growth=new RunGrowthState(1,null,null,new[]{"W01","P12","P13"});
            foreach(string id in new[]{"P12","P13"})
            { growth.EnqueueLevelUpReward(); Assert.That(growth.TryChoose(growth.OfferToken,id),Is.True); }
            var combat=new CombatModule(Quiet(),1); combat.EnableRunContent(growth,1);
            combat.DebugSpawnEnemy(new Vec2(3,0),10000,0); combat.DebugSpawnEnemy(new Vec2(4.5f,0),10000,0);
            int direct=0,combined=0;
            for(int i=0;i<60;i++)
            {
                foreach(var e in combat.StepCombat(Input(i*.05f)).Events)
                {
                    if(e.Type==PresentationEventType.EnemyHit) direct++;
                    if(e.Type==PresentationEventType.EchoPropagationHit)
                    { combined++; Assert.That(e.SourceId,Is.EqualTo("P12_P13")); Assert.That(e.RootSourceId,Is.EqualTo("W01")); }
                }
                combat.Release();
            }
            Assert.That(combined,Is.GreaterThan(0)); Assert.That(combined,Is.LessThanOrEqualTo(direct));
        }
        static RunSimulation Host() => new RunSimulation(new RunConfig { useRunContent=true,seed=1,runDuration=600,levelUpInputGuard=0,
            runContentAllowList=new[]{"W01","P12","E01"}},new NullRunLog(),Quiet());
        static void Level(RunSimulation sim, ref long frame)
        {
            sim.DebugGiveXp(XpRules.XpToNext(sim.State.Level)-sim.State.Xp);
            sim.Tick(.05f,default,++frame,frame);
            Assert.That(sim.State.Phase,Is.EqualTo(RunPhase.LevelUp));
            var view=sim.ReadContentView(); var card=view.Choices.First(c=>c.Id!="HEAL");
            sim.BeginFrame(++frame,frame);
            Assert.That(sim.TryChooseContent(view.OfferToken,card.Id),Is.True);
        }
        [Test]
        public void RealXpQueueEliteEvolutionAndNewRunShareOneHost()
        {
            var sim=Host(); long frame=0;
            Assert.That(sim.Growth.RankOf("P12"),Is.Zero);
            for(int i=0;i<6;i++) Level(sim,ref frame);
            Assert.That(sim.State.PendingLevelUps,Is.Zero); Assert.That(sim.Growth.RankOf("W01"),Is.EqualTo(5));
            Assert.That(sim.Growth.RankOf("P12"),Is.GreaterThanOrEqualTo(2));
            sim.DebugSetTime(240); sim.Combat.DebugSpawnPickup("ELITE",Vec2.Zero);
            sim.Tick(.05f,default,++frame,frame);
            var view=sim.ReadContentView(); Assert.That(view.Choices.Any(c=>c.Id=="E01"),Is.True);
            sim.BeginFrame(++frame,frame); Assert.That(sim.TryChooseContent(view.OfferToken,"E01"),Is.True);
            Assert.That(sim.State.PendingLevelUps,Is.Zero); Assert.That(sim.State.Phase,Is.EqualTo(RunPhase.Playing));
            Assert.That(sim.Combat.RunWeapons.Single().Id,Is.EqualTo("E01"));
            Assert.That(sim.TryChooseContent(view.OfferToken,"E01"),Is.False);
            var fresh=Host(); Assert.That(fresh.Growth.RankOf("W01"),Is.EqualTo(1)); Assert.That(fresh.Growth.RankOf("P12"),Is.Zero);
        }
        [Test]
        public void SimultaneousLevelUpsDoNotDuplicateResourcesOrResumeEarly()
        {
            var sim=Host(); sim.DebugGiveXp(30); sim.Tick(.05f,default,1,1);
            int pending=sim.State.PendingLevelUps; Assert.That(pending,Is.EqualTo(3));
            var old=sim.ReadContentView(); sim.BeginFrame(2,2);
            Assert.That(sim.TryRerollContent(old.OfferToken),Is.True);
            Assert.That(sim.TryChooseContent(old.OfferToken,old.Choices[0].Id),Is.False);
            Assert.That(sim.State.PendingLevelUps,Is.EqualTo(pending));
            long frame=2;
            while(sim.State.PendingLevelUps>0)
            {
                var v=sim.ReadContentView(); sim.BeginFrame(++frame,frame);
                Assert.That(sim.TryChooseContent(v.OfferToken,v.Choices[0].Id),Is.True);
                Assert.That(sim.TryChooseContent(v.OfferToken,v.Choices[0].Id),Is.False);
                if(sim.State.PendingLevelUps>0) Assert.That(sim.State.Phase,Is.EqualTo(RunPhase.LevelUp));
            }
            Assert.That(sim.State.Phase,Is.EqualTo(RunPhase.Playing));
        }
    }
}
