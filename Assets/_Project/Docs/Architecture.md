# Dustline Arena Architecture

## Runtime

Gameplay code lives under `Assets/_Project/Scripts/Runtime` and is compiled through
`DustlineArena.Runtime.asmdef`.

- `Common`: shared contracts and team ownership.
- `Config`: ScriptableObject balance data.
- `Health`: reusable health and damage handling.
- `Player`: input, movement, aiming, weapon control.
- `Weapons`: projectile weapon and projectile behavior.
- `Enemies`: enemy movement and melee attack behavior.
- `Spawning`: spawn points and wave playback.
- `Camera`: top-down camera follow.
- `Pickups`: world pickups.
- `Feedback`: small non-critical feedback components.

## Dependency Policy

The first playable slice avoids hard dependencies on DOTween or Zenject so the project
keeps compiling before those packages are imported. When DOTween is added, feedback code
should be placed behind a small adapter instead of spreading `DG.Tweening` calls through
gameplay logic.

Zenject/Extenject is useful once the project has persistent services, scene installers,
save data, audio services, and factories shared across scenes. For the first arena MVP,
serialized references, ScriptableObject configs, and narrow interfaces are enough.

## URP

`com.unity.render-pipelines.universal` is declared in `Packages/manifest.json`. After Unity
installs it, create a URP pipeline asset from Unity and assign it in Graphics and Quality
settings, then run the Synty/KayKit material conversion tools if needed.
