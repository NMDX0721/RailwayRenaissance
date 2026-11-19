# 岁月 Cubism 头部样机导入

2026-11-17 眼珠角度：在 `suiyue_eye_repair_candidate_20261117.cmo3` 的双眼睁开关键值，将两侧虹膜网格顺着面部轻微顺时针旋转；Cubism 开眼截图 `../cubism_eye_rig_review/angle_reopen_both.png` 和闭眼截图 `angle_close_both.png` 已检查。保存后的视线运动、半闭眼插值、重新打开和 Unity 导出仍待验证；目前不是身体角度的动态绑定。

2026-11-17 右眼截断修复：把角色 L 虹膜源图恢复到旧网格覆盖范围并重导入，再在睁眼关键值移动整个 `eye_L_iris_complete` 网格。Cubism 415.6% 截图 `../cubism_eye_rig_review/eye_reopen_both.png` 显示虹膜完整；`eye_close_both.png` 显示双眼闭合时无旧眼珠残留。另存 `suiyue_eye_repair_candidate_20261117.cmo3`；中间开度、视线运动、重新打开模型和 Unity 导出尚未验收。当前瞳孔仍在虹膜内。

2026-11-17 Cubism 实测：右眼修正版 PSD 已重导入现有模型，当前编辑器有未保存变更。415.6% 放大截图 `../cubism_eye_rig_review/suiyue_iris_mesh_selected_20261117.png` 显示画面右侧虹膜在原有 ArtMesh 右边界被垂直截断；选中 `eye_L_iris_complete` 后其网格边界与截断位置一致。该候选尚不能保存或导出为完成版；需要扩大或重建虹膜网格并核对纹理映射，再验证闭眼和视线。原 cmo3 仍在磁盘上。

2026-11-17 右眼异常再检查：眼珠位置之外，画面右侧（角色 L）的眼白层本身带有外侧大块灰色，导致开眼时看起来仍像图层重叠。`tools/vn/integrate_suiyue_hd_irises.py --clean-sclera` 产生独立的 `eye_underlay_repair/suiyue_head_eyes_sclera_repair_candidate.psd`，沿用原眼白透明轮廓及 70 层结构，只将外侧灰块按平滑动漫白眼材质修色。与上一候选相比，合成改动 392 像素且均在右眼眼白盒内；`hd_anime_clean_sclera_clipped_crop.png` 为离线剪贴预览。现有 cmo3 已在 Cubism 打开，但该候选仍未导入、未保存、未导出和未做 Unity 验证。

2026-11-17 右侧眼珠位置复核：`tools/vn/measure_suiyue_eye_alignment.py` 比较双眼眼白与虹膜的 alpha 重心。画面右侧眼是角色 L 眼；其虹膜在原候选中比眼白高约 3.6 原稿像素，现下移 3 像素。按眼白裁切后，右眼虹膜/眼白纵向重心分别为 239.51/239.46，左眼为 225.71/225.34。`eye_underlay_repair/hd_anime_clipped_crop.png` 是剪贴关系的离线预览，普通 PSD 合成图没有显示 Cubism 的剪贴效果。此调整仍需在 Cubism 的开闭眼与视线运动、Unity 实际画面中核验。

2026-11-15 清晰非像素眼部候选：`tools/vn/integrate_suiyue_hd_irises.py` 将图像生成的高清平滑动漫虹膜纳入修复底图的 70 层 PSD，输出 `eye_underlay_repair/suiyue_head_eyes_hd_anime_candidate.psd` 和 `hd_anime_open_crop.png`。右侧眼原独立重绘的瞳孔偏向鼻梁，候选改用瞳孔居中的同一高清虹膜源，并把右侧虹膜盒向眼白中心移动 9 原稿像素。离线合成已检查；Cubism 重导入、眼开闭/视线运动、模型保存、moc3 导出及 Unity 画面均未验证，不能视为最终修复。当前瞳孔仍在虹膜层内；后续独立视线控制需要将双侧瞳孔拆为各自 ArtMesh 并重验剪贴及参数关键值。美术目标是清晰的平滑动漫插画，不使用像素画处理。

