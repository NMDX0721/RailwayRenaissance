# 原声自动对齐证据汇总

自动指标用于定位疑点，不能代替听音核对；不修改游戏资源。

|候选|行数|平均原文字符召回率|最低召回率|异常|
|---|---:|---:|---:|---|
|base\chollima_ride.review.json|49/49|0.563|0.000|low_asr_agreement: 23, zero_duration_word: 7, invalid_interval: 2|
|base\sleepers.review.json|33/33|0.694|0.000|zero_duration_word: 11, low_asr_agreement: 10, invalid_interval: 1, long_line: 1|
|base\sleepers.sleepers.performance.anchored.review.json|35/35|0.971|0.839|zero_duration_word: 1|
|base\sleepers.sleepers.performance.review.json|35/35|0.826|0.000|zero_duration_word: 9, low_asr_agreement: 5, long_line: 2|
|base\south_wind.review.json|44/44|0.581|0.000|low_asr_agreement: 23, zero_duration_word: 7, invalid_interval: 4|
|base\starlit_rails.review.json|52/52|0.628|0.000|low_asr_agreement: 24, long_line: 1, invalid_interval: 7, zero_duration_word: 7|
|small\chollima_ride.authored-prompt.review.json|49/49|0.635|0.000|low_asr_agreement: 20, invalid_interval: 1, zero_duration_word: 6|
|small\chollima_ride.review.json|49/49|0.635|0.000|low_asr_agreement: 20, invalid_interval: 1, zero_duration_word: 6|
|small\chollima_ride.window-refined.review.json|49/49|0.635|0.000|low_asr_agreement: 20, invalid_interval: 1, zero_duration_word: 6, invalid_interval_after_refinement: 1|
|small\south_wind.authored-prompt.review.json|39/39|0.865|0.000|low_asr_agreement: 4, zero_duration_word: 1|
|small\south_wind.review.json|44/44|0.793|0.000|low_asr_agreement: 8, zero_duration_word: 6, invalid_interval: 3, long_line: 1|
|small\south_wind.window-refined.review.json|44/44|0.793|0.000|low_asr_agreement: 8, zero_duration_word: 6, invalid_interval: 3, long_line: 1, invalid_interval_after_refinement: 3|
|small\starlit_rails.authored-prompt.review.json|47/47|0.804|0.000|low_asr_agreement: 11, zero_duration_word: 5|
|small\starlit_rails.review.json|52/52|0.742|0.000|low_asr_agreement: 16, zero_duration_word: 9, invalid_interval: 6|
|small\starlit_rails.window-refined.review.json|52/52|0.742|0.000|low_asr_agreement: 16, zero_duration_word: 9, invalid_interval: 6, invalid_interval_after_refinement: 6|

各候选的原音频哈希及独立识别/强制对齐详情见对应 review.json；不同模型的一致不构成人工听校。

## 原稿与独立识别差异

以下为全曲单调匹配，区别于上表的局部时间窗口比较；包括低文字召回、压缩时间和零时长识别词。

|曲目|模型|需核对行数|
|---|---|---:|
|chollima_ride|base|24/49|
|south_wind|base|21/44|
|starlit_rails|base|19/52|
|chollima_ride|small|20/49|
|south_wind|small|15/44|
|starlit_rails|small|17/52|
