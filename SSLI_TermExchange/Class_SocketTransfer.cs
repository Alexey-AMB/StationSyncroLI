using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;

namespace SocketTransfer
{
    public class SocketTransfer
    {
        private const int lenghtReciveBuffer = 4096; //длинна приемного буфера для обмена по ТСР max 8192
        public bool bAbort_thread
        {
            get
            {
                return abort_thread;
            }
            set
            {
                if (netStream != null)
                {
                    netStream.Close();
                    netStream = null;
                }
                if (socketForClient != null)
                {
                    socketForClient.Close();
                    socketForClient = null;
                }
                if (tcpListener != null)
                {
                    tcpListener.Stop();
                    tcpListener = null;
                }
                if (handler != null)
                {
                    handler.abort_thread = value;
                }
                if (client != null) client.abort_thread = value;
                abort_thread = value;
            }
        }
        private const int iLenHeader = 15;
        private const int iNumRepet = 100;   //максимальное количество презапросов
        private static System.Threading.TimerCallback tDelegate = null;
        private bool abort_thread = false;

        private TcpListener tcpListener = null;
        private Socket socketForClient = null;

        public SocketTransferHandler handler = null;
        public SocketTransferClient client = null;

        private NetworkStream netStream = null;

        public void InitHandler(string sIPServerAdress, int sIPPort)
        {
            IPAddress localAddr = IPAddress.Parse(sIPServerAdress);
            tcpListener = new TcpListener(localAddr, sIPPort);
            tcpListener.Start();

            while (!tcpListener.Pending())
            {
                if (abort_thread)
                {
                    return;
                }
                System.Threading.Thread.Sleep(30);
                if (tcpListener == null) return;
            }
            try
            {
                socketForClient = tcpListener.AcceptSocket();
                netStream = new NetworkStream(socketForClient);
                handler = new SocketTransferHandler(netStream);
                tDelegate = new System.Threading.TimerCallback(handler.OnTimerWriteFile);
                handler.StartRead();
            }
            catch { }

            if (netStream != null)
            {
                netStream.Close();
                netStream = null;
            }
            if (socketForClient != null)
            {
                socketForClient.Close();
                socketForClient = null;
            }
            if (tcpListener != null)
            {
                tcpListener.Stop();
                tcpListener = null;
            }
        }
        public bool InitClient(string sIPServerAdress, int sIPPort)
        {
            bool bRet = false;
            try
            {
                TcpClient tcpSocket = new TcpClient(sIPServerAdress, sIPPort);
                NetworkStream streamToServer = tcpSocket.GetStream();
                client = new SocketTransferClient(streamToServer);
                bRet = true;
            }
            catch //(Exception ex)
            {
                bRet = false;
            }
            return bRet;
        }

        public delegate void STC_SendFileTo_Progress(int procent);

        public class SocketTransferHandler
        {
            private byte[] buffer;
            private byte[] buffer_out;
            private byte[] buffer_old;
            private string sFullFileName = null;
            private NetworkStream networkStream;
            private System.IO.Stream fileReadStream;
            private System.IO.Stream fileWriteStream;
            private Int32 iLenghtBlock = 0;
            private Int32 iCRC = 0;
            public Int32 iNumPacket = 0;
            private Int32 iNumPacketExpected = 1;
            private int iBuf_old_len = 0;
            private int iNumFalseRepet = 0; //текущее количество перезапросов
            /// <summary>
            /// true = Завалить слушателя
            /// </summary>
            public bool abort_thread = false;
            private object oLock = new object();

            public delegate void MyEv_NewMessage(string mess);
            public static event MyEv_NewMessage NewMessage;
            /// <summary>
            /// 0-клиент еще ничего не передал и не принял, 1 - клиент жив, -1 - клиент сдох
            /// </summary>
            public int iStatus = 0;

            private System.Threading.Timer timerWriteFile = null;

            public SocketTransferHandler(NetworkStream netSreamForClient)
            {
                buffer = new byte[lenghtReciveBuffer];
                buffer_out = new byte[lenghtReciveBuffer];
                buffer_old = new byte[lenghtReciveBuffer];
                networkStream = netSreamForClient;
                iStatus = 0;
            }
            public void StartRead()		// begin reading the string from the client
            {
                try
                {
                    Buffer.SetByte(buffer, 0, 0);
                    WaitForOK();
                }
                catch
                {
                    Destroy();
                }
            }

