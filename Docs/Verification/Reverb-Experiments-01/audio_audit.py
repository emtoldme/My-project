from pathlib import Path
import wave, json, hashlib
import numpy as np
root = Path(__file__).resolve().parents[3]
data = []
rows = []
for p in sorted((root / 'Assets/Experiments/AudioSync').glob('*.wav')):
    if p.stem.endswith('_v2'):
        continue  # Historical V1 audit must not pair A V1 with A V2 after new assets arrive.
    with wave.open(str(p), 'rb') as reader:
        frames = reader.getnframes()
        width = reader.getsampwidth()
        channels = reader.getnchannels()
        frequency = reader.getframerate()
        raw = reader.readframes(frames)
    assert width == 3
    b = np.frombuffer(raw, dtype=np.uint8).reshape(-1, 3).astype(np.int32)
    pcm = b[:, 0] | (b[:, 1] << 8) | (b[:, 2] << 16)
    pcm = ((pcm ^ 0x800000) - 0x800000) / 8388608.0
    pcm = pcm.reshape(frames, channels)
    data.append(pcm)
    rows.append(dict(file=p.name, frames=frames, channels=channels, sample_rate=frequency, bit_depth=24,
                     seconds=frames/frequency, sample_peak_dbfs=float(20*np.log10(np.abs(pcm).max())),
                     end_jump=float(np.abs(pcm[0]-pcm[-1]).max()), sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
assert data[0].shape == data[1].shape
mixes = []
for a, b in [(1, 0.7), (.8, 1), (.7, .6)]:
    mixed = data[0] * a + data[1] * b
    mixes.append(dict(gains=[a,b], sample_peak_dbfs=float(20*np.log10(np.abs(mixed).max())),
                      samples_above_full_scale=int((np.abs(mixed) > 1).sum()),
                      output_trim=0.5,
                      trimmed_sample_peak_dbfs=float(20*np.log10(np.abs(mixed * .5).max())),
                      trimmed_samples_above_full_scale=int((np.abs(mixed * .5) > 1).sum())))
result = dict(label='TEST_ONLY; offline PCM analysis, no listening or DSP-output certification', files=rows, mixes=mixes)
path = root / 'Docs/Verification/Reverb-Experiments-01/audio-audit.json'
path.parent.mkdir(parents=True, exist_ok=True)
path.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8', newline='\n')
print(json.dumps(result, indent=2))
