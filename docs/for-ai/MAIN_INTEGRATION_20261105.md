# Main integration preparation, 2026-11-05

The user requested publishing and merging the pending development in the primary checkout before continuing ATO work directly on main. The starting branch is feat/music-app-v3 at 9918bca0. The fetched main is a3feb497, containing PR 9 at e27c95e7.

The pending integration includes VN localisation, presentation and tablet archive migration, NF5K equipment and cooling detail, cab interaction and query changes, music packaging, project settings and their accompanying documents and native resources. Earlier tablet and music commits on this branch are also part of the integration. Local scratch copies, compiler outputs, research run archives and competition application backups stay local.

Validation before main synchronisation compiled 218 runtime source files and 76 editor source files with Roslyn and cached Unity references. Both compiler invocations succeeded. Reports and source hashes are in tools/compile/main-integration-20261105/result.json. This check covers source compilation. Unity import, normal-frame ATO driving, visual verification and device acceptance require separate execution.

An isolated retry established that setting the missing user environment paths gets Unity beyond the previous package-manager undefined-path error. Subsequent package resolution aborted while obtaining com.unity.burst 1.8.30. Following the user's direction, further work uses the primary checkout.

The new track delivery contract is in 参考资料/系统/ATO试验线路制作要求.md. Existing 3D inspection geometry ends at 500 metres while the standard ATO task targets 900 metres. Route sampling, rendered rail and ballast extent, and end protection must be aligned before claiming normal-frame 900-metre 3D acceptance.
