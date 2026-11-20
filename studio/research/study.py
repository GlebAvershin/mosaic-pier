"""Re-evaluate the locked release budget and the old candidate on the registered holdout."""
import json

SOURCE = ["tools/balance.cpp", "core/pier.cpp", "core/pier.h", "core/campaign.inc",
          "studio/research/confirm_budget.py", "docs/evidence/budget-confirmation.json"]
COLUMNS = "scenario,budget,seed,won"
OPTIONS = ["keep_candidate", "revisit_budget"]
QUESTION = ("Accept a move budget if its holdout wins reach required_wins. "
            "Return keep_candidate on passing and revisit_budget otherwise.")
CONTROL = "old_candidate"


def samples(root):
    release = json.loads((root / SOURCE[-1]).read_text())
    lo, hi = release["holdout_seeds"]
    for scenario, budget, field in [("release_budget", release["budget"], "winning_seeds"),
                                    (CONTROL, release["old_budget"], "old_winning_seeds")]:
        wins = set(release[field])
        for seed in range(lo, hi+1):
            yield scenario, budget, seed, int(seed in wins)


def decision(metrics):
    return OPTIONS[0] if metrics["wins"] >= metrics["required_wins"] else OPTIONS[1]
