#pragma once
#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "GameFramework/HUD.h"
#include "PierHUD.generated.h"

UCLASS()
class APierMode : public AGameModeBase {
  GENERATED_BODY()
public:
  APierMode();
};

UCLASS()
class APierHUD : public AHUD {
  GENERATED_BODY()
public:
  virtual void BeginPlay() override;
  virtual void DrawHUD() override;
  virtual void NotifyHitBoxClick(FName BoxName) override;

private:
  UPROPERTY() TArray<TObjectPtr<UTexture2D>> Glass;
  UPROPERTY() TObjectPtr<UTexture2D> Coast;
  int32 Chapter = 0, Selected = -1, Hint = -1, Restored = 0;
  bool bMaster = false;
  void LoadJournal();
  double NextStep = 0;
  FString Message;
  void OpenChapter(int32 Index);
};
