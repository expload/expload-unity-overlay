You can check documentation at https://developers.expload.com/documentation/integration/unity-overlay/how-to-use/ or use this file.

===== HOW TO USE
This project is based on https://github.com/aleab/cef-unity-sample

===== OVERVIEW
This sample shows how to use the Expload platform overlay in the Unity application.

This overlay currently renders Expload dialogs (transaction confirmation, for example).

===== HOW IT WORKS
It consists of a CEF(Chromium Embedded Framework) instance that draws the page hosted by expload desktop app and located at http://localhost:8087/ui/overlay/ address by default.

The page is drawn in the Off-Screen Rendering mode to the pixel buffer.
See files OffscreenCEF.cs, OffscreenCEFClient.cs

===== USAGE
For adding the Expload overlay to your Unity game project:
 - 1. Download expload.unitypackage
 - 2. Add the downloaded package expload.unitypackage: Assets -> Import package -> Select expload.unitypackage
 - 3. Add the empty object to your game’s initial scene and add Expload Behaviour component to it.

That's all. Now run the Expload Desktop app and then run your game.

===== PLEASE NOTE:
Expload Unity Overlay DOES NOT work inside Unity Editor, it will only work in standalone build. To test it run the game via File -> Build And Run