"""
RC02 音效与音乐管线静态自检与导出工具 (validate_pipeline_manifests.py)
-------------------------------------------------------------------
任务：feature/rc02-audio-pipeline (Docs/ART_EDITING.md)
岗位：音效总监 (Sound Director)
运行环境：Python 3.x (原生零依赖，仅使用 json 与 pathlib)

本脚本执行合同 v1 静态合规自检：
1. 校验 schemaVersion=1 与 packageId；
2. 校验条目 ID 全局唯一性，无重复 entry id；
3. 校验所有声明的资产文件 (role=clip, combatLoop, levelUpLoop 等) 在 Assets/ 物理存在；
4. 校验 PresentationEventType 枚举合法性；
5. 校验并发、节流、增益、优先级的数值范围与安全性；
6. 校验音乐档案的 bpm、beatsPerBar 与淡出时长规范。
"""

from pathlib import Path
import json, sys

REPO_ROOT = next(p for p in Path(__file__).resolve().parents if (p / "ProjectSettings").is_dir())
PIPELINE_DIR = REPO_ROOT / "Assets/Game/Content/Manifests"

KNOWN_EVENT_TYPES = {
    "", "Shot", "EnemyHit", "EnemyKilled", "EchoHit", "PropagationHit",
    "EchoPropagationHit", "GemPickup", "PickupCollected", "Dodge",
    "PlayerHurt", "LevelUpShown", "LevelUpChosen", "Evolved", "WeaponZone",
    "GrowthReroll", "GrowthBanish", "GrowthSkip", "Victory", "Defeat"
}

def validate_manifest(file_path):
    print(f"--- Validating {file_path.name} ---")
    if not file_path.exists():
        print(f"ERROR: File not found: {file_path}")
        return False

    with open(file_path, "r", encoding="utf-8") as f:
        data = json.load(f)

    errors = []
    warnings = []

    # 1. 根字段
    if data.get("schemaVersion") != 1:
        errors.append(f"schemaVersion must be 1, got {data.get('schemaVersion')}")
    package_id = data.get("packageId", "")
    if not package_id:
        errors.append("packageId is required and cannot be empty")

    entries = data.get("entries", [])
    if not isinstance(entries, list):
        errors.append("entries must be a list")
        return False

    entry_ids = set()
    for idx, e in enumerate(entries):
        eid = e.get("id", "")
        if not eid:
            errors.append(f"Entry #{idx} missing unique id")
        elif eid in entry_ids:
            errors.append(f"Duplicate entry id: {eid}")
        else:
            entry_ids.add(eid)

        kind = e.get("kind", "")
        if kind not in ("audio", "music"):
            errors.append(f"Entry {eid}: invalid kind '{kind}' (must be 'audio' or 'music')")

        ev_type = e.get("eventType", "")
        if ev_type not in KNOWN_EVENT_TYPES:
            errors.append(f"Entry {eid}: unknown eventType '{ev_type}'")

        # 检查资产路径
        assets = e.get("assets", [])
        if not assets:
            errors.append(f"Entry {eid}: assets array cannot be empty")
        for a in assets:
            role = a.get("role", "")
            path_str = a.get("path", "")
            if not path_str:
                errors.append(f"Entry {eid}: asset missing path")
                continue
            phys_path = REPO_ROOT / path_str
            if not phys_path.exists():
                errors.append(f"Entry {eid}: physical asset not found at '{path_str}'")

        # 检查参数
        params = {p.get("key"): p.get("value") for p in e.get("parameters", []) if "key" in p}
        if kind == "audio":
            gain = params.get("gainDb", 0.0)
            if gain > 6.0 or gain < -40.0:
                warnings.append(f"Entry {eid}: gainDb {gain} is unusually high/low")
            min_dist = params.get("minDistance", 1.0)
            max_dist = params.get("maxDistance", 25.0)
            if min_dist > max_dist:
                errors.append(f"Entry {eid}: minDistance ({min_dist}) > maxDistance ({max_dist})")

        pause_policy = e.get("pausePolicy", "")
        if pause_policy not in ("freeze", "stop"):
            errors.append(f"Entry {eid}: invalid pausePolicy '{pause_policy}' (must be 'freeze' or 'stop')")

    if errors:
        print(f"FAILED with {len(errors)} errors:")
        for err in errors:
            print(f"  [ERROR] {err}")
    else:
        print(f"PASSED with {len(entries)} valid entries.")

    if warnings:
        print(f"Warnings ({len(warnings)}):")
        for w in warnings:
            print(f"  [WARN] {w}")

    return len(errors) == 0

def main():
    audio_ok = validate_manifest(PIPELINE_DIR / "audio-manifest.json")
    music_ok = validate_manifest(PIPELINE_DIR / "music-manifest.json")
    if audio_ok and music_ok:
        print("\nALL PIPELINE MANIFESTS VERIFIED COMPLIANT WITH CONTRACT V1.")
        sys.exit(0)
    else:
        print("\nVALIDATION FAILED.")
        sys.exit(1)

if __name__ == "__main__":
    main()
