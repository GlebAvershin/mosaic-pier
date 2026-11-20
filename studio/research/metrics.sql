, budgets AS (
    SELECT scenario,budget,
        SUM(CASE WHEN seed % 2=0 THEN won ELSE 0 END) AS train_wins,
        SUM(CASE WHEN seed % 2=0 THEN 1 ELSE 0 END) AS train_n,
        SUM(CASE WHEN seed % 2=1 THEN won ELSE 0 END) AS holdout_wins,
        SUM(CASE WHEN seed % 2=1 THEN 1 ELSE 0 END) AS holdout_n
    FROM samples GROUP BY scenario,budget
), candidate AS (
    SELECT scenario, MIN(CASE WHEN 4*train_wins>=3*train_n THEN budget END) AS chosen
    FROM budgets GROUP BY scenario
)
SELECT c.scenario, COALESCE(c.chosen,0) AS budget,
       COALESCE(b.train_wins,0) AS train_wins, COALESCE(b.train_n,0) AS train_n,
       COALESCE(b.holdout_wins,0) AS holdout_wins, COALESCE(b.holdout_n,0) AS holdout_n
FROM candidate c LEFT JOIN budgets b ON b.scenario=c.scenario AND b.budget=c.chosen ORDER BY c.scenario