            private bool CheckCRC()
            {
                Int32 iRealCRC = 0;
                iLenghtBlock = BitConverter.ToInt32(buffer, 5);
                iCRC = BitConverter.ToInt32(buffer, 9);
                Int32 iLen = iLenghtBlock;
                if (iLenghtBlock > lenghtReciveBuffer) iLen = lenghtReciveBuffer - iLenHeader;
                try
                {
                    for (int i = iLenHeader; i < iLen + iLenHeader; i++) iRealCRC = (Int32)(iRealCRC + buffer[i]);
                    if (iCRC == iRealCRC) return true;
                }
                catch { return false; }
                return false;
            }
            private bool CheckCRCHeader()
            {
                bool bRet = false;
                Int16 iRealCRC = 0;
                try
                {
                    Int16 iCRC = BitConverter.ToInt16(buffer, 13);
                    Int32 iLen = BitConverter.ToInt32(buffer, 5);

                    for (int i = 0; i < iLenHeader - 2; i++) iRealCRC = (Int16)(iRealCRC + buffer[i]);
                    if (iCRC == iRealCRC) bRet = true;
                }
                catch { bRet = false; }
                return bRet;
            }
            private void ParseTypeCommand(byte bCommand)
            {
                int iReadBytes = 0;

                switch (bCommand)
                {
                    case 0:	//сообщение или комманда для терминала тело комманды в блоке
                        if (NewMessage != null)
                        {
                            NewMessage(System.Text.Encoding.UTF8.GetString(buffer, iLenHeader, iLenghtBlock));
                        }
                        SendToNet(iNumPacket.ToString() + ":OK", iNumPacket, 0);
                        break;
                    case 1:	//создание нового файла имя файла в блоке
                        sFullFileName = System.Text.Encoding.UTF8.GetString(buffer, iLenHeader, iLenghtBlock);
                        try
                        {
                            if (File.Exists(sFullFileName)) File.Delete(sFullFileName);
                            fileWriteStream = File.Create(sFullFileName);
                            timerWriteFile = new System.Threading.Timer(tDelegate, null, 30000, -1);
                            SendToNet("Create " + sFullFileName + ":OK", iNumPacket, 0);
                        }
                        catch (Exception ex)
                        {
                            SendToNet(sFullFileName + " - CREATE ERROR:" + ex.Message, iNumPacket, 0);
                        }
                        break;
                    case 2: //запись в открытый файл в блоке данные
                        if (fileWriteStream != null)
                        {
                            if (fileWriteStream.CanWrite)
                            {
                                fileWriteStream.Write(buffer, iLenHeader, iLenghtBlock);
                                if (timerWriteFile != null) timerWriteFile.Change(30000, -1);
                                SendToNet(iNumPacket.ToString() + ":OK", iNumPacket, 0);
                            }
                            else SendToNet(sFullFileName + " - ERROR WRITE ", iNumPacket, 0);
                        }
                        else SendToNet(sFullFileName + " - ERROR NOT CREATE ", iNumPacket, 0);
                        break;
                    case 3: //закрытие файла блок пуст
                        if (fileWriteStream != null)
                        {
                            timerWriteFile.Change(-1, -1);
                            timerWriteFile.Dispose();
                            timerWriteFile = null;
                            fileWriteStream.Flush();
                            fileWriteStream.Close();
                            fileWriteStream = null;
                            SendToNet("Close:OK", iNumPacket, 0);
                        }
                        else SendToNet(sFullFileName + " - ERROR CAN'T CLOSE", iNumPacket, 0);
                        break;
                    case 4: //есть ли такой файл имя файла в блоке
                        sFullFileName = System.Text.Encoding.UTF8.GetString(buffer, iLenHeader, iLenghtBlock);
                        if (File.Exists(sFullFileName)) SendToNet(sFullFileName + " :OK", iNumPacket, 5);
                        else SendToNet(sFullFileName + " :NONE", iNumPacket, 6);
                        break;
                    case 5:	//передача файла в сеть имя файла в блоке
                        sFullFileName = System.Text.Encoding.UTF8.GetString(buffer, iLenHeader, iLenghtBlock);
                        fileReadStream = File.OpenRead(sFullFileName);
                        iReadBytes = fileReadStream.Read(buffer_out, iLenHeader, buffer.Length - iLenHeader);
                        SendToNet(ref buffer_out, iNumPacket, 0, iReadBytes);
                        break;
                    case 6:	//продолжение передачи файла в сеть блок пуст
                        if (fileReadStream != null)
                        {
                            if (fileReadStream.CanRead)
                            {
                                iReadBytes = fileReadStream.Read(buffer_out, iLenHeader, buffer.Length - iLenHeader);
                                if (iReadBytes <= 0)
                                {
                                    fileReadStream.Close();
                                    fileReadStream = null;
                                    SendToNet("EOF", iNumPacket, 7);
                                }
                                else SendToNet(ref buffer_out, iNumPacket, 0, iReadBytes);
                            }
                            else SendToNet("ERROR FILE NOT READABLE", iNumPacket, 7);
                        }
                        else SendToNet("ERROR FILE NOT OPEN OR EOF", iNumPacket, 7);
                        break;
                    case 7:	//список файлов, имя папки в блоке
                        sFullFileName = System.Text.Encoding.UTF8.GetString(buffer, iLenHeader, iLenghtBlock);
                        if (Directory.Exists(sFullFileName))
                        {
                            DirectoryInfo di = new DirectoryInfo(sFullFileName);
                            FileInfo[] diFile = di.GetFiles();
                            string sRes = "";
                            foreach (FileInfo s in diFile) sRes = sRes + s.Name + "\n\r";
                            SendToNet(sRes, iNumPacket, 4);
                        }
                        else SendToNet(sFullFileName + " :NONE", iNumPacket, 6);
                        break;
                    case 8: //список папок, имя папки в блоке
                        sFullFileName = System.Text.Encoding.UTF8.GetString(buffer, iLenHeader, iLenghtBlock);
                        if (Directory.Exists(sFullFileName))
                        {
                            DirectoryInfo di = new DirectoryInfo(sFullFileName);
                            DirectoryInfo[] diSub = di.GetDirectories();
                            string sRes = "";
                            foreach (DirectoryInfo s in diSub) sRes = sRes + s.Name + "\n\r";
                            SendToNet(sRes, iNumPacket, 3);
                        }
                        else SendToNet(sFullFileName + " :NONE", iNumPacket, 6);
                        break;
                    case 9: //ошибка срс повторите
                        SendToNet(ref buffer_old, iNumPacket - 1, 0, iBuf_old_len);
                        break;
                    case 10: //удалить файл имя файла в блоке
                        sFullFileName = System.Text.Encoding.UTF8.GetString(buffer, iLenHeader, iLenghtBlock);
                        if (File.Exists(sFullFileName))
                        {
                            File.Delete(sFullFileName);
                            SendToNet(sFullFileName + " :DELETED", iNumPacket, 6);
                        }
                        else SendToNet(sFullFileName + " :NONE", iNumPacket, 6);
                        break;
                    case 11: //место на диске
                        string sDirName = System.Text.Encoding.UTF8.GetString(buffer, iLenHeader, iLenghtBlock);
                        long iFree, iTotal;
                        MemoryStatus.GetStorageInfo(sDirName, out iTotal, out iFree);
                        SendToNet(iFree.ToString(), iNumPacket, 8);
                        break;
                    case 12: //закрыть сервер
                        abort_thread = true;
                        SendToNet("CLOSE:OK", iNumPacket, 0);
                        System.Threading.Thread.Sleep(500);
                        Destroy();
                        break;
                }
            }

