# PR #6 审查修复与合并核验（2026-10-26）

## 仓库与分支

编辑前验证：origin=https://github.com/NMDX0721/RailwayRenaissance.git，当前分支codex/research，HEAD=fe849c4298d2fefd2454bcaf6902c353d29c5c0c，与GitHub PR #6远端head完全相同；初始工作树干净。目标main=da144408e0ec5ea2ca4ef9e71aa1f3a5c6a3ffb3。

GitHub原状态为CONFLICTING。核查发现三条运行时修复已由main提交d36602ce4488ff1a24e8368ba85309f12aea53bc实现，故本轮同步已有修复，不重复写第二套运行时逻辑。

## 三项反馈

1. **制动联锁阻止退牵引档**：CabEngineState.SetThrottle仅在请求档位高于当前档位时检查增加牵引的联锁；降低档位或归零仍可执行。新增实际手轮逐档回零、独立制动/停放制动下拒绝加档、缓解后仍为零的回归。
2. **灯光面板点击穿透**：室内/室外灯光组件共用绘制矩形与BlocksPointer，可见且启用才阻挡；CabContextInteraction.OverOtherUI查询这两个组件。新增鼠标主绑定、中心瞄准时分别点击两个面板、检查真实输入分发未改变制动的回归，并保留未覆盖区域可交互检查。
3. **停机后不能回中立**：动作可用性先计算请求的换向目标，停机只禁止进入前进/后退位，允许回Neutral。新增从Forward和Reverse分别停机、回中立、确认停机时不能选行驶方向、实际重新启动的回归。

无需澄清的未决设计事项；三条均可落实，且目标分支已提供最小运行时修复。

## 冲突处理与范围

将main合入PR分支，冲突文件选用main已整合且更新的实现、资源与文档。解决冲突后、追加本轮检查前，暂存树与main完全一致：旧分支功能已由目标分支整合或更新，无需把旧轮组、旧驾驶接口或旧场景覆盖回去。未手改Unity场景YAML，未变更车辆动力学、经济、剧情或UI机制。

本轮新增差异限于两份现有编辑器回归、此记录、代码地图条目和验收输出。主分支已有ATO和后续驾驶功能保留。编辑器自动导入产生的资源/meta/项目设置变化不纳入提交。

## 验证入口

Unity6000.5.8f1，独立PR工作树；入口NF5KContextInteractionReview.BatchPR6。打开已提交CabSimulationReview，不重建或保存场景；同时执行既有内核、实际射线、启动/充风、运行/换向联锁、制动、checkpoint、设备及交互回归。

首轮进程退出0，三项PR6标记与NF5K_SIMULATION_PLAYMODE_OK全部出现。首轮冷启动另有UnityEditor.Search.SearchDatabase内部索引异常，堆栈不在项目源码中，单独保留日志；最终复跑结果见后续追加记录，不能将首轮称为全日志无异常。

AI辅助：Codex核对源码、处理分支冲突、补充回归并执行本地验证。未生成实验数据或新增算法结论。PR按仓库要求使用squash方式合入main；远端最终状态另行核验。

最终复跑：tools/compile/pr6_review_verified_play.log，进程退出0，PR6_THROTTLE_INTERLOCK_OK、PR6_LIGHTING_POINTER_OK、PR6_STOPPED_REVERSER_OK及NF5K_SIMULATION_PLAYMODE_OK均通过，无项目C#编译错误。UnityEditor.Search.SearchDatabase索引异常在复跑中仍出现，堆栈仅在Unity编辑器搜索模块；不把它掩盖为无异常日志，也不扩展本次PR修改编辑器缓存/工具链。三条审查涉及的实际行为断言均完成。
