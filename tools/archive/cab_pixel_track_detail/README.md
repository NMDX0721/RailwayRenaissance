# 归档：旧像素风驾驶室窗外轨道细节（未绑定到游戏）

2026-11-10 为旧版 `Cab3DSceneController`（640×360 像素风窗外场景）写的轨道细节：碎石、扣件、分层钢轨，
以及车速驱动的道砟振动和枕木下沉。后来确认游戏用的是 ATO 轨道套件（`tools/ato_track`），
这些文件移出 `Assets/`，不参与编译。要复用时放回 `Assets/Scripts/Cab/` 与 `Assets/Editor/`，
并在 `Cab3DSceneController.BuildTrackSystem` / `UpdateFrame` 里接入。
