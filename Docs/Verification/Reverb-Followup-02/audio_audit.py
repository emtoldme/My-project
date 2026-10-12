"""Offline PCM audit. No True Peak, listening, or hardware output certification."""
from pathlib import Path
import hashlib, json, subprocess, sys, tempfile, wave
import numpy as np

root = Path(__file__).resolve().parents[3]
assets = root / 'Assets/Experiments/AudioSync'
rows, pairs = [], {}
def load(path):
    with wave.open(str(path), 'rb') as f:
        assert f.getsampwidth() == 3
        count, channels, rate = f.getnframes(), f.getnchannels(), f.getframerate()
        raw = np.frombuffer(f.readframes(count), dtype=np.uint8).reshape(-1, 3).astype(np.int32)
    pcm = raw[:, 0] | raw[:, 1] << 8 | raw[:, 2] << 16
    pcm = (((pcm ^ 0x800000) - 0x800000) / 8388608).reshape(count, channels)
    return pcm, count, channels, rate

for version, suffix in [('V1', ''), ('V2', '_v2')]:
    pair = []
    for stem in ['a_base', 'b_shimmer']:
        path = assets / f'TEST_ONLY_track_{stem}_16s{suffix}.wav'
        pcm, count, channels, rate = load(path)
        assert (count, channels, rate) == (768000, 2, 48000)
        pair.append(pcm)
        rows.append(dict(version=version, file=path.name, frames=count, channels=channels, sample_rate=rate,
            first=pcm[0].tolist(), last=pcm[-1].tolist(), first_above_probe_threshold_frame=int(np.flatnonzero(np.max(np.abs(pcm), axis=1) >= 1e-6)[0]),
            end_jump=float(np.abs(pcm[0] - pcm[-1]).max()),
            start_slope=(pcm[1] - pcm[0]).tolist(), end_slope=(pcm[-1] - pcm[-2]).tolist(),
            slope_difference=float(np.abs((pcm[1] - pcm[0]) - (pcm[-1] - pcm[-2])).max()),
            sample_peak_dbfs=float(20*np.log10(np.abs(pcm).max())), sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    mixes = []
    for a, b in [(1, .7), (.8, 1), (.7, .6), (1, 1)]:
        mixed = pair[0] * a + pair[1] * b
        mixes.append(dict(gains=[a,b], peak_dbfs=float(20*np.log10(np.abs(mixed).max())),
            samples_above_full_scale=int((np.abs(mixed) > 1).sum()),
            trimmed_peak_dbfs=float(20*np.log10(np.abs(mixed * .5).max()))))
    pairs[version] = mixes

with tempfile.TemporaryDirectory(prefix='reverb-e03-v2-') as temporary:
    subprocess.run([sys.executable, str(assets / 'generate_e03_v2.py'), '--output-dir', temporary], check=True)
    reproduced = []
    for path in sorted(Path(temporary).glob('*.wav')):
        original, *_ = load(assets / path.name)
        regenerated, *_ = load(path)
        difference = float(np.abs(original - regenerated).max())
        assert difference <= 1.0 / 8388608, (path.name, difference)
        reproduced.append(dict(file=path.name, pcm_identical=difference == 0, max_difference_lsb=difference * 8388608,
            bytes_identical=path.read_bytes() == (assets / path.name).read_bytes()))

result = dict(scope=__doc__, files=rows, mixes=pairs, reproduction=reproduced,
    limits='V2 endpoint values are zero, but nonzero start slopes preclude a C1/no-click claim. Gains above 1, processing, resampling and hardware output are outside this audit.')
output = Path(__file__).with_name('audio-audit.json')
output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8', newline='\n')
print(json.dumps(result, indent=2))
