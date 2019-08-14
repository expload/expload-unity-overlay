# How to Use

This project is based on [cef-unity-sample](https://github.com/aleab/cef-unity-sample)

## Overview

This sample shows how to use the Expload platform overlay in the Unity application.

This overlay currently renders Expload dialogs (transaction confirmation, for example).

## How it Works

It consists of a CEF(Chromium Embedded Framework) instance that draws the page hosted by expload desktop app and located at http://localhost:8087/ui/overlay/ address by default.

The page is drawn in the Off-Screen Rendering mode to the pixel buffer. [OffscreenCEF.cs](https://github.com/expload/expload-unity-overlay/blob/master/Assets/Resources/Expload/OffscreenCEF.cs), [OffscreenCEFClient.cs](https://github.com/expload/expload-unity-overlay/blob/master/Assets/Resources/Expload/OffscreenCEFClient.cs)

It has a special `transparency` overlay color, that is clipped in [CefOverlay.shader](https://github.com/expload/expload-unity-overlay/blob/master/Assets/Resources/Expload/CefOverlay.shader). It is currently Green (0,1,0,1).

## Usage

For adding the Expload overlay to your Unity game project:
1. Download [expload.unitypackage](https://github.com/expload/expload-unity-overlay-sample/releases)
1. Add the downloaded package expload.unitypackage: `Assets` -> `Import package` -> Select expload.unitypackage
2. Add the empty object to your game’s initial scene and set `Expload Behaviour` as `Script`.

![](https://raw.githubusercontent.com/expload/expload-unity-overlay/master/pics/unity-screen.png)

That's all.
Now run the `Expload Desktop` app and then run your game.

**Please note**: CEF does not work inside IDE, run the game via `File` -> `Build And Run`
