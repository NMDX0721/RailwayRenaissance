# 岁月眼部重建

当前 cmo3 为实验候选，不能作为美术完成版。用户指出画面右侧眼珠仍与眼眶不协调。

## 高清效果候选

面部精修版为 `suiyue_face_polish_candidate.cmo3`。`tools/vn/polish_suiyue_face_layers.py` 在虹膜比例版的 1984×3172 画布上修改 18 个现有层。鼻口重画后在底脸补齐旧切片边缘，采用小鼻尖暗面、淡鼻梁侧影、断开的闭口笑线和下唇浅色。保留原面部比例，增加刘海投影、暖色腮红、更明确的颈部暗面、下睫毛、虹膜上方暗部和次高光。画面右眼的眼白与上下眼睑横向收窄约 5%，眉色调整为暗灰紫。PSD 重新打开后验证了层位置、顺序、剪贴、可见性和透明度。将 PSD 替换到已有 Cubism 模型并另存，现有眼睛参数和变形器继续使用。眉区、下颌和领口仍可见部分来自拍摄前就存在的素材拼接细边。[Cubism 精修前后](../../cubism_eye_rig_review/face_polish_cubism_comparison.png)、[PSD 精修对照](../../cubism_eye_rig_review/face_polish_psd_comparison.png)。

在 Cubism 检查精修版正视、左右扫视、半闭、全闭及恢复睁眼。扫视时虹膜受同侧眼眶剪贴，全闭未见虹膜残留。[精修版动态参数截图](../../cubism_eye_rig_review/face_polish_motion_review.png)。

美术参考 `suiyue_face_polish_art_reference.png` 由内置 imagegen 编辑模式生成，输入为同姿势的旧版 Cubism 画面与用户提供的全身立绘。参考图用于对照光影与笔触，模型以原有分层逐层调整。

<details>
<summary>面部参考图生成提示</summary>

Edit the first reference (close crop of Suiyue face, neck and collar) into a clean finished Japanese cel-animation style paintover for a Live2D artist. Second reference (original full-body Suiyue) shows identity and intended palette; preserve the first image's framing, head tilt, hair silhouette and bangs, face proportions, neutral smile, iris size and gaze, collar and jacket design. Keep eye shapes spatially aligned with the base, with the eye on SCREEN RIGHT slightly narrower as the farther eye, still centered and aligned to other gaze. No pixel art, clean finely antialiased high-resolution illustration. Remove the thin white tracing/slice outlines all over the image, especially the squared nose and mouth outlines; these are production artifacts. Nose should be a tiny warm triangular penumbra at the tip with a short tapered mark and a very faint side-of-bridge tone, minimal highlight. Build a more expressive small closed smile: subtle darker little mouth corners, short soft break at the center line, a very delicate peach shadow under the lower lip. Keep mouth closed. Add a clear but subtle cel-shadow directly under the foreground bangs across forehead and temples, soft pale warm apricot blush on both cheeks, clean harder edge on chin-cast shadow across the neck with a warm muted tone. Add a thin light lower lid contour with two restrained lower-lash marks, iris top gradient cast shadow and one tiny secondary highlight, primary glint retained. Brows should be muted deep gray-purple matching hair, lightened a touch. Keep neck anatomy, clothing, colors and facial identity. Do not introduce new graphics or writing. Natural illustrative color with minimal texture and no airstreak overlays.

</details>

虹膜比例版为 `suiyue_iris_fit_candidate.cmo3`。通过 `tools/vn/fit_suiyue_iris_height.py` 改的是完整虹膜纹理。以 PSD 中 alpha ≥ 128 的像素计，画面左眼由 78 px 压到 56 px，右眼由 78 px 压到 52 px。黑色瞳孔各自绕中心压短约 12%，保持独立层。重新读取 PSD 验证只有两侧虹膜和瞳孔四层像素变化，层坐标、顺序和剪贴标记保持一致。用该 PSD 替换已有模型的校色素材，保留其眼珠倾角与动作键形。[PSD 无剪贴和正常剪贴对照](../../cubism_eye_rig_review/iris_height_comparison.png)。

眼珠角度版为 `suiyue_eye_skin_tilt_candidate.cmo3`。`Eye_L_IrisTilt`（画面右眼）和 `Eye_R_IrisTilt`（画面左眼）分别绕各自眼珠中心追加约 5.5°、5.9° 的顺时针旋转。每个变形器带动同侧虹膜、瞳孔和高光，眼白、上下眼睑、剪贴和已有参数键形继续沿用。重开保存文件的同哈希副本，隐藏编辑器手柄，检查正视、左右视线极限、半闭、全闭和恢复睁眼。未见可见虹膜越过眼睑或闭眼时残留。[Cubism 参数对照](../../cubism_eye_rig_review/tilt_mask_and_motion_review.png)。这次调整的是静止姿势的倾斜，动态头部转动尚未联动。

肤色源稿由 `tools/vn/calibrate_suiyue_hd_skin.py` 生成，输出 `suiyue_fullbody_hd_skin_candidate.psd`。根据全身参考的肤色取样，微调脸部亮面、画面右耳和颈部，颈部阴影与亮面采用不同校色幅度。重新打开 PSD 后对照所有图层，位置、透明度、alpha 和剪贴标记相同，眼部纹理未发生变化。在 Cubism 上替换原 PSD 并另存为 `suiyue_eye_skin_review_candidate.cmo3`，同坐标取样确认颈部亮面 RGB 从 252/222/204 变为 249/206/190，参考取样为 251/204/188。已复查半闭、全闭和左右扫视并恢复正视。[面部对照](../../cubism_eye_rig_review/eye_skin_comparison.png)、[全身对照](../../cubism_eye_rig_review/eye_skin_full_comparison.png)、[动态参数截图](../../cubism_eye_rig_review/eye_skin_motion_review.png)。

