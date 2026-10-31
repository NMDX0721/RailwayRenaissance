# Spotify / Folia service

Local experimental helper adapted from the Folia fork, AGPL-3.0. Upstream files are retained under vendor, with hashes in UPSTREAM.json and full license in COPYING. New bridge.js is also AGPL-3.0. Distributing this helper or a combined product requires satisfying applicable AGPL source and notice obligations; the project MIT label does not cover these files. No release approval is implied.

Uses Folia browser PKCE on 127.0.0.1:5588/login, the same pinned Spotify query operations, and node-librespot 0.5.1. Spotify credentials stay in the child process; no plaintext tokens or credential cache. Closing the program signs out. The audio listener binds only 127.0.0.1 on a random port and accepts only URLs issued in the current session. Premium playback follows the upstream check. No account-library mutations are exposed by the wrapper.

The upstream uses the Spotify desktop client identifier. That behavior is retained for local comparison, not described as our registered official application. Real authorization, query compatibility and playback must be verified with the user account; platform permission and distribution review remain separate. No credentials are requested in chat.

Install: npm ci --ignore-scripts. Run: node bridge.js. JSON-line protocol methods: ping, login_start, login_check, status, logout, search, playlists, playlist_tracks, resolve, lyric. On stdin close the service stops both loopback listeners.
