"""Sweep deterministic native hint-agent runs, selecting on calibration seeds only."""
import json
import subprocess
import tempfile
from pathlib import Path

SOURCE = ["tools/balance.cpp", "core/pier.cpp", "core/pier.h", "core/campaign.inc"]
COLUMNS = "scenario,budget,seed,won"
OPTIONS = ["keep_candidate", "revisit_budget"]
QUESTION = ("Keep the calibrated move budget only if it exists and at least 75% of the held-out "
            "boards were won. Otherwise revisit the budget. A missing candidate is recorded as zero.")
CONTROL = "one_move_control"


def samples(root):
    with tempfile.TemporaryDirectory() as folder:
        binary = Path(folder) / "probe"
        subprocess.run(["c++", "-O2", "-std=c++17", root/SOURCE[0], root/SOURCE[1], "-o", binary], check=True)
        for budget in (1, 8, 12, 16, 20, 24, 28):
            scenario = CONTROL if budget == 1 else "thirteen_panes"
            for seed in range(401, 465):
                result = subprocess.check_output([binary, str(seed), str(budget), "13"], text=True)
                yield scenario, budget, seed, int(json.loads(result)["won"])


def decision(metrics):
    return OPTIONS[0] if metrics["budget"] and 4*metrics["holdout_wins"] >= 3*metrics["holdout_n"] else OPTIONS[1]


def calibration_table(rows):
    counts = {}
    for _, budget, seed, won in rows:
        split = "calibration" if seed % 2 == 0 else "holdout"
        bucket = counts.setdefault(budget, {}).setdefault(split, {"boards": 0, "wins": 0})
        bucket["boards"] += 1
        bucket["wins"] += won
    return counts
