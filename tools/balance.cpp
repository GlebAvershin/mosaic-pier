#include "../core/pier.h"
#include <cstdio>
#include <cstdlib>
int main(int argc, char **argv) {
  if (argc != 4)
    return 2;
  unsigned seed = static_cast<unsigned>(std::strtoul(argv[1], nullptr, 10));
  int budget = std::atoi(argv[2]), panes = std::atoi(argv[3]);
  if (!seed || budget < 1 || budget > 40 || panes < 1 || panes > 36)
    return 2;
  pier::Board b{};
  b.start({seed, budget, panes});
  int sw = 0, depth = 0;
  while (b.remaining && b.moves) {
    int pair = b.hint();
    if (pair < 0 || !b.swap(pair / 36, pair % 36))
      return 3;
    int pass = 0;
    while (true) {
      bool any = false;
      for (int c : b.clear)
        if (c)
          any = true;
      if (!any)
        break;
      if (++pass > 128)
        return 4;
      b.step();
    }
    sw += b.sweeps;
    if (pass > depth)
      depth = pass;
  }
  std::printf("{\"won\":%s,\"turns\":%d,\"spare\":%d,\"sweeps\":%d,\"cascade\":"
              "%d,\"remaining\":%d}\n",
              b.remaining ? "false" : "true", budget - b.moves, b.moves, sw,
              depth, b.remaining);
}
