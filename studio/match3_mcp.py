#!/usr/bin/env python3
"""Mosaic Pier Match-3 Game AI & Level Design MCP Server.

Exposes MCP tools for:
- Automated campaign progression curve evaluation (via C++17 Monte Carlo simulation)
- Live AI Game Design consultation via Databricks Model Serving
- Telemetry emission for Amplitude and Sentry
"""
from __future__ import annotations

import asyncio
import json
import os
from pathlib import Path
import subprocess
import sys
from typing import Any, Dict

from mcp.server.fastmcp import FastMCP

mcp = FastMCP("MosaicPier-Match3-AI")
ROOT = Path(__file__).resolve().parents[1]
EVALUATOR_BIN = ROOT / "build/progression_evaluator"


DDA_BIN = ROOT / "build/dda_balancer"


HEATMAP_BIN = ROOT / "build/level_heatmap"


@mcp.tool()
def generate_level_heatmap(chapter: int = 5, simulations: int = 2000) -> str:
    """Analyze spatial tile bottlenecks and Tile Frustration Index (TFI) across a 6x6 grid."""
    if not HEATMAP_BIN.exists():
        subprocess.run(
            ["clang++", "-std=c++17", "-O2", str(ROOT / "core/pier.cpp"), str(ROOT / "tools/level_heatmap.cpp"),
             "-o", str(HEATMAP_BIN)],
            check=True
        )
    res = subprocess.run([str(HEATMAP_BIN), str(chapter), str(simulations)], capture_output=True, text=True, check=True)
    return res.stdout


@mcp.tool()
def evaluate_dda_campaign(boards_per_chapter: int = 500) -> str:
    """Run C++17 Monte Carlo simulation of all 6 chapters with Dynamic Difficulty Adjustment (DDA)."""
    if not DDA_BIN.exists():
        subprocess.run(
            ["clang++", "-std=c++17", "-O2", str(ROOT / "core/pier.cpp"), str(ROOT / "tools/dda_balancer.cpp"),
             "-o", str(DDA_BIN)],
            check=True
        )
    result = subprocess.run([str(DDA_BIN), str(boards_per_chapter)], capture_output=True, text=True, check=True)
    return result.stdout


@mcp.tool()
def evaluate_campaign(seeds_per_chapter: int = 64) -> str:
    """Run C++17 Monte Carlo simulation of all 6 chapters to measure difficulty curve, win rates and bottlenecks."""
    if not EVALUATOR_BIN.exists():
        subprocess.run(
            ["clang++", "-std=c++17", "-O2", str(ROOT / "core/pier.cpp"), str(ROOT / "tools/progression_evaluator.cpp"),
             "-o", str(EVALUATOR_BIN)],
            check=True
        )
    result = subprocess.run([str(EVALUATOR_BIN), str(seeds_per_chapter)], capture_output=True, text=True, check=True)
    return result.stdout


@mcp.tool()
def consult_ai_balancer(chapter: int, current_budget: int, panes: int, win_rate: float) -> str:
    """Consult Playrix-grade Game Economy & Balance AI via Databricks Model Serving or deterministic heuristics."""
    prompt = (
        f"You are a Senior Match-3 Level Designer at Playrix. Chapter {chapter} currently has {panes} frozen panes "
        f"and a budget of {current_budget} moves. Simulated AI Bot Win Rate is {win_rate * 100:.1f}%. "
        f"Target casual audience win rate is 80-85%. Recommend exact move budget adjustment and difficulty curve advice. "
        f"Answer concisely in Russian in 3 sentences."
    )
    
    # Try calling Databricks serving endpoint if databricks CLI is available
    try:
        cmd = [
            "databricks", "serving-endpoints", "query", "databricks-qwen3-next-80b-a3b-instruct",
            "--json", json.dumps({"messages": [{"role": "user", "content": prompt}]})
        ]
        res = subprocess.run(cmd, capture_output=True, text=True, timeout=15)
        if res.returncode == 0:
            data = json.loads(res.stdout)
            return data["choices"][0]["message"]["content"]
    except Exception as err:
        pass

    # High-quality fallback rule
    if win_rate < 0.70:
        recommended = current_budget + 2
        return (f"Для главы {chapter} винрейт {win_rate*100:.1f}% ниже целевого коридора (80-85%). "
                f"Рекомендуется увеличить бюджет с {current_budget} до {recommended} ходов (+2 хода). "
                f"Это снизит отток игроков на ранней стадии и выровняет кривую вовлечения.")
    elif win_rate > 0.90:
        recommended = max(panes, current_budget - 1)
        return (f"Глава {chapter} проходится слишком легко ({win_rate*100:.1f}% побед). "
                f"Рекомендуется сократить бюджет с {current_budget} до {recommended} ходов для создания вызова.")
    else:
        return (f"Глава {chapter} находится в идеальном балансе Playrix ({win_rate*100:.1f}% побед). "
                f"Текущий бюджет в {current_budget} ходов сохраняет оптимальный уровень вовлечения без фрустрации.")


@mcp.tool()
def record_level_telemetry(chapter: int, metric_name: str, value: float) -> str:
    """Format and record telemetry event for Sentry / Amplitude pipeline."""
    payload = {
        "event_type": f"match3.{metric_name}",
        "game": "Mosaic Pier",
        "chapter": chapter,
        "value": value,
        "sentry_env": "portfolio-qa",
        "amplitude_schema": "game.study_evaluated"
    }
    log_path = ROOT / "docs/evidence/telemetry_events.jsonl"
    log_path.parent.mkdir(parents=True, exist_ok=True)
    with log_path.open("a", encoding="utf-8") as f:
        f.write(json.dumps(payload) + "\n")
    return f"Logged telemetry event: {payload['event_type']} for Chapter {chapter} with value {value}"


if __name__ == "__main__":
    mcp.run()
