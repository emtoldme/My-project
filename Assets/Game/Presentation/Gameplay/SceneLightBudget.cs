using System.Collections.Generic;
using UnityEngine;
using Waffle;

namespace Reverb.Drafts.RC01.Presentation
{
    /// <summary>Decorative local lights share a bounded nearby budget. Never changes narrative intensity or real danger cues.</summary>
    [DefaultExecutionOrder(250)]
    public sealed class SceneLightBudget : MonoBehaviour
    {
        public GameLoop loop;public GardenWeather weather;public int maxLocalLights=8;public float radius=32;
        sealed class Entry{public Light light;public bool enabled;public float score;}
        readonly List<Entry> lights=new List<Entry>(64);float discoverAt,nextSelect;
        public int ManagedCount=>lights.Count;
        public int EnabledCount {get;private set;}
        void Start(){if(loop==null)loop=GetComponent<GameLoop>();if(weather==null)weather=FindAnyObjectByType<GardenWeather>();Discover();}
        bool Protected(Light l)
        {
            if(l.type==LightType.Directional||l.name.Contains("impact light")||l.name.Contains("Player soft"))return true;
            if(weather?.searchlights!=null)foreach(var p in weather.searchlights)if(p==l)return true;
            return false;
        }
        void Discover()
        {
            lights.RemoveAll(e=>e.light==null);
            foreach(var light in FindObjectsByType<Light>())
            {
                if(Protected(light)||lights.Exists(e=>e.light==light)||!light.enabled)continue;
                lights.Add(new Entry{light=light,enabled=true});
            }
        }
        void LateUpdate()
        {
            if(loop?.Sim==null)return;float now=loop.Sim.State.Now;
            if(now>=discoverAt){discoverAt=now+1.5f;Discover();}
            if(now<nextSelect)return;nextSelect=now+.16f;
            var player=new Vector3(loop.Sim.PlayerX,0,loop.Sim.PlayerZ);
            foreach(var e in lights)
            {
                if(e.light==null){e.score=float.MaxValue;continue;}
                float d=Vector3.Distance(player,e.light.transform.position);
                e.score=d>radius+e.light.range?float.MaxValue:d/(e.light.range+4)*(e.light.enabled?.85f:1);
            }
            lights.Sort((a,b)=>a.score.CompareTo(b.score));EnabledCount=0;
            foreach(var e in lights)if(e.light!=null){bool on=EnabledCount<maxLocalLights&&e.score<float.MaxValue;e.light.enabled=on;if(on)EnabledCount++;}
        }
        void OnDisable(){foreach(var e in lights)if(e.light!=null)e.light.enabled=e.enabled;}
    }
}
