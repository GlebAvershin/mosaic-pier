#include "../core/pier.h"
#include <cstdio>
#include <cstdlib>
#include <vector>
#include <cmath>
#include <string>
#include <algorithm>

struct LevelConfig {
    int chapter;
    int budget;
    int panes;
    int base_seed;
};

const LevelConfig kCampaign[6] = {
    {1, 10, 3, 41},
    {2, 9, 5, 108},
    {3, 11, 7, 130},
    {4, 13, 9, 735},
    {5, 14, 11, 315},
    {6, 22, 13, 900}
};

struct SimulationResult {
    bool won;
    int moves_used;
    int remaining_panes;
    int sweeps;
};

SimulationResult run_single_board(unsigned seed, int budget, int panes, bool dda_active) {
    int effective_budget = dda_active ? (budget + 2) : budget;
    pier::Board b{};
    b.start({seed, effective_budget, panes});
    
    int total_sweeps = 0;
    while (b.remaining && b.moves) {
        int pair = b.hint();
        if (pair < 0 || !b.swap(pair / 36, pair % 36)) {
            break;
        }
        int pass = 0;
        while (true) {
            bool any = false;
            for (int c : b.clear) {
                if (c) any = true;
            }
            if (!any) break;
            if (++pass > 128) break;
            b.step();
        }
        total_sweeps += b.sweeps;
    }
    return {b.remaining == 0, effective_budget - b.moves, b.remaining, total_sweeps};
}

void compute_wilson_ci(int wins, int total, double &out_low, double &out_high) {
    if (total == 0) { out_low = out_high = 0.0; return; }
    double z = 1.95996;
    double p = static_cast<double>(wins) / total;
    double denom = 1.0 + z * z / total;
    double centre = (p + z * z / (2.0 * total)) / denom;
    double spread = (z * std::sqrt((p * (1.0 - p) + z * z / (4.0 * total)) / total)) / denom;
    out_low = std::max(0.0, centre - spread);
    out_high = std::min(1.0, centre + spread);
}

int main(int argc, char **argv) {
    int boards_per_chapter = 500;
    if (argc >= 2) {
        boards_per_chapter = std::atoi(argv[1]);
        if (boards_per_chapter < 50) boards_per_chapter = 50;
        if (boards_per_chapter > 5000) boards_per_chapter = 5000;
    }

    std::printf("{\n  \"engine\": \"MosaicPier-DDA-MonteCarlo-C++17\",\n  \"boards_per_chapter\": %d,\n  \"total_simulations\": %d,\n  \"chapters\": [\n",
                boards_per_chapter, boards_per_chapter * 6 * 2);

    for (int i = 0; i < 6; ++i) {
        const auto &cfg = kCampaign[i];

        // 1. Baseline Simulation (No DDA)
        int base_wins = 0;
        int base_near_misses = 0; // Lost with 1 or 2 panes left
        int base_total_moves = 0;

        for (int s = 0; s < boards_per_chapter; ++s) {
            unsigned seed = static_cast<unsigned>(cfg.base_seed + s * 179 + 31);
            auto res = run_single_board(seed, cfg.budget, cfg.panes, false);
            if (res.won) {
                base_wins++;
                base_total_moves += res.moves_used;
            } else if (res.remaining_panes <= 2) {
                base_near_misses++;
            }
        }

        // 2. Adaptive DDA Simulation (Assist triggered on difficult seeds)
        int dda_wins = 0;
        int dda_near_misses = 0;
        int dda_total_moves = 0;
        int dda_triggered_count = 0;

        for (int s = 0; s < boards_per_chapter; ++s) {
            unsigned seed = static_cast<unsigned>(cfg.base_seed + s * 179 + 31);
            // Simulate player having lost previous match, so DDA is active
            bool dda_assist = (s % 4 == 0); // 25% of sessions qualify for dynamic assist
            if (dda_assist) dda_triggered_count++;

            auto res = run_single_board(seed, cfg.budget, cfg.panes, dda_assist);
            if (res.won) {
                dda_wins++;
                dda_total_moves += res.moves_used;
            } else if (res.remaining_panes <= 2) {
                dda_near_misses++;
            }
        }

        double base_win_rate = static_cast<double>(base_wins) / boards_per_chapter;
        double dda_win_rate = static_cast<double>(dda_wins) / boards_per_chapter;
        double base_ci_low, base_ci_high, dda_ci_low, dda_ci_high;
        compute_wilson_ci(base_wins, boards_per_chapter, base_ci_low, base_ci_high);
        compute_wilson_ci(dda_wins, boards_per_chapter, dda_ci_low, dda_ci_high);

        double base_frustration = (boards_per_chapter - base_wins > 0) ?
            (static_cast<double>(base_near_misses) / (boards_per_chapter - base_wins)) : 0.0;
        double dda_frustration = (boards_per_chapter - dda_wins > 0) ?
            (static_cast<double>(dda_near_misses) / (boards_per_chapter - dda_wins)) : 0.0;

        std::printf("    {\n");
        std::printf("      \"chapter\": %d,\n", cfg.chapter);
        std::printf("      \"panes\": %d,\n", cfg.panes);
        std::printf("      \"base_budget\": %d,\n", cfg.budget);
        std::printf("      \"baseline\": {\n");
        std::printf("        \"wins\": %d,\n", base_wins);
        std::printf("        \"win_rate\": %.4f,\n", base_win_rate);
        std::printf("        \"wilson_ci\": [%.4f, %.4f],\n", base_ci_low, base_ci_high);
        std::printf("        \"near_miss_rate\": %.4f\n", base_frustration);
        std::printf("      },\n");
        std::printf("      \"with_dda\": {\n");
        std::printf("        \"wins\": %d,\n", dda_wins);
        std::printf("        \"win_rate\": %.4f,\n", dda_win_rate);
        std::printf("        \"wilson_ci\": [%.4f, %.4f],\n", dda_ci_low, dda_ci_high);
        std::printf("        \"near_miss_rate\": %.4f,\n", dda_frustration);
        std::printf("        \"dda_assists\": %d\n", dda_triggered_count);
        std::printf("      },\n");
        std::printf("      \"win_rate_uplift_pp\": %.2f,\n", (dda_win_rate - base_win_rate) * 100.0);
        std::printf("      \"frustration_reduction_pp\": %.2f\n", (base_frustration - dda_frustration) * 100.0);
        std::printf("    }%s\n", (i == 5) ? "" : ",");
    }

    std::printf("  ]\n}\n");
    return 0;
}
