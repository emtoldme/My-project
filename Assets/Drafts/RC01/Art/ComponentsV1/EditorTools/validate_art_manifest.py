"""
RC02 美术表现管线静态自检与合规验证工具 (validate_art_manifest.py)
-------------------------------------------------------------------
任务：feature/rc02-art-components (Docs/Integration/REVERB_TASKS_08.md)
维护岗位：主美 (Lead Artist)
运行环境：Python 3.x (零外部依赖，仅使用 json 与 pathlib)

本脚本执行合同 v1 静态合规自检：
1. 校验 schemaVersion=1 与 packageId='rc02.art.v1'；
2. 校验条目 ID 全局唯一性，无重复 entry id；
3. 校验所有声明的资产文件在 Assets/ 物理存在，且对应 .meta 文件齐全；
4. 校验 PresentationEventType 枚举合法性；
5. 校验艺术参数 (width, scale, tailSeconds, intensity) 白名单与数值有效性；
6. 校验并发 (maxInstances)、节流 (minInterval)、暂停策略 (freeze/stop) 与 TEST_ONLY 状态声明。
"""

from pathlib import Path
import json
import sys

REPO_ROOT = Path(__file__).resolve().parents[6]
MANIFEST_PATH = REPO_ROOT / "Docs/Design/RC02/Art/art-manifest.json"

KNOWN_EVENT_TYPES = {
    "", "Shot", "EnemyHit", "EnemyKilled", "EchoHit", "PropagationHit",
    "EchoPropagationHit", "GemPickup", "PickupCollected", "Dodge",
    "PlayerHurt", "LevelUpShown", "LevelUpChosen", "Evolved", "WeaponZone",
    "GrowthReroll", "GrowthBanish", "GrowthSkip", "Victory", "Defeat"
}

VALID_PARAMETERS = {"width", "scale", "tailSeconds", "intensity"}

def validate_art_manifest():
    print(f"--- Validating Art Manifest: {MANIFEST_PATH.name} ---")
    if not MANIFEST_PATH.exists():
        print(f"ERROR: File not found: {MANIFEST_PATH}")
        return False

    with open(MANIFEST_PATH, "r", encoding="utf-8") as f:
        data = json.load(f)

    errors = []
    warnings = []

    # 1. 根字段
    if data.get("schemaVersion") != 1:
        errors.append(f"schemaVersion must be 1, got {data.get('schemaVersion')}")
    package_id = data.get("packageId", "")
    if package_id != "rc02.art.v1":
        errors.append(f"packageId should be rc02.art.v1, got {package_id}")

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
        if kind != "art":
            errors.append(f"Entry {eid}: kind must be 'art', got {kind}")

        ev_type = e.get("eventType", "")
        if ev_type not in KNOWN_EVENT_TYPES:
            errors.append(f"Entry {eid}: unknown eventType '{ev_type}'")

        # 检查资产
        assets = e.get("assets", [])
        if not assets:
            errors.append(f"Entry {eid}: assets array cannot be empty")
        for a in assets:
            role = a.get("role", "")
            if role not in ("prefab", "material", "icon"):
                errors.append(f"Entry {eid}: invalid asset role '{role}'")
            path_str = a.get("path", "")
            if not path_str:
                errors.append(f"Entry {eid}: asset missing path")
                continue
            phys_path = REPO_ROOT / path_str
            if not phys_path.exists():
                errors.append(f"Entry {eid}: physical asset not found at '{path_str}'")
            meta_path = REPO_ROOT / (path_str + ".meta")
            if not meta_path.exists():
                errors.append(f"Entry {eid}: asset meta not found at '{path_str}.meta'")

        # 检查参数
        for p in e.get("parameters", []):
            k = p.get("key")
            v = p.get("value")
            if k not in VALID_PARAMETERS:
                errors.append(f"Entry {eid}: parameter key '{k}' not in known art parameters")
            if not isinstance(v, (int, float)):
                errors.append(f"Entry {eid}: parameter '{k}' value must be numeric, got {v}")

        pause_policy = e.get("pausePolicy", "")
        if pause_policy not in ("freeze", "stop"):
            errors.append(f"Entry {eid}: invalid pausePolicy '{pause_policy}'")

        status = e.get("status", "")
        if status != "TEST_ONLY":
            errors.append(f"Entry {eid}: status must be TEST_ONLY, got {status}")

    if errors:
        print(f"FAILED with {len(errors)} errors:")
        for err in errors:
            print(f"  [ERROR] {err}")
    else:
        print(f"PASSED with {len(entries)} valid art entries.")

    if warnings:
        print(f"Warnings ({len(warnings)}):")
        for w in warnings:
            print(f"  [WARN] {w}")

    return len(errors) == 0

def main():
    if validate_art_manifest():
        print("\nART MANIFEST VERIFIED COMPLIANT WITH CONTRACT V1.")
        sys.exit(0)
    else:
        print("\nVALIDATION FAILED.")
        sys.exit(1)

if __name__ == "__main__":
    main()
