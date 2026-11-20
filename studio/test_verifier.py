"""Exercise the verifier's release thresholds independently of model suggestions."""
import unittest
from unittest.mock import patch
from agent_review import verify_choice

class ReleaseThresholds(unittest.TestCase):
    def test_spare_moves_boundary(self):
        for spare,expected in ((1,False),(2,True),(5,True),(6,False)):
            with self.subTest(spare=spare), patch("agent_review.simulate",return_value={
                    "won":True,"spare":spare,"sweeps":2,"cascade":3}):
                self.assertEqual(verify_choice("kingfisher")["accepted"],expected)

    def test_unknown_candidate_never_runs_native_process(self):
        with patch("agent_review.simulate") as sim:
            self.assertFalse(verify_choice("unknown")["accepted"])
            sim.assert_not_called()

if __name__=="__main__":
    unittest.main()
