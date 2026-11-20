def facts(metrics):
    wins = metrics["wins"]
    minimum = metrics["required_wins"]
    return {"observed_wins": wins, "minimum_wins_inclusive": minimum,
            "additional_wins_needed_to_reach_minimum": max(0, minimum - wins),
            "boards": metrics["boards"]}
