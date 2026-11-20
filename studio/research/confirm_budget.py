"""Pick a move budget on calibration seeds and test the locked choice on fresh boards."""
import json
import math
import subprocess
import tempfile
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CALIBRATION = range(10001, 10129)
HOLDOUT = range(20001, 20257)
BUDGETS = range(14, 41, 2)


def wilson_lower(wins, n):
    z = 1.95996398454
    p = wins / n
    return (p + z*z/(2*n) - z*math.sqrt(p*(1-p)/n + z*z/(4*n*n))) / (1+z*z/n)


def sweep(binary, seeds, budget):
    def play(seed):
        data = subprocess.check_output([binary, str(seed), str(budget), "13"], text=True)
        return json.loads(data)["won"]
    with ThreadPoolExecutor(max_workers=4) as workers:
        return [seed for seed, won in zip(seeds, workers.map(play, seeds)) if won]


def main():
    with tempfile.TemporaryDirectory() as temp:
        binary = Path(temp)/"probe"
        subprocess.run(["c++", "-std=c++17", "-O2", ROOT/"core/pier.cpp", ROOT/"tools/balance.cpp", "-o", binary], check=True)
        calibration = {}
        for budget in BUDGETS:
            wins = len(sweep(binary, CALIBRATION, budget))
            calibration[budget] = wins
            if wins * 10 >= 9 * len(CALIBRATION):
                break
        else:
            raise RuntimeError("No qualifying budget in the registered grid")
        won = sweep(binary, HOLDOUT, budget)
        old = sweep(binary, HOLDOUT, 12)
        lower = wilson_lower(len(won), len(HOLDOUT))
        result = dict(panes=13, calibration_seeds=[CALIBRATION.start, CALIBRATION.stop-1],
                      calibration_wins=calibration, budget=budget, holdout_seeds=[HOLDOUT.start, HOLDOUT.stop-1],
                      wins=len(won), boards=len(HOLDOUT), wilson_lower_95=lower,
                      winning_seeds=won, old_budget=12, old_winning_seeds=old, passed=lower >= 0.75)
        print(json.dumps(result, indent=2))
        if not result["passed"]:
            raise SystemExit(1)


if __name__ == "__main__":
    main()