            public void SendToNet(string sMessToNet)
            {
                byte[] bufNumPack = new byte[4];
                byte[] bufLenght = new byte[4];
                byte[] bufCrc = new byte[4];
                lock (oLock)
                {
                    try
                    {
                        if (sMessToNet.Length > lenghtReciveBuffer - iLenHeader) sMessToNet = sMessToNet.Substring(0, lenghtReciveBuffer - iLenHeader);
                        for (int i = 0; i < lenghtReciveBuffer; i++) Buffer.SetByte(buffer_out, i, 0);
                        Int32 ilenght = sMessToNet.Length;
                        System.Text.Encoding.UTF8.GetBytes(sMessToNet, 0, ilenght, buffer_out, iLenHeader);
                        bufLenght = BitConverter.GetBytes((Int32)ilenght);
                        bufNumPack = BitConverter.GetBytes((Int32)0);
                        Int32 icrc = 0;
                        for (int i = iLenHeader; i < (ilenght + iLenHeader); i++) icrc = icrc + buffer_out[i];
                        bufCrc = BitConverter.GetBytes(icrc);
                        Buffer.BlockCopy(bufNumPack, 0, buffer_out, 1, 4);
                        Buffer.BlockCopy(bufLenght, 0, buffer_out, 5, 4);
                        Buffer.BlockCopy(bufCrc, 0, buffer_out, 9, 4);
                        buffer_out[0] = 0;

                        byte[] bufCrcHead = new byte[2];
                        Int16 icrch = 0;
                        for (int i = 0; i < iLenHeader - 2; i++) icrch = (Int16)(icrch + buffer_out[i]);
                        bufCrcHead = BitConverter.GetBytes((Int16)icrch);
                        Buffer.BlockCopy(bufCrcHead, 0, buffer_out, 13, 2);

                        Buffer.BlockCopy(buffer_out, 0, buffer_old, 0, ilenght + iLenHeader);
                        iBuf_old_len = ilenght + iLenHeader;
                        networkStream.Write(buffer_out, 0, ilenght + iLenHeader);
                    }

                    catch
                    {
                        iStatus = -1;
                    }
                }
            }
            public void SendToNet(string sMessToNet, Int32 iNumPack, byte byResult)
            {
                byte[] bufNumPack = new byte[4];
                byte[] bufLenght = new byte[4];
                byte[] bufCrc = new byte[4];
                lock (oLock)
                {
                    try
                    {

                        if (sMessToNet.Length > lenghtReciveBuffer - iLenHeader) sMessToNet = sMessToNet.Substring(0, lenghtReciveBuffer - iLenHeader);
                        for (int i = 0; i < lenghtReciveBuffer; i++) Buffer.SetByte(buffer_out, i, 0);
                        Int32 ilenght = sMessToNet.Length;
                        System.Text.Encoding.UTF8.GetBytes(sMessToNet, 0, ilenght, buffer_out, iLenHeader);
                        bufLenght = BitConverter.GetBytes((Int32)ilenght);
                        bufNumPack = BitConverter.GetBytes((Int32)iNumPack);
                        Int32 icrc = 0;
                        for (int i = iLenHeader; i < (ilenght + iLenHeader); i++) icrc = icrc + buffer_out[i];
                        bufCrc = BitConverter.GetBytes(icrc);
                        Buffer.BlockCopy(bufNumPack, 0, buffer_out, 1, 4);
                        Buffer.BlockCopy(bufLenght, 0, buffer_out, 5, 4);
                        Buffer.BlockCopy(bufCrc, 0, buffer_out, 9, 4);
                        buffer_out[0] = byResult;

                        byte[] bufCrcHead = new byte[2];
                        Int16 icrch = 0;
                        for (int i = 0; i < iLenHeader - 2; i++) icrch = (Int16)(icrch + buffer_out[i]);
                        bufCrcHead = BitConverter.GetBytes((Int16)icrch);
                        Buffer.BlockCopy(bufCrcHead, 0, buffer_out, 13, 2);

                        Buffer.BlockCopy(buffer_out, 0, buffer_old, 0, ilenght + iLenHeader);
                        iBuf_old_len = ilenght + iLenHeader;
                        networkStream.Write(buffer_out, 0, ilenght + iLenHeader);
                        networkStream.Flush();
                    }
                    catch
                    {
                        iStatus = -1;
                    }
                }
            }
            public void SendToNet(ref byte[] bBuferToNet, Int32 iNumPack, byte byResult, Int32 ilenght)
            {
                byte[] bufNumPack = new byte[4];
                byte[] bufLenght = new byte[4];
                byte[] bufCrc = new byte[4];
                lock (oLock)
                {
                    try
                    {
                        bufLenght = BitConverter.GetBytes((Int32)ilenght);
                        bufNumPack = BitConverter.GetBytes((Int32)iNumPack);
                        Int32 icrc = 0;
                        for (int i = iLenHeader; i < (ilenght + iLenHeader); i++) icrc = icrc + bBuferToNet[i];
                        bufCrc = BitConverter.GetBytes(icrc);
                        Buffer.BlockCopy(bufNumPack, 0, bBuferToNet, 1, 4);
                        Buffer.BlockCopy(bufLenght, 0, bBuferToNet, 5, 4);
                        Buffer.BlockCopy(bufCrc, 0, bBuferToNet, 9, 4);
                        bBuferToNet[0] = byResult;

                        byte[] bufCrcHead = new byte[2];
                        Int16 icrch = 0;
                        for (int i = 0; i < iLenHeader - 2; i++) icrch = (Int16)(icrch + buffer_out[i]);
                        bufCrcHead = BitConverter.GetBytes((Int16)icrch);
                        Buffer.BlockCopy(bufCrcHead, 0, buffer_out, 13, 2);

                        Buffer.BlockCopy(bBuferToNet, 0, buffer_old, 0, ilenght + iLenHeader);
                        iBuf_old_len = ilenght + iLenHeader;
                        networkStream.Write(bBuferToNet, 0, ilenght + iLenHeader);
                        networkStream.Flush();
                    }
                    catch
                    {
                        iStatus = -1;
                    }
                }
            }

            private void WaitForOK()
            {
                Int32 iLenExpected = 0;
                try
                {
                    while (!abort_thread)
                    {
                        if (networkStream.DataAvailable)
                        {
                            int iBytesRead = networkStream.Read(buffer, 0, iLenHeader);
                            if (iBytesRead >= iLenHeader)
                            {
                                if (CheckCRCHeader())
                                {
                                    iLenExpected = BitConverter.ToInt32(buffer, 5);
                                    do
                                    {
                                        iBytesRead = iBytesRead + networkStream.Read(buffer, iBytesRead, iLenExpected - iBytesRead + iLenHeader);

                                    } while (iBytesRead < (iLenExpected + iLenHeader));
                                    //string sFullFileName = System.Text.Encoding.UTF8.GetString(buffer, iLenHeader, iLenExpected);
                                    if (CheckCRC())
                                    {
                                        ParseBlock(iBytesRead);
                                    }
                                    else SendToNet("ERROR CRC BODY", iNumPacket, (byte)1); //перезапрос пакета
                                }
                                else SendToNet("ERROR CRC HEADER", iNumPacket, (byte)1);
                            }
                            else
                            {
                                SendToNet("ERROR LENGTH", iNumPacket, (byte)1); //перезапрос пакета
                            }
                        }
                        System.Threading.Thread.Sleep(1);
                    }
                }
                catch //(Exception ex)
                {

                }
                iStatus = -1;
            }
            
            private void ParseBlock(int iBytesRead)
            {
                byte bTypeCommand = 0;

                if (iBytesRead >= iLenHeader)
                {
                    bTypeCommand = buffer[0];
                    iNumPacket = BitConverter.ToInt32(buffer, 1);
                    iLenghtBlock = BitConverter.ToInt32(buffer, 5);
                    iCRC = BitConverter.ToInt32(buffer, 9);
                }
                //else
                //{
                //    iNumFalseRepet++;
                //    if (abort_thread) return;
                //    SendToNet("ERROR LENGTH", iNumPacket, (byte)1); //перезапрос пакета
                //    //networkStream.BeginRead(buffer_in, 0, buffer_in.Length, acbNetRead, networkStream);
                //    return;
                //}
                //if (!CheckCRC())
                //{
                //    iNumFalseRepet++;
                //    while (networkStream.DataAvailable) networkStream.Read(buffer, 0, lenghtReciveBuffer);
                //    if (abort_thread) return;
                //    SendToNet("ERROR CRC", iNumPacket, (byte)1); //перезапрос пакета
                //    //networkStream.BeginRead(buffer_in, 0, buffer_in.Length, acbNetRead, networkStream);
                //    return;
                //}
                //else
                //{
                if (iNumPacket < iNumPacketExpected)
                {
                    iNumFalseRepet++;
                    SendToNet("iNumPacket <" + iNumPacketExpected.ToString(), iNumPacket, (byte)0); //перезапрос пакета
                    //networkStream.BeginRead(buffer_in, 0, buffer_in.Length, acbNetRead, networkStream);
                    return;
                }
                if (iNumPacket > iNumPacketExpected)
                {
                    iNumFalseRepet++;
                    SendToNet("iNumPacket >" + iNumPacketExpected.ToString(), iNumPacket, (byte)1); //перезапрос пакета
                    //networkStream.BeginRead(buffer_in, 0, buffer_in.Length, acbNetRead, networkStream);
                    return;
                }
                if (iNumPacket == iNumPacketExpected)
                {
                    iNumPacketExpected = iNumPacket + 1;
                    iNumFalseRepet = 0;
                    try
                    {
                        ParseTypeCommand(bTypeCommand);
                    }
                    catch { }
                }
            }
            public void OnTimerWriteFile(object statusInfo)
            {
                if (fileWriteStream != null)
                {
                    timerWriteFile.Change(-1, -1);
                    timerWriteFile.Dispose();
                    timerWriteFile = null;
                    fileWriteStream.Flush();
                    fileWriteStream.Close();
                    fileWriteStream = null;
                    //Может быть недописанный файл надо удалять?
                }
            }
            public void Destroy()
            {
                abort_thread = true;
                if (networkStream != null)
                {
                    networkStream.Flush();
                    networkStream.Close();
                }
                if (fileReadStream != null)
                {
                    fileReadStream.Flush();
                    fileReadStream.Close();
                }
                if (fileWriteStream != null)
                {
                    timerWriteFile.Change(-1, -1);
                    timerWriteFile.Dispose();
                    timerWriteFile = null;
                    fileWriteStream.Flush();
                    fileWriteStream.Close();
                }
                fileReadStream = null;
                networkStream = null;
                fileWriteStream = null;
                iStatus = -1;
            }
        }
        public class SocketTransferClient
        {
            private NetworkStream streamToServer = null;
            private byte[] buffer = new byte[lenghtReciveBuffer];
            private byte[] buffer_old = new byte[lenghtReciveBuffer];
            private byte[] buffer_input = new byte[lenghtReciveBuffer];
            private byte[] buffer_input_tmp = new byte[lenghtReciveBuffer];
            public Int32 iNumPacket = 1;
            private int iLen_buffer_old = 0;
            private bool bOK = false;
            private Object oLock = new object();
            private ArrayList arMessage = new ArrayList();
            private ArrayList arFolders = new ArrayList();
            private ArrayList arFiles = new ArrayList();
            private bool bFileExist = false;
            private bool bFileEND = false;
            private long lFreeDiskSpace = 0;
            public bool abort_thread = false;
            private int PercentDone;
            /// <summary>
            /// Передано в процентах
            /// </summary>
            public int PercentDoneEx
            {
                get { return PercentDone; }
            }
            /// <summary>
            /// Событие во время копирования
            /// </summary>
            public event STC_SendFileTo_Progress OnProgress;

