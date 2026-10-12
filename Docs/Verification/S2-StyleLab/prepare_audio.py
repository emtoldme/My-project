"""Rebuild derived S2 audio from preserved S03 masters; requires numpy."""
from pathlib import Path
import wave, hashlib, json
import numpy as np
root = Path(__file__).resolve().parents[3]
output = root / "Assets/Waffle/S2/Audio"
output.mkdir(parents=True, exist_ok=True)
rows = []
for source in sorted((root / "Assets/Audio/S03").glob("*.wav")):
    with wave.open(str(source), "rb") as reader:
        assert reader.getsampwidth() == 3
        channels, hz = reader.getnchannels(), reader.getframerate()
        data = np.frombuffer(reader.readframes(reader.getnframes()), dtype=np.uint8).reshape(-1, 3).astype(np.int32)
        samples = data[:, 0] | (data[:, 1] << 8) | (data[:, 2] << 16)
        audio = (np.where(samples >= 8388608, samples - 16777216, samples) / 8388608).reshape(-1, channels)
    gain_db = -4.13 if "postrock" in source.name else 0.0
    audio *= 10 ** (gain_db / 20)
    attack = 0.012 if source.name.startswith("bgm") else 0.001
    release = 0.012 if source.name.startswith("bgm") else 0.008
    na, nr = int(hz * attack), int(hz * release)
    audio[:na] *= (0.5 - 0.5 * np.cos(np.linspace(0, np.pi, na)))[:, None]
    audio[-nr:] *= (0.5 + 0.5 * np.cos(np.linspace(0, np.pi, nr)))[:, None]
    quant = np.rint(np.clip(audio, -1, 1 - 1 / 8388608) * 8388608).astype(np.int32).ravel()
    raw = np.stack([quant & 255, (quant >> 8) & 255, (quant >> 16) & 255], axis=1).astype(np.uint8).tobytes()
    target = output / source.name
    with wave.open(str(target), "wb") as writer:
        writer.setnchannels(channels); writer.setsampwidth(3); writer.setframerate(hz); writer.writeframes(raw)
    rows.append(dict(source=source.relative_to(root).as_posix(), output=target.relative_to(root).as_posix(),
        gain_db=gain_db, attack_fade_seconds=attack, release_fade_seconds=release, seconds=len(audio)/hz,
        first_sample=audio[0].tolist(), last_sample=audio[-1].tolist(),
        source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(), output_sha256=hashlib.sha256(target.read_bytes()).hexdigest()))
Path(__file__).with_name("audio-processing.json").write_text(json.dumps(rows, ensure_ascii=False, indent=2), encoding="utf-8", newline="\n")
print("Rebuilt 8 derived files; originals preserved. Listening remains required.")
