# 歌词时间校准复核包

所有时间仍为自动证据；模型一致不等于听音通过。原稿不删改；南风39句已接入明确标注待听校的自动校准稿。

## Whisper 局部交叉诊断（历史轮次）

|曲目|原稿行数|局部双模型边界争议|
|---|---:|---:|
|south_wind|39|6|
|starlit_rails|47|22|
|chollima_ride|49|20|
|sleepers|34|17|

## 最新音素复核与自动发布门槛

失败数为检查项数，不等于句数；没有人工听校通过声明。

|曲目|自动门槛未通过项|处理|
|---|---:|---|
|south_wind|0|39句自动校准逐行稿已接入，待听校|
|starlit_rails|7|新音素候选未发布，保留既有阅读/草稿|
|chollima_ride|3|新音素候选未发布，保留既有阅读/草稿|
|sleepers|18|新音素候选未发布，保留既有阅读/草稿|

南风疑点片段（原曲裁取，未变速；时间列是原曲区间）：

- 第2句，原曲 12.12–20.20s：작은 간판 하나；[试听](south_wind-line-02.mp3)
- 第5句，原曲 26.88–34.88s：학생들이 줄 서서；[试听](south_wind-line-05.mp3)
- 第19句，原曲 67.38–73.20s：점원이 속삭였죠；[试听](south_wind-line-19.mp3)
- 第31句，原曲 98.84–104.86s：초상화도 모른 척；[试听](south_wind-line-31.mp3)
- 第36句，原曲 133.46–140.74s：통일 도시락...；[试听](south_wind-line-36.mp3)
- 第38句，原曲 141.30–148.65s：왠지...；[试听](south_wind-line-38.mp3)

starlit_rails 最新未通过检查的原曲片段（starlit_rails.local.sections.provider.boundaries，不是已听校结论）：

- 第4句，原曲 33.96–41.70s：우리를 둘러쌌지；[试听](starlit_rails.local.sections.provider.boundaries-line-04.mp3)
- 第14句，原曲 69.82–77.20s：다시 밝아와；[试听](starlit_rails.local.sections.provider.boundaries-line-14.mp3)
- 第26句，原曲 130.88–139.18s：알고 있어요；[试听](starlit_rails.local.sections.provider.boundaries-line-26.mp3)
- 第41句，原曲 181.34–191.26s：인간은 이걸 '위로'라고 부르죠；[试听](starlit_rails.local.sections.provider.boundaries-line-41.mp3)
- 第46句，原曲 212.82–225.02s：Mmm... mmm...；[试听](starlit_rails.local.sections.provider.boundaries-line-46.mp3)

chollima_ride 最新未通过检查的原曲片段（chollima_ride.spoken.local.sections.allsections.provider.boundaries，不是已听校结论）：

- 第17句，原曲 65.62–73.48s：천리마 신시대에；[试听](chollima_ride.spoken.local.sections.allsections.provider.boundaries-line-17.mp3)
- 第42句，原曲 160.88–167.50s：100전 100승!；[试听](chollima_ride.spoken.local.sections.allsections.provider.boundaries-line-42.mp3)
- 第44句，原曲 187.30–201.36s：아—— 천리마——；[试听](chollima_ride.spoken.local.sections.allsections.provider.boundaries-line-44.mp3)

以上试听片段仍保留历史疑点，南风最新时轴见Assets/Resources/Music/Lyrics/south_wind.txt及其info证据索引。

