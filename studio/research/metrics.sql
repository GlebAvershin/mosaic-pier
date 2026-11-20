-- At n=256 this is the minimum win count for a two-sided 95% Wilson lower bound of 75%.
SELECT scenario, MAX(budget) AS budget, SUM(won) AS wins, COUNT(*) AS boards, 206 AS required_wins
FROM samples GROUP BY scenario HAVING COUNT(*)=256 ORDER BY scenario
