using System;
using System.Collections.Generic;
using UnityEngine;
using Waffle;

namespace Reverb.Drafts.RC01.Pipeline
{
    // Serialized catalog provider; AudioDirector consumes it before constructing its existing channels.
    public sealed class RC02AudioConfiguration : MonoBehaviour, IAudioDirectorConfiguration
    {
        public RC02ContentCatalog catalog;
        public string musicProfileId = "harvest_road";
        public void Configure(AudioDirector audio)
        {
            if (catalog == null) return;
            var bindings = new Dictionary<(PresentationEventType,string), AudioContentBinding>();
            foreach (var b in audio.contentBindings) if (b != null) bindings[(b.eventType,b.contentId)] = b;
            foreach (var entry in catalog.entries)
            {
                if (entry.kind == "music" && entry.profileId == musicProfileId)
                {
                    audio.combatLoop = entry.Asset<AudioClip>("combatLoop") ?? audio.combatLoop;
                    audio.levelUpLoop = entry.Asset<AudioClip>("levelUpLoop") ?? audio.levelUpLoop;
                    audio.musicDb = entry.Parameter("musicGainDb", audio.musicDb);
                    audio.levelUpCrossfade = audio.resumeFadeIn = Mathf.Max(.05f, entry.Parameter("fadeSeconds",1));
                    continue;
                }
                if (entry.kind != "audio" || !Enum.TryParse(entry.eventType, out PresentationEventType type)) continue;
                var clips = new List<AudioClip>();
                foreach (var a in entry.assets) if (a.role == "clip" && a.asset is AudioClip clip) clips.Add(clip);
                if (clips.Count == 0) continue;
                float low = entry.Parameter("pitchMin",1), high = entry.Parameter("pitchMax",1);
                var rule = new AudioEventRule { name=entry.id, clips=clips.ToArray(), sequential=true,
                    volumeDb=entry.Parameter("gainDb",-5), pitch=(low+high)/2, pitchRandom=Mathf.Abs(high-low)/2,
                    maxVoices=Mathf.Clamp(entry.maxInstances,1,16), minInterval=entry.minInterval,
                    spatialBlend=entry.Parameter("spatialBlend",0), minDistance=entry.Parameter("minDistance",30),
                    maxDistance=entry.Parameter("maxDistance",65), priority=(int)entry.Parameter("priority",128),
                    clearOnPause=entry.pausePolicy==RC02PausePolicy.Stop, dropWhenFull=type!=PresentationEventType.LevelUpChosen };
                bool core = false;
                if (entry.contentId == "*")
                {
                    switch(type)
                    {
                        case PresentationEventType.EnemyHit: audio.enemyHit=rule; core=true; break;
                        case PresentationEventType.EnemyKilled: audio.enemyDeath=rule; core=true; break;
                        case PresentationEventType.LevelUpShown: audio.levelUpAppear=rule; core=true; break;
                        case PresentationEventType.LevelUpChosen: audio.levelUpConfirm=rule; core=true; break;
                    }
                }
                if (entry.contentId=="D01" && type==PresentationEventType.GemPickup) { audio.gemPickup=rule; core=true; }
                if (entry.contentId=="P12" && type==PresentationEventType.EchoHit) { audio.echo=rule; core=true; rule.minInterval=Mathf.Max(.1f,rule.minInterval); }
                if (entry.contentId=="P13" && type==PresentationEventType.PropagationHit) { audio.propagation=rule; core=true; rule.minInterval=Mathf.Max(.09f,rule.minInterval); }
                if (entry.contentId=="P12_P13" && type==PresentationEventType.EchoPropagationHit) { audio.echoPropagation=rule; core=true; }
                if (type==PresentationEventType.EnemyKilled) rule.minInterval=Mathf.Max(.1f,rule.minInterval);
                if (core) bindings.Remove((type,entry.contentId));
                else bindings[(type,entry.contentId)] = new AudioContentBinding { contentId=entry.contentId,eventType=type,rule=rule };
                if (entry.contentId=="W01" && type==PresentationEventType.Shot && bindings.TryGetValue((type,"W01"),out var w01))
                    bindings[(type,"E01")] = new AudioContentBinding { contentId="E01",eventType=type,rule=w01.rule };
            }
            audio.contentBindings = new List<AudioContentBinding>(bindings.Values).ToArray();
            audio.gemPickup.volumeDb=-5.5f;
        }
    }
}
