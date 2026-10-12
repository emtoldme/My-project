"""Offline comparison: identical event timeline and per-file RMS. No BGM/Unity spatial rolloff/hardware evidence."""
from pathlib import Path
import wave, json, math, argparse
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
parser=argparse.ArgumentParser();parser.add_argument("--output",required=True);args=parser.parse_args()
OUT=Path(args.output);OUT.mkdir(parents=True,exist_ok=True)
SR=48000

def read(path):
    with wave.open(str(ROOT/path),"rb") as f:
        rate,channels,width=f.getframerate(),f.getnchannels(),f.getsampwidth();raw=f.readframes(f.getnframes())
    assert rate==SR,path
    if width==2: data=np.frombuffer(raw,dtype="<i2").astype(np.float64)/32768
    elif width==3:
        b=np.frombuffer(raw,dtype=np.uint8).reshape(-1,3).astype(np.int32)
        v=b[:,0]+(b[:,1]<<8)+(b[:,2]<<16);data=np.where(v>=8388608,v-16777216,v).astype(np.float64)/8388608
    else: raise ValueError(width)
    return data.reshape(-1,channels).mean(axis=1)

old=json.loads((ROOT/"Docs/Design/RC01/Audio/cue-map.json").read_text(encoding="utf-8-sig"))["categories"]
new=json.loads((ROOT/"Docs/Design/RC01/Audio/PolishR1/cue-map.json").read_text(encoding="utf-8-sig"))["categories"]
oldc={c["id"]:c for group in old.values() for c in group};newc={c["id"]:c for group in new.values() for c in group}
groups=["W01","W02","W03","W04","EchoHit","PropagationHit","EnemyDeath","UI_Card_Chosen","UI_Evolution_Chest"]
oldids={"EchoHit":"P12","PropagationHit":"P13"}
oldclips={};newclips={};gains={}
for g in groups:
    c=oldc[oldids.get(g,g)];paths=c.get("variants",[c["assetPath"]])
    if g=="W01":paths=[f"Assets/Audio/SFX/sfx_player_shoot_{i:02d}.wav" for i in range(1,4)]
    oldclips[g]=[read(x) for x in paths]
    c2=newc[g];newclips[g]=[read(x) for x in c2.get("variants",[c2["assetPath"]])]
    # Same group gain in both versions: isolating the delivered timbre instead of matching Unity mix settings.
    gains[g]=10**((-4 if g in ["W01","W02","W03","W04"] else -6)/20)

events=[]
for i,g in enumerate(groups):events.append((.3+.9*i,g,0))
for i in range(24):
    t=10+i*.5
    events.extend([(t,"W01",-.2),(t+.1,"PropagationHit",.35),(t+.5,"EchoHit",-.35)])
    if i%2==0:events.extend([(t+.15,"W02",.15),(t+.25,"EnemyDeath",-.1)])
    if i%3==0:events.append((t+.3,"W03",.3))
    if i%5==0:events.append((t+.6,"W04",-.3))
events.extend([(23,"UI_Card_Chosen",0),(23.7,"UI_Evolution_Chest",0)])
events.sort();report={"sampleRate":SR,"seconds":25,"eventCount":len(events),"note":"Same timeline, group gains, stereo pans; matched RMS -24dBFS. Not LUFS matched, no BGM or Unity rolloff. Old uses Feedback01 W01 variants.","files":{}}
for label,bank in [("Feedback01_same_timeline",oldclips),("SoundR1_same_timeline",newclips)]:
    mix=np.zeros((SR*25,2));counts={}
    for t,g,pan in events:
        idx=counts.get(g,0);counts[g]=idx+1;clip=bank[g][idx%len(bank[g])]*gains[g];start=int(t*SR);length=min(len(clip),len(mix)-start)
        angle=(pan+1)*math.pi/4
        mix[start:start+length,0]+=clip[:length]*math.cos(angle);mix[start:start+length,1]+=clip[:length]*math.sin(angle)
    rms=float(np.sqrt(np.mean(mix*mix)));gain=(10**(-24/20))/rms;mix*=gain;peak=float(np.max(np.abs(mix)))
    assert peak<.99,(label,peak)
    filename=label+".wav"
    with wave.open(str(OUT/filename),"wb") as f:
        f.setnchannels(2);f.setsampwidth(2);f.setframerate(SR);f.writeframes(np.round(mix*32767).astype("<i2").tobytes())
    report["files"][filename]={"rmsDbFs":20*math.log10(float(np.sqrt(np.mean(mix*mix)))),"peakDbFs":20*math.log10(peak),"normalizationGainDb":20*math.log10(gain)}
(OUT/"comparison.json").write_text(json.dumps(report,indent=2),encoding="utf-8")
print(json.dumps(report,indent=2))
