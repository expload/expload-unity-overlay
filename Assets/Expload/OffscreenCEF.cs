using System;
using System.Security;
using UnityEngine;
using UnityEngine.UI;
using Xilium.CefGlue;
using System.Threading;
using System.IO;

namespace Expload
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshRenderer))]
    public class OffscreenCEF : MonoBehaviour
    {
        [SerializeField]
        private string overlayUrl = "http://localhost:8087/ui/overlay-transparent/";

        [Space]
        [SerializeField]
        private bool hideScrollbars = true;

#if !UNITY_EDITOR
        private bool shouldQuit = false;
        private OffscreenCEFClient cefClient;

        public Texture2D BrowserTexture { get; private set; }

        private int windowWidth = Screen.width;
        private int windowHeight = Screen.height;

        private void Awake()
        {
            if (shouldQuit)
                return;

            this.BrowserTexture = new Texture2D(this.windowWidth, this.windowHeight, TextureFormat.BGRA32, false);
            var fillColorArray = this.BrowserTexture.GetPixels32();

            for (var i = 0; i < fillColorArray.Length; ++i)
            {
                fillColorArray[i] = new Color32(0, 0, 0, 0);
            }

            this.BrowserTexture.SetPixels32(fillColorArray);
            this.BrowserTexture.Apply();

            this.GetComponent<RawImage>().texture = this.BrowserTexture;
            Material mat = Resources.Load<Material>("ExploadOverlay");
            this.GetComponent<RawImage>().material = mat;
        }

        private void Start()
        {
            this.StartCef();

            DontDestroyOnLoad(this.gameObject.transform.root.gameObject);
        }

        void OnDestroy()
        {
            this.Quit();
        }

        void OnApplicationQuit()
        {
            this.Quit();
        }

        private void StartCef()
        {
#if UNITY_EDITOR
            CefRuntime.Load(Path.Combine(Application.dataPath, "Plugins", "Cef", "Windows"));
#else
            CefRuntime.Load();
#endif


            var cefMainArgs = new CefMainArgs(new string[] { });
            var cefApp = new OffscreenCEFClient.OffscreenCEFApp();

            // This is where the code path diverges for child processes.
            if (CefRuntime.ExecuteProcess(cefMainArgs, cefApp, IntPtr.Zero) != -1)
                Debug.LogError("Could not start the secondary process.");

            var cefSettings = new CefSettings
            {
                //ExternalMessagePump = true,
                MultiThreadedMessageLoop = false,
                SingleProcess = true,
                LogSeverity = CefLogSeverity.Verbose,
                LogFile = "cef.log",
                WindowlessRenderingEnabled = true,
                NoSandbox = true
            };

            // Start the browser process (a child process).
            CefRuntime.Initialize(cefMainArgs, cefSettings, cefApp, IntPtr.Zero);

            // Instruct CEF to not render to a window.
            CefWindowInfo cefWindowInfo = CefWindowInfo.Create();
            cefWindowInfo.SetAsWindowless(IntPtr.Zero, true);

            // Settings for the browser window itself (e.g. enable JavaScript?).
            CefBrowserSettings cefBrowserSettings = new CefBrowserSettings()
            {
            };

            Debug.Log("Start with window: " + this.windowWidth + ", " + this.windowHeight);

            // Initialize some of the custom interactions with the browser process.
            this.cefClient = new OffscreenCEFClient(
                this.windowWidth,
                this.windowHeight,
                this.hideScrollbars,
                this.BrowserTexture,
                this
            );

            // Start up the browser instance.
            CefBrowserHost.CreateBrowser(cefWindowInfo, this.cefClient, cefBrowserSettings, string.IsNullOrEmpty(this.overlayUrl) ? "http://www.google.com" : this.overlayUrl);
        }

        static Thread mainThread = Thread.CurrentThread;

        [SecurityCritical]
        private void Quit()
        {
            if (this.shouldQuit)
                return;

            Debug.Log("Shutdown CEF (mainThread = " + (mainThread == Thread.CurrentThread) + ")");
            this.shouldQuit = true;
            this.cefClient.Shutdown();
            this.StopAllCoroutines();
            Debug.Log("Before CEF shutdown");
            CefRuntime.Shutdown();
            Debug.Log("CEF shuttdown successful");
        }

        void FixedUpdate()
        {
            if (!this.shouldQuit)
            {
                CefRuntime.DoMessageLoopWork();
            }
        }

        private Vector3 prevMousePos;

        //private readonly CefMouseButtonType[] cefMouseButtons = { CefMouseButtonType.Left, CefMouseButtonType.Right, CefMouseButtonType.Middle };

        void Update()
        {
            if (shouldQuit)
                return;

            var p = Input.mousePosition;
            int i = 0;  //  use only left mouse button

            if (!prevMousePos.Equals(p))
            {
                var e = new CefMouseEvent {
                    X = (int)p.x,
                    Y = windowHeight - (int)p.y,
                    Modifiers = Input.GetMouseButtonDown(i) ? CefEventFlags.LeftMouseButton : 0
                };

                cefClient.SendMouseMove(e);
                prevMousePos = p;
            }
            if (Input.GetMouseButtonDown(i))
            {
                cefClient.SendMouseClick((int)p.x, windowHeight - (int)p.y, CefMouseButtonType.Left, false);
            }
            if (Input.GetMouseButtonUp(i))
            {
                cefClient.SendMouseClick((int)p.x, windowHeight - (int)p.y, CefMouseButtonType.Left, true);
            }

            //foreach (char code in Input.inputString)
            //{
            //    var ev = new CefKeyEvent
            //    {
            //        WindowsKeyCode = code,
            //        NativeKeyCode = code,
            //        EventType = CefKeyEventType.KeyDown
            //    };
            //    cefClient.SendKey(ev);
            //    ev.EventType = CefKeyEventType.Char;
            //    cefClient.SendKey(ev);
            //    ev.EventType = CefKeyEventType.KeyUp;
            //    cefClient.SendKey(ev);
            //}
        }

        private void OnGUI()
        {
            Event e = Event.current;
            if (e.isKey)
            {
                var ev = new CefKeyEvent();
                
                //ev.Character = e.character;
                ev.WindowsKeyCode = (int)e.keyCode;
                ev.NativeKeyCode = (int)e.keyCode;
                ev.Modifiers =
                    (e.modifiers.HasFlag(EventModifiers.CapsLock) ? CefEventFlags.CapsLockOn  : 0) |
                    (e.modifiers.HasFlag(EventModifiers.Control)  ? CefEventFlags.ControlDown : 0) |
                    (e.modifiers.HasFlag(EventModifiers.Command)  ? CefEventFlags.CommandDown : 0) |
                    (e.modifiers.HasFlag(EventModifiers.Alt)      ? CefEventFlags.AltDown     : 0) |
                    (e.modifiers.HasFlag(EventModifiers.Shift)    ? CefEventFlags.ShiftDown   : 0);

                if (e.type == EventType.KeyDown)
                {
                    ev.EventType = CefKeyEventType.KeyDown;
                    cefClient.SendKey(ev);
                }
                else if (e.type == EventType.KeyUp)
                {
                    ev.EventType = CefKeyEventType.Char;
                    cefClient.SendKey(ev);
                    ev.EventType = CefKeyEventType.KeyUp;
                    cefClient.SendKey(ev);
                }
            }
        }
#endif
    }
}