# Mosaic Pier

A seaside glasshouse has lost its colour. Swap pieces of stained glass, clear the frost and restore six sections of the pier, from the tide steps to the kingfisher at the top of the cupola.

**[Play in your browser](https://glebavershin.github.io/mosaic-pier/)** · [Download for macOS](https://github.com/GlebAvershin/mosaic-pier/releases/tag/v1.1.0) · [Watch a playthrough](docs/evidence/playthrough.mp4) · [Технологии и результаты](docs/TECHNOLOGIES.md)

![The first chapter of Mosaic Pier with the kingfisher window and the playable board](docs/evidence/workshop.jpg)

## Playing

Swap two neighbouring pieces by tapping each piece or swiping. Match three over a frosted pane to clear it. Four or more in a row or column sweep the entire line. Each chapter has a move budget. Illegal swaps are free. The hint previews a legal move. In the browser, arrow keys move focus and Space selects a piece. In the Unity player, H highlights a pair and sets the keyboard cursor on its first piece. Choose **Play masterwork** for six more demanding commissions. A double rim marks two layers of frost. A normal match chips one layer. A line sweep clears both. **Undo move** rewinds one exchange per attempt, including all refills and cascades. Each mode has its own journal on the device. In Unity, Z undoes a move and the Motion switch reduces animation.

## Inside the game

| Component | What you can inspect |
| --- | --- |
| [C++17 rules](core/pier.cpp) | A seeded 6×6 board, horizontal and vertical matches, simultaneous clears, cascades, dead-board recovery and one-move lookahead. The story and masterwork commissions have six seeded contracts each. |
| [Browser client](web) | The C++ rules compiled to WebAssembly, responsive controls, reduced motion, synthesized glass chimes and move-history replay. [Mobile screenshot](docs/evidence/mobile.jpg) |
| [Unity project](engines/Unity) | Unity 2022.3.62f1, C# presentation, coroutine animation and a native C++ plug-in. The universal macOS player completed all twelve boards. [Player screenshot](docs/evidence/unity-player.jpg) · [Player replay](docs/evidence/unity-player.csv) |
| [Unreal Engine project](engines/Unreal) | UE 5.5 C++ game module, game mode, Canvas HUD, hit boxes, staged textures and an automation test. Delivered as source. Editor compilation and playtesting are pending. |
| [AI level workshop](studio/agent_review.py) | The model chooses between measured candidates, then an MCP tool reruns the game simulation. Codex and Gemini selected the finale shipped in the campaign. [Codex](docs/evidence/codex.json) · [Gemini](docs/evidence/gemini.json) |
| [AI layout review](studio/layout_review.py) | Chromium measured three layouts at six viewport sizes. Qwen selected the board-first mobile layout and the verifier checked the interaction targets. [Decision](docs/evidence/layout.json) |
| [Cloud CI](.github/workflows/workshop.yml) | Two Kubernetes replicas, two llama.cpp model routes, FastAPI and a shared PostgreSQL cache. The on-demand CI run also captured a PR review and local Sentry and Amplitude SDK events. [Level review](docs/evidence/cloud.json) · [Cloud UI layout review](docs/evidence/cloud-layout.json) |

## Running from source

**Web.** Run `python3 -m http.server 8000 --directory web` from the repository root, then open `http://localhost:8000`. The checked-in WebAssembly runs without a model server. To rebuild it, set `WASM_CXX` to the absolute path to WASI SDK 25's `clang++` and run `tools/build-web.sh`. CI compares the rebuilt file with the release version.

**Unity.** Open `engines/Unity` in Unity 2022.3.62f1, open `Assets/Scenes/Pier.unity` and press Play. The macOS plug-in is included. On Linux run `tools/unity-plugin.sh` first. For a standalone macOS build, set `MOSAIC_BUILD` to an absolute output path ending in `.app` and call the editor with `-batchmode -nographics -quit -projectPath <absolute Unity project directory> -executeMethod PierBuild.Mac`.

**Unity editor MCP.** On macOS with Unity CLI 1.0.0 on `PATH` and editor 6000.6.10f1 installed, run `studio/unity/start-workshop.sh`. It opens an isolated copy with Unity Pipeline 1.0.1. Pass the printed project path to `uv run --script studio/unity/replay.py "<workshop path>"` to replay both six-chapter journals in the editor and compare each result with the released player. Close that copy with `unity close "<workshop path>"`. [Recorded MCP response](docs/evidence/unity-mcp.json)

Package the built player with its font license using `tools/package-macos.sh "<path>/Mosaic Pier.app" "<path>/Mosaic-Pier-1.1-macOS.zip"`.

**Unreal.** Keep the checkout intact so Unreal Build Tool can find the shared `core` directory. Open `engines/Unreal/MosaicPier.uproject` with UE 5.5 and build the MosaicPierEditor target. Play uses the engine's Entry map and the game's native HUD. The Automation window exposes `MosaicPier.Gameplay.Campaign`.

**Verification.** Run `tools/check.sh` with a C++17 compiler, the address and undefined-behaviour sanitizers, and Node.js. For browser input tests, install Playwright for Python and its Chromium browser, start the local web server and run `python tools/playtest.py http://localhost:8000 --out build/browser-report`. A standalone Unity replay can be run without changing player progress.

```sh
"build/macos/Mosaic Pier.app/Contents/MacOS/Mosaic Pier" -mosaic-replay "$PWD/build/unity-replay"
```

## Replaying an AI decision

Run `tools/check.sh` first to build the native level simulator. The account CLI adapter and cloud gateway are shared with Lantern Atlas. To reproduce the subscription review from the game root, use the platform revision pinned in CI and a signed-in Gemini or Codex CLI.

```sh
git clone https://github.com/GlebAvershin/lantern-atlas build/platform
git -C build/platform checkout a4c8bef02f7d9e035c4a23a116f9ad8e13eaa03b
uv run --project build/platform/studio --locked python studio/account_review.py build/platform gemini
```

Substitute `codex` for the final argument to use the Codex CLI. For the responsive layout decision, `tools/layout-probe.py` measures candidates against the running browser game. The [Level workshop workflow](.github/workflows/workshop.yml) runs both MCP adapters on GitHub-hosted Kubernetes clusters with Qwen and SmolLM2. Each cluster is removed after its measurements are attached.

The kingfisher window was generated for this project with ImageGen. The five silhouettes and their facets are rendered by the [original sprite generator](tools/glass.py). The web and Unity editions use Cormorant Garamond under its [SIL Open Font License](web/assets/OFL.txt).
