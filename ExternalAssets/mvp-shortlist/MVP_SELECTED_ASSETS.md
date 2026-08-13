# RailwayRenaissance MVP 精选素材清单

这份清单只保留当前第一可玩切片最值得先导入 Unity 的素材，目标是：**够用、清爽、不把工程弄乱**。

位置：`D:\Unity Project\RailwayRenaissance\ExternalAssets\mvp-shortlist`

## 1. 地图 / 场景

### `map/tiny-town_tilemap_packed.png`
来源：Kenney Tiny Town (CC0)

用途：
- 作为当前俯视角小站地图的主 tileset 占位
- 可先拿来拼：草地、道路、建筑边缘、小镇地表

建议优先级：**最高**

### `map/tiny-town_preview.png`
用途：
- 仅作为人工预览参考
- 帮你在导入前快速确认整体风格

## 2. UI

### `ui/button_primary.png`
用途：
- 主按钮底图
- 可用于：结束当天、确认、主要操作

### `ui/button_secondary.png`
用途：
- 次级按钮底图
- 可用于：切换方案、辅助操作

### `ui/input_rectangle.png`
用途：
- 输入框 / 面板内条目底图
- 后续若做任务栏、文本输入、数值框会用得上

### `ui/slider_horizontal.png`
用途：
- 滑条 / 进度条占位
- 后续适合做：车况、信任、客流可视化

### `ui/icon_checkmark.png`
用途：
- 完成 / 已确认 / 正向反馈图标

### `ui/icon_cross.png`
用途：
- 取消 / 失败 / 风险提示图标

## 3. 音效

### `audio/ui_click.ogg`
用途：按钮点击

### `audio/ui_switch.ogg`
用途：方案切换

### `audio/ui_confirm.ogg`
用途：结算确认 / 完成反馈

### `audio/ui_error.ogg`
用途：非法操作 / 风险提示 / 阻塞反馈

## 4. 列车占位

### `train/train_placeholder.png`
来源：OpenGameArt 列车占位图

用途：
- 当前项目里最先补上的列车视觉占位
- 适合先让画面里“有火车”
- 不建议长期作为最终主美术

## 当前最推荐先导入 Unity 的 6 个文件

如果你只想先导入最小够用的一批，就先导这 6 个：

1. `map/tiny-town_tilemap_packed.png`
2. `ui/button_primary.png`
3. `ui/button_secondary.png`
4. `audio/ui_click.ogg`
5. `audio/ui_confirm.ogg`
6. `train/train_placeholder.png`

## 不建议现在就导入的内容

以下内容先别一口气全导：
- starter-pack 里的整包 UI 颜色变体
- 大量重复按钮样式
- 全部 interface sounds 音效
- Tiny Town 的全部单独 tile 小图

原因：
- 现在还是原型期
- 先让当前切片能玩、能看、能有反馈
- 晚点再扩素材库，不然 Unity 工程很快会乱

## 下一步建议

### 如果继续偏玩法
下一步应该把这些素材接到：
- 小站主场景 Tilemap
- 底部按钮皮肤
- EndDay / 切换操作音效
- 列车占位图

### 如果继续偏美术整理
下一步可以再单独筛：
- 站务服务用图标
- 对外事务用图标
- 人物/NPC 占位 sprite
- 更适合铁路题材的建筑或工业风 tile
