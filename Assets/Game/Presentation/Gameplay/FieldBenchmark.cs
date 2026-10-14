using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Unity.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Waffle;

namespace Reverb.Drafts.RC01.Presentation
{
    /// <summary>Explicit command-line diagnostic, absent from normal play. Fixed seed, rule ticks, camera and resolution.</summary>
    [DefaultExecutionOrder(300)]
    public sealed class FieldBenchmark : MonoBehaviour
    {
        static readonly CultureInfo CI=CultureInfo.InvariantCulture;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args=Environment.GetCommandLineArgs();if(!args.Contains("-reverb-benchmark"))return;
            var field=FindAnyObjectByType<ProceduralFieldStreamer>();if(field!=null){field.randomizeSeed=false;field.worldSeed=14104;}
            new GameObject("Explicit field benchmark").AddComponent<FieldBenchmark>();
        }
        GameLoop loop;CombatViews views;ProceduralFieldStreamer field;GardenWeather weather;Camera camera;
        ProfilerRecorder batches,triangles,gc,mainThread;
        readonly FrameTiming[] timings=new FrameTiming[1];
        readonly List<string> rows=new List<string>(1600);
        string output;int phase=-1,frame;float before;Vector2 origin;
        RenderTexture renderTarget;Texture2D syncPixel;
        void Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-benchmark-output");output=at>=0?args[at+1]:Path.Combine(Application.persistentDataPath,"FieldBenchmark");Directory.CreateDirectory(output);
            Application.runInBackground=true;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;QualitySettings.maxQueuedFrames=1;
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
            renderTarget=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);renderTarget.Create();syncPixel=new Texture2D(1,1,TextureFormat.RGB24,false);
            loop=FindAnyObjectByType<GameLoop>();views=loop.GetComponent<CombatViews>();field=FindAnyObjectByType<ProceduralFieldStreamer>();weather=FindAnyObjectByType<GardenWeather>();camera=Camera.main;
            loop.combatConfig.spawningEnabled=false;
            batches=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count",1);
            triangles=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count",1);
            gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
            mainThread=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);
            File.WriteAllText(Path.Combine(output,"device.txt"),SystemInfo.graphicsDeviceName+"\n"+SystemInfo.graphicsDeviceType+"\n"+SystemInfo.processorType+"\n"+Screen.width+"x"+Screen.height+"\nframeTiming="+FrameTimingManager.IsFeatureEnabled());
            rows.Add("phase,frame,wall_ms,cpu_ms,gpu_ms,main_ms,batches,triangles,gc_bytes,resident_vertices,enemies,x,z,memory_bytes");before=Time.realtimeSinceStartup;
        }
        void Update()
        {
            if(loop?.Sim?.Places==null)return;
            if(phase<0)
            {
                if(loop.BeforeDeparture)loop.StartHomecomingJourney();loop.Sim.God=true;
                // Benchmark owns fixed ticks only for this diagnostic process.
                loop.enabled=false;loop.Sim.SetMoveBasis(1,0,0,1);
                loop.Sim.PlayerX=61;loop.Sim.PlayerZ=61;origin=new Vector2(61,61);
                if(field.LoadedChunkCount<49){Tick(0);return;}
                phase=0;frame=0;
            }
            float wall=(Time.realtimeSinceStartup-before)*1000;before=Time.realtimeSinceStartup;
            Tick(phase==2?1:0);
            weather.weather=phase==0?GardenWeatherKind.Clear:GardenWeatherKind.Rain;weather.lightning=false;
            FrameTimingManager.CaptureFrameTimings();uint count=FrameTimingManager.GetLatestTimings(1,timings);
            if(frame>=120)
            {
                var t=timings[0];rows.Add(string.Join(",",new[]{phase.ToString(),frame.ToString(),wall.ToString("F4",CI),count>0?t.cpuFrameTime.ToString("F4",CI):"0",count>0?t.gpuFrameTime.ToString("F4",CI):"0",(mainThread.LastValue/1000000.0).ToString("F4",CI),batches.LastValue.ToString(),triangles.LastValue.ToString(),gc.LastValue.ToString(),field.ResidentWheatVertices.ToString(),views.VisibleEnemies.ToString(),loop.Sim.PlayerX.ToString("F2",CI),loop.Sim.PlayerZ.ToString("F2",CI),UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong().ToString()}));
            }
            frame++;
            if(frame==300)Capture("phase-"+phase+".png");
            if(frame<480)return;
            File.WriteAllLines(Path.Combine(output,"frames.csv"),rows);
            if(++phase<3){frame=0;return;}
            Debug.Log("FIELD_BENCHMARK_DONE "+output);Application.Quit(0);
        }
        void LateUpdate()
        {
            if(camera==null||renderTarget==null)return;
            // Batch players do not present a normal scene every frame. Explicit SRP
            // rendering + one-pixel readback measures real work, with the SAME sync
            // overhead in both versions. This is synchronous throughput, not game FPS.
            var previous=RenderTexture.active;
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=renderTarget});
            RenderTexture.active=renderTarget;syncPixel.ReadPixels(new Rect(0,0,1,1),0,0,false);RenderTexture.active=previous;
        }
        void Tick(float move)
        {
            if(loop.Sim.State.Phase==RunPhase.Paused)loop.TogglePause();
            if(loop.Sim.State.Phase==RunPhase.LevelUp){var v=loop.ReadView();if(v?.Choices.Count>0)loop.TryChoose(v.OfferToken,v.Choices[0].Id);else loop.Choose(0);}
            var input=InputSnapshot.None;input.MoveX=move;
            loop.Sim.Tick(1f/60,input,Time.frameCount,Time.unscaledTimeAsDouble);loop.Sim.StepHomecoming();loop.SyncAfterStateChange();loop.playerView.position=new Vector3(loop.Sim.PlayerX,0,loop.Sim.PlayerZ);views.Sync();loop.DispatchPresentation();
        }
        void Capture(string file)
        {
            field.PrepareCapture();
            var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);rt.Create();var previous=RenderTexture.active;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try{RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,file),image.EncodeToPNG());}
            finally{RenderTexture.active=previous;Destroy(image);rt.Release();Destroy(rt);}
        }
        void OnDestroy(){batches.Dispose();triangles.Dispose();gc.Dispose();mainThread.Dispose();if(renderTarget!=null){renderTarget.Release();Destroy(renderTarget);}if(syncPixel!=null)Destroy(syncPixel);}
    }
}
