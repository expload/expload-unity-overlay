using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using UnityEngine;
using Xilium.CefGlue;

namespace Expload
{
    internal class OffscreenCEFClient : CefClient
    {
        private readonly OffscreenLoadHandler _loadHandler;
        private readonly OffscreenRenderHandler _renderHandler;
        private readonly int _windowWidth;
        private readonly int _windowHeight;

        private byte[] sPixelBuffer;
        private byte[] sPopupPixelBufer;
        private CefRectangle _popupSize;
        private bool _popupShow;
        private Texture2D _texture;

        private CefBrowserHost sHost;

        private OffscreenCEF behaviour;

        public OffscreenCEFClient(int windowWidth, int windowHeight, bool hideScrollbars, Texture2D texture, OffscreenCEF behaviour)
        {
            this._texture = texture;
            this._windowWidth = windowWidth;
            this._windowHeight = windowHeight;
            this._loadHandler = new OffscreenLoadHandler(this, hideScrollbars);
            this._renderHandler = new OffscreenRenderHandler(this);
            this.behaviour = behaviour;

            this.sPixelBuffer = new byte[windowWidth * windowHeight * 4];

            Debug.Log("Constructed Offscreen Client");
        }

        private void UpdateTexture()
        {
            if (this.sHost == null)
              return;
              
            byte[] buffer = sPixelBuffer;

            if (_popupShow && sPopupPixelBufer != null) {
                // Clone view buffer
                buffer = new byte[sPixelBuffer.Length];
                sPixelBuffer.CopyTo(buffer, 0);
                // Copy subrect of popup
                for (int y = 0; y < _popupSize.Height; y++)
                {
                    int sourceOffset = _popupSize.Width * y * 4;
                    int targetOffset = (_popupSize.Y + y) * _windowWidth * 4 + _popupSize.X;
                    Array.Copy(sPopupPixelBufer, sourceOffset, buffer, targetOffset, _popupSize.Width * 4);
                }
            }

            _texture.LoadRawTextureData(buffer);
            _texture.Apply(false);
        }

        public void SendMouseMove(CefMouseEvent e)
        {
            if (this.sHost == null)
                return;

            this.sHost.SendMouseMoveEvent(e, false);
        }

        public void SendMouseClick(int x, int y, CefMouseButtonType button, bool mouseUp)
        {
            if (this.sHost == null)
                return;

            this.sHost.SendMouseClickEvent(new CefMouseEvent(x, y, 0), button, mouseUp, 1);
        }

        public void SendKey(CefKeyEvent e)
        {
            if (this.sHost == null)
                return;
            this.sHost.SendKeyEvent(e);
        }

        [SecurityCritical]
        public void Shutdown()
        {
            if (this.sHost == null)
                return;

            Debug.Log("Host Cleanup");
            var host = this.sHost;
            this.sHost = null;
            host.CloseBrowser(false);
            host.Dispose();
        }

        #region Interface

        protected override CefRenderHandler GetRenderHandler()
        {
            return this._renderHandler;
        }

        protected override CefLoadHandler GetLoadHandler()
        {
            return this._loadHandler;
        }

        private void AjustPopupBuffer(int width, int height)
        {
            int size = width * height * 4;
            // Reallocate popup buffer if nessasery
            if (sPopupPixelBufer == null || sPopupPixelBufer.Length != size)
                sPopupPixelBufer = new byte[size];
        }


        #endregion Interface

        #region Handlers

        internal class OffscreenLoadHandler : CefLoadHandler
        {
            private OffscreenCEFClient client;
            private bool hideScrollbars;

            public OffscreenLoadHandler(OffscreenCEFClient client, bool hideScrollbars)
            {
                this.client = client;
                this.hideScrollbars = hideScrollbars;
            }

            protected override void OnLoadStart(CefBrowser browser, CefFrame frame, CefTransitionType transitionType)
            {
                if (browser != null)
                    this.client.sHost = browser.GetHost();

                if (frame.IsMain)
                    Debug.LogFormat("START: {0}", browser.GetMainFrame().Url);
            }

