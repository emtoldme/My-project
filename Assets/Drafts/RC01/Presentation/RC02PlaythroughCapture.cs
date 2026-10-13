using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Waffle;
using Waffle.Combat;
using Reverb.Drafts.RC01.Pipeline;

namespace Reverb.Drafts.RC01.Presentation
{
    // Explicit command-line capture only; ordinary play never runs this short wiring fixture.
    public sealed class RC02PlaythroughCapture : MonoBehaviour
    {
        readonly HashSet<string> hits = new HashSet<string>();
        GameLoop captureLoop;
        void Update()
        {
            // Focus loss must not strand this explicit automated fixture on the pause menu.
            if(captureLoop?.Sim!=null && captureLoop.Sim.State.Phase==RunPhase.Paused) captureLoop.TogglePause();
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs(); int flag=Array.IndexOf(args,"--rc02-capture");
            if(flag<0 || flag+1>=args.Length) yield break;
            Application.runInBackground=true; Application.targetFrameRate=60;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            string output=Path.GetFullPath(args[flag+1]); Directory.CreateDirectory(output);
            yield return null;
            var loop=GetComponent<GameLoop>(); var sim=loop.Sim;
            captureLoop=loop;
            sim.God=true; sim.Combat.Config.spawningEnabled=false;
            loop.PresentationDispatched+=Observe;
            sim.DebugGiveXp(2000);
            var goals=new Dictionary<string,int> { {"W05",1},{"W06",3},{"P17",1},{"P03",1},{"P05",1},{"P08",1} };
            for(int i=0;i<60;i++)
            {
                yield return new WaitForSecondsRealtime(.4f);
                if(sim.State.Phase==RunPhase.Playing) break;
                var view=loop.ReadView();
                var card=view.Choices.FirstOrDefault(c=>goals.TryGetValue(c.Id,out int rank)&&c.CurrentRank<rank);
                if(card.Id==null) card=view.Choices.FirstOrDefault(c=>c.CurrentRank>0);
                if(card.Id==null && view.Skips>0) { loop.TrySkip(view.OfferToken); continue; }
                if(card.Id==null && view.Rerolls>0) { loop.TryReroll(view.OfferToken); continue; }
                if(card.Id==null) card=view.Choices.FirstOrDefault(c=>c.Id.StartsWith("P")||c.Id=="HEAL");
                if(card.Id==null) card=view.Choices[0];
                loop.TryChoose(view.OfferToken,card.Id);
            }
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4;
                sim.Combat.DebugSpawnEnemy(new Vec2(sim.PlayerX+Mathf.Cos(a)*2.5f,sim.PlayerZ+Mathf.Sin(a)*2.5f),50000,0);
            }
            var keyboard=InputSystem.AddDevice<Keyboard>("RC02CaptureKeyboard");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W)); yield return new WaitForSecondsRealtime(.9f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.S)); yield return new WaitForSecondsRealtime(.9f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space)); yield return new WaitForSecondsRealtime(.2f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return new WaitForSecondsRealtime(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"gameplay.png"));
            yield return new WaitForEndOfFrame(); yield return new WaitForSecondsRealtime(.2f);
            var presenter=GetComponent<RC02CatalogPresenter>();
            bool ok=goals.All(g=>sim.Growth.RankOf(g.Key)>=g.Value)&&hits.Contains("W05")&&hits.Contains("W06")&&hits.Contains("P17")&&presenter.SpawnedCount>0;
            File.WriteAllLines(Path.Combine(output,"capture.txt"),new[]{
                "goals="+string.Join(",",goals.Select(g=>g.Key+":"+sim.Growth.RankOf(g.Key))),
                "actualDamageSources="+string.Join(",",hits),"pooledVisualSpawns="+presenter.SpawnedCount,
                "passed="+ok,"debugXP=2000 god=true spawning=false; wiring capture, not natural balance evidence" });
            Debug.Log("RC02_CAPTURE passed="+ok); Application.Quit(ok?0:2);
        }
        void Observe(IReadOnlyList<PresentationEvent> events)
        { foreach(var item in events) if(item.Type==PresentationEventType.EnemyHit && item.SourceId!=null) hits.Add(item.SourceId); }
    }
}