            public SocketTransferClient(NetworkStream netSreamForClient) //open
            {
                try
                {
                    iNumPacket = 1;
                    streamToServer = netSreamForClient;
                    for (int i = 0; i < lenghtReciveBuffer; i++) Buffer.SetByte(buffer_input, i, 0);
                }
                catch //(Exception ex)
                {
                }
            }

            public bool SendFileTo(string sFullFileNameSrc, string sFullFileNameDest)
            {
                bool bRet = false;
                //Общее количество считанных байт
                long totalBytesRead = 0;
                PercentDone = 0;
                if (abort_thread) return false;
                try
                {
                    if (streamToServer.DataAvailable) streamToServer.Read(buffer_input, 0, lenghtReciveBuffer);

                    SendPacket(1, sFullFileNameDest);
                    if (!WaitOfOK()) return false;
                    FileStream fs = File.Open(sFullFileNameSrc, FileMode.Open);                    
                    long sLenght = fs.Length;                       //Получаем длину исходного файла
                    int iReadBytesFs = 0;
                    do
                    {
                        if (abort_thread) return false;
                        for (int i = 0; i < lenghtReciveBuffer; i++) Buffer.SetByte(buffer, i, 0);
                        iReadBytesFs = fs.Read(buffer, iLenHeader, buffer.Length - iLenHeader);
                        if (iReadBytesFs > 0)
                        {

                            totalBytesRead += iReadBytesFs;         //Для статистики запоминаем сколько уже байт записали
                            double pctDone = (double)((double)totalBytesRead / (double)sLenght);
                            PercentDone = (int)(pctDone * 100);     //Получаем сколько процентов выполнено
                            OnProgress(PercentDone);

                            SendPacket(2, iNumPacket, iReadBytesFs);
                            iNumPacket++;
                            if (!WaitOfOK()) return false;
                        }
                    } while (iReadBytesFs > 0);
                    fs.Close();
                    SendPacket(3, "CloseFile");
                    if (!WaitOfOK()) return false;
                    bRet = true;
                }
                catch //(Exception ex)
                {
                    bRet = false;
                }
                return bRet;
            }
            public bool ReciveFileFrom(string sFullFileNameSrc, string sFullFileNameDest)
            {
                bool bRet = false;
                int iBytesRead = 0;
                Int32 iLenExpected = 0;
                if (abort_thread) return false;
                try
                {
                    if (streamToServer.DataAvailable) streamToServer.Read(buffer_input, 0, lenghtReciveBuffer);
                    bFileExist = false;
                    bFileEND = false;
                    SendPacket(4, sFullFileNameSrc);
                    if (!WaitOfOK()) return false;
                    if (bFileExist)
                    {
                        SendPacket(5, sFullFileNameSrc);
                        FileStream fs = File.Open(sFullFileNameDest, FileMode.Create);
                        while (!bFileEND)
                        {
                            int iTimeCount = 0;
                            if (abort_thread) return false;
                            while (true)
                            {
                                if (abort_thread) return false;
                                if (streamToServer.DataAvailable) break;
                                System.Threading.Thread.Sleep(1);
                                iTimeCount++;
                                if (iTimeCount >= 10000)
                                {
                                    fs.Flush();
                                    fs.Close();
                                    if (File.Exists(sFullFileNameDest)) File.Delete(sFullFileNameDest);
                                    return false;
                                }
                            }


                            iBytesRead = streamToServer.Read(buffer_input, 0, iLenHeader);
                            if (iBytesRead >= iLenHeader)
                            {
                                if (CheckCRCHeader())
                                {
                                    iLenExpected = BitConverter.ToInt32(buffer_input, 5);
                                    do
                                    {
                                        iBytesRead = iBytesRead + streamToServer.Read(buffer_input, iBytesRead, iLenExpected - iBytesRead + iLenHeader);

                                    } while (iBytesRead < (iLenExpected + iLenHeader));
                                }
                                else SendPacket(9, "ERROR CRC HEADER");
                            }
                            else
                            {
                                SendPacket(9, "ERROR LENGTH");
                            }

                            //iBytesRead = streamToServer.Read(buffer_input, 0, lenghtReciveBuffer);
                            if (CheckCRC())
                            {
                                WorkWithRecived(iBytesRead);
                            }
                            else SendPacket(9, "ERROR CRC BODY");
                            if (buffer_input[0] != 1)   //если это не перезапрос
                            {
                                if ((iBytesRead > 0) && (!bFileEND)) fs.Write(buffer_input, iLenHeader, iBytesRead - iLenHeader);
                                SendPacket(6, " еще ");
                            }
                        }
                        fs.Flush();
                        fs.Close();
                    }
                    bRet = true;
                }
                catch
                {
                    bRet = false;
                }
                return bRet;
            }
            public bool GetFolders(ref ArrayList arFoldersList, string sFullFolderName)
            {
                bool bRet = false;
                if (abort_thread) return false;
                try
                {
                    if (streamToServer.DataAvailable) streamToServer.Read(buffer_input, 0, lenghtReciveBuffer);

                    lock (arFolders.SyncRoot)
                    {
                        arFolders.Clear();
                    }
                    SendPacket(8, sFullFolderName);
                    if (!WaitOfOK()) return false;
                    lock (arFoldersList.SyncRoot)
                    {
                        arFoldersList.Clear();
                        if (arFolders.Count > 0)
                        {
                            foreach (string s in arFolders) arFoldersList.Add(s);
                            bRet = true;
                        }
                        else arFoldersList.Add("Error");
                    }
                }
                catch
                {
                    bRet = false;
                }
                return bRet;
            }
            public bool GetFiles(ref ArrayList arFilesList, string sFullFolderName)
            {
                bool bRet = false;
                if (abort_thread) return false;
                try
                {
                    if (streamToServer.DataAvailable) streamToServer.Read(buffer_input, 0, lenghtReciveBuffer);

                    lock (arFiles.SyncRoot)
                    {
                        arFiles.Clear();
                    }
                    SendPacket(7, sFullFolderName);
                    if (!WaitOfOK()) return false;
                    lock (arFilesList.SyncRoot)
                    {
                        arFilesList.Clear();
                        if (arFiles.Count > 0)
                        {
                            foreach (string s in arFiles) arFilesList.Add(s);
                            bRet = true;
                        }
                        else arFilesList.Add("Error");
                    }
                }
                catch
                {
                    bRet = false;
                }
                return bRet;
            }
            public bool DeleteFile(string sFullFileName)
            {
                bool bRet = false;
                if (abort_thread) return false;
                try
                {
                    if (streamToServer.DataAvailable) streamToServer.Read(buffer_input, 0, lenghtReciveBuffer);

                    bFileExist = false;
                    SendPacket(4, sFullFileName);
                    if (!WaitOfOK()) return false;
                    if (bFileExist)
                    {
                        SendPacket(10, sFullFileName);
                        if (!WaitOfOK()) return false;
                    }
                    bRet = true;
                }
                catch
                {
                    bRet = false;
                }
                return bRet;
            }
            public bool FileExits(string sFullFileName)
            {
                bool bRet = false;
                if (abort_thread) return false;
                try
                {
                    if (streamToServer.DataAvailable) streamToServer.Read(buffer_input, 0, lenghtReciveBuffer);

                    bFileExist = false;
                    SendPacket(4, sFullFileName);
                    if (!WaitOfOK()) return false;
                    if (bFileExist)
                    {
                        bRet = true;
                    }
                }
                catch
                {
                    bRet = false;
                }
                return bRet;
            }
            public long GetFreeSpace(string sFullDirName)
            {
                long lRet = 0;
                if (abort_thread) return -997;
                try
                {
                    if (streamToServer.DataAvailable) streamToServer.Read(buffer_input, 0, lenghtReciveBuffer);

                    lFreeDiskSpace = 0;
                    SendPacket(11, sFullDirName);
                    if (!WaitOfOK()) return -998;
                    lRet = lFreeDiskSpace;
                }
                catch
                {
                    lRet = -999;
                }
                return lFreeDiskSpace;
            }
            public bool SendMessage(string sMessage)
            {
                bool bRet = true;
                if (abort_thread) return false;
                try
                {
                    if (streamToServer.DataAvailable) streamToServer.Read(buffer_input, 0, lenghtReciveBuffer);

                    SendPacket(0, sMessage);
                    if (!WaitOfOK()) return false;
                }
                catch
                {
                    bRet = false;
                }
                return bRet;
            }
            public bool CloseServer()
            {
                bool bRet = false;
                if (abort_thread) return false;
                try
                {
                    if (streamToServer.DataAvailable) streamToServer.Read(buffer_input, 0, lenghtReciveBuffer);

                    SendPacket(12, "Пипец тебе");
                    if (!WaitOfOK()) return false;
                    bRet = true;
                }
                catch
                {
                    bRet = false;
                }
                return bRet;
            }

            private bool SendPacket(byte byCommand, string sbody)
            {
                bool bRes = false;
                byte[] bufNumPack = new byte[4];
                byte[] bufLenght = new byte[4];
                byte[] bufCrc = new byte[4];

                lock (oLock)
                {
                    bOK = false;

                    try
                    {
                        Int32 ilenght = sbody.Length;
                        for (int i = 0; i < lenghtReciveBuffer; i++) Buffer.SetByte(buffer, i, 0);
                        System.Text.Encoding.UTF8.GetBytes(sbody, 0, ilenght, buffer, iLenHeader);
                        bufLenght = BitConverter.GetBytes((Int32)ilenght);
                        bufNumPack = BitConverter.GetBytes((Int32)iNumPacket);
                        Int32 icrc = 0;
                        for (int i = iLenHeader; i < (ilenght + iLenHeader); i++) icrc = icrc + buffer[i];
                        bufCrc = BitConverter.GetBytes(icrc);
                        Buffer.BlockCopy(bufNumPack, 0, buffer, 1, 4);
                        Buffer.BlockCopy(bufLenght, 0, buffer, 5, 4);
                        Buffer.BlockCopy(bufCrc, 0, buffer, 9, 4);
                        buffer[0] = byCommand;

                        byte[] bufCrcHead = new byte[2];
                        Int16 icrch = 0;
                        for (int i = 0; i < iLenHeader - 2; i++) icrch = (Int16)(icrch + buffer[i]);
                        bufCrcHead = BitConverter.GetBytes((Int16)icrch);
                        Buffer.BlockCopy(bufCrcHead, 0, buffer, 13, 2);

                        iNumPacket++;
                        for (int i = 0; i < lenghtReciveBuffer; i++) Buffer.SetByte(buffer_old, i, 0);
                        Buffer.BlockCopy(buffer, 0, buffer_old, 0, ilenght + iLenHeader);
                        iLen_buffer_old = ilenght + iLenHeader;
                        streamToServer.Write(buffer, 0, ilenght + iLenHeader /*buffer.Length*/);
                        bRes = true;
                    }
                    catch //(Exception ex)
                    {
                        bRes = false;
                    }
                }
                return bRes;
            }
            private bool SendPacket(byte byComm, Int32 iNumPac, int iLen_buf_out)
            {
                bool bRes = false;
                byte[] bufNumPack = new byte[4];
                byte[] bufLenght = new byte[4];
                byte[] bufCrc = new byte[4];
                lock (oLock)
                {
                    bOK = false;

                    try
                    {
                        Int32 ilenght = iLen_buf_out;

                        bufLenght = BitConverter.GetBytes((Int32)ilenght);
                        bufNumPack = BitConverter.GetBytes((Int32)iNumPac);
                        Int32 icrc = 0;
                        for (int i = iLenHeader; i < (ilenght + iLenHeader); i++) icrc = icrc + buffer[i];
                        bufCrc = BitConverter.GetBytes(icrc);
                        Buffer.BlockCopy(bufNumPack, 0, buffer, 1, 4);
                        Buffer.BlockCopy(bufLenght, 0, buffer, 5, 4);
                        Buffer.BlockCopy(bufCrc, 0, buffer, 9, 4);
                        buffer[0] = byComm;

                        byte[] bufCrcHead = new byte[2];
                        Int16 icrch = 0;
                        for (int i = 0; i < iLenHeader - 2; i++) icrch = (Int16)(icrch + buffer[i]);
                        bufCrcHead = BitConverter.GetBytes((Int16)icrch);
                        Buffer.BlockCopy(bufCrcHead, 0, buffer, 13, 2);

                        Buffer.SetByte(buffer_old, 0, 0);
                        Buffer.BlockCopy(buffer, 0, buffer_old, 0, ilenght + iLenHeader);
                        iLen_buffer_old = ilenght + iLenHeader;
                        streamToServer.Write(buffer, 0, ilenght + iLenHeader /*buf_out.Length*/);
                        bRes = true;
                    }
                    catch //(Exception ex)
                    {
                        bRes = false;
                    }
                }
                return bRes;
            }
            private bool SendPacket(byte[] buf_out)
            {
                bool bRes = true;
                lock (oLock)
                {
                    bOK = false;

                    try
                    {
                        streamToServer.Write(buf_out, 0, iLen_buffer_old);
                        bRes = true;
                    }
                    catch //(Exception ex)
                    {
                        bRes = false;
                    }
                }
                return bRes;
            }

