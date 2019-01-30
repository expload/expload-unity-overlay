# expload-unity-overlay-sample
This sample shows how to use Expload platform overlay in Unity application.

For now this overlay renders Expload transaction confirmation dialog.
## How it works
It consists of CEF(Chromium Embedded Framework) instance that draws page that hosts by expload desktop app and located at http://localhost:8087/ui/overlay/ address by default.

Page is drawn in Off-Screen Rendering mode to pixel buffer.

It has special `transparency` overlay color, that is clipped in [CefOverlay.shader](Assets/Shaders/CefOverlay.shader). For now it is Green (0,1,0,1).

## Usage
For adding expload overlay to your Unity project:
1. Copy `Assets\Cef`, `Assets\Editor`, `Assets\Materials`, `Assets\Plugins`, `Assets\Shaders` folders to Unity project.
2. Add full screen RawImage UI to the top of scene for rendering Expload overlay.
3. Set `CefOverlay` as Material and `Offscreen CEF` as Script.