            protected override void OnLoadError(CefBrowser browser, CefFrame frame, CefErrorCode errorCode, string errorText, string failedUrl)
            {
                if (frame.IsMain)
                {
                    Debug.LogFormat("ERROR: {0}, {1}, {2}", errorCode, errorText, failedUrl);

                    this.client.behaviour.StartCoroutine(RetryConnecting(browser));
                }
            }

            private IEnumerator RetryConnecting(CefBrowser browser)
            {
                yield return new WaitForSeconds(2);
                browser.Reload();
            }

            protected override void OnLoadEnd(CefBrowser browser, CefFrame frame, int httpStatusCode)
            {
                if (frame.IsMain)
                {
                    Debug.LogFormat("END: {0}, {1}", browser.GetMainFrame().Url, httpStatusCode.ToString());

                    if (this.hideScrollbars)
                        this.HideScrollbars(frame);
                }
            }

            private void HideScrollbars(CefFrame frame)
            {
                string jsScript = "var head = document.head;" +
                                  "var style = document.createElement('style');" +
                                  "style.type = 'text/css';" +
                                  "style.appendChild(document.createTextNode('::-webkit-scrollbar { visibility: hidden; }'));" +
                                  "head.appendChild(style);";
                frame.ExecuteJavaScript(jsScript, string.Empty, 107);
            }

        }

        internal class OffscreenRenderHandler : CefRenderHandler
        {
            private OffscreenCEFClient client;

            public OffscreenRenderHandler(OffscreenCEFClient client)
            {
                this.client = client;
            }

            protected override bool GetRootScreenRect(CefBrowser browser, ref CefRectangle rect)
            {
                return GetViewRect(browser, ref rect);
            }

            protected override bool GetScreenPoint(CefBrowser browser, int viewX, int viewY, ref int screenX, ref int screenY)
            {
                screenX = viewX;
                screenY = viewY;
                return true;
            }

            protected override bool GetViewRect(CefBrowser browser, ref CefRectangle rect)
            {
                rect.X = 0;
                rect.Y = 0;
                rect.Width = client._windowWidth;
                rect.Height = client._windowHeight;
                return true;
            }

            protected override void OnPopupShow(CefBrowser browser, bool show)
            {
                client._popupShow = show;
                client.UpdateTexture();
            }

            protected override void OnPopupSize(CefBrowser browser, CefRectangle rect)
            {
                client._popupSize = rect;
                client.AjustPopupBuffer(rect.Width, rect.Height);
            }

            [SecurityCritical]
            protected override void OnPaint(CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects, IntPtr buffer, int width, int height)
            {
                if (browser == null)
                    return;

                byte[] targetBuffer = client.sPixelBuffer;
                if (type == CefPaintElementType.Popup)
                {
                    client.AjustPopupBuffer(width, height);
                    targetBuffer = client.sPopupPixelBufer;
                }

                Marshal.Copy(buffer, targetBuffer, 0, targetBuffer.Length);
                client.UpdateTexture();
            }

            protected override bool GetScreenInfo(CefBrowser browser, CefScreenInfo screenInfo)
            {
                return false;
            }

            protected override void OnCursorChange(CefBrowser browser, IntPtr cursorHandle, CefCursorType type, CefCursorInfo customCursorInfo)
            {
            }

            protected override void OnScrollOffsetChanged(CefBrowser browser, double x, double y)
            {
            }

            protected override void OnImeCompositionRangeChanged(CefBrowser browser, CefRange selectedRange, CefRectangle[] characterBounds)
            {
            }
        }

        #endregion Handlers

        public class OffscreenCEFApp : CefApp
        {
            protected override void OnBeforeCommandLineProcessing(string processType, CefCommandLine commandLine)
            {
                Console.WriteLine("OnBeforeCommandLineProcessing: {0} {1}", processType, commandLine);

                // TODO: currently on linux platform location of locales and pack files are determined
                // incorrectly (relative to main module instead of libcef.so module).
                // Once issue http://code.google.com/p/chromiumembedded/issues/detail?id=668 will be resolved this code can be removed.
                if (CefRuntime.Platform == CefRuntimePlatform.Linux)
                {
                    var path = new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath;
                    path = Path.GetDirectoryName(path);

                    commandLine.AppendSwitch("resources-dir-path", path);
                    commandLine.AppendSwitch("locales-dir-path", Path.Combine(path, "locales"));
                }
            }
        }
    }
}