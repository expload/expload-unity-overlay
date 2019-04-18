using System;
using System.Security;
using UnityEngine;
using UnityEngine.UI;
using Xilium.CefGlue;
using System.Threading;
using UnityEngine.Experimental.Input;

namespace Expload
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshRenderer))]
    public class OffscreenCEF : MonoBehaviour
    {
        [SerializeField]
        private string url = "http://localhost:8087/ui/overlay/";

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
            this.GetComponent<RawImage>().texture = this.BrowserTexture;
            Material mat = Resources.Load<Material>("Expload/ExploadOverlay");
            this.GetComponent<RawImage>().material = mat;
        }

        private void Start()
        {
            this.StartCef();

            Keyboard.current.onTextInput += (char charCode) =>
            {
                var ev = new CefKeyEvent();
                ev.WindowsKeyCode = charCode;
                ev.Character = charCode;
                ev.EventType = CefKeyEventType.KeyDown;
                cefClient.SendKey(ev);
                ev.EventType = CefKeyEventType.Char;
                cefClient.SendKey(ev);
                ev.EventType = CefKeyEventType.KeyUp;
                cefClient.SendKey(ev);
            };

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
            CefRuntime.Load();

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
            cefWindowInfo.SetAsWindowless(IntPtr.Zero, false);

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
                this.BrowserTexture
            );

            // Start up the browser instance.
            CefBrowserHost.CreateBrowser(cefWindowInfo, this.cefClient, cefBrowserSettings, string.IsNullOrEmpty(this.url) ? "http://www.google.com" : this.url);
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
                    X = (int) p.x, 
                    Y = windowHeight - (int) p.y,
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

#endif
    }
}
