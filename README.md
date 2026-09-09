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

## Build and publish the web client (terminal, no editor UI)

Pinned editor: `6000.0.39f1` with the WebGL module. From the repo root:

```sh
/Applications/Unity/Hub/Editor/6000.0.39f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit -projectPath Contigo -buildTarget WebGL \
  -executeMethod WebGLBuild.Build -logFile /tmp/unity-webgl.log
git add web && git commit -m "Publish web client" && git push
```

`WebGLBuild.Build` writes into `web/`; pushing `web/` to `main` is the deploy
(`.github/workflows/pages.yml` → play-staging.gridschool.org). First build ~7 min,
rebuilds ~30 s. Test any build against any server with `?server=wss://host/ws`.
The server the page connects to is decided by hostname in `ServerEndpoint.cs`.
