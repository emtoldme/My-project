using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using Waffle;
using Waffle.Combat;

namespace Reverb.Drafts.RC01.Presentation
{
    // Explicit automation only. Debug XP/time/God below never run in a normal launch.
    public sealed class RC01StandaloneSmoke : MonoBehaviour
    {
        static bool awaitingRestart;
        static string output;
        static readonly List<string> observations = new List<string>();
        readonly HashSet<string> damageSources = new HashSet<string>();
        bool passed = true;
        GameLoop loop;
        AudioSource[] audioVoices;
        readonly HashSet<string> playedAudio=new HashSet<string>();
        bool polished;
        bool DeathTailPlaying()
        {
            foreach(var voice in audioVoices) if(voice!=null && voice.isPlaying && voice.clip!=null
                && voice.clip.name.StartsWith("TEST_ONLY_sfx_enemy_death_v")) return true;
            return false;
        }
        void Check(bool ok,string label) { observations.Add(label+"="+ok); passed &= ok; }
        void Record(IReadOnlyList<PresentationEvent> events)
        {
            foreach(var e in events) if(e.Type==PresentationEventType.EnemyHit && e.SourceId!=null) damageSources.Add(e.SourceId);
            if(audioVoices!=null) foreach(var voice in audioVoices)
                if(voice!=null && voice.isPlaying && voice.clip!=null) playedAudio.Add(voice.clip.name);
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs(); int flag=Array.IndexOf(args,"--rc01-smoke-output");
            if(flag<0 || flag+1>=args.Length) yield break;
            output=Path.GetFullPath(args[flag+1]); Directory.CreateDirectory(output);
            Application.runInBackground=true; Application.targetFrameRate=60;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            loop=GetComponent<GameLoop>();
            yield return null;
            if(awaitingRestart)
            {
                Check(loop.Sim.Growth.RankOf("W01")==1 && loop.Sim.Growth.RankOf("P12")==0
                    && loop.Sim.Growth.RankOf("E01")==0 && loop.Sim.State.PendingLevelUps==0
                    && loop.Sim.Combat.Zones.Count==0 && loop.Sim.Combat.Pickups.Count==0,"freshRestart");
                Finish(); yield break;
            }
            observations.Clear(); Check(loop.Sim.Growth!=null,"growthHost");
            var audio=GetComponent<AudioDirector>();
            audioVoices=GetComponentsInChildren<AudioSource>();
            polished=audio!=null && audio.enemyDeath.clearOnPause;
            if(polished)
            {
                bool wired=audio.echo.clips.Length==2 && audio.propagation.clips.Length==2 && audio.levelUpConfirm.clips.Length==2;
                foreach(var binding in audio.contentBindings)
                    if(binding.contentId=="W01" || binding.contentId=="W02" || binding.contentId=="W03" || binding.contentId=="W04")
                        wired &= binding.rule.clips.Length==3 && binding.rule.minDistance==30 && binding.rule.sequential;
                Check(wired,"polishR1Bindings");
            }
            if(loop.config.extraXpStepAfterFour>0)
                Check(audio!=null && audio.enemyDeath.clips.Length==3 && audio.enemyHit.clips.Length==3
                    && audio.dodge.clips.Length==2 && audio.gemPickup.clips.Length==5,"feedbackAudioVariations");
            Check(loop.Sim.Growth.RankOf("P12")==0 && loop.Sim.Growth.RankOf("P13")==0,"noFreeMechanisms");
            loop.PresentationDispatched+=Record;
            if(loop.Sim.State.Phase==RunPhase.Paused) loop.TogglePause();
            var keyboard=InputSystem.AddDevice<Keyboard>("RC01SmokeKeyboard");
            Vector3 start=loop.playerView.position;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W)); yield return new WaitForSecondsRealtime(.25f);
            Vector3 before=loop.playerView.position;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.Space)); yield return new WaitForSecondsRealtime(.15f);
            Check(Vector3.Distance(start,before)>.2f,"movement");
            Check(Vector3.Distance(before,loop.playerView.position)>1f && loop.playerView.localScale==Vector3.one,"dodgeAndRootScale");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return new WaitForSecondsRealtime(.1f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape)); yield return new WaitForSecondsRealtime(.1f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); float frozen=loop.Sim.State.Now;
            yield return new WaitForSecondsRealtime(.15f);
            Check(loop.Sim.State.Phase==RunPhase.Paused && loop.Sim.State.Now==frozen,"pause");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape)); yield return new WaitForSecondsRealtime(.1f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            int width=Screen.width,height=Screen.height;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F11)); yield return new WaitForSecondsRealtime(.6f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); Check(Screen.fullScreen,"fullscreen");
            yield return new WaitForSecondsRealtime(.1f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F11)); yield return new WaitForSecondsRealtime(.6f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            Check(!Screen.fullScreen && Screen.width==width && Screen.height==height,"restoreWindow");
            var sim=loop.Sim;
            sim.DebugSetHp(50); sim.Combat.DebugSpawnPickup("D02",new Vec2(sim.PlayerX,sim.PlayerZ));
            yield return new WaitForSecondsRealtime(.1f); Check(sim.State.Hp>50,"actualHealPickup"); sim.God=true;
            sim.Combat.DebugSpawnEnemy(new Vec2(sim.PlayerX+6,sim.PlayerZ),20,0);
            sim.Combat.DebugSpawnPickup("D04",new Vec2(sim.PlayerX,sim.PlayerZ));
            yield return new WaitForSecondsRealtime(.1f); Check(damageSources.Contains("D04"),"actualPulsePickup");
            if(polished)
            {
                Check(DeathTailPlaying(),"deathTailStarted");
                loop.TogglePause();Check(!DeathTailPlaying(),"deathTailStoppedOnPause");
                loop.TogglePause();yield return new WaitForSecondsRealtime(.03f);
                Check(!DeathTailPlaying(),"deathTailNotReplayed");
            }
            sim.DebugGiveXp(2000);
            yield return new WaitForSecondsRealtime(.1f);
            float deadline=Time.realtimeSinceStartup+35;
            bool numericChecked=false;
            while(Time.realtimeSinceStartup<deadline)
            {
                if(sim.State.Phase!=RunPhase.LevelUp) { yield return new WaitForSecondsRealtime(.15f); if(sim.State.Phase!=RunPhase.LevelUp && sim.QueuedXp==0) break; continue; }
                yield return new WaitForSecondsRealtime(.35f);
                var view=sim.ReadContentView();
                if(!numericChecked && loop.config.extraXpStepAfterFour>0)
                {
                    bool detailed=true; foreach(var c in view.Choices) detailed &= c.EffectText.Contains("\n");
                    Check(detailed,"detailedChoiceDescriptions");
                }
                if(!numericChecked)
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,"growth-ui.png"));
                    yield return new WaitForSecondsRealtime(.1f);
                    int pending=sim.State.PendingLevelUps;
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Digit1)); yield return new WaitForSecondsRealtime(.1f);
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                    Check(sim.State.PendingLevelUps==pending-1,"numericChoice"); numericChecked=true; continue;
                }
                string chosen=view.Choices[0].Id; int best=-1;
                foreach(var c in view.Choices)
                {
                    int score= c.Id=="W01" ? 120 : c.Id=="P12" ? 115 : c.Id=="W02" ? 100 : c.Id=="W03" ? 95
                        : c.Id=="W04" ? 90 : c.Id=="P13" ? 85 : c.Id=="P15" ? 80 : c.Id=="P18" ? 75 : 10;
                    if(score>best) { best=score; chosen=c.Id; }
                }
                Check(loop.TryChoose(view.OfferToken,chosen),"choiceAccepted");
            }
            Check(sim.State.Phase==RunPhase.Playing && sim.State.PendingLevelUps==0,"queueDrained");
            Check(sim.Growth.RankOf("W01")==5 && sim.Growth.RankOf("P12")>=2,"evolutionPrerequisites");
            if(sim.State.Phase!=RunPhase.Playing) { Finish(); yield break; }
            // Prevent a debug time jump from filling the enemy pool with historical scheduled waves.
            sim.Combat.Config.spawningEnabled=false;
            sim.DebugSetTime(241); sim.Combat.DebugSpawnPickup("ELITE",new Vec2(sim.PlayerX,sim.PlayerZ));
            yield return new WaitForSecondsRealtime(.4f);
            var evo=sim.ReadContentView(); bool offered=false; foreach(var c in evo.Choices) if(c.Id=="E01") offered=true;
            Check(offered,"eliteOffersEvolution"); if(offered) Check(loop.TryChoose(evo.OfferToken,"E01"),"evolved");
            Check(sim.Combat.DebugSpawnEnemy(new Vec2(sim.PlayerX+1.8f,sim.PlayerZ),10000,0)!=null,"nearTargetSpawned");
            Check(sim.Combat.DebugSpawnEnemy(new Vec2(sim.PlayerX+3,sim.PlayerZ+1),10000,0)!=null,"neighborSpawned");
            yield return new WaitForSecondsRealtime(3);
            Check((damageSources.Contains("W01") || damageSources.Contains("E01")) && damageSources.Contains("W02") && damageSources.Contains("W03") && damageSources.Contains("W04"),"fourWeaponDamageFamilies");
            Check(sim.Combat.RunEchoHits>0 && sim.Combat.RunPropagationHits>0,"acquiredMechanismsDealDamage");
            Check(Capture(loop.gameplayCamera,Path.Combine(output,"world.png")),"rendered");
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"combat-ui.png"));
            yield return new WaitForSecondsRealtime(.15f);
            if(polished)
            {
                var expected=new[] {"w01_pulse","w02_spread","w03_orbit_hit","w04_zone_drop","echo_hit","propagation","enemy_death","ui_growth_confirm","evolution_flourish"};
                foreach(var group in expected)
                {
                    bool heard=false; foreach(var name in playedAudio) heard |= name.StartsWith("TEST_ONLY_sfx_"+group+"_v");
                    Check(heard,"audioSourcePlayed_"+group);
                }
                observations.Add("audioSourceClips="+string.Join(",",playedAudio));
            }
            observations.Add("damageSources="+string.Join(",",damageSources));
            observations.Add("debugXp=2000 debugTime=241 god=true spawning=false for jump; this is wiring evidence, not natural balance");
            if(!passed) { Finish(); yield break; }
            awaitingRestart=true; loop.Restart();
        }
        void Finish()
        { observations.Add("passed="+passed); File.WriteAllLines(Path.Combine(output,"smoke.txt"),observations); Debug.Log("RC01_STANDALONE_SMOKE passed="+passed); Application.Quit(passed?0:2); }
        static bool Capture(Camera camera,string path)
        {
            var target=RenderTexture.GetTemporary(1280,720,24); var old=RenderTexture.active; Texture2D image=null;
            try
            {
                var request=new RenderPipeline.StandardRequest { destination=target };
                if(!RenderPipeline.SupportsRenderRequest(camera,request)) return false;
                RenderPipeline.SubmitRenderRequest(camera,request); RenderTexture.active=target;
                image=new Texture2D(1280,720,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
                File.WriteAllBytes(path,image.EncodeToPNG()); return true;
            }
            finally { RenderTexture.active=old; RenderTexture.ReleaseTemporary(target); if(image!=null) Destroy(image); }
        }
    }
}
