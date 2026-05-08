# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repo is

A Unity 6000.3 project that exists primarily to host and develop a single UPM package: `io.studio555.riveuitoolkitsupport` (at `Packages/io.studio555.riveuitoolkitsupport/`). The package is the deliverable — everything in `Assets/` (the `SampleScene`, `Sample.cs`, `SampleWindow.uxml`, the `.riv` files) is a sandbox for trying it out, not part of the shipped library.

The package bridges [Rive](https://github.com/rive-app/rive-unity) (`app.rive.rive-unity`) into Unity's UI Toolkit (UIElements/UXML), so designers can drop Rive animations into UI Toolkit layouts as a `<RiveElement>` UXML element, just like a `Button` or `Image`.

## Architecture

Two files do everything. Read both before changing either — they are tightly coupled.

**`RiveElement.cs`** — a `[UxmlElement] partial class : VisualElement` exposing `RiveAsset` (Rive `Asset`) and `Fit` as `[UxmlAttribute]`s so they appear in UI Builder and in UXML. It owns no rendering itself; it holds a reference to a `RiveWidget` + `RivePanel` pair created on its behalf.

**`RiveUIToolkitSupport.cs`** — a `MonoBehaviour` singleton (`[DefaultExecutionOrder(-1000)]`, auto-spawned via `Instance` getter, `DontDestroyOnLoad`) that, for each `RiveElement.Register(...)` call, builds a child GameObject hierarchy: `RiveElement-{guid}` (RectTransform, sized to the element's pixel rect) → `RivePanel` (with its auto-attached `SimpleRenderTargetStrategy`) → child `RiveWidget` loaded with the asset.

**No panel reuse.** `Unregister` always destroys the hierarchy. An earlier parking pool (commits `9a738fb`/`0c17f46`) was removed because reusing a `RivePanel` + `RiveWidget` across `Load(newAsset)` calls carried state across bindings (state machine residue, RT/asset binding mismatches) and was the recurring source of visibility/texture-state bugs. `RiveUIToolkitSupport.ReleaseAll()` is the explicit scene-transition hook: consumers call it before `SceneManager.LoadScene(...)` so the `RendererUtils.ReleaseRenderer` end-of-frame coroutines run now (one batched stall) rather than competing with the next scene's load. Cleanup is **manual** — the package does not subscribe to `SceneManager.sceneUnloaded`.

**The bridge**: each `RivePanel` renders to a `RenderTexture` owned by its `SimpleRenderTargetStrategy`; `RiveElement.UpdateBackgroundFromPanel()` assigns it as `style.backgroundImage` via `Background.FromRenderTexture(rt)`. There is no custom shader or draw call — UI Toolkit just paints the RT as a background image of the VisualElement.

**Lifecycle is event-driven, not Update-driven.** `RiveElement` registers in `AttachToPanelEvent` (only when `Application.isPlaying` — editor preview is intentionally a no-op), unregisters in `DetachFromPanelEvent`, and on `GeometryChangedEvent` toggles `_widget.enabled` / `_rivePanel.enabled` based on whether the rect has area. This is how hidden / `display: none` elements stop driving the GPU. Recent commits (`5ae5823`, `5c3f5f9`, `4d12b08`) all addressed visibility/texture-state edge cases here — be careful when refactoring this path; the current shape exists because of specific bugs.

**Asset reassignment** has two branches in `RiveElement.RiveAsset.set`: if the widget already exists, just `_widget.Load(value)` and re-apply `Fit`; if it doesn't, `Unregister()` + `RegisterOnce()`. Don't collapse these — the first avoids destroying/recreating the GameObject hierarchy and its render texture.

**Public API beyond UXML attributes**: `Widget` getter (exposes the underlying `RiveWidget` for advanced use) and `TryFireTrigger(string)` (looks up a state machine trigger by name and fires it; logs and returns `false` on any miss). On `RiveUIToolkitSupport`: `ReleaseAll()` (see scene-transition note above).

## Versioning & dependencies

- The package's version lives in `Packages/io.studio555.riveuitoolkitsupport/package.json` (currently `0.1.3`). When bumping the package, update this file.
- Rive is pinned to a git tag in `Packages/manifest.json`: `app.rive.rive-unity` → `git@github.com:rive-app/rive-unity.git?path=package#v0.4.3-canary.18`. Recent commits show the pattern is to bump this tag and the package version together (`d2be495`, `058aeb8`).
- The package has zero `dependencies` in its own `package.json` — Rive is assumed to be present in the consuming project. The asmdef references it by GUID (`Rive.Runtime` and Unity's `UIElementsModule`).

## Editing & running

- Open the repo in **Unity 6000.3.12f1** (see `ProjectSettings/ProjectVersion.txt`). Other 6000.x patch versions usually work but Unity may force-upgrade `Library/`.
- The sandbox: open `Assets/Scenes/SampleScene.unity`, press Play. `Sample.cs` toggles a `RiveElement` named `RoboDude` in `SampleWindow.uxml` via a button.
- There is no test suite, no CI, no build script. `build/` contains a manually-produced Android APK and is gitignored.
- `*.csproj` and `*.sln` files at the repo root are checked in but are also listed in `.gitignore` — they're regenerated by Unity/Rider; don't hand-edit them.

## Conventions worth knowing

- Namespace is lowercase: `io.studio555.riveuitoolkitsupport`. UXML references the type with the full lowercase namespace (`<io.studio555.riveuitoolkitsupport.RiveElement .../>`).
- `RiveElement` must be `partial` — Unity's source generator emits the UXML traits/serialization half from `[UxmlElement]`/`[UxmlAttribute]`. Don't remove `partial`.
- Don't access `RiveUIToolkitSupport.Instance` from `OnDestroy`/teardown paths without checking — the getter returns `null` after `OnApplicationQuit` (`_isQuitting` guard) and during edit mode.