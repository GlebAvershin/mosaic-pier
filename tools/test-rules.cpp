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
  // Restore the RNG as well as the visible board, even after an invalid swap.
  for (int ch = 0; ch < mp_count(); ++ch) {
    mp_start(ch);
    int pair = mp_hint(), moves = mp_moves(), initial[36], settled[36];
    for (int i = 0; i < 36; ++i)
      initial[i] = mp_tile(i);
    assert(!mp_rewind() && mp_swap(pair / 36, pair % 36));
    assert(!mp_rewind());
    while (mp_status() == 1)
      mp_step();
    int score = mp_score(), left = mp_left();
    for (int i = 0; i < 36; ++i)
      settled[i] = mp_tile(i);
    assert(!mp_swap(-1, 2) && mp_rewind() && !mp_can_rewind());
    assert(mp_score() == 0 && mp_moves() == moves && mp_hint() == pair);
    for (int i = 0; i < 36; ++i)
      assert(initial[i] == mp_tile(i));
    assert(mp_swap(pair / 36, pair % 36));
    while (mp_status() == 1)
      mp_step();
    for (int i = 0; i < 36; ++i)
      assert(settled[i] == mp_tile(i));
    assert(mp_score() == score && mp_left() == left && !mp_rewind());
  }
  int lost = 0;
  for (int ch = 0; ch < mp_count(); ch++) {
    mp_start(ch);
    if (ch == 5) assert(mp_moves() == 22 && mp_left() == 13);
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
  int turns[] = {5, 8, 10, 13, 16, 20};
  for (int ch = 0; ch < mp_count(); ++ch) {
    assert(mp_start_master(ch));
    int initial = mp_moves(), played = 0;
    while (mp_status() == 0) {
      int pair = mp_hint();
      assert(pair >= 0 && mp_swap(pair / 36, pair % 36));
      while (mp_status() == 1) {
        int before[36], clear[36];
        for (int i = 0; i < 36; ++i) {
          before[i] = mp_frost(i);
          clear[i] = mp_clear(i);
        }
        mp_step();
        int layers = 0;
        for (int i = 0; i < 36; ++i) {
          int expected = before[i];
          if (clear[i] == 2)
            expected = 0;
          else if (clear[i] && expected)
            --expected;
          assert(mp_frost(i) == expected);
          layers += expected;
        }
        assert(layers == mp_left());
      }
      ++played;
    }
    assert(mp_status() == 2 && played == turns[ch] && initial - played == 3);
    std::printf("master %d %d %d\n", ch, mp_moves(), mp_score());
  }
}
