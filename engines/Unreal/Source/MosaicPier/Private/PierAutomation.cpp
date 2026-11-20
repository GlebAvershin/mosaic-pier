#include "Misc/AutomationTest.h"
#include "pier.h"
#if WITH_DEV_AUTOMATION_TESTS
IMPLEMENT_SIMPLE_AUTOMATION_TEST(FPierCampaignTest,
                                 "MosaicPier.Gameplay.Campaign",
                                 EAutomationTestFlags::EditorContext |
                                     EAutomationTestFlags::EngineFilter)
bool FPierCampaignTest::RunTest(const FString &Parameters) {
  for (const auto &Chapter : pier::campaign) {
    pier::Board Board{};
    Board.start(Chapter);
    while (Board.remaining && Board.moves) {
      int Pair = Board.hint();
      if (!TestTrue(TEXT("Legal suggestion"),
                    Pair >= 0 && Board.swap(Pair / 36, Pair % 36)))
        return false;
      for (int Tick = 0; Tick < 128; Tick++) {
        bool Pending = false;
        for (int Piece : Board.clear)
          Pending |= Piece != 0;
        if (!Pending)
          break;
        Board.step();
      }
    }
    TestEqual(TEXT("Restored every pane"), Board.remaining, 0);
  }
  return true;
}
#endif
