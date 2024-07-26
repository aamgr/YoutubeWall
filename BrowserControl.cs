using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;

using Control = System.Windows.Forms.Control;


namespace YoutubeSuiveur
{
    public partial class BrowserMouse
    {
#if false
        public IWinFormsChromiumWebBrowser Browser  {   get; private set;   }
        Form1 ptrForm1;
        private ChromiumWidgetNativeWindow messageInterceptor;
        //private bool multiThreadedMessageLoopEnabled=true;

        public BrowserMouse(EO.WinForm.WebControl/*ChromiumWebBrowser*/ cWB,Form1 ptr)
        {
            Browser = cWB;
            ptrForm1 = ptr;
            Browser.IsBrowserInitializedChanged += OnIsBrowserInitializedChanged;
        }

        ~BrowserMouse()
        {
            Dispose(true);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (messageInterceptor != null)
                {
                    messageInterceptor.ReleaseHandle();
                    messageInterceptor = null;
                }
            }
            //base.Dispose(disposing);
        }

    //    [return: MarshalAs(UnmanagedType.Bool)]
    //    [DllImport("user32.dll", SetLastError = true)]

        public void OnIsBrowserInitializedChanged(object sender, EventArgs e)
        {
            SetupMessageInterceptor();
        }

        /// <summary>
        /// The ChromiumWebBrowserControl does not fire MouseEnter/Move/Leave events, because Chromium handles these.
        /// This method provides a demo of hooking the Chrome_RenderWidgetHostHWND handle to receive low level messages.
        /// You can likely hook other window messages using this technique, drag/drog etc
        /// </summary>
        public struct eventlist
        {
            public int num;
            public int num_msg;
        };
        static public List<eventlist> event_lists = new List<eventlist>();

        private void SetupMessageInterceptor()
        {
            //const int WM_MOUSEACTIVATE = 0x0021;
            //const int WM_MOUSEMOVE = 0x0200;
            //const int WM_MOUSELEAVE = 0x02A3;
            const int WM_NCLBUTTONDOWN = 0x00A1;
            const int WM_DESTROY    = 0x0002;
            const int WM_RBUTTONDOWN = 0x0204;
            const int WM_LBUTTONDOWN = 0x0201;
            const int WM_KEYDOWN = 256;

            const int VK_ESCAPE = 0x1B; //escape touch

            if (messageInterceptor != null)
            {
                messageInterceptor.ReleaseHandle();
                messageInterceptor = null;
            }

            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        IntPtr chromeWidgetHostHandle;
                        if (Browser.BrowserCore!=null && ChromiumRenderWidgetHandleFinder.TryFindHandle(Browser.BrowserCore, out chromeWidgetHostHandle))
                        {
                            messageInterceptor = new ChromiumWidgetNativeWindow((Control)Browser, chromeWidgetHostHandle);

                            messageInterceptor.OnWndProc(message =>
                            {
                            int num_chromiumWebBrowser;
                                if (int.TryParse(((ChromiumWebBrowser)Browser).Name.Substring("chromiumWebBrowser".Length), out num_chromiumWebBrowser))
                                {
                                    // Render process switch happened, need to find the new handle
                                    if (message.Msg == WM_DESTROY)
                                    {
                                        SetupMessageInterceptor();
                                        return false;
                                    }
                                    switch (message.Msg)
                                    {
                                        case WM_NCLBUTTONDOWN:
                                            Console.WriteLine("WM_NCLBUTTONDOWN");
                                            break;
                                        case WM_LBUTTONDOWN:
                                            /*Task.Run(async () =>*/ ptrForm1.video_resize(num_chromiumWebBrowser,true);//);
                                            break;
                                        case WM_RBUTTONDOWN:
                                            Console.WriteLine("WM_RBUTTONDOWN");
                                            break;
                                        case WM_KEYDOWN:
                                            if (message.WParam.ToInt32() == VK_ESCAPE)
                                                ptrForm1.video_resize(num_chromiumWebBrowser,false);
                                            break;
                                    }
                                    if (message.Msg!= 70 && message.Msg != 71 && message.Msg != 24 && message.Msg != 3 && message.Msg != 132 && message.Msg != 34 && message.Msg != 32 && message.Msg != 512
                                    && message.Msg != 675 && message.Msg != 33 && message.Msg != 15 && message.Msg != 61 && message.Msg != 131 && message.Msg != 124 && message.Msg != 125
                                     && message.Msg != 133 && message.Msg != 20 && message.Msg != 5 && message.Msg != 0)
                                    { 
                                        eventlist evtItems;
                                        evtItems.num = message.Msg;
                                        evtItems.num_msg = num_chromiumWebBrowser;
                                        event_lists.Add(evtItems);
                                    }
                                    return false;
                                }
                                return true;
                            });

                            break;
                        }
                        else
                        {
                            // Chrome hasn't yet set up its message-loop window.
                            await Task.Delay(10);
                        }
                    }
                }
                catch
                {
                    // Errors are likely to occur if browser is disposed, and no good way to check from another thread
                }
            });
        }

#endif
    }
}