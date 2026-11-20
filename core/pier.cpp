#include "pier.h"

namespace pier {
#include "campaign.inc"
static bool inside(int i) { return i >= 0 && i < Cells; }
static bool adjacent(int a, int b) {
  if (!inside(a) || !inside(b))
    return false;
  if (b < a) {
    int tmp = a;
    a = b;
    b = tmp;
  }
  return b - a == Width || (b - a == 1 && a / Width == b / Width);
}
static void exchange(int &a, int &b) {
  int tmp = a;
  a = b;
  b = tmp;
}
uint32_t Board::next() {
  rng ^= rng << 13;
  rng ^= rng >> 17;
  rng ^= rng << 5;
  return rng;
}
static bool triple(const int *tiles, int i) {
  int x = i % Width, y = i / Width, color = tiles[i];
  for (int axis = 0; axis < 2; ++axis) {
    int stride = axis ? Width : 1, pos = axis ? y : x, run = 1;
    for (int k = 1; k <= pos && tiles[i - k * stride] == color; ++k)
      ++run;
    for (int k = 1; k < Width - pos && tiles[i + k * stride] == color; ++k)
      ++run;
    if (run >= 3)
      return true;
  }
  return false;
}
bool Board::legal(int a, int b) {
  if (!adjacent(a, b) || tiles[a] == tiles[b])
    return false;
  exchange(tiles[a], tiles[b]);
  bool valid = triple(tiles, a) || triple(tiles, b);
  exchange(tiles[a], tiles[b]);
  return valid;
}
bool Board::mobile() {
  for (int i = 0; i < Cells; i++)
    if (legal(i, i + 1) || legal(i, i + Width))
      return true;
  return false;
}
void Board::deal() {
  do {
    for (int i = 0; i < Cells; i++) {
      int v;
      do {
        v = 1 + int(next() % 5);
      } while ((i % Width >= 2 && tiles[i - 1] == v && tiles[i - 2] == v) ||
               (i / Width >= 2 && tiles[i - Width] == v &&
                tiles[i - 2 * Width] == v));
      tiles[i] = v;
    }
  } while (!mobile());
}
void Board::start(Contract spec) {
  *this = Board{};
  rng = spec.seed ? spec.seed : 1;
  moves = spec.moves;
  deal();
  int order[Cells];
  for (int i = 0; i < Cells; i++)
    order[i] = i;
  for (int i = 0; i < spec.panes && i < Cells; i++) {
    int j = i + int(next() % uint32_t(Cells - i));
    exchange(order[i], order[j]);
    frost[order[i]] = 1;
    ++remaining;
  }
}
bool Board::mark() {
  bool found = false;
  for (int &c : clear)
    c = 0;
  for (int axis = 0; axis < 2; axis++)
    for (int line = 0; line < Width; line++) {
      int start = axis ? line : line * Width, stride = axis ? Width : 1;
      for (int p = 0; p < Width;) {
        int end = p + 1;
        while (end < Width &&
               tiles[start + end * stride] == tiles[start + p * stride])
          ++end;
        if (end - p >= 3) {
          found = true;
          bool sweep = end - p >= 4;
          if (sweep)
            ++sweeps;
          for (int k = sweep ? 0 : p; k < (sweep ? Width : end); ++k) {
            int &flag = clear[start + k * stride];
            if (sweep || !flag)
              flag = sweep ? 2 : 1;
          }
        }
        p = end;
      }
    }
  return found;
}
bool Board::swap(int a, int b) {
  if (!remaining || moves <= 0 || !legal(a, b))
    return false;
  exchange(tiles[a], tiles[b]);
  --moves;
  chain = 1;
  sweeps = 0;
  shuffled = 0;
  mark();
  return true;
}
void Board::step() {
  int removed = 0;
  for (int i = 0; i < Cells; i++) {
    fall[i] = 0;
    if (clear[i]) {
      ++removed;
      if (frost[i]) {
        int broken = clear[i] == 2 ? frost[i] : 1;
        frost[i] -= broken;
        remaining -= broken;
      }
    }
  }
  if (!removed)
    return;
  score += removed * 10 * chain;
  for (int x = 0; x < Width; x++) {
    int dst = Width - 1;
    for (int y = Width - 1; y >= 0; y--) {
      int i = y * Width + x;
      if (!clear[i]) {
        tiles[dst * Width + x] = tiles[i];
        fall[dst * Width + x] = dst - y;
        --dst;
      }
    }
    int height = dst + 1;
    for (int y = dst; y >= 0; y--) {
      tiles[y * Width + x] = 1 + int(next() % 5);
      fall[y * Width + x] = height;
    }
  }
  if (mark()) {
    ++chain;
    return;
  }
  if (remaining && moves && !mobile()) {
    deal();
    shuffled = 1;
    for (int &n : fall)
      n = Width;
  }
}
int Board::hint() {
  int best = -1, bestValue = -1;
  for (int i = 0; i < Cells; i++)
    for (int axis = 0; axis < 2; axis++) {
      int stride = axis ? Width : 1;
      int j = i + stride;
      if (!legal(i, j))
        continue;
      Board test = *this;
      test.swap(i, j);
      for (int k = 0; k < 64; k++) {
        bool pending = false;
        for (int v : test.clear)
          if (v)
            pending = true;
        if (!pending)
          break;
        test.step();
      }
      int value = (remaining - test.remaining) * 10000 + test.score - score;
      if (value > bestValue) {
        bestValue = value;
        best = i * Cells + j;
      }
    }
  return best;
}
} // namespace pier
namespace {
pier::Board active, previous;
bool hasPrevious = false, rewindSpent = false;
int stage = 3;
int update() {
  for (int c : active.clear)
    if (c)
      return stage = 1;
  return stage = !active.remaining ? 2 : (active.moves ? 0 : 3);
}
} // namespace
extern "C" {
int mp_start(int ch) {
  if (ch < 0 || ch >= pier::Chapters)
    return 0;
  active.start(pier::campaign[ch]);
  hasPrevious = rewindSpent = false;
  update();
  return 1;
}
int mp_start_master(int ch) {
  if (ch < 0 || ch >= pier::Chapters) return 0;
  // Independent commissions let existing journals keep their original seeds.
  const pier::Contract commissions[] = {
    {5005, 8, 6}, {5102, 11, 8}, {5236, 13, 9},
    {5300, 16, 11}, {5404, 19, 13}, {5514, 23, 16}
  };
  active.start(commissions[ch]);
  int thick = 0;
  for (int i = 0; i < pier::Cells; ++i) {
    if (!active.frost[i]) continue;
    // Reinforce alternating goal cells in reading order.
    if (thick++ % 2 == 0) { ++active.frost[i]; ++active.remaining; }
  }
  hasPrevious = rewindSpent = false;
  update();
  return 1;
}
int mp_tile(int i) { return i >= 0 && i < pier::Cells ? active.tiles[i] : 0; }
int mp_frost(int i) { return i >= 0 && i < pier::Cells ? active.frost[i] : 0; }
int mp_clear(int i) { return i >= 0 && i < pier::Cells ? active.clear[i] : 0; }
int mp_fall(int i) { return i >= 0 && i < pier::Cells ? active.fall[i] : 0; }
int mp_moves() { return active.moves; }
int mp_left() { return active.remaining; }
int mp_score() { return active.score; }
int mp_chain() { return active.chain; }
int mp_sweeps() { return active.sweeps; }
int mp_shuffle() { return active.shuffled; }
int mp_status() { return stage; }
int mp_swap(int a, int b) {
  if (stage != 0)
    return 0;
  auto before = active;
  if (!active.swap(a, b))
    return 0;
  if (!rewindSpent) {
    previous = before;
    hasPrevious = true;
  }
  update();
  return 1;
}
int mp_step() {
  if (stage == 1) {
    active.step();
    update();
  }
  return stage;
}
int mp_hint() { return stage == 0 ? active.hint() : -1; }
int mp_can_rewind() { return hasPrevious && !rewindSpent && stage != 1; }
int mp_rewind() {
  if (!mp_can_rewind())
    return 0;
  active = previous;
  rewindSpent = true;
  update();
  return 1;
}
int mp_count() { return pier::Chapters; }
}
