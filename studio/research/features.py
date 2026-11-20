def facts(m):
    return {"calibrated_budget": m["budget"] or None,
            "validation_win_percent": 100*m["holdout_wins"]/m["holdout_n"] if m["holdout_n"] else None,
            "validation_threshold_percent": 75}
