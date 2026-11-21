#include "../core/pier.h"
#include <cstdio>
#include <cstdlib>
#include <vector>
#include <string>

struct ChapterConfig {
    int id;
    int budget;
    int panes;
    int base_seed;
};

const ChapterConfig kChapters[6] = {
    {1, 10, 3, 41},
    {2, 9, 5, 108},
    {3, 11, 7, 130},
    {4, 13, 9, 735},
    {5, 14, 11, 315},
    {6, 22, 13, 900}
};

bool simulate_game(unsigned seed, int budget, int panes, int &out_turns, int &out_remaining) {
    pier::Board b{};
    b.start({seed, budget, panes});
    while (b.remaining && b.moves) {
        int pair = b.hint();
        if (pair < 0 || !b.swap(pair / 36, pair % 36)) {
            out_turns = budget - b.moves;
            out_remaining = b.remaining;
            return false;
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
    }
    out_turns = budget - b.moves;
    out_remaining = b.remaining;
    return b.remaining == 0;
}

int main(int argc, char **argv) {
    int seeds_per_chapter = 64;
    if (argc >= 2) {
        seeds_per_chapter = std::atoi(argv[1]);
        if (seeds_per_chapter < 10) seeds_per_chapter = 10;
        if (seeds_per_chapter > 500) seeds_per_chapter = 500;
    }

    std::printf("{\n  \"evaluator\": \"MosaicPier-ProgressionCurve-C++17\",\n  \"seeds_per_chapter\": %d,\n  \"chapters\": [\n", seeds_per_chapter);

    for (int i = 0; i < 6; ++i) {
        const auto &cfg = kChapters[i];
        int wins = 0;
        int total_turns_won = 0;
        int near_misses = 0; // Lost with only 1 or 2 panes left

        for (int s = 0; s < seeds_per_chapter; ++s) {
            unsigned seed = static_cast<unsigned>(cfg.base_seed + s * 1337 + 7);
            int turns = 0, remaining = 0;
            bool won = simulate_game(seed, cfg.budget, cfg.panes, turns, remaining);
            if (won) {
                wins++;
                total_turns_won += turns;
            } else if (remaining <= 2) {
                near_misses++;
            }
        }

        double win_rate = static_cast<double>(wins) / seeds_per_chapter;
        double avg_turns_won = (wins > 0) ? (static_cast<double>(total_turns_won) / wins) : 0.0;
        double near_miss_rate = static_cast<double>(near_misses) / (seeds_per_chapter - wins > 0 ? (seeds_per_chapter - wins) : 1);
        bool is_bottleneck = win_rate < 0.65;

        std::printf("    {\n");
        std::printf("      \"chapter\": %d,\n", cfg.id);
        std::printf("      \"panes\": %d,\n", cfg.panes);
        std::printf("      \"budget\": %d,\n", cfg.budget);
        std::printf("      \"wins\": %d,\n", wins);
        std::printf("      \"total\": %d,\n", seeds_per_chapter);
        std::printf("      \"win_rate\": %.4f,\n", win_rate);
        std::printf("      \"avg_turns_won\": %.2f,\n", avg_turns_won);
        std::printf("      \"near_miss_rate\": %.4f,\n", near_miss_rate);
        std::printf("      \"is_bottleneck\": %s\n", is_bottleneck ? "true" : "false");
        std::printf("    }%s\n", (i == 5) ? "" : ",");
    }

    std::printf("  ]\n}\n");
    return 0;
}
