# Rush Refactor - safety net (Stage 0)

Folder `RushBaseline/` = recorded state of the project BEFORE the refactor. Tools live in `Assets/RushRefactor/Editor/`
(Editor only, never part of the WebGL build, they do not edit scenes, prefabs or scripts).

## Run these gates after EVERY refactor step (in this order)

| # | Gate | How | Time | Pass means |
|---|------|-----|------|------------|
| 1 | Compile clean | Console has 0 errors | - | code builds |
| 2 | Contracts | Menu `Tools/Rush Refactor/Contracts/Check Against Baseline` | 2 s | every button binding, Invoke string, JSON field and JS bridge name still exists |
| 3 | Data snapshot | Menu `Tools/Rush Refactor/Snapshot/Capture Current And Compare`, then `Snapshot/Status` | 10-30 s | every serialized value in 94 scenes / 270 prefabs / 361 assets is unchanged |
| 4 | Smoke test | Menu `Tools/Rush Refactor/Smoke/Run All Scenes (current) And Compare` | 15 min | every scene still plays; quest flow API still works |

Notes
- Smoke menu only RUNS the scenes and writes `smoke_current.txt`. Compare it with `RushSmoke.CompareToBaseline()` (Claude can run this for you).
- Smoke test enters and leaves Play mode by itself and mutes the editor. Do not click in the editor while it runs. Abort: `Tools/Rush Refactor/Smoke/Abort`.
- Smoke test never submits the LAST quest of a scene (that would POST the result to the real API).
- Results: `contracts_diff.txt`, `snapshot_diff.txt`, `smoke_diff.txt` (MISSING / CHANGED lines = problem, ADDED = fine).

## Declaring an intended change
- Renaming a serialized field: add `[FormerlySerializedAs("oldName")]` in code. The snapshot picks it up automatically.
- Renaming a class or moving an asset: add a line to `RushBaseline/rename_map.txt`
  - `Class|Old.Full.Name|New.Full.Name`
  - `Field|Declaring.Type.NewName|oldField|newField`
  - `Path|Assets/old/prefix|Assets/new/prefix`
- A deliberate contract change (e.g. a button method is renamed AND the button is rebound) means re-capturing the baseline: `Contracts/Capture Baseline`. Do this only on purpose, in its own commit.

## Known problems that exist in the baseline (not regressions)
- 11 broken UnityEvent bindings: ChallengeManager.Next x4, ChallengeManager.ResetQuest x3, GameManager.Restart x2, ConversationPresenter x2.
- 11 missing scripts in test scenes: Dev_Frontend, Dev_TestRoom, Dev_TestRoomApril, TextAnimation/Demo.
- ContentManager declares `GetBrowserWidth/Height` with `DllImport("__Internal__")` but no .jslib implements them.
- Runtime errors in baseline: `m_Music of GameManager has not been assigned` (62 scenes), NullReferenceException (15 scenes), `Failed to load data, challenge will return empty` (3 scenes).
