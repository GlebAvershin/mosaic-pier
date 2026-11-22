#!/usr/bin/env python3
"""Unit tests for Mosaic Pier LiveOps Sync Adapter."""
import unittest
from research.liveops_sync import simulate_liveops_sync


class TestLiveOpsSync(unittest.TestCase):
    def test_sync_simulation_output(self):
        report = simulate_liveops_sync(500)
        self.assertIn("liveops_metrics", report)
        self.assertGreater(report["liveops_metrics"]["contracts_completed"], 0)
        self.assertGreater(report["liveops_metrics"]["linucb_tile_claim_rate_pct"], 0)


if __name__ == "__main__":
    unittest.main()
