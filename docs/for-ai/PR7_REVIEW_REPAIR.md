# PR #7 合并冲突与审查修复

## 合并范围

在 `codex/tablet-system-ux` 上将最新 `origin/main` 合入，保持分支历史，不改写已推送提交。编辑前工作区干净，GitHub 明确报告 `CONFLICTING / DIRTY`。冲突涉及 13 个源码、编辑器回归和说明文件；采用 main 更新的驾驶/检查点/照明修复及其回归，保留本分支平板实现和 T315 派单历史。代码地图保留 main 最新内容并补回平板入口，未手改场景 YAML。

## 五条反馈

1. **V2 备份不完整仍删除现有存档**：Unity 会把缺失的空集合字段补成空列表，因此使用非空哨兵配合 FromJsonOverwrite 检出缺失字段。验证要求创建时间、固定档案列表、手动书签索引与列表存在，固定键集合必须与白名单完全相同；所有校验在首次存储写入前完成。合法的完整空快照仍可精确删除已存在档案，V1/旧文本兼容路径不变。
2. **计算器回收丢失待算操作**：引擎保存于任务 AppState，重建时绑定同一个实例并立即绘制当前值/表达式。视图挂载/卸载订阅或解除事件，避免引擎持有已回收控件。主动关闭任务仍清空引擎。
3. **停机后换向器无法回中立**：保留 main 已实现的目标位置判断；停机允许回 Neutral，禁止挂入行驶方向。既有上下文 Play 回归覆盖前进/后退两方向及重新启动。
4. **暂停保存使用过时命令**：保留 main 在暂停捕获检查点前 ReadCommands 的实现，不推进 Tick。既有 ATO Play 回归覆盖暂停手动紧急制动、保存/恢复及零牵引保持。
5. **运动后车内光照分区错乱**：保留 main 将接收器局部包围盒中心及灯位置变换到车体坐标的实现，避免平移/转弯改变客室分区与纵向衰减。

## 验证

新增 `TabletInteractionReview.Batch` 回归：版本号孤立对象、缺失固定键/列表/索引必须失败且存储写入次数为零；完整非空/空快照往返；计算器输入 `12 + 3`，后台临界回收后继续计算为 `15`，关闭后重开为 `0`。实际运行结果及未验证边界在执行后追加。

最终四项进程均退出 0，无项目 C# 编译错误：

- `tools/compile/pr7_tablet_final.log`：`TABLET_INTERACTION_REVIEW_OK` 含 `calculator=1 backup_completeness=1`。
- `tools/compile/pr7_diffuse_final.log`：`PR7_DIFFUSE_LOCAL_FRAME_OK`，实际 Renderer 的客室/驾驶室 SH 系数在平移至 `(31,2,-100)` 并偏航 63°、关灯/再开灯后保持分区和衰减；这是数值回归，不代替最终画面审阅。
- `tools/compile/pr7_context_final.log`：`PR6_STOPPED_REVERSER_OK`，实际 Play 中从两个方向停机、实体换向回中立、重新启动均通过。
- `tools/compile/pr7_ato_graphics_verified.log`：`ATO_PAUSED_MANUAL_CHECKPOINT_OK`、`ATO_V2_PLAY_ACCEPTED`、`ATO_PLAY_ACCEPTED`，暂停手动紧急制动与零牵引经过保存/恢复保持，未推进物理 Tick。

环境边界：初次包解析因 Unity CDN 连接重置中断，未执行测试；临时复用主工程 Burst 1.8.30 同版本本地包后完成以上验证，随后移除临时包并还原依赖锁。ATO 的无图形 Play 曾在实时反射初始化中原生崩溃（`pr7_ato_final.log`），改用图形批处理后通过。Unity 自动改写的材质、渲染管线资源及临时检查点不纳入修复。Windows/Android 真机触控、整体 UI 图形与实际音频仍不在本轮验收范围内。
