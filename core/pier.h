#pragma once
#include <stdint.h>

namespace pier {
constexpr int Width = 6, Cells = Width * Width, Chapters = 6;
struct Contract {
  uint32_t seed;
  int moves;
  int panes;
};
struct Board {
  uint32_t rng;
  int tiles[Cells], frost[Cells], clear[Cells], fall[Cells];
  int moves, remaining, score, chain, sweeps, shuffled;
  uint32_t next();
  void deal();
  void start(Contract spec);
  bool mark();
  bool legal(int a, int b);
  bool swap(int a, int b);
  void step();
  bool mobile();
  int hint();
};
extern const Contract campaign[Chapters];
} // namespace pier

extern "C" {
int mp_start(int chapter);
int mp_start_master(int chapter);
int mp_tile(int cell);
int mp_frost(int cell);
int mp_clear(int cell);
int mp_fall(int cell);
int mp_moves();
int mp_left();
int mp_score();
int mp_chain();
int mp_sweeps();
int mp_shuffle();
int mp_status(); // 0 input, 1 resolving, 2 restored, 3 out of turns
int mp_swap(int a, int b);
int mp_step();
int mp_hint(); // a * 36 + b, or -1
int mp_can_rewind();
int mp_rewind();
int mp_count();
}
