using System.Linq;
using NUnit.Framework;
using Waffle.Combat;
using Waffle.Finale;
using Waffle.GameplayNext;
using Waffle.Rules.Encounters;

namespace Waffle.Tests
{
    public sealed class ExplorationBoundaryTests
    {
        static RunSimulation Make(bool unbounded=true,bool modules=false)
        {
            var config=new RunConfig{unboundedWorld=unbounded,runDuration=600,damage=modules?0:10,fireInterval=.1f,gameplayNext=new GameplayNextSettings{enabled=modules,endingMode=RunEndingMode.BossClear}};
            var sim=new RunSimulation(config,null,new CombatConfig{spawningEnabled=false,aliveCap=48,targetRange=12});sim.God=true;
            if(modules)sim.InstallGameplayModules(new GameplayModules(new EncounterModule(),null,new FinaleBossModule(12345)));
            return sim;
        }
        static void Tick(RunSimulation sim,InputSnapshot input,int count=1)
        {for(int i=0;i<count;i++)sim.Tick(.05f,input,sim.State.Frame+1,sim.State.Frame+100);}
        [TestCase(1,0)][TestCase(-1,0)][TestCase(0,1)][TestCase(0,-1)]
        public void WalkingAndDodgingCrossTheOldBoundary(float x,float z)
        {
            var sim=Make();sim.PlayerX=x*29;sim.PlayerZ=z*29;
            var input=InputSnapshot.None;input.MoveX=x;input.MoveY=z;Tick(sim,input,12);
            Assert.That(sim.PlayerX*x+sim.PlayerZ*z,Is.GreaterThan(31));
            input.DodgePressed=true;Tick(sim,input);input.DodgePressed=false;Tick(sim,input,5);
            Assert.That(sim.PlayerX*x+sim.PlayerZ*z,Is.GreaterThan(34));
        }
        [Test]public void LegacyArenaStillClampsWhenExplorationIsOff()
        {
            var sim=Make(false);sim.PlayerX=29;var input=InputSnapshot.None;input.MoveX=1;Tick(sim,input,20);
            Assert.That(sim.PlayerX,Is.EqualTo(sim.Config.arenaHalfExtent));
        }
        [Test]public void EnemiesPursueAndProjectilesHitAtDistantCoordinates()
        {
            var sim=Make();sim.PlayerX=516;sim.PlayerZ=-320;
            var enemy=sim.Combat.DebugSpawnEnemy(new Vec2(512,-320),1000,1);Tick(sim,InputSnapshot.None,40);
            Assert.That(enemy.Position.X,Is.GreaterThan(512.5f));Assert.That(enemy.Position.Y,Is.LessThan(-300));
            Assert.That(sim.Combat.Counters.Hits,Is.GreaterThan(0));
        }
        [Test]public void EncounterAndFinaleSpawnsFollowTheExplorer()
        {
            var sim=Make(true,true);sim.PlayerX=-720;sim.PlayerZ=480;Tick(sim,InputSnapshot.None,180);
            Assert.That(sim.Combat.Enemies.Count(e=>e.IsAlive),Is.GreaterThan(0));
            Assert.That(sim.Combat.Enemies.Where(e=>e.IsAlive).All(e=>e.Position.X< -650&&e.Position.Y>400),Is.True);
            sim.DebugSetTime(540);Tick(sim,InputSnapshot.None,100);
            var boss=sim.Combat.Enemies.Single(e=>e.IsBoss&&e.IsAlive);
            Assert.That(Vec2.Distance(boss.Position,new Vec2(sim.PlayerX,sim.PlayerZ)),Is.LessThan(35));
        }
    }
}
