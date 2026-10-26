# 驾驶内核可行性探针

用途：第一阶段核实原始源码能否脱离Unity编译、重放，以及对制动/质量/坡度/黏着变化是否有可测响应。没有实现自动停车策略，不是正式统计实验，也不是实车验证。

在仓库根目录执行：

```powershell
dotnet build docs/research/probes/AuditProbe.csproj --ignore-failed-sources
dotnet docs/research/probes/bin/Debug/net6.0/AuditProbe.dll
```

已在.NET SDK 6.0.428 / Runtime 6.0.36执行。`smoke_results.json`为当次真实输出。共10条记录，包含一条重复基准；重复不是独立样本。运行结束后另一个新进程输出完全一致。程序直接链接游戏源码；源码后续变化时须重新构建、核对哈希，不能继续引用旧结果。

数值为自建集总模型响应，不是NF-5K标定数据。程序只读游戏文件，不访问Unity或玩家存档。配置与工况解释见[研究评估](../literature_and_direction_review.md)。探针由Codex辅助编写并执行，保留源代码供学生复核。
