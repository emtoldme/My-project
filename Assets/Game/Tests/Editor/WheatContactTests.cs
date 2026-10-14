using NUnit.Framework;
using UnityEngine;
using Reverb.Drafts.RC01.Presentation;

namespace Reverb.Wheat.Tests
{
    public sealed class WheatContactTests
    {
        static void Hold(WheatContactField field,Vector2 player,ref float time,int frames)
        {for(int i=0;i<frames;i++){time+=.01f;field.Step(player,time,true);}}
        [Test] public void TouchBuildsGraduallyInsteadOfErasing()
        {
            var f=new WheatContactField();float t=0;f.Step(Vector2.zero,t,true);var probe=new Vector2(.55f,0);
            Hold(f,Vector2.zero,ref t,1);float first=f.Sample(probe).magnitude;Hold(f,Vector2.zero,ref t,30);
            Assert.That(first,Is.LessThan(.05f));Assert.That(f.Sample(probe).magnitude,Is.GreaterThan(.25f));Assert.That(f.Peak,Is.LessThanOrEqualTo(.9f));
        }
        [Test] public void PassedPlantsRetainThenRecoverTheirBend()
        {
            var f=new WheatContactField();float t=0;f.Step(Vector2.zero,t,true);var probe=new Vector2(.55f,0);Hold(f,Vector2.zero,ref t,40);
            Hold(f,new Vector2(0,3),ref t,10);Assert.That(f.Sample(probe).magnitude,Is.GreaterThan(.1f));
            Hold(f,new Vector2(0,3),ref t,250);Assert.That(f.Sample(probe).magnitude,Is.LessThan(.01f));
        }
        [Test] public void PausedFieldDoesNotEvolve()
        {
            var f=new WheatContactField();float t=0;f.Step(Vector2.zero,t,true);Hold(f,Vector2.zero,ref t,20);
            var before=f.Sample(new Vector2(.6f,0));Assert.That(f.Step(new Vector2(3,0),t+1,false),Is.False);
            Assert.That(f.Sample(new Vector2(.6f,0)),Is.EqualTo(before));
        }
        [Test] public void GridShiftPreservesWorldAnchoredHistoryAtNegativeCoordinates()
        {
            var f=new WheatContactField();float t=0;var player=new Vector2(-1,-1);var probe=player+new Vector2(.5f,0);
            f.Step(player,t,true);Hold(f,player,ref t,30);var before=f.Sample(probe);
            f.Step(player+new Vector2(-1.6f,0),t+.00001f,true);
            Assert.That(Vector2.Distance(before,f.Sample(probe)),Is.LessThan(.001f));
        }
        [Test] public void TeleportClearsHistoryWithoutDrawingAnInfiniteWake()
        {
            var f=new WheatContactField();float t=0;f.Step(Vector2.zero,t,true);Hold(f,Vector2.zero,ref t,30);int resets=f.Resets;
            f.Step(new Vector2(2000,-2000),t+.02f,true);
            Assert.That(f.Resets,Is.EqualTo(resets+1));Assert.That(f.Peak,Is.Zero);Assert.That(f.Sample(Vector2.zero),Is.EqualTo(Vector2.zero));
        }
        [Test] public void FastMovementTouchesTheSegmentBetweenFrames()
        {
            var f=new WheatContactField();f.Step(Vector2.zero,0,true);f.Step(new Vector2(4,0),.05f,true);
            Assert.That(f.Sample(new Vector2(2,.4f)).magnitude,Is.GreaterThan(.03f));
        }
        [Test] public void PickupsUseTheSameSoftRecoveryAndBoundedStrength()
        {
            var f=new WheatContactField();float t=0;f.Step(Vector2.zero,0,true);var drops=new[]{new Vector4(4,0,0,1)};
            for(int i=0;i<40;i++){t+=.01f;f.Step(Vector2.zero,t,true,drops,1);}
            var probe=new Vector2(4.35f,0);float before=f.Sample(probe).magnitude;
            Assert.That(before,Is.InRange(.08f,.33f));t+=.01f;f.Step(Vector2.zero,t,true);
            Assert.That(f.Sample(probe).magnitude,Is.GreaterThan(before*.9f));
        }
    }
}