            private void WorkWithRecived(int iBytesRead)
            {
                string sRecive = System.Text.Encoding.UTF8.GetString(buffer_input, iLenHeader, iBytesRead - iLenHeader);
                byte bResCommand = buffer_input[0];
                switch (bResCommand)
                {
                    case 0:	//предыдущий пакет принят ОК
                        lock (oLock)
                        {
                            bOK = true;
                        }
                        break;
                    case 1:	//ошибка crc
                        SendPacket(buffer_old);
                        break;
                    case 2:	//сообщение
                        sRecive = System.Text.Encoding.UTF8.GetString(buffer_input, iLenHeader, iBytesRead - iLenHeader);
                        lock (arMessage.SyncRoot)
                        {
                            arMessage.Add(sRecive);
                        }
                        lock (oLock)
                        {
                            bOK = true;
                        }
                        break;
                    case 3:	//список папок
                        sRecive = System.Text.Encoding.UTF8.GetString(buffer_input, iLenHeader, iBytesRead - iLenHeader);
                        lock (arFolders.SyncRoot)
                        {
                            arFolders.Clear();
                            StringReader sr = new StringReader(sRecive);
                            string sLine = "";
                            do
                            {
                                sLine = sr.ReadLine();
                                if ((sLine != null) && (sLine != "")) arFolders.Add(sLine);
                            } while (sLine != null);
                        }
                        lock (oLock)
                        {
                            bOK = true;
                        }
                        break;
                    case 4:	//список файлов
                        sRecive = System.Text.Encoding.UTF8.GetString(buffer_input, iLenHeader, iBytesRead - iLenHeader);
                        lock (arFiles.SyncRoot)
                        {
                            arFiles.Clear();
                            StringReader sr = new StringReader(sRecive);
                            string sLine = "";
                            do
                            {
                                sLine = sr.ReadLine();
                                if ((sLine != null) && (sLine != "")) arFiles.Add(sLine);
                            }
                            while (sLine != null);
                        }
                        lock (oLock)
                        {
                            bOK = true;
                        }
                        break;
                    case 5:	//файл есть
                        lock (oLock)
                        {
                            bFileExist = true;
                            bOK = true;
                        }
                        break;
                    case 6:	//файла нет
                        lock (oLock)
                        {
                            bFileExist = false;
                            bOK = true;
                        }
                        break;
                    case 7:	//конец файла
                        lock (oLock)
                        {
                            bFileEND = true;
                            bOK = true;
                        }
                        break;
                    case 8:	//свободное место на диске
                        lock (oLock)
                        {
                            sRecive = System.Text.Encoding.UTF8.GetString(buffer_input, iLenHeader, iBytesRead - iLenHeader);
                            lFreeDiskSpace = Convert.ToInt64(sRecive);
                            bOK = true;
                        }
                        break;
                }
                //this.Invoke
                //        ((MethodInvoker)delegate
                //        {
                //            if(sRecive!=null) this.listBox1.Items.Add(sRecive);
                //            this.labelCommand.Text = bResCommand.ToString();
                //            this.labelErr.Text += iNumPacket.ToString() + ",";
                //        });
            }
            private bool WaitOfOK()
            {
                bool bRet = false;
                //Int32 iLen = 0;
                Int32 iLenExpected = 0;
                try
                {
                    int iTimeCount = 0;
                    while ((iTimeCount < 30000) && (!abort_thread))
                    {
                        if (streamToServer.DataAvailable)
                        {
                            int iBytesRead = streamToServer.Read(buffer_input, 0, iLenHeader);
                            if (iBytesRead >= iLenHeader)
                            {
                                if (CheckCRCHeader())
                                {
                                    iLenExpected = BitConverter.ToInt32(buffer_input, 5);
                                    do
                                    {
                                        iBytesRead = iBytesRead + streamToServer.Read(buffer_input, iBytesRead, iLenExpected - iBytesRead + iLenHeader);

                                    } while (iBytesRead < (iLenExpected + iLenHeader));
                                                                        
                                    //iLen = BitConverter.ToInt32(buffer_input, 5);
                                    //iBytesRead = iBytesRead + streamToServer.Read(buffer_input, iLenHeader, iLen);

                                    if (CheckCRC())
                                    {
                                        WorkWithRecived(iBytesRead);
                                        bRet = true;
                                    }
                                    else SendPacket(9, "ERROR CRC BODY");
                                }
                                else SendPacket(9, "ERROR CRC HEADER");
                            }
                            else
                            {
                                iTimeCount += (int)10000 / iNumRepet;
                                SendPacket(9, "ERROR LENGTH");
                            }
                        }
                        lock (oLock)
                        {
                            if (bOK) break;
                        }
                        iTimeCount++;
                        System.Threading.Thread.Sleep(1);
                    }
                }
                catch
                {

                }
                return bRet;
            }

