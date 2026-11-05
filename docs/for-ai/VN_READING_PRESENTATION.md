# VN reading presentation — 2026-11-03

2026-11-04 visual polish. Speaker labels use size 32 and truncate only when the reserved name column overflows. Voice seek uses a rounded rail with a brass fill and a small cream playhead. The full-width rectangular bottom band has translucent warm tint and fine grain, visible around the inset dark capsule and its rounded ends. Single-line dialogue starts at the top of the text area. The 2560×1440 editor capture and review log are in `tools/vn/reading_outer_frost_20261104/`.

Final placement clarification: the progress bar is a root-level sibling **outside** the dialogue box, filling the band between the dialogue bottom and the screen baseline. There is no footer/backing layer or embedded progress inside the dialogue. Primary Windows review uses the full safe area `(0,0,2560,1440)`, not synthetic 32-pixel insets. Assertions require text bottom = bar top and bar bottom = screen bottom.

The dialogue panel has its own closed outline and rounded lower corners. The frosted background extends 24 units upward behind the panel and spans the full width beneath both side borders. The 18-unit desktop capsule sits inside a 26-unit bottom band with four units of frosted space above and below and 16 units on either side.

User sketch implementation: three icon-only expanded menu actions (40×40, 24×24 glyphs); bookmark to the left of the speaker; framed loudspeaker replay/scrubbing; 18-high rectangular progress body with separate left/right arc end-cap components and a train cursor; revised fast-forward glyph; 72×152 Japanese five-aspect atmosphere signal (switch only in Settings). Dialogue panel minimum height is 160. Auto/Menu retain 88×40 sizing. Latest user requirement: primary acceptance only at 2560×1440, other targets unified later.

Design, all 19 route mappings, preferences and audio delivery schema: [VN 阅读界面与章节线路](../../参考资料/系统/VN阅读界面与章节线路.md).

## Ownership

- `VNManager.Presentation.cs`: speaker-independent control wiring, playback lifetime, atmosphere signal, preferences. `VNManager.cs` binds after each displayed text line and clears on every transition/end/destroy, including history/save restore paths through ShowCurrentDialogue.
- `VNVoicePlayer.cs`: separate AudioSource, source/translation hashes, ready-translation requirement, locale/script catalogues, positive/negative resource caches; replay, seek, pause and volume. No provider API.
- `DialogueBox.Presentation.cs`: controls near speaker name, narration bookmarking, real voice transport, route hit testing. It preserves existing localization rendering.
- `VNRailwayArt.cs`: original UI Toolkit Painter2D glyphs, five-aspect A head based on viewed Nippon Signal diagram/Hankyu photo, 19 authored route profiles, scenery placeholders, milestones and train. Final scenic art must use the built-in Image tool in a subsequent art pass.
- `VNViewportLayout.cs`: safe-area positioning, route/text separation, reading target sizes for tablets.
- Tablet settings: signal switch, independent voice language and volume.

## Verification

Latest runtime source compilation: `tools/vn/reading_chrome_bottom_band_20261103/result.json` (206 source inputs, no compatibility overrides). `VNReadingPresentationReview.BatchPrimary` performs the latest full-frame 2560×1440 layout/voice/bilingual checks. The general `Batch` method also makes a 19-route gallery; those earlier renders predate the final placement.

`VNReadingPresentationReview.Batch` only runs inside `Library/VNCompiledReviewProject`. It writes a synthetic tone, reviewed translation and audio catalogue into that isolated project. No fixture audio or fake translation is added to production Resources. Latest checks use only 2560×1440 per user direction: icon-only menu with no signal action, original toolbar size, thin capsule geometry, bookmark input suppression, control overlap, safe area, voice playback/seek/pause/replay/Auto, stale source, absent audio, protagonist/narration/thought exclusions and settings preference toggle. Captures all 19 route profiles. Earlier four-viewport captures predate the compact capsule revision and are not acceptance for the final version.

Latest evidence: `tools/vn/reading_presentation_bottom_band_acceptance_20261103/verification.json`, `source_compile.json`, `unity.log`, main and dual-subtitle screenshots. Final log: `reading_presentation_20261103_bottom_band.log`. Earlier endcap, integrated-footer and four-viewport snapshots are historical and are not acceptance for the final placement.

Latest passed markers: `VN_READING_LAYOUT_OK windows_2_5k`, `VN_VOICE_TRANSPORT_OK`, `VN_COMPACT_BILINGUAL_OK 2560x1440`, final `VN_READING_PRESENTATION_OK`. Visually inspected the full-frame main screenshot: enlarged hooded signal, framed loudspeaker/bookmark controls, original Auto/Menu size, compact menu, larger dialogue panel and separate round endcaps on the bottom band. The primary check explicitly verifies that the bar is outside the panel and that both vertical gaps are zero. Hash comparisons and any concurrent changes elsewhere in the shared workspace are recorded in the latest verification JSON.

The known `UnityEditor.Search.SearchDatabase` indexing exception is retained separately in `editorDiagnostics`; target runtime assertions have no errors. Earlier launch failures (sandbox licensing, isolated UPM setup, test fixture format and previous geometry assertions) are preserved in their respective logs and do not count as passing evidence. This is an Editor rendering/playback check, not physical tablet or Windows Player acceptance.

## Remaining work

- Approved Japanese voice and translation assets are not present; controls intentionally hide until ready.
- Per-script scenic art currently uses authored vector silhouettes. Hand-painted, more detailed scenery can replace the draw layer without changing seek or voice semantics.
- Existing Shift ×2 remains text fast-forward; pitch-preserving audio time-stretch is not implemented.
- Previous requested comprehensive Chinese story audit remains incomplete. UI work does not count as completion of that review.
