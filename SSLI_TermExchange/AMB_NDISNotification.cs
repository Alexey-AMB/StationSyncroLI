using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

namespace AMB_NDISNOTI
{
    /// <summary>
    /// Класс оповещения об изменении статуса сетевых устройств
    /// сначала Init, потом Run.
    /// В конце Dispose (или его вызовет уборка мусора)
    /// </summary>
    public class AMB_NDISNotification : IDisposable
    {
        #region Объявления

        private const int WAIT_FAILED = -1;
        private const int WAIT_OBJECT_0 = 0;
        private const int WAIT_TIMEOUT = 0x102;
        private const int WAIT_ABANDONED = 0x80;

        private const string NDISUIO_DEVICE_NAME = "UIO1:";

        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x1;
        private const uint FILE_SHARE_WRITE = 0x2;
        private const uint CREATE_ALWAYS = 2;
        private const uint OPEN_EXISTING = 3;
        private const uint WRITE_ERROR = 0;
        private const uint READ_ERROR = 0;
        private const int INVALID_HANDLE = -1;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private const uint FILE_FLAG_OVERLAPPED = 0x40000000;
        private const uint METHOD_BUFFERED = 0;
        private const uint FILE_ANY_ACCESS = 0;

        [DllImport("coredll", SetLastError = true)]
        static extern IntPtr CreateFile(
            String lpFileName,
            UInt32 dwDesiredAccess,
            UInt32 dwShareMode,
            IntPtr lpSecurityAttributes,
            UInt32 dwCreationDisposition,
            UInt32 dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("coredll.dll", EntryPoint = "DeviceIoControl", SetLastError = true)]
        internal static extern int DeviceIoControlCE(
            IntPtr hDevice,
            uint dwIoControlCode,
            ref NDISUIO_REQUEST_NOTIFICATION lpInBuffer,
            int nInBufferSize,
            byte[] lpOutBuffer,
            int nOutBufferSize,
            ref int lpBytesReturned,
            IntPtr lpOverlapped);

        [DllImport("CoreDLL", SetLastError = true)]
        private extern static int WaitForSingleObject(IntPtr hEvent, int timeout);

        [DllImport("CoreDLL", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.I4)]
        private static extern int WaitForMultipleObjects(int objectCount, IntPtr[] eventList, bool waitAll, int timeOut);

        [DllImport("CoreDLL", SetLastError = true)]
        private static extern IntPtr CreateEvent(
            IntPtr AlwaysNull0,
            [In, MarshalAs(UnmanagedType.Bool)]  bool ManualReset,
            [In, MarshalAs(UnmanagedType.Bool)]  bool bInitialState,
            [In, MarshalAs(UnmanagedType.BStr)]  string Name);

        [DllImport("CoreDLL", SetLastError = true)]
        private static extern IntPtr CreateMsgQueue
        (
            string lpName,
            ref MSGQUEUEOPTIONS options
        );

        [DllImport("CoreDLL", SetLastError = true)]
        private static extern IntPtr CloseMsgQueue
        (
            IntPtr hMsgQueue
        );


        [DllImport("coredll.dll", SetLastError = true)]
        extern private static Int32 ReadMsgQueue(IntPtr hMsgQ, byte[] lpBuffer, UInt32 cbBufferSize,
            out UInt32 lpNumberOfBytesRead, UInt32 dwTimeout, out UInt32 pdwFlags);


