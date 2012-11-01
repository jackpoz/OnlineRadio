using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineRadio.Core;
using System.Runtime.InteropServices;

namespace OnlineRadio.Plugins.WLM
{
    public class WLMPlugin : IPlugin , IDisposable
    {
        #region InteropServices
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hwnd, uint wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "FindWindowExA")]
        private static extern IntPtr FindWindowEx(IntPtr hWnd1, IntPtr hWnd2, string lpsz1, string lpsz2);

        [StructLayout(LayoutKind.Sequential)]
        private struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }
        private const int WM_COPYDATA = 0x4A;
        #endregion

        #region WLM SetMusic
        private static COPYDATASTRUCT data;
        private static IntPtr VarPtr(object e)
        {
            GCHandle GC = GCHandle.Alloc(e, GCHandleType.Pinned);
            IntPtr gc = GC.AddrOfPinnedObject();
            GC.Free();
            return gc;
        }

        public void SetSong(string title, string artist)
        {
            SetMSNMusic(true, title, artist);
        }

        public void RemoveSong()
        {
            SetMSNMusic(false);
        }

        private void SetMSNMusic(bool enable, string title = "", string artist = "")
        {
            string buffer = "\\0Music\\0" + (enable ? "1" : "0") + "\\0{0} - {1}\\0" + title + "\\0" + artist + "\\0\\0\0";
            int handle = 0;
            IntPtr handlePtr = new IntPtr(handle);

            data.dwData = (IntPtr)0x0547;
            data.lpData = VarPtr(buffer);
            data.cbData = buffer.Length * 2;

            // Call method to update IM's - PlayingNow
            handlePtr = FindWindowEx(IntPtr.Zero, handlePtr, "MsnMsgrUIManager", null);
            if (handlePtr.ToInt32() > 0)
                SendMessage(handlePtr, WM_COPYDATA, IntPtr.Zero, VarPtr(data));
        }
        #endregion

        public void Dispose()
        {
            RemoveSong();
        }

        string IPlugin.Name
        {
            get { return "WLMPlugin"; }
        }

        void IPlugin.OnCurrentSongChanged(object sender, CurrentSongEventArgs args)
        {
            SetSong(args.NewSong.Title, args.NewSong.Artist);
        }

        void IPlugin.OnStreamUpdate(object sender, StreamUpdateEventArgs args)
        {}
    }
}
