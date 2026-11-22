#!/usr/bin/env python3
"""Unit tests for Mosaic Pier Reverse-Play PCG Mahjong Generator."""
import unittest
from research.pcg_mahjong import MahjongBoard, ReversePlayMahjongGenerator


class TestPCGMahjong(unittest.TestCase):
    def setUp(self):
        self.generator = ReversePlayMahjongGenerator(seed=42)

    def test_mahjong_board_free_rules(self):
        # Place 3 tiles horizontally
        slots = [(0, 0, 0), (1, 0, 0), (2, 0, 0)]
        board = MahjongBoard(slots)
        board.occupied = {s: "A" for s in slots}

        # (1, 0, 0) has neighbors on left and right, so it should NOT be free
        self.assertFalse(board.is_free((1, 0, 0)))
        # Outer tiles (0, 0, 0) and (2, 0, 0) should be free
        self.assertTrue(board.is_free((0, 0, 0)))
        self.assertTrue(board.is_free((2, 0, 0)))

        # Adding a tile on top of (0, 0, 0) blocks it
        board.occupied[(0, 0, 1)] = "B"
        self.assertFalse(board.is_free((0, 0, 0)))

    def test_generate_board_guaranteed_solvability(self):
        board_data = self.generator.generate_board("Pier Boardwalk", target_pairs=16)
        self.assertIsNotNone(board_data)
        self.assertEqual(board_data["total_pairs"], 16)
        self.assertEqual(board_data["solvability_status"], "GUARANTEED_100_PCT_SOLVABLE")
        self.assertGreater(board_data["mean_branching_factor"], 1.0)

    def test_all_layouts_generation(self):
        layouts = ["Pier Boardwalk", "Lighthouse Pyramid", "Tide Basin", "Marina Cross"]
        for layout in layouts:
            b = self.generator.generate_board(layout, target_pairs=12)
            self.assertIsNotNone(b, f"Failed on {layout}")
            self.assertEqual(b["total_pairs"], 12)


if __name__ == "__main__":
    unittest.main()
