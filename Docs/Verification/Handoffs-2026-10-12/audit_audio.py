from pathlib import Path
import wave, hashlib, json, math
import numpy as np
root=Path(__file__).resolve().parents[3]
rows=[]
for p in sorted((root/'Assets/Audio').rglob('*.wav')):
    with wave.open(str(p),'rb') as w:
        raw=w.readframes(w.getnframes()); sw=w.getsampwidth()
        if sw==3:
            b=np.frombuffer(raw,dtype=np.uint8).reshape(-1,3).astype(np.int32)
            s=b[:,0]|(b[:,1]<<8)|(b[:,2]<<16); s=np.where(s>=8388608,s-16777216,s)/8388608
        elif sw==2: s=np.frombuffer(raw,dtype='<i2')/32768
        else: s=np.array([0.0])
        peak=float(np.max(np.abs(s)))
        rows.append(dict(file=str(p.relative_to(root)).replace('\\','/'),seconds=round(w.getnframes()/w.getframerate(),4),hz=w.getframerate(),bits=sw*8,channels=w.getnchannels(),sample_peak_dbfs=round(20*math.log10(peak),3) if peak else None,sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
print(json.dumps(rows,ensure_ascii=False,indent=2))
Path(__file__).with_name('audio-file-audit.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
