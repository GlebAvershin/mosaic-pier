#include "../core/pier.h"
#include <cassert>
#include <cstdio>
#include <cstring>
int main() {
  for (unsigned seed = 1; seed <= 512; seed++) {
    pier::Board b{};
    b.start({seed, 14, 13});
    assert(!b.mark() && b.mobile());
    for (int turn = 0; turn < 4 && b.remaining; turn++) {
      int p = b.hint(), before = b.moves;
      assert(p >= 0 && b.swap(p / 36, p % 36) && b.moves == before - 1);
      for (int depth = 0;; depth++) {
        bool marked = false;
        for (int c : b.clear)
          marked |= c != 0;
        if (!marked)
          break;
        assert(depth < 128);
        b.step();
        int count = 0;
        for (int i = 0; i < 36; i++) {
          assert(b.tiles[i] >= 1 && b.tiles[i] <= 5);
          count += b.frost[i];
        }
        assert(b.remaining == count);
      }
    }
  }
  int lost = 0;
  for (int ch = 0; ch < mp_count(); ch++) {
    mp_start(ch);
    assert(!mp_swap(-1, 0) && !mp_swap(5, 6) && !mp_swap(0, 35));
    while (mp_status() == 0) {
      int p = mp_hint();
      assert(p >= 0 && mp_swap(p / 36, p % 36));
      assert(!mp_swap(1, 2));
      while (mp_status() == 1)
        mp_step();
    }
    assert(mp_status() == 2 && mp_left() == 0 && mp_hint() == -1);
    std::printf("%d %d %d\n", ch, mp_moves(), mp_score());
    mp_start(ch);
    while (mp_status() == 0) {
      bool played = false;
      for (int i = 0; i < 36 && !played; i++)
        played = mp_swap(i, i + 1) || mp_swap(i, i + 6);
      assert(played);
      while (mp_status() == 1)
        mp_step();
    }
    if (mp_status() == 3) {
      ++lost;
      assert(mp_moves() == 0 && mp_left() > 0 && mp_hint() == -1);
    }
  }
  assert(lost > 0);
}
