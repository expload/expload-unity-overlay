# expload-unity-overlay-sample
This sample shows how to use Expload platform overlay in Unity application.
This overlay renders Expload transaction confirmation dialog.
## Usage
For adding expload overlay to your Unity project:
1. Copy `Assets\Cef`, `Assets\Editor`, `Assets\Materials`, `Assets\Plugins`, `Assets\Shaders` folders to Unity project.
2. Add full screen RawImage UI on the top of scene for rendering Expload overlay.
3. Set `CefOverlay` as Material and `Offscreen CEF` as Script.