        [DllImport("CoreDLL", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WriteMsgQueue(
            IntPtr hMsgQ,
            byte[] buffer,
            int cbDataSize,
            int dwTimeout,
            int flags);

        [DllImport("CoreDLL", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMsgQueueInfo(IntPtr hHandle, ref MessageQueueInfo info);

        [DllImport("CoreDLL")]
        private static extern bool CloseHandle(IntPtr hObject);

        #endregion

        #region Структуры

        public struct NDISUIO_REQUEST_NOTIFICATION
        {
            public IntPtr hMsgQueue;
            public uint dwNotificationTypes;
        }

        public struct MessageQueueInfo
        {
            public int dwSize;
            public int dwFlags;
            public int dwMaxMessages;
            public int cbMaxMessages;
            public int dwCurrentMessages;
            public int dwMaxQueueMessages;
            public Int16 NumReaders;
            public Int16 NumWriters;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSGQUEUEOPTIONS
        {
            public UInt32 dwSize;
            public UInt32 dwFlags;
            public UInt32 dwMaxMessages;
            public UInt32 cbMaxMessage;
            public Int32 bReadAccess;
        }

        #endregion

        private IntPtr myQueue = IntPtr.Zero;
        private bool bAbort = false;
        private Thread thLissen = null;
        private IntPtr hReg = IntPtr.Zero;
        /// <summary>
        /// Событие при изменении статуса сетевых устройств
        /// </summary>
        /// <param name="sNameNDIS">Имя устройства вызвавшего событие</param>
        /// <param name="bStatus">TRUE - было подключено, FALSE - было отключено</param>
        /// <returns></returns>
        public delegate void MyEv_NewNotifi(string sNameNDIS, bool bStatus);
        public static event MyEv_NewNotifi NewNotification;
        /// <summary>
        /// Начало работы. Если все нормально возвращает TRUE.
        /// </summary>
        /// <returns></returns>
        public bool Init()
        {
            try
            {
                MSGQUEUEOPTIONS options = new MSGQUEUEOPTIONS();
                options.dwSize = (UInt32)Marshal.SizeOf(options);
                options.cbMaxMessage = (UInt32)532;
                options.bReadAccess = 1;

                myQueue = CreateMsgQueue(null, ref options);
                if (myQueue.Equals(IntPtr.Zero)) return false;
                hReg = CreateFile(
                               NDISUIO_DEVICE_NAME,                            //	Object name.
                               0x00,                                           //	Desired access.
                               0x00,                                           //	Share Mode.
                               IntPtr.Zero,                                           //	Security Attr
                               OPEN_EXISTING,                                  //	Creation Disposition.
                               FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OVERLAPPED,   //	Flag and Attributes..
                               IntPtr.Zero);
                if (hReg.Equals(IntPtr.Zero)) return false;
                NDISUIO_REQUEST_NOTIFICATION NdisNotify = new NDISUIO_REQUEST_NOTIFICATION();
                NdisNotify.hMsgQueue = myQueue;
                NdisNotify.dwNotificationTypes = 0x6ff;	//0x00000040 | 0x00000080;

                uint IOCTL_NDISUIO_REQUEST_NOTIFICATION = CTL_CODE(0x12, 0x207, METHOD_BUFFERED, FILE_ANY_ACCESS);
                int i = 0;
                int iRet = DeviceIoControlCE(
                    hReg,
                    IOCTL_NDISUIO_REQUEST_NOTIFICATION,
                    ref NdisNotify,
                    Marshal.SizeOf(NdisNotify),
                    null,
                    0,
                    ref i,
                    IntPtr.Zero);
                if (iRet == 1) return true;
                else return false;
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
            catch
            {
                throw new Exception("The method LissenQueue error.");
            }
        }

        private void ReadMsgQ()
        {
            UInt32 bytesRead;
            uint timeout = 0;
            UInt32 flags;
            try
            {
                byte[] buff = new byte[532];
                int result = ReadMsgQueue(
                        myQueue,
                        buff,
                        532,
                        out bytesRead,
                        timeout,
                        out flags);

                int iNotificationType = BitConverter.ToInt32(buff, 0);
                System.Text.UnicodeEncoding TextEncoder = new UnicodeEncoding();
                string sDeviceName = TextEncoder.GetString(buff, 4, (int)bytesRead - 4);
                sDeviceName = sDeviceName.Substring(0, sDeviceName.IndexOf('\0'));
                bool bStat = false;
                if ((iNotificationType == 4) || (iNotificationType == 16)) bStat = true;   //4 = подключен, 8 - отключен
                NewNotification(sDeviceName, bStat);
            }
            catch
            {

            }
        }
        
        private uint CTL_CODE(uint DeviceType, uint Function, uint Method, uint Access)
        {
            return ((DeviceType << 16) | (Access << 14) | (Function << 2) | Method);
        }
        /// <summary>
        /// Завершение слушателя
        /// </summary>
        public void Dispose()
        {
            bAbort = true;
            if(thLissen!= null)thLissen.Abort();

            NDISUIO_REQUEST_NOTIFICATION NdisNotify = new NDISUIO_REQUEST_NOTIFICATION();
            uint IOCTL_NDISUIO_REQUEST_NOTIFICATION = CTL_CODE(0x12, 0x208, METHOD_BUFFERED, FILE_ANY_ACCESS);
            int i = 0;
            try
            {
                int iRet = DeviceIoControlCE(
                    hReg,
                    IOCTL_NDISUIO_REQUEST_NOTIFICATION,
                    ref NdisNotify,
                    0,
                    null,
                    0,
                    ref i,
                    IntPtr.Zero);
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
            }
            catch { }
        }

    }

}
