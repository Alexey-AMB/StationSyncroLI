using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

namespace AMB_USBFLASHNOTY
{
    /// <summary>
    /// Класс оповещения об изменении статуса устройств USB и Storage Card
    /// сначала Init, потом Run.
    /// В конце Dispose (или его вызовет уборка мусора)
    /// </summary>
    public class AMB_USBFlashNotification : IDisposable
    {
        #region Объявления
        Guid guidFatfsMount = new Guid (0x169e1941, 0x4ce, 0x4690, 0x97, 0xac, 0x77, 0x61, 0x87, 0xeb, 0x67, 0xcc);	//FATFS_MOUNT_GUID
        private const int MAX_DEVCLASS_NAMELEN = 64;
        private const int WAIT_FAILED = -1;
        private const int WAIT_OBJECT_0 = 0;
        private const int WAIT_TIMEOUT = 0x102;
        private const int WAIT_ABANDONED = 0x80;


        [DllImport("CoreDLL", SetLastError = true)]
        private static extern IntPtr CreateMsgQueue
        (
            string lpName,
            ref MSGQUEUEOPTIONS options
        );

        [DllImport("CoreDLL", SetLastError = true)]
        private static extern IntPtr RequestDeviceNotifications
        (
            ref Guid thisGUID,
            IntPtr hMsgQ,
            bool AllOrOne
        );

        [DllImport("CoreDLL", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.I4)]
        private static extern int WaitForMultipleObjects(int objectCount, IntPtr[] eventList, bool waitAll, int timeOut);

        [DllImport("CoreDLL", SetLastError = true)]
        private static extern IntPtr CreateEvent(
            IntPtr AlwaysNull0,
            [In, MarshalAs(UnmanagedType.Bool)]  bool ManualReset,
            [In, MarshalAs(UnmanagedType.Bool)]  bool bInitialState,
            [In, MarshalAs(UnmanagedType.BStr)]  string Name);

        [DllImport("coredll.dll", SetLastError = true)]
        extern private static Int32 ReadMsgQueue(IntPtr hMsgQ, byte[] lpBuffer, UInt32 cbBufferSize,
            out UInt32 lpNumberOfBytesRead, UInt32 dwTimeout, out UInt32 pdwFlags);

        [DllImport("CoreDLL")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("CoreDLL", SetLastError = true)]
        private static extern IntPtr CloseMsgQueue
        (
            IntPtr hMsgQueue
        );

        [DllImport("CoreDLL", SetLastError = true)]
        private static extern bool StopDeviceNotifications
        (
            IntPtr hReg
        );

        #endregion

        #region Структуры

        [StructLayout(LayoutKind.Sequential)]
        private struct MSGQUEUEOPTIONS
        {
            public UInt32 dwSize;
            public UInt32 dwFlags;
            public UInt32 dwMaxMessages;
            public UInt32 cbMaxMessage;
            public Int32 bReadAccess;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DEVDETAIL
        {
            public Guid guidDevClass;
            public UInt32 dwReserved;
            public bool fAttached;
            public int cbName;
            public char[] szName;
        }

        #endregion

        private IntPtr myQueue = IntPtr.Zero;
        private IntPtr hReg = IntPtr.Zero;
        private bool bAbort = false;
        private Thread thLissen = null;

        public delegate void MyEv_NewNotifi(string sNameDevice, bool bStatus);
        public static event MyEv_NewNotifi NewNotification;
        /// <summary>
        /// Начало работы. Если все нормально возвращает TRUE.
        /// </summary>
        /// <returns></returns>
        public bool Init()
        {
            try
            {
                //DEVDETAIL dd = new DEVDETAIL();
                //byte[] pPNPBuf = new byte[Marshal.SizeOf(dd) + MAX_DEVCLASS_NAMELEN * sizeof(char)];
                byte[] pPNPBuf = new byte[153];

                MSGQUEUEOPTIONS options = new MSGQUEUEOPTIONS();
                options.dwSize = (UInt32)Marshal.SizeOf(options);
                options.dwFlags = 0;
                options.dwMaxMessages = 0;
                options.cbMaxMessage = 153; //sizeof(POWER_BROADCAST) + MAX_NAMELEN;
                options.bReadAccess = 1;

                myQueue = CreateMsgQueue(null, ref options);
                if (myQueue.Equals(IntPtr.Zero)) return false;
                hReg = RequestDeviceNotifications(ref guidFatfsMount, myQueue, true);
                if (hReg.Equals(IntPtr.Zero)) return false;
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Запуск слушателя
        /// </summary>
        public void Run()
        {
            bAbort = false;
            thLissen = new System.Threading.Thread(new System.Threading.ThreadStart(this.LissenQueue));
            thLissen.Start();
        }

        private void LissenQueue()
        {
            try
            {
                IntPtr hExitEvent = CreateEvent(IntPtr.Zero, false, false, null);
                IntPtr[] hWait = new IntPtr[2];
                hWait[0] = hExitEvent;
                hWait[1] = myQueue;

                int dwRet = 0;
                while (!bAbort)
                {
                    dwRet = WaitForMultipleObjects(2, hWait, false, 1000);
                    if (bAbort) break;
                    switch (dwRet)
                    {
                        case WAIT_FAILED:
                            return;// FALSE;
                        case WAIT_OBJECT_0:
                            return;// TRUE;
                        case WAIT_OBJECT_0 + 1:
                            ReadMsgQ();
                            break;
                        case WAIT_TIMEOUT:
                            break;
                    }
                }
            }
            catch //(Exception ex)
            {
                //throw new Exception("The method LissenQueue error.");
            }
        }

        private void ReadMsgQ()
        {
            UInt32 bytesRead;
            uint timeout = 0;
            UInt32 flags;
            try
            {
                if(myQueue.Equals(IntPtr.Zero)) return;
                //DEVDETAIL dd = new DEVDETAIL();
                //byte[] pPNPBuf = new byte[Marshal.SizeOf(dd) + MAX_DEVCLASS_NAMELEN * sizeof(char)];
                byte[] pPNPBuf = new byte[153];
                int result = ReadMsgQueue(
                        myQueue,
                        pPNPBuf,
                        153,
                        out bytesRead,
                        timeout,
                        out flags);

                System.Text.UnicodeEncoding TextEncoder = new UnicodeEncoding();
                string sDeviceName = TextEncoder.GetString(pPNPBuf, 28, (int)pPNPBuf[24]);
                sDeviceName = sDeviceName.Substring(0, sDeviceName.IndexOf('\0'));
                bool bStat = false;
                if(pPNPBuf[20] >0) bStat = true;
                NewNotification(sDeviceName, bStat);
            }
            catch //(Exception ex)
            {
            }
        }

        /// <summary>
        /// Завершение слушателя
        /// </summary>
        public void Dispose()
        {
            bAbort = true;            

            try
            {
                StopDeviceNotifications(hReg);
            }
            catch { }

            try
            {
                CloseHandle(hReg);
            }
            catch { }
            try
            {
                CloseMsgQueue(myQueue);
                myQueue = IntPtr.Zero;
            }
            catch { }

            if (thLissen != null) thLissen.Abort();
        }
    }
}
