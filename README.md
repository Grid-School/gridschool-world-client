# GridSchool world — client

Unity 6 (URP) client for **the world**, the persistent place built and operated by
[GridSchool](https://gridschool.org) students. Server: [gridschool-world-server](../../gridschool-world-server).
Live at [play.gridschool.org](https://play.gridschool.org).

WebGL is the primary target — the world is watchable from a browser link, no install.
NativeWebSocket carries the connection; remote players interpolate at 10 Hz.

## Client track (students, week 3+)

The server track (see the server repo's `ONBOARDING.md`) needs no Unity and is where everyone
starts. This repo is the optional client track: Unity Hub + the pinned Unity 6 version (~10 GB).
Scenes, models, and the brand look belong to the maintainer — students own **systems**
(chat UI, interpolation, reconnect), not meshes.

## Run against a local server

Open `Contigo/Assets/Scenes/MainClientScene.unity`, point the server URL at
`ws://localhost:8080/ws`, press Play. Two editor instances (or editor + WebGL build) = two humans.