2026-11-15 眼部底图修补候选：`tools/vn/repair_suiyue_eye_underlay.py` 从现有导入 PSD 生成 `eye_underlay_repair/suiyue_head_eyes_clean_candidate.psd`。它清理四个静态脸层眼区的旧轮廓，把补肤像素并入已有 `facial_edge_pixels`，保持 70 图层/8 分组及原有层名。重读 PSD 后，眼区外最大像素差为 1；`eye_before_after_comparison.png` 并排展示原稿与修正版的开眼/透明闭眼，旧眼眶重影和硬拼接边在栅格预览中消失。`result.json` 记录源/产物哈希及改动范围。Cubism 已进入现有模型的 PSD 替换流程，但桌面失焦/不可见后尚未确认替换；当前 cmo3/moc3 仍为旧版，不能把栅格预览当成模型验收。现有 ArtMesh 对扩展补肤像素的覆盖仍需在 Editor 和 Unity 检查。PSD 替换方法见 [Cubism 官方重导入说明](https://docs.live2d.com/en/cubism-editor-manual/psd-re-import/)。

2026-11-15 眼部重影诊断：`unity_blink_acceptance/eye_open_crop.png` 和 `eye_zero_crop.png` 可见眼眶残线。`eye_underlay_diagnostic_20261115.png` 分层检查表明 `facial_edge_pixels`、`face_base_visible` 在活动眼白/虹膜下仍保留旧眼眶轮廓，闭眼透明度为 0 时残线完全露出。`face_under_eyes` 也有局部旧眼部笔触，需一并审视。当前眼部效果存在可见重叠，不能作为最终美术验收。修复需清理这些底层眼眶像素、保留独立上眼睑与眉毛，重导入 Cubism 并重做开闭眼视觉核验；当前 cmo3/moc3 尚未修复。

2026-11-15 定时眨眼：`src/SuiyueBlinkLoop.cs` 在 Unity `Update` 中按间隔推进闭合、短暂停留、睁开三个阶段，`SetPaused(true)` 立即睁眼并停表，恢复后重新等待一个周期。隔离工程将其挂在岁月模型上并连接 SDK `CubismEyeBlinkController`；`SuiyueAutoBlinkReview.Batch` 的实际 Play 记录见 `unity_auto_blink_acceptance/result.json`，观察到闭眼、重睁、暂停后保持睁眼、恢复后的第二次闭眼。正式 VN 尚未加载此组件；隔离工程的 `Assets/Review/SuiyueBlink/SuiyueBlinkLoop.cs` 是编译副本，规范源码在本目录 `src/`。

2026-11-15 SDK 控制器验收：隔离场景为 `ParamEyeLOpen`、`ParamEyeROpen` 加 `CubismEyeBlinkParameter`，在模型上接入 `CubismEyeBlinkController`（Override）并刷新 SDK 更新顺序。`SuiyuePrototypeReview.BatchController` 在 Unity Play 下通过控制器设置眼睛开度 1→0→1，`unity_controller_acceptance/result.json` 记录闭眼 1732 个变化像素、重新睁眼 0 个差异像素。这验证控制器接线；正式 VN 模型加载仍未接入。

2026-11-15 眨眼原型：`suiyue_head_prototype.cmo3` 已为双眼虹膜、眼白分别建立开闭参数的三个关键值；开眼 1 时四层透明度 100%，闭眼 0 时为 0%，上眼睑仍显示。旧静态工程备份为 `suiyue_head_prototype_atlas_baseline_20261115.cmo3`。新运行时导出为 `suiyue_head_blink_prototype.moc3`、同名 model3/cdi3 与单张 2048 贴图。隔离 Unity Play 验收见 `unity_blink_acceptance/result.json`：闭眼较中立帧变化 1732 像素，重新睁眼与中立帧差异 0 像素；开/闭眼 PNG 和眼部放大图均在该目录。这是透明度驱动的首轮眨眼，还没有雕刻闭眼眼睑形状、其他表情或正式 VN 接入。验收脚本为直接设置参数临时禁用 `CubismParameterStore`，并推进两次 SDK 双缓冲更新后读图。

2026-11-15 静态基线：Cubism FREE 版生成单张 2048×2048 纹理集，按 SDK 5.0 兼容选项导出 `suiyue_head_prototype.moc3`、`suiyue_head_prototype.model3.json`、`suiyue_head_prototype.cdi3.json` 与 `suiyue_head_prototype.2048/texture_00.png`。模型 JSON 引用均存在。将文件复制到隔离 Unity 工程后，SDK 5-r.6.1 自动生成 Prefab；`src/SuiyuePrototypeReview.cs` 在 Unity 6000.5.8f1 Play 下读回 70 个 Renderer、27 个参数和 2560×1440 透明画面。修正双缓冲与参数恢复干扰后重测，双眼开闭参数为 0 时仍与中立帧差异 0 像素，确认旧导出尚无闭眼关键形。结果见 `unity_acceptance/result.json` 与 PNG。

2026-11-13 实机进展：在 Cubism Editor 5.3.05 beta1 FREE 版中导入本目录的 `suiyue_head_import.psd`，画布可见完整立绘与分件树，另存为 `suiyue_head_prototype.cmo3`。两只虹膜的剪贴 ID 已分别设为同侧眼白 `eye_R_sclera_complete`、`eye_L_sclera_complete`，Inspector 读回图在 `../cubism_eye_rig_review/right_clip_set_20261113.png` 和 `left_clip_set_20261113.png`。这是可编辑的导入原型；参数关键形尚未完成。下方参数和动作步骤仍待执行。

用 `suiyue_head_import.psd` 建模。它从已校色的 83 层工作稿提取 70 个可见分件和 8 个文件夹，保留原画 992×1586 坐标。文件为 RGB、8 位每通道，嵌入 sRGB。导入稿使用独立完整虹膜。眨眼闭合参考保留在上一轮的 `../suiyue_layers_refinement_20261106/face_state_review.png`。

## Editor 首轮步骤

1. 在 [Cubism Editor 官方下载页](https://www.live2d.com/en/cubism/download/editor/) 下载 Windows 稳定版，按 [官方安装说明](https://docs.live2d.com/en/cubism-editor-manual/cubism-editor-activation/) 阅读并接受许可后安装。
2. 在 Modeling 工作区打开 `suiyue_head_import.psd`，保存可编辑的 `suiyue_head_prototype.cmo3`。导出运行时文件时选 SDK 5.0 兼容选项；已用本机 SDK 5-r.6.1 做隔离 Unity Play 验证。
3. 在两只虹膜的 ArtMesh Inspector 中分别填入同侧眼白的实际 ArtMesh ID 作为 Clipping ID。左、右对应关系见 `preflight.json`。这里的 R/L 是角色自身方向。把遮罩后的中立帧与 `neutral_masked_2560x1440.png` 对照。
4. 把脸、耳、眉眼及前发分件纳入头部变形组。保留颈部与身体为独立层。首轮只做小幅转头，在中立和左右小角度检查额角、耳前及颈部的遮挡。前发各层按来源命名可在清单中检索，发根随头部、发梢渐进位移。
5. 验证以下参数，闭眼后再回到中立，视线到极限时检查虹膜有无溢出。保存并重开 cmo3 后重复检查，再导出 `.moc3`、`.model3.json` 与贴图供 2560×1440 Unity 验证。

| 动作 | 参数 | 第一轮关键值 |
|---|---|---|
| 轻转头 | `ParamAngleX` | −10、0、+10 |
| 左右眨眼 | `ParamEyeLOpen`、`ParamEyeROpen` | 0、0.5、1 |
| 水平与垂直视线 | `ParamEyeBallX`、`ParamEyeBallY` | −1、0、+1，先限制实际位移 |

实际关键形使用 [Cubism 标准参数](https://docs.live2d.com/en/cubism-editor-manual/standard-parameter-list/)。先用小动作定位原稿残留的耳前接缝，模型的动作范围根据实测决定。SDK 导出流程见 [Data for Embedded Use](https://docs.live2d.com/en/cubism-editor-manual/export-moc3-motion3-files/)。

## 已验证范围

已重开 PSD 验证图层名、颜色模式、分件数量和两侧虹膜遮罩关系。离线恢复眼睛遮罩后，中立合成与校色稿在 8 位 RGB 上最大差 1，alpha 一致。Editor 导入、cmo3、两侧剪贴 ID、双眼透明度关键形和隔离 Unity 往返眨眼 Play 已完成。剪贴边界的运动检查、闭眼眼睑形变、头部/嘴部动作与正式 VN 验证待完成。
