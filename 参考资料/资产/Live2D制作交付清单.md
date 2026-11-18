# Live2D 制作交付清单：岁月优先，嘉颖徐随后

2026-11-06用户调整顺序：岁月优先，嘉颖徐随后。岁月持平板完整定稿已进入首轮像素拆层；工作PSD与隐藏补画层见`tools/vn/suiyue_layers_20261106/`及[验证边界](../../docs/for-ai/VN_LIVE2D_LAYERING.md)。原画、分区PSD都不是模型成品；轮廓精修、补画审核、网格和变形绑定必须实际完成。

## 软件与组件

| 工具／组件 | 用途 |
|---|---|
| 支持分层 PSD 的绘图工具 | 整理统一基准、分层、补画遮挡；不限定品牌 |
| Live2D Cubism Editor | 网格、变形器、参数、物理、表情与模型导出 |
| Cubism SDK for Unity 完整官方包 | Core 原生库、Unity 框架、导入器、URP 渲染；只复制 GitHub 框架不足以运行 |
| MotionSync Plugin，可选 | 更细的预录日语口型；基础小样先用 AudioSource 驱动张口 |
| 本项目 VN 适配层 | 表情映射、站位、语音状态、道具阶段、回看恢复 |

官方：[导入 SDK](https://docs.live2d.com/en/cubism-sdk-tutorials/getting-started/)、[URP 配置](https://docs.live2d.com/en/cubism-sdk-tutorials/urp-import/)。项目为 Unity 6000.5.8；manifest声明URP 17.0.3，实际缓存为17.5.0。官方R6.1已在独立离线源码环境通过2.5K模型Play检查，普通UPM导入仍未解决。详见[接入验证记录](../../docs/for-ai/VN_LIVE2D_INTEGRATION.md)。

Unity 模型需要涉及的控制器包括 `CubismModel`、模型渲染控制器／URP Pass、`CubismUpdateController`、`CubismEyeBlinkController` + `CubismAutoEyeBlinkInput`、`CubismMouthController` + `CubismAudioMouthInput`、`CubismExpressionController`；动作与物理组件按需要配置。具体装配以 SDK 导入结果为准。这些组件已在独立评估工程中实际运行，尚未安装进正式VN。

官方：[眨眼设置](https://docs.live2d.com/en/cubism-sdk-tutorials/eyeblink/)、[口型设置](https://docs.live2d.com/en/cubism-sdk-tutorials/lipsync/)。

## 原画拆分

嘉颖徐按24岁年轻成年人固定脸型、小体积盘发；黑西装、白衬衫、红领带、玫瑰胸花、珍珠耳饰保留。新基准为 `tools/vn/character_art_20261105/jiaying_young_master_candidate.png`，尚未分层。

建议 PSD 分组（项目交付约定，不冒称 SDK 强制图层名）：

```text
hair/back, fringe, side_L, side_R, bun
face/base, ear_L, ear_R, nose
eyes/L/white, iris, highlight, upper_lid, lower_lid
eyes/R/white, iris, highlight, upper_lid, lower_lid
brows/L, R
mouth/upper_lip, lower_lip, inner, teeth, tongue
body/neck, shirt, jacket, tie
arms/L_upper, L_forearm, L_hand, R_upper, R_forearm, R_hand
accessories/earring_L, earring_R, rose_corsage
props/bottle
```

眼眉嘴下面的脸底、刘海下面的额头、手臂后面的衣服都要补齐；道具不能和嘴或呼吸一起拉伸。所有部件在同一画布上对齐，不能为每个表情另造脸型和肩宽。

岁月另交无平板与持平板两套完整手臂／道具姿态。不能只隐藏平板、留下悬空的持握手。自定义 `ParamTabletPose`：0=无平板，1=持平板，需要建模人员实际制作，不由 SDK 自动生成。

## 参数与文件

基础参数：头部 `ParamAngleX/Y/Z`、左右眼开合 `ParamEyeLOpen/ROpen`、眼球 `ParamEyeBallX/Y`、嘴 `ParamMouthOpenY` / `ParamMouthForm`、呼吸 `ParamBreath`。小样先做小幅度，确保不破脸、不穿帮。必要表情先有 normal、serious、smile、gentle、curious，剧情实际使用的其他表情也需有数据或明确映射。

源文件放 `参考资料/资产/Live2D源文件/jiaying_xu/`：PSD、可编辑 `.cmo3`，有动画时交 `.can3`。

运行导出放：

```text
Assets/Live2DModels/jiaying_xu/
  jiaying_xu.model3.json
  jiaying_xu.moc3
  textures/*.png
  expressions/*.exp3.json
  motions/idle.motion3.json
  *.physics3.json   # 有物理时
  *.pose3.json      # 有部件姿态配置时
  jiaying_xu.prefab # SDK 正确导入配置后保存
```

岁月按 `suiyue` 同样交付。不要用空文件或改扩展名的 PNG 冒充模型。配置在 `Assets/Resources/VNPortraits/catalogue.json`，目前两角色均 disabled，不替换正式立绘。

## 接入与验收

1. 先用官方模型在独立场景验证 SDK、URP 透明与遮罩，再接嘉颖徐模型。
2. 用现有 `e` / `reactions` 映射动态表情，角色 ID 和书签坐标不变。模型经独立渲染输出接 UI Toolkit 的方案尚需实现和验证。
3. 口型读取台词真实 AudioSource；暂停、静音、停音和重放结束均闭口。旁白、男主不做说话口型。语音声源接口已准备，Cubism 口型桥已在官方模型通过测试，尚未绑定正式VN。
4. 平板阶段按当前剧本位置恢复；从后期回看早期也要无平板。不能用永久账号激活状态判断。
5. 主验收2560×1440，检查单／双／三人比例、眼高、眨眼、嘴部与发丝穿帮；随后做 Windows Player、Android 平板实机。

## 本轮实际状态

- 已建角色配置、表情映射、参数与文件约定、缺件检查。运行 `python tools/vn/live2d_readiness.py`，当前输出 `assetsReadyForImport=false`；正常退出只代表检查执行成功。
- `VNVoicePlayer` 已开放 `PlaybackSource`、`Speaker`、`IsAudible`，尚未连接实际 Cubism 模型。
- 岁月已按 `TABLET_SETUP` 前后的播放位置选立绘；之前只找无平板图片，缺表情回退无平板 normal。动态姿态仍待建模。
- 缓存Unity引用的主工程Roslyn源码编译和305项剧情阶段检查保留。新增官方模型独立Unity Play、2560×1440截图、真实音频口型及常态恢复检查通过；不代表正式角色或设备验收。
- 仍缺：**主工程普通SDK导入、分层PSD、可编辑角色模型与运行导出、VN动态展示适配器、正式角色及设备验收**。当前没有把静态 PNG 的位移缩放当作 Live2D 完成。
