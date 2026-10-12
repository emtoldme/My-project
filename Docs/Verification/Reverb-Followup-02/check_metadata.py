from pathlib import Path
from collections import defaultdict
import re, json
root = Path(__file__).resolve().parents[3]
guids = defaultdict(list)
for path in (root / 'Assets').rglob('*.meta'):
    match = re.search(r'^guid: ([0-9a-f]{32})$', path.read_text(encoding='utf-8-sig', errors='replace'), re.M)
    if match:
        guids[match[1]].append(str(path.relative_to(root)))
duplicates = {guid: paths for guid, paths in guids.items() if len(paths) > 1}
assert not duplicates, duplicates
missing = []
for path in (root / 'Assets/Experiments').rglob('*'):
    if path.name.endswith('~') or any(p.endswith('~') for p in path.parts):
        continue
    if path.suffix == '.meta' or path.name.startswith('.'):
        continue
    if not Path(str(path) + '.meta').exists():
        missing.append(str(path.relative_to(root)))
assert not missing, missing
result = dict(unique_asset_guids=len(guids), duplicate_guids=duplicates, experiment_missing_meta=missing,
              excluded='Unity ignored DotnetRunner~ and dotfiles')
(root / 'Docs/Verification/Reverb-Followup-02/metadata-check.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8', newline='\n')
print(json.dumps(result))
