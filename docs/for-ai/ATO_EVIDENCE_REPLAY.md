# ATO 冻结证据复现工具

2026-11-01新增独立 Python 入口 `research/ato-evidence/replay_followup.py`。输入为V3末端衰减反馈历史运行目录和不存在的新输出目录，调用原冻结.NET运行器，仅增加完整命令及终态文件保存。使用原协议归档核验、记录修改后归档、比对全部新旧指标及原始字节指纹；失败保留输出，不覆盖旧数据。

实测108条与历史结果逐条一致，详见 [恢复记录](../research/ato_evidence_resume_20261101.md)。工具不修改Unity控制器、物理内核、场景或默认算法；这是回归复现，不是新测试或性能改善证据。现阶段只支持 `study_schedule_taper_holdout.py` 对应归档。

## 单变量开发实验（2026-11-01）

`research/ato-evidence/taper_ablation.py freeze|run OUTPUT`：在已有开发工况上比较原V3、固定0.04增益/0.5m/s上限反馈、相同反馈加末端衰减。冻结最终生成的三个控制器源码字节、运行器和方案后才执行。独立 `verify_ablation.py OUTPUT` 从保存文件核验源码、完整命令/终态指纹、重复、配对初态与成功判据。它不重新执行物理内核，也不能仅由终态文件重算全部轨迹指标。

结果和解释见 [末端衰减消融](../research/ato_taper_ablation_20261101.md)。研究工具保留在独立目录，不接入Unity默认控制器。

## 团队竞赛材料修订

2026-11-01用户确认团体方向，三名成员及40/35/25比例保留，最终不提交。`research/ato-evidence/build_competition_revision.py` 从保存结果生成匿名报告、图表附注及文献/AI工作底稿；入口见 [修订清单](../research/competition_revision_20261101/README.md)。查新和AI材料尚待官方模板及本人核对，不能视为已完成正式附件。工程控制器和冻结实验不因申报文案修改。

## 末端专项诊断

`terminal_diagnostics.py`从已冻结开发源码生成仅加观测的副本，记录50Hz物理状态和每控制周期目标速度/参考时间，9例全部旧指标及命令/终态指纹一致。`analyze_terminal.py`可独立核验归档与文件指纹并生成分段时间账本和图。未修改默认控制器，未增加未知测试样本。详见[机制分析](../research/competition_revision_20261101/机制分析升级.md)，内部训练另见[强化作业](../research/competition_revision_20261101/专项强化作业.md)。`build_specialist_pack.py`生成两份本地PDF，内部作业含姓名不得当匿名附件上传。

## PR9驻留约束复核与修复候选（2026-11-02）

`audit_brake_dwell.py`确认冻结V3存在大量不足25Tick的制动切换，旧V2/V3比较不能称完全相同动作约束。`prepare_pr9_fix.py`和`prepare_pr9_hold_aware.py`仅在隔离副本准备补丁；最终候选使用状态版本3与合法驻留预测前缀，旧版本1保留原回放。`run_pr9_grid.py`先冻结再执行54次已查看工况回归，三模式各9/9成功、短驻留/干预/越速均0。`verify_pr9_grid.py`可只读复核完整输出。检查点构造改为成功后统一更新，并允许恢复旧非活动route-only存档。详见[源码审计与补丁](../research/pr9_audit_20261102/README.md)。当前PR和主工作区均未应用或推送补丁，Unity验收待执行。