全身参考复核版为 `suiyue_natural_eye_spacing_candidate.cmo3`。在紧凑版基础上微调双眼眼眶位置和高低差，画面右眼（角色左眼）的虹膜、瞳孔和高光通过 `Eye_L_GazeCenter` 单独定中，取眼眶开口和双眼注视方向为依据。全身参考用于核对轮廓和姿态。对照见[全身](../../cubism_eye_rig_review/natural_full_comparison.png)和[面部](../../cubism_eye_rig_review/natural_eye_spacing_comparison.png)。已在 Cubism 检查半闭、全闭和横向视线两端，[动态参数截图](../../cubism_eye_rig_review/natural_eye_motion_review.png)。原立绘拼接边缘在模型放大时仍可见，尚未验证 Unity。

眼睛大小和间距调整版另存为 `suiyue_compact_eye_rig_candidate.cmo3`。左右各以一个父级弯曲变形器统一调整眼白、虹膜、瞳孔、高光和上下眼睑，眉毛留在各自的原位置。双眼缩小约 8–9%，中心距收窄约 8%。在 Cubism 中复查睁眼、半闭、全闭和横向视线两个极限，见[动态参数截图](../../cubism_eye_rig_review/compact_eye_rig_review.png)。恢复至睁眼和中性视线。原模型仍可用于对照。组装脚本的 `--compact-eyes` 可另行生成同方向的 76 层 PSD 比例参考，当前 CMO3 使用已有模型的变形器保留绑定。

`tools/vn/assemble_suiyue_hd_eye_model.py` 将两组眼部定位到 1984×3172 的整身画布，保留原眉毛，移除被替代的旧眼部层。输出 76 层 `suiyue_fullbody_hd_eyes_candidate.psd`。在 Cubism FREE 版创建并保存了独立的 `suiyue_fullbody_hd_eyes_candidate.cmo3`。已在 Cubism 中将每侧虹膜、瞳孔和高光指定到同侧眼白进行剪贴。睁眼横向三档视线检查见 [Cubism 视线截图](../../cubism_eye_rig_review/hd_cubism_gaze_review.png)。

左右眼独立绑定开合。闭眼时缩合并下移眼白网格，遮住虹膜、瞳孔和高光，同时淡出下眼睑。上睫毛设置了半闭关键形。已在 Cubism 检查睁开、半闭、全闭和重新睁眼，见 [Cubism 截图](../../cubism_eye_rig_review/hd_cubism_blink_review.png)。全闭画面无可见虹膜残留，半闭时外眼角仍可细修。放大仍可见原立绘部分拼接边缘。垂直视线、随头部转动、重开验证和 Unity 导出待核验。

2026-11-18 已生成 `suiyue_eyes_hd_parts.psd`。两只眼各有眼白、虹膜、瞳孔、高光及上下眼睑，共 12 层。虹膜、瞳孔及高光保留完整纹理，PSD 内按同侧眼白剪贴。分层稿以 1683×935 的脸部参考图坐标保存。`tools/vn/build_suiyue_hd_eye_layers.py` 生成的视线与眨眼对照图用于离线审查，随后以整身候选在 Cubism 内完成了上述绑定。

`suiyue_eye_rebuild_hd_reference.png` 为内置 imagegen 编辑模式生成的整脸重绘参考。提示重点：保持身份、脸部姿态、紫灰虹膜和微笑，修复两眼视线与透视，上下眼睑自然遮挡虹膜，恢复细下眼睑和暖肤过渡，去除外眼角硬灰块、白贴片和编辑器标记，清晰平滑非像素插画。该图也重新细化了脸部与睫毛，未验证全身衔接；尚未分层或导入 Cubism，不能作为 moc3 成果。

## 已确认缺陷

- 独立椭圆虹膜下缘几乎完整暴露，外观像贴在眼白上；下眼睑没有有效遮挡虹膜。
- 将所有暗色像素洗白会连同下眼睑灰粉色边缘一起删除；恢复原层又带回外眼角大灰块。两种候选均未通过用户验收。
- 原画眼珠只有约 29×41 像素；400% 编辑器放大显示不能作为高清素材交付。
- PSD 内移动纹理曾超过旧 ArtMesh，造成竖直截断。恢复源图坐标并移动整体网格消除了该截断，但不等于眼部造型通过。

## 重建要求

先以完整脸部重绘确认两眼一致的视线和透视，保留角色身份、头部姿态、紫灰虹膜及厚上睫毛。上眼睑遮住虹膜上缘，下眼睑自然遮挡下缘；眼白为自然杏仁形，外眼角没有硬灰块。保持清晰平滑的非像素插画。

高清重绘是美术参考，不能直接宣称为可动模型。分层须保留干净底脸、眼白、完整虹膜、独立瞳孔、上下眼睑及高光。现有模型移动和旋转只编辑了睁眼关键值；需核验半闭眼与闭眼、视线边界、重开 cmo3 和 Unity 画面。高清源图不应再无条件缩回 29×41。
