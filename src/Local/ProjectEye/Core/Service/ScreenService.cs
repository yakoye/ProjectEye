using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace ProjectEye.Core.Service
{
    /// <summary>
    /// 屏幕监听服务
    /// 用于处理插拔显示器、屏幕锁定等事件
    /// </summary>
    public class ScreenService : IService
    {
        private const int WM_DISPLAYCHANGE = 0x007e;
        private const int WM_POWERBROADCAST = 0x0218;
        private const int PBT_APMSUSPEND = 0x0004;
        private const int PBT_APMRESUMESUSPEND = 0x0007;
        private const int PBT_APMRESUMEAUTOMATIC = 0x0012;
        private const int ga = 0x020A;
        
        private readonly DispatcherTimer timer;
        private HwndSource source;
        private readonly HwndSourceHook hwndSourceHook;

        /// <summary>
        /// 屏幕锁定事件
        /// </summary>
        public event EventHandler OnScreenLocked;

        /// <summary>
        /// 屏幕解锁事件
        /// </summary>
        public event EventHandler OnScreenUnlocked;

        public ScreenService()
        {
            hwndSourceHook = new HwndSourceHook(WndProc);

            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 3);

        }

        private void timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            WindowManager.UpdateAllScreensWindow("TipWindow", true);
        }

        public void Init()
        {
            //创建一个隐藏的窗口，用于接收显示器拔插消息
            Window hookWindow = new Window();
            hookWindow.Width = 0;
            hookWindow.Height = 0;
            hookWindow.ShowInTaskbar = false;
            hookWindow.WindowStyle = WindowStyle.None;
            //hookWindow.WindowState = WindowState.Minimized;
            hookWindow.Visibility = Visibility.Hidden;
            hookWindow.SourceInitialized += new EventHandler(hookWindow_SourceInitialized);
            hookWindow.Show();
        }

        public void Dispose()
        {
            if (source != null)
            {
                source.RemoveHook(hwndSourceHook);
                source.Dispose();
            }
        }

        private void hookWindow_SourceInitialized(object sender, EventArgs e)
        {
            source = HwndSource.FromHwnd(new WindowInteropHelper((Window)sender).Handle);
            source.AddHook(hwndSourceHook);

        }



        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_DISPLAYCHANGE:
                    timer.Start();
                    break;
                case WM_POWERBROADCAST:
                    HandlePowerBroadcast(wParam);
                    break;
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// 处理电源广播消息
        /// </summary>
        private void HandlePowerBroadcast(IntPtr wParam)
        {
            int powerEvent = (int)wParam;
            switch (powerEvent)
            {
                case PBT_APMSUSPEND:
                    // 系统进入睡眠或屏幕锁定
                    OnScreenLocked?.Invoke(this, EventArgs.Empty);
                    break;
                case PBT_APMRESUMESUSPEND:
                case PBT_APMRESUMEAUTOMATIC:
                    // 系统恢复或屏幕解锁
                    OnScreenUnlocked?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
    }
}
