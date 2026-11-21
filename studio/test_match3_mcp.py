#!/usr/bin/env python3
import json
import unittest
from pathlib import Path
import match3_mcp

class TestMatch3MCP(unittest.TestCase):
    def test_evaluate_campaign(self):
        output = match3_mcp.evaluate_campaign(32)
        data = json.loads(output)
        self.assertEqual(data["evaluator"], "MosaicPier-ProgressionCurve-C++17")
        self.assertEqual(len(data["chapters"]), 6)
        for ch in data["chapters"]:
            self.assertIn("win_rate", ch)
            self.assertGreater(ch["win_rate"], 0.0)

    def test_consult_ai_balancer(self):
        advice = match3_mcp.consult_ai_balancer(chapter=5, current_budget=14, panes=11, win_rate=0.75)
        self.assertIsInstance(advice, str)
        self.assertGreater(len(advice), 20)

    def test_record_level_telemetry(self):
        res = match3_mcp.record_level_telemetry(chapter=6, metric_name="win_rate", value=0.8906)
        self.assertIn("Logged telemetry event", res)
        log_file = match3_mcp.ROOT / "docs/evidence/telemetry_events.jsonl"
        self.assertTrue(log_file.exists())

if __name__ == "__main__":
    unittest.main()
