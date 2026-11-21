#include "../core/pier.h"
#include <cstdio>
#include <cstdlib>
#include <vector>
#include <string>
#include <algorithm>

int main(int argc, char **argv) {
    int target_chapter = 5;
    int simulations = 2000;
    if (argc >= 2) target_chapter = std::atoi(argv[1]);
    if (argc >= 3) simulations = std::atoi(argv[2]);

    if (target_chapter < 1 || target_chapter > 6) target_chapter = 5;
    if (simulations < 100) simulations = 100;
    if (simulations > 20000) simulations = 20000;

    const auto &cfg = pier::campaign[target_chapter - 1];

    int defeats = 0;
    int wins = 0;
    std::vector<int> frost_left_histogram(pier::Cells, 0);
    std::vector<int> swap_activity(pier::Cells, 0);

    for (int s = 0; s < simulations; ++s) {
        unsigned seed = static_cast<unsigned>(cfg.seed + s * 1337 + 17);
        pier::Board b{};
        b.start({seed, cfg.moves, cfg.panes});

        while (b.remaining && b.moves) {
            int pair = b.hint();
            if (pair < 0) break;
            int cell_a = pair / 36;
            int cell_b = pair % 36;
            swap_activity[cell_a]++;
            swap_activity[cell_b]++;

            if (!b.swap(cell_a, cell_b)) break;

            int pass = 0;
            while (true) {
                bool any = false;
                for (int c : b.clear) if (c) any = true;
                if (!any) break;
                if (++pass > 128) break;
                b.step();
            }
        }

        if (b.remaining == 0) {
            wins++;
        } else {
            defeats++;
            // Record where frost remained unresolved
            for (int c = 0; c < pier::Cells; ++c) {
                if (b.frost[c] > 0) {
                    frost_left_histogram[c]++;
                }
            }
        }
    }

    // JSON Output
    std::printf("{\n");
    std::printf("  \"chapter\": %d,\n", target_chapter);
    std::printf("  \"panes\": %d,\n", cfg.panes);
    std::printf("  \"budget\": %d,\n", cfg.moves);
    std::printf("  \"simulations\": %d,\n", simulations);
    std::printf("  \"wins\": %d,\n", wins);
    std::printf("  \"defeats\": %d,\n", defeats);
    std::printf("  \"win_rate\": %.4f,\n", static_cast<double>(wins) / simulations);
    std::printf("  \"spatial_grid\": [\n");

    int max_frustration_cell = 0;
    double max_frustration_val = -1.0;

    for (int y = 0; y < pier::Width; ++y) {
        std::printf("    [");
        for (int x = 0; x < pier::Width; ++x) {
            int idx = y * pier::Width + x;
            double tfi = (defeats > 0) ? (static_cast<double>(frost_left_histogram[idx]) / defeats) : 0.0;
            if (tfi > max_frustration_val) {
                max_frustration_val = tfi;
                max_frustration_cell = idx;
            }
            std::printf("{\"cell\": %d, \"x\": %d, \"y\": %d, \"tfi\": %.4f, \"swaps\": %d}%s",
                        idx, x, y, tfi, swap_activity[idx], (x == pier::Width - 1) ? "" : ", ");
        }
        std::printf("]%s\n", (y == pier::Width - 1) ? "" : ",");
    }
    std::printf("  ],\n");
    std::printf("  \"max_bottleneck\": {\"cell\": %d, \"x\": %d, \"y\": %d, \"tfi\": %.4f}\n",
                max_frustration_cell, max_frustration_cell % pier::Width, max_frustration_cell / pier::Width, max_frustration_val);
    std::printf("}\n");

    return 0;
}
