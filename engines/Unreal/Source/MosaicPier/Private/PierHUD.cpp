#include "PierHUD.h"
#include "Engine/Canvas.h"
#include "Engine/Engine.h"
#include "Engine/Texture2D.h"
#include "Engine/World.h"
#include "GameFramework/PlayerController.h"
#include "ImageUtils.h"
#include "Misc/ConfigCacheIni.h"
#include "Misc/Paths.h"
#include "pier.h"

namespace {
const TCHAR *Chapters[] = {TEXT("Tide Steps"),     TEXT("Brass Arcade"),
                           TEXT("Citrus Court"),   TEXT("Sea Lanterns"),
                           TEXT("The Sun Window"), TEXT("Kingfisher Crown")};
}
APierMode::APierMode() {
  HUDClass = APierHUD::StaticClass();
  DefaultPawnClass = nullptr;
}
void APierHUD::BeginPlay() {
  Super::BeginPlay();
  auto *PC = GetOwningPlayerController();
  if (!PC)
    return;
  PC->bShowMouseCursor = true;
  PC->bEnableClickEvents = true;
  PC->SetInputMode(FInputModeGameAndUI().SetHideCursorDuringCapture(false));
  FString Path = FPaths::ProjectContentDir() / TEXT("Glass");
  Coast = FImageUtils::ImportFileAsTexture2D(Path / TEXT("kingfisher.jpg"));
  for (const TCHAR *File :
       {TEXT("lagoon.png"), TEXT("honey.png"), TEXT("coral.png"),
        TEXT("orchid.png"), TEXT("fern.png")})
    Glass.Add(FImageUtils::ImportFileAsTexture2D(Path / File));
  GConfig->GetBool(TEXT("MosaicPier"), TEXT("MasterMode"), bMaster, GGameUserSettingsIni);
  LoadJournal();
}
void APierHUD::LoadJournal() {
  Restored = 0;
  GConfig->GetInt(TEXT("MosaicPier"), bMaster ? TEXT("MasterRestored") : TEXT("Restored"), Restored,
                  GGameUserSettingsIni);
  Restored = FMath::Clamp(Restored, 0, 6);
  OpenChapter(FMath::Min(Restored, 5));
}
void APierHUD::OpenChapter(int32 Index) {
  Chapter = Index;
  if (bMaster) mp_start_master(Chapter);
  else mp_start(Chapter);
  Selected = Hint = -1;
  Message = bMaster ? TEXT("Double rims mark two layers. A line sweep breaks both.") : TEXT(
      "Match three over the pale panes. Four in a line sweeps the whole line.");
}
void APierHUD::DrawHUD() {
  Super::DrawHUD();
  if (!Canvas || Glass.Num() != 5)
    return;
  const float W = Canvas->ClipX, H = Canvas->ClipY;
  const bool Wide = W >= H;
  const float S =
      FMath::Min(W / (Wide ? 1000.f : 540.f), H / (Wide ? 640.f : 814.f));
  const float Ox = (W - (Wide ? 1000 : 540) * S) / 2,
              Oy = (H - (Wide ? 640 : 814) * S) / 2;
  const float X = Ox + (Wide ? 460 : 0) * S, Y = Oy + (Wide ? 0 : 174) * S;
  const FLinearColor Sea(.055f, .16f, .18f), Paper(.91f, .86f, .72f),
      Frost(.57f, .81f, .77f), Gold(1.f, .76f, .35f);
  DrawRect(Sea, 0, 0, W, H);
  const float ArtW = Wide ? 460 : 540, ArtH = Wide ? 640 : 174;
  if (Coast) {
    const float Ratio = ArtW / ArtH,
                Source = float(Coast->GetSizeX()) / Coast->GetSizeY();
    const float U = FMath::Min(1.f, Ratio / Source),
                V = FMath::Min(1.f, Source / Ratio);
    DrawTexture(Coast, Ox, Oy, ArtW * S, ArtH * S, (1 - U) / 2, (1 - V) / 2, U,
                V);
  }
  DrawRect(FLinearColor(.03f, .12f, .13f, .9f), Ox, Oy + (ArtH - 94) * S,
           ArtW * S, 94 * S);
  DrawText(TEXT("Mosaic Pier"), Paper, Ox + 20 * S, Oy + (ArtH - 85) * S,
           GEngine->GetLargeFont(), 1.55f * S);
  DrawText(FString::Printf(TEXT("The Pier Glasshouse   |   %d / 6 restored"),
                           Restored),
           Paper, Ox + 22 * S, Oy + (ArtH - 39) * S, GEngine->GetMediumFont(),
           S);
  auto Panel = [&](float A, float B, float C, float D, FLinearColor Ink) {
    DrawRect(Ink, X + A * S, Y + B * S, C * S, D * S);
  };
  auto Text = [&](const FString &T, float A, float B, float Scale) {
    DrawText(T, Sea, X + A * S, Y + B * S, GEngine->GetMediumFont(), Scale * S);
  };
  auto Hit = [&](float A, float B, float C, float D, FName Name) {
    AddHitBox(FVector2D(X + A * S, Y + B * S), FVector2D(C * S, D * S), Name,
              true);
  };
  Panel(0, 0, 540, 640, Paper);
  Text(FString::Printf(TEXT("CHAPTER %02d / 06"), Chapter + 1), 72, 12, .9f);
  Text(Chapters[Chapter], 72, 38, 1.8f);
  Text(FString::Printf(TEXT("Moves  %d        Frost  %d        Score  %d"),
                       mp_moves(), mp_left(), mp_score()),
       72, 85, 1);
  Panel(72, 118, 396, 396, Sea);
  const double Now = GetWorld()->GetTimeSeconds();
  if (mp_status() == 1 && Now >= NextStep) {
    mp_step();
    NextStep = Now + .28;
  }
  for (int32 I = 0; I < 36; I++) {
    const float A = 76 + (I % 6) * 65, B = 122 + (I / 6) * 65;
    const bool Marked =
        Selected == I || (Hint >= 0 && (I == Hint / 36 || I == Hint % 36));
    Panel(A, B, 62, 62,
          Marked        ? Gold
          : mp_frost(I) ? Frost
                        : FLinearColor(.17f, .31f, .32f));
    if (mp_frost(I))
      for (int32 J = 0; J < 3; J++)
        Panel(A + 4 + J * 6, B + 2, 2, 8, Paper);
    if (mp_frost(I)==2) {
      Panel(A+3, B+3, 55, 2, Gold);
      Panel(A+55, B+4, 2, 52, Gold);
    }
    auto *Gem = Glass[mp_tile(I) - 1].Get();
    const float Size = mp_status() == 1 && mp_clear(I) ? 42.f : 54.f;
    if (Gem)
      DrawTexture(Gem, X + (A + (62 - Size) / 2) * S,
                  Y + (B + (62 - Size) / 2) * S, Size * S, Size * S, 0, 0, 1,
                  1);
    Hit(A, B, 62, 62, FName(*FString::Printf(TEXT("Cell%d"), I)));
  }
  const int State = mp_status();
  Text(State == 2   ? TEXT("All panes clear. Fit the restored glass.")
       : State == 3 ? TEXT("Out of moves. Try the chapter again.")
                    : Message,
       20, 534, .78f);
  auto Button = [&](float A, const TCHAR *Title, FName Name) {
    Panel(A, 573, 148, 45, FLinearColor(.69f, .73f, .61f));
    Text(Title, A + 7, 583, .85f);
    Hit(A, 573, 148, 45, Name);
  };
  if (State >= 2)
    Button(42, State == 3 ? TEXT("Try again") : TEXT("Restore glass"),
           TEXT("Continue"));
  else {
    Button(42, TEXT("Show hint"), TEXT("Hint"));
    Button(356, TEXT("Restart level"), TEXT("Retry"));
  }
  if (mp_can_rewind()) Button(199, TEXT("Undo move"), TEXT("Undo"));
  DrawText(bMaster ? TEXT("Return to story") : TEXT("Play masterwork"), Paper, Ox+21*S, Oy+(ArtH-26)*S, GEngine->GetMediumFont(), .8f*S);
  AddHitBox(FVector2D(Ox+16*S,Oy+(ArtH-28)*S), FVector2D(180*S,28*S),TEXT("Mode"),true);
}
void APierHUD::NotifyHitBoxClick(FName BoxName) {
  Super::NotifyHitBoxClick(BoxName);
  if (mp_status() == 1)
    return;
  const FString Box = BoxName.ToString();
  if (Box == TEXT("Mode")) {
    bMaster = !bMaster;
    GConfig->SetBool(TEXT("MosaicPier"), TEXT("MasterMode"), bMaster, GGameUserSettingsIni);
    GConfig->Flush(false, GGameUserSettingsIni);
    LoadJournal();
    return;
  }
  if (Box == TEXT("Undo") && mp_rewind()) {
    Selected = Hint = -1;
    Message = TEXT("Previous exchange undone.");
    return;
  }
  if (Box == TEXT("Continue") && mp_status() >= 2) {
    if (mp_status() == 3) {
      OpenChapter(Chapter);
      return;
    }
    Restored = FMath::Max(Restored, Chapter + 1);
    GConfig->SetInt(TEXT("MosaicPier"), bMaster ? TEXT("MasterRestored") : TEXT("Restored"), Restored,
                    GGameUserSettingsIni);
    GConfig->Flush(false, GGameUserSettingsIni);
    OpenChapter((Chapter + 1) % 6);
  } else if (Box == TEXT("Retry"))
    OpenChapter(Chapter);
  else if (Box == TEXT("Hint")) {
    Hint = mp_hint();
    Selected = -1;
    Message = TEXT("Swap the two highlighted pieces.");
  } else if (Box.StartsWith(TEXT("Cell")) && mp_status() == 0) {
    const int32 I = FCString::Atoi(*Box.Mid(4));
    Hint = -1;
    if (I == Selected) {
      Selected = -1;
      return;
    }
    if (Selected < 0 ||
        FMath::Abs(Selected / 6 - I / 6) + FMath::Abs(Selected % 6 - I % 6) !=
            1) {
      Selected = I;
      return;
    }
    if (mp_swap(Selected, I)) {
      NextStep = GetWorld()->GetTimeSeconds() + .28;
      Message = TEXT("Glass matched.");
    } else
      Message = TEXT("Make a line of three. That swap cost no moves.");
    Selected = -1;
  }
}
