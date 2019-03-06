using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Xilium.CefGlue;

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
            this.BrowserTexture = new Texture2D(this.windowWidth, this.windowHeight, TextureFormat.BGRA32, false);
            this.GetComponent<RawImage>().texture = this.BrowserTexture;
            Material mat = Resources.Load<Material>("Expload/ExploadOverlay");
            this.GetComponent<RawImage>().material = mat;
        }

        private void Start()
        {
            this.StartCef();
            this.StartCoroutine(this.MessagePump());
            DontDestroyOnLoad(this.gameObject.transform.root.gameObject);
        }

        private void OnDestroy()
        {
            this.Quit();
        }

        private void OnApplicationQuit()
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
            this.cefClient = new OffscreenCEFClient(this.windowWidth, this.windowHeight, this.hideScrollbars);

            // Start up the browser instance.
            CefBrowserHost.CreateBrowser(cefWindowInfo, this.cefClient, cefBrowserSettings, string.IsNullOrEmpty(this.url) ? "http://www.google.com" : this.url);
        }

        private void Quit()
        {
            this.shouldQuit = true;
            this.StopAllCoroutines();
            this.cefClient.Shutdown();
            CefRuntime.Shutdown();
        }

        private IEnumerator MessagePump()
        {
            while (!this.shouldQuit)
            {
                CefRuntime.DoMessageLoopWork();
                if (!this.shouldQuit)
                {
                    this.cefClient.UpdateTexture(this.BrowserTexture);
                }
                yield return new WaitForEndOfFrame();
            }
        }

        void Update()
        {
            var p = Input.mousePosition;
            for (int i = 0; i < 3; ++i)
            {
                if (Input.GetMouseButtonDown(i))
                {
                    cefClient.SendMouseClick((int)p.x, windowHeight - (int)p.y, i, false);
                }
                if (Input.GetMouseButtonUp(i))
                {
                    cefClient.SendMouseClick((int)p.x, windowHeight - (int)p.y, i, true);
                }
            }
            foreach (char code in Input.inputString)
            {
                var ev = new CefKeyEvent();
                ev.WindowsKeyCode = code;
                ev.EventType = CefKeyEventType.KeyDown;
                cefClient.SendKey(ev);
                ev.EventType = CefKeyEventType.Char;
                cefClient.SendKey(ev);
                ev.EventType = CefKeyEventType.KeyUp;
                cefClient.SendKey(ev);
            }
        }
#endif
    }
}
