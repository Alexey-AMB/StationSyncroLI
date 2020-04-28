using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace AMB_SPI
{
    public class AMB_SPI
    {
        #region Импорт Dll
        [DllImport("coredll.dll", EntryPoint = "DeviceIoControl", SetLastError = true)]
        internal static extern bool DeviceIoControl(
            IntPtr hDevice,
            int dwIoControlCode,
            UInt32[] lpInBuffer,
            int nInBufferSize,
            UInt32[] lpOutBuffer,
            int nOutBufferSize,
            ref int lpBytesReturned,
            IntPtr lpOverlapped);

        [DllImport("coredll", SetLastError = true)]
        static extern IntPtr CreateFile(
            String lpFileName, 
            UInt32 dwDesiredAccess, 
            UInt32 dwShareMode, 
            IntPtr lpSecurityAttributes, 
            UInt32 dwCreationDisposition, 
            UInt32 dwFlagsAndAttributes, 
            IntPtr hTemplateFile);

        [DllImport("coredll.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        private const int IOCTL_SPI_SET_PARAMS = 0x00222008;    //		CTL_CODE(FILE_DEVICE_UNKNOWN, 2050, METHOD_BUFFERED, FILE_ANY_ACCESS)		// 0x00222008
        private const int IOCTL_SPI_RW = 0x00222004;            //		CTL_CODE(FILE_DEVICE_UNKNOWN, 2049, METHOD_BUFFERED, FILE_ANY_ACCESS)		// 0x00222004
        #endregion

        private IntPtr ipHandle = IntPtr.Zero;

        /// <summary>
        /// Инициализация работы с SPI.
        /// </summary>
        /// <param name="iDataSize">Размер слова SPI в битах.</param>
        /// <param name="iFreq">Частота обмена по шине в килогерцах.</param>
        /// <returns>TRUE - все хорошо.</returns>
        public bool Init(int iDataSize, int iFreq)
        {
            bool bRet = false;

            ipHandle = CreateFile("SPI1:", 0, 0, IntPtr.Zero, 0, 0, IntPtr.Zero);

            if (ipHandle.ToInt32() == -1) return false;

            int dwRet = 0;
            UInt32[] bBufIn = new UInt32[2];
            bBufIn[0] = (UInt32)iDataSize;
            bBufIn[1] = (UInt32)iFreq;

            bRet = DeviceIoControl(ipHandle, IOCTL_SPI_SET_PARAMS, bBufIn, sizeof(UInt32) * 2, null, 0, ref dwRet, IntPtr.Zero);

            return bRet;
        }
        /// <summary>
        /// Закрыть обмен.
        /// </summary>
        public void Close()
        {
            try
            {
                if (!ipHandle.Equals(IntPtr.Zero)) CloseHandle(ipHandle);
            }
            catch { }
        }
        /// <summary>
        /// Обмен по SPI. Отправка и прием за одно действие.
        /// </summary>
        /// <param name="bBufRecive">Массив dword пришедший С шины.</param>
        /// <param name="bBufSend">Массив dword для записи НА шину.</param>
        /// <returns></returns>
        public bool GetSendBuffer(ref UInt32[] bBufRecive, ref UInt32[] bBufSend)
        {
            bool bRet = false;
            int dwRet = 0;

            bRet = DeviceIoControl(ipHandle, IOCTL_SPI_RW, bBufSend, bBufSend.Length, bBufRecive, bBufRecive.Length, ref dwRet, IntPtr.Zero);

            return bRet;
        }
    }
}
