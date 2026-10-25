# ATO研究开发运行器

本目录是开发平台，正式测试协议尚未冻结。直接链接现有纯内核和研究控制器，不读取Unity/PlayerPrefs或经济存档。

```powershell
dotnet build research/ato/AtoResearch.csproj --ignore-failed-sources
dotnet research/ato/bin/Debug/net6.0/AtoResearch.dll
```

默认生成带UTC时间的新开发目录，执行两工况检查与帧分组回放。追加输出目录和`--grid`执行开发网格：

```powershell
dotnet research/ato/bin/Debug/net6.0/AtoResearch.dll research/ato/results/new-development-run --grid
```

不覆盖已有结果。正式发布样本前先冻结初态、工况表、模型/算法版本、成功与失败定义及数据分析脚本。当前开发种子1000/1001已用于调试，不能再作为独立测试集。

模型20ms步进，每5步更新控制；人工准备快照充风300秒，之后各策略从同一快照开始900m/16m/s任务。停车为速度绝对值<0.01m/s持续100步，成功容差±5m；300秒不停车计失败。±5m只是开发验收参数，不是行业停车精度标准。

保留原始v1–v3失败/调试输出；v4开始保存源码快照、完整配置和初态，作为可复现开发记录。结果包括270条轨迹（5方法×54同条件配对），不能当作270个独立重复。详情见[接口、公式、已执行验证和边界](../../docs/for-ai/ATO_RESEARCH_IMPLEMENTATION.md)。
