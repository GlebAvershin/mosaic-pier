"""Check which held-out boards change outcome when the finale budget changes."""
import json
import runpy
import subprocess
import tempfile
from pathlib import Path

SOURCE = "docs/evidence/budget-confirmation.json"


def load_input(root):
    release = json.loads((root / SOURCE).read_text())
    return {key: release[key] for key in ("holdout_seeds", "winning_seeds", "old_winning_seeds", "budget", "old_budget", "panes")}


def analyze(data):
    first, last = data["holdout_seeds"]
    boards = set(range(first, last + 1))
    old, new = set(data["old_winning_seeds"]), set(data["winning_seeds"])
    if not (old | new) <= boards:
        raise ValueError("Unknown board in win list")
    return {"boards": len(boards), "old_budget": data["old_budget"], "new_budget": data["budget"],
            "won_both": len(old & new), "only_new": sorted(new - old), "only_old": sorted(old - new),
            "lost_both": len(boards - (old | new)), "win_delta_pp": round(100 * (len(new) - len(old)) / len(boards), 5)}


def verify(root, result):
    study = runpy.run_path(str(root / "studio/research/confirm_budget.py"))
    data = load_input(root)
    start, end = data["holdout_seeds"]
    seeds = range(start, end + 1)
    with tempfile.TemporaryDirectory() as folder:
        exe = Path(folder) / "balance"
        subprocess.run(["c++", "-std=c++17", "-O2", root / "core/pier.cpp", root / "tools/balance.cpp", "-o", exe], check=True)
        old, new = [set(study["sweep"](exe, seeds, data[key])) for key in ("old_budget", "budget")]
    assert old == set(data["old_winning_seeds"]) and new == set(data["winning_seeds"])
    assert result["only_new"] == sorted(new - old) and result["only_old"] == sorted(old - new)
    assert result["won_both"] == len(new & old) and result["lost_both"] == len(set(seeds) - (new | old))
    assert result["win_delta_pp"] == round(100 * (len(new) - len(old)) / len(seeds), 5)