            private bool CheckCRC()
            {
                bool bRet = false;
                Int32 iRealCRC = 0;
                try
                {
                    Int32 iCRC = BitConverter.ToInt32(buffer_input, 9);
                    Int32 iLen = BitConverter.ToInt32(buffer_input, 5);

                    for (int i = iLenHeader; i < iLen + iLenHeader; i++) iRealCRC = (Int32)(iRealCRC + buffer_input[i]);
                    if (iCRC == iRealCRC) bRet = true;
                }
                catch { bRet = false; }
                return bRet;
            }

            private bool CheckCRCHeader()
            {
                bool bRet = false;
                Int16 iRealCRC = 0;
                try
                {
                    Int16 iCRC = BitConverter.ToInt16(buffer_input, 13);
                    Int32 iLen = BitConverter.ToInt32(buffer_input, 5);

                    for (int i = 0; i < iLenHeader - 2; i++) iRealCRC = (Int16)(iRealCRC + buffer_input[i]);
                    if (iCRC == iRealCRC) bRet = true;
                }
                catch { bRet = false; }
                return bRet;
            }

            public void Destroy()	//close
            {
                abort_thread = true;
                System.Threading.Thread.Sleep(500);
                if (streamToServer != null)
                {
                    streamToServer.Flush();
                    streamToServer.Close();
                }
            }
        }
    }
    public class MemoryStatus
    {
        [DllImport("coredll.dll")]//[DllImport("kernel32.dll")]
        public static extern bool GetDiskFreeSpaceEx(
          string lpDirectoryName,
          out ulong lpFreeBytesAvailableToCaller,
          out ulong lpTotalNumberOfBytes,
          out ulong lpTotalNumberOfFreeBytes
        );

        public const string STORAGE_INTERNAL = "\\";
        public const string STORAGE_CARD = "\\storage card\\";
        public MemoryStatus()
        {
        }

        public static void GetStorageInfo(string storagePath, out long totalBytes, out long availBytes)
        {
            ulong freeBytesAvail, totalBytesAvail, freeBytesTotal;
            bool result = GetDiskFreeSpaceEx(storagePath, out freeBytesAvail, out totalBytesAvail, out freeBytesTotal);

            if (result == true)
            {
                totalBytes = (long)(totalBytesAvail);
                availBytes = (long)freeBytesAvail;
            }
            else
            {
                totalBytes = -1;
                availBytes = -1;
            }
        }
    } 

}
