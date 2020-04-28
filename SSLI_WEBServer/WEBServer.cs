using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SSLI
{
    class WEBServer : SSLI.ClassAMBRenewedService
    {
        private string sPd = Path.DirectorySeparatorChar.ToString();
        private TcpListener myListener = null;
        private Socket mySocket = null;
        private string sMyWebServerRoot = null;
        private bool bAbort = false;
        private string sExecutePath = "";
        private int iDebugLevel = 2;
        private string sPathToWebTemp = null;

        public override bool Init()
        {
            lock (Utils.oSyncroLoadSaveInit)
            {
                sExecutePath = Utils.strConfig.sPathToExecute;
                iDebugLevel = Utils.strConfig.iDebugLevel;
                sPathToWebTemp = Utils.sFolderNameMain + sPd + Utils.strConfig.strWebServer.sWWWrootDir;
            }
            WriteDebugString("---------------------------", 1);
            if (Directory.Exists(sExecutePath + sPd + "WWWdata"))
            {
                WriteDebugString("Init:OK - Found WWWdata directory.", 2);
                if (!Directory.Exists(sPathToWebTemp))
                {
                    Directory.CreateDirectory(sPathToWebTemp);
                    Directory.CreateDirectory(sPathToWebTemp + sPd + "image");
                    File.Copy(sExecutePath + sPd + "WWWdata" + sPd + "image" + sPd + "TR_1.jpg", sPathToWebTemp + sPd + "image" + sPd + "TR_1.jpg");
                }
            }
            else
            {
                if (Directory.Exists(Utils.sFolderNameMain + sPd + "WWW" + sPd + "WWWdata"))    //!!
                {
                    Directory.CreateDirectory(sExecutePath + sPd + "WWWdata");
                    DirectoryInfo di = new DirectoryInfo(Utils.sFolderNameMain + sPd + "WWW" + sPd + "WWWdata");
                    FileInfo[] fi = di.GetFiles();
                    foreach (FileInfo f in fi) File.Copy(f.FullName, sExecutePath + sPd + "WWWdata" + sPd + f.Name);
                }
                else
                {
                    WriteDebugString("Init:ERROR - Not found WWWdata directory.", 0);
                    return false;
                }
            }
            if (WEBServerInit(Utils.strConfig.strNet.sIPAdressLocal, Utils.strConfig.strWebServer.iPortWWWserver, sPathToWebTemp))
            {
                WriteDebugString("Init:OK", 2);
                return true;
            }
            else
            {
                WriteDebugString("Init:ERROR - Missing parametrs.", 0);
                return false;
            }
        }

        public override void Start()
        {
            WriteDebugString("Start.Entrance:OK", 2);
            
            if (myListener != null)
            {
                Thread.Sleep(2000);
                WorkWithHTMLFile();
                try
                {
                    myListener.Start();
                    //Thread th = new Thread(new ThreadStart(StartListen));
                    //th.Start();
                    WriteDebugString("Start:OK", 2);
                }
                catch (Exception ex)
                {
                    WriteDebugString("Start:ERROR - possible LAN is not connected. Message: " + ex.Message, 1);
                    return;
                }
                StartListen();
            }

            WriteDebugString("Start.Exit:OK", 2);
            WriteDebugString("===========================", 1);
        }

        public override void Stop()
        {
            bAbort = true;
            try
            {
                if (myListener != null) myListener.Stop();
                if (mySocket != null)
                {
                    mySocket.Close();
                }
            }
            catch { }
            WriteDebugString("Stop:OK", 1);
        }

        private bool WriteDebugString(string strWr, int iLevelDebud)
        {
            bool bRet = true;
            if (iLevelDebud <= iDebugLevel)
            {
                try
                {
                    bRet = Utils.WriteDebugString(sExecutePath, " -WEBService- " + strWr);
                }
                catch { bRet = false; }
            }
            if (iLevelDebud == 0)
            {
                try
                {
                    lock (Utils.oSyncroLoadSaveInit)
                    {
                        Utils.strConfig.sTimeLastCriticalError = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                        Utils.strConfig.sTextLastCriticalError = strWr;
                    }
                    Utils.SaveInit();
                }
                catch //(Exception ex)
                {
                    bRet = false;
                }
            }
            return bRet;
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="IPserver">ИП севера</param>
        /// <param name="port">порт сервера</param>
        /// <param name="sRootDir">корневая папка сервера</param>
        private bool WEBServerInit(string IPserver, int port, string sRootDir)
        {
            bool bRet = false;
            try
            {
                if ((sRootDir != "") && (sRootDir != null)) sMyWebServerRoot = sRootDir;
                if (!Directory.Exists(sMyWebServerRoot)) Directory.CreateDirectory(sMyWebServerRoot);
                IPAddress ipAdr = IPAddress.Parse(IPserver);
                myListener = new TcpListener(ipAdr, port);
                
                bRet = true;
            }
            catch //(Exception e)
            {
                bRet = false;
            }
            return bRet;
        }

        private string GetTheDefaultFileName(string sLocalDirectory)
        {
            StreamReader sr = null;
            String sLine = "";

            try
            {
                sr = new StreamReader(sExecutePath + sPd + "WWWdata" + sPd + "Default.Dat");
                while ((sLine = sr.ReadLine()) != null)
                {
                    if (File.Exists(sLocalDirectory + sPd + sLine) == true)
                        break;
                }
            }
            catch //(Exception e)
            {
            }
            finally
            {
                if (sr != null) sr.Close();
            }
            if (File.Exists(sLocalDirectory + sPd + sLine) == true)
                return sLine;
            else
                return "";
        }
        private string GetMimeType(string sRequestedFile)
        {
            StreamReader sr = null;
            String sLine = "";
            String sMimeType = "";
            String sFileExt = "";
            String sMimeExt = "";

            sRequestedFile = sRequestedFile.ToLower();
            int iStartPos = sRequestedFile.IndexOf(".");
            sFileExt = sRequestedFile.Substring(iStartPos);
            try
            {
                sr = new StreamReader(sExecutePath + sPd + "WWWdata" + sPd + "mime.dat");
                while ((sLine = sr.ReadLine()) != null)
                {
                    sLine.Trim();
                    if (sLine.Length > 0)
                    {
                        iStartPos = sLine.IndexOf(";");
                        sLine = sLine.ToLower();
                        sMimeExt = sLine.Substring(0, iStartPos);
                        sMimeType = sLine.Substring(iStartPos + 1);
                        if (sMimeExt == sFileExt)
                            break;
                    }
                }
            }
            catch (Exception e)
            {
            }
            finally
            {
                if (sr != null) sr.Close();
            }
            if (sMimeExt == sFileExt)
                return sMimeType;
            else
                return "";
        }
        private string GetLocalPath(string sMyWebServerRoot, string sDirName)
        {

            StreamReader sr = null;
            String sLine = "";
            String sVirtualDir = "";
            String sRealDir = "";
            int iStartPos = 0;
            sDirName.Trim();
            sMyWebServerRoot = sMyWebServerRoot.ToLower();
            sDirName = sDirName.ToLower();
            try
            {
                sr = new StreamReader(sExecutePath + sPd + "WWWdata" + sPd + "vdirs.dat");
                while ((sLine = sr.ReadLine()) != null)
                {
                    sLine.Trim();
                    if (sLine.Length > 0)
                    {
                        iStartPos = sLine.IndexOf(";");
                        //sLine = sLine.ToLower();
                        sVirtualDir = sLine.Substring(0, iStartPos);
                        sRealDir = sLine.Substring(iStartPos + 1);
                        if (sVirtualDir == sDirName)
                        {
                            break;
                        }
                    }
                }
            }
            catch// (Exception e)
            {
            }
            finally
            {
                if (sr != null) sr.Close();
            }
            if (sVirtualDir == sDirName)
                return sRealDir;
            else
                return "";
        }

        /// <summary>
        /// This function send the Header Information to the client (Browser)
        /// </summary>
        /// <param name="sHttpVersion">HTTP Version</param>
        /// <param name="sMIMEHeader">Mime Type</param>
        /// <param name="iTotBytes">Total Bytes to be sent in the body</param>
        /// <param name="mySocket">Socket reference</param>
        /// <returns></returns>
        private void SendHeader(string sHttpVersion, string sMIMEHeader, int iTotBytes, string sStatusCode, ref Socket mySocket)
        {

            String sBuffer = "";
            if (sMIMEHeader.Length == 0)
            {
                sMIMEHeader = "text/html";  // Default Mime Type is text/html
            }

            sBuffer = sBuffer + sHttpVersion + sStatusCode + "\r\n";
            sBuffer = sBuffer + "Server: cx1193719-b\r\n";
            sBuffer = sBuffer + "Content-Type: " + sMIMEHeader + "\r\n";
            sBuffer = sBuffer + "Accept-Ranges: bytes\r\n";
            sBuffer = sBuffer + "Content-Length: " + iTotBytes + "\r\n\r\n";

            Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);
            SendToBrowser(bSendData, ref mySocket);
        }

        private void SendToBrowser(String sData, ref Socket mySocket)
        {
            SendToBrowser(Encoding.ASCII.GetBytes(sData), ref mySocket);
        }
        private void SendToBrowser(Byte[] bSendData, ref Socket mySocket)
        {
            try
            {
                if (mySocket.Connected)
                {
                    mySocket.Send(bSendData, bSendData.Length, 0);
                }
            }
            catch //(Exception e)
            {
            }
        }
        private void SendToBrowser(Byte[] bSendData, ref Socket mySocket, int iLenght)
        {
            try
            {
                if (mySocket.Connected)
                {
                    mySocket.Send(bSendData, iLenght, 0);
                }
            }
            catch //(Exception e)
            {
            }
        }

        private void StartListen()
        {
            while (!bAbort)
            {
                try
                {
                    Work();
                }
                catch (Exception ex)
                {
                    WriteDebugString("StartListen:ERROR - " + ex.Message, 1);
                }
            }
        }

        private void Work()
        {

            int iStartPos = 0;
            String sRequest;
            String sDirName;
            String sRequestedFile;
            String sErrorMessage;
            String sLocalDir;

            String sPhysicalFilePath = "";

            Byte[] bReceive = new Byte[1024];

            mySocket = myListener.AcceptSocket();
            if ((mySocket.Connected) && (!bAbort))
            {
                WriteDebugString("Work.Client connected:OK", 2);
                
                int i = mySocket.Receive(bReceive, bReceive.Length, 0);
                string sBuffer = Encoding.ASCII.GetString(bReceive, 0, i);
                if (sBuffer.Substring(0, 3) != "GET")
                {
                    mySocket.Close();
                    return;
                }
                iStartPos = sBuffer.IndexOf("HTTP", 1);
                string sHttpVersion = sBuffer.Substring(iStartPos, 8);
                sRequest = sBuffer.Substring(0, iStartPos - 1);
                sRequest.Replace("\\", "/");
                if ((sRequest.IndexOf(".") < 1) && (!sRequest.EndsWith("/")))
                {
                    sRequest = sRequest + "/";
                }
                iStartPos = sRequest.LastIndexOf("/") + 1;
                sRequestedFile = sRequest.Substring(iStartPos);
                sDirName = sRequest.Substring(sRequest.IndexOf("/"), sRequest.LastIndexOf("/") - 3);
                // Identify the Physical Directory
                if (sDirName == "/")
                    sLocalDir = sMyWebServerRoot;
                else
                {
                    //Get the Virtual Directory
                    sLocalDir = GetLocalPath(sMyWebServerRoot, sDirName);
                }
                if (sLocalDir.Length == 0)
                {
                    sErrorMessage = "<H2>Error!! Requested Directory does not exists</H2><Br>";
                    SendHeader(sHttpVersion, "", sErrorMessage.Length, " 404 Not Found", ref mySocket);
                    SendToBrowser(sErrorMessage, ref mySocket);
                    mySocket.Close();
                    return;
                }

                // Identify the File Name
                //If The file name is not supplied then look in the default file list
                if (sRequestedFile.Length == 0)
                {
                    // Get the default filename
                    sRequestedFile = "index.htm";
                }

                // Get TheMime Type
                String sMimeType = GetMimeType(sRequestedFile);

                //Build the physical path
                sPhysicalFilePath = sLocalDir + sPd + sRequestedFile;
                if (File.Exists(sPhysicalFilePath) == false)
                {
                    sErrorMessage = "<H2>404 Error! File Does Not Exists...</H2>";
                    SendHeader(sHttpVersion, "", sErrorMessage.Length, " 404 Not Found", ref mySocket);
                    SendToBrowser(sErrorMessage, ref mySocket);
                }
                else
                {
                    WorkWithHTMLFile();
                    FileStream fs = new FileStream(sPhysicalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    // Create a reader that can read bytes from the FileStream.
                    BinaryReader reader = new BinaryReader(fs);
                    if (fs.Length < 10000)
                    {
                        byte[] bytes = new byte[fs.Length];
                        int read = reader.Read(bytes, 0, bytes.Length);
                        reader.Close();
                        fs.Close();
                        SendHeader(sHttpVersion, sMimeType, read, " 200 OK", ref mySocket);
                        SendToBrowser(bytes, ref mySocket);
                        bytes = null;
                    }
                    else
                    {
                        int read;
                        byte[] bytes = new byte[4096];

                        SendHeader(sHttpVersion, sMimeType, (int)fs.Length, " 200 OK", ref mySocket);

                        while ((read = reader.Read(bytes, 0, bytes.Length)) > 0)
                        {
                            SendToBrowser(bytes, ref mySocket, read);
                        }
                        reader.Close();
                        fs.Close();
                        bytes = null;
                    }
                }
                mySocket.Close();
            }

        }

        private void WorkWithHTMLFile()
        {
            WriteDebugString("WorkWithHTMLFile.Entrance:OK", 2);
            if (File.Exists(sMyWebServerRoot + sPd + "index.htm")) File.Delete(sMyWebServerRoot + sPd + "index.htm");
            MakeHTMLFile(sMyWebServerRoot + sPd + "index.htm");

            if (File.Exists(sMyWebServerRoot + sPd + "SSLI.txt")) File.Delete(sMyWebServerRoot + sPd + "SSLI.txt");
            File.Copy(Utils.strConfig.sPathToExecute + sPd + Utils.sFileNameLog, sMyWebServerRoot + sPd + "SSLI.txt");

            WriteDebugString("WorkWithHTMLFile.Exit:OK", 2);
        }
        /// <summary>
        /// Самая главная функция. Создает HTML - страницу.
        /// </summary>
        /// <param name="sNameFileHTML">Полный путь и имя HTML файла.</param>
        private void MakeHTMLFile(string sNameFileHTML)
        {
            try
            {
                using (StreamWriter sw = File.CreateText(sNameFileHTML))
                {
                    sw.WriteLine("<html>");
                    sw.WriteLine("<head>");
                    sw.WriteLine("<Title>Станция синхронизации</Title>");
                    sw.WriteLine("<Meta Http-equiv=\"Content-Type\" Content=\"text/html; charset=utf-8\">");
                    sw.WriteLine("<Meta name=\"author\" Content=\"Алексей Ананенков\">");
                    sw.WriteLine("<Meta name=\"Reply-to\" Content=\"alexey@ambintech.ru\">");
                    sw.WriteLine("</head>");
                    sw.WriteLine("<body>");
                    sw.WriteLine("<img src=\"/image/TR_1.jpg\" width=\"200\" height=\"195\" alt=\"Транспортная милиция\">");  //!!
                    sw.WriteLine("<h2>Это страница станции синхронизации</h2>");

                    sw.WriteLine("<h1>" + Utils.cCurrStatus.sNamePodrazdelenie + "</h1>");
                    sw.WriteLine("<hr>");

                    if (Utils.cCurrStatus.arBaseInfo != null)
                    {
                        if (Utils.cCurrStatus.arBaseInfo.Length > 0)
                        {
                            sw.WriteLine("<h3>Розыскные базы на станции</h3>");
                            sw.WriteLine("<OL>");
                            foreach (stBaseInfo sB in Utils.cCurrStatus.arBaseInfo)
                            {
                                if (sB.sName != null)
                                {
                                    sw.WriteLine("<LI>" + sB.sName);
                                    sw.WriteLine("<ul><li> Дата актуальности: " + sB.sDate + "<li> Последнее обновление: " + sB.sLUpd + "</ul>");
                                }
                            }
                            sw.WriteLine("</OL>");
                            sw.WriteLine("<br>");
                        }
                    }

                    if (Utils.cCurrStatus.arstTermInMemory.Length > 0)
                    {
                        sw.WriteLine("<h3>Терминалы</h3>");
                        sw.WriteLine("<table border=\"1\" style=\"border-collapse: collapse\" bordercolor=\"#111111\">");
                        sw.WriteLine("<tr><td><h4>Имя терминала</h4><td><h4>Дата синзронизации</h4><td><h4>Последнее обновление</h4></h4><td><h4>Уникальный номер</h4></tr>");
                        foreach (stTermInMem sT in Utils.cCurrStatus.arstTermInMemory)
                        {
                            sw.WriteLine("<tr><td>" + sT.sNameTerminal + "<td>" + sT.sLastSyncronized + "<td>" + sT.sLastUpdatesBaseLica + "<td>" + sT.sIDTerminal + "</tr>");
                        }
                        sw.WriteLine("</table>");
                        sw.WriteLine("<br><a href=\"http://" + Utils.strConfig.strGetFiles.sIPServerUVD + ":" + Utils.strConfig.strGetFiles.sPortServerUVD_HTTP + "\\SUVDWeb\">Статистика работы терминалов на сервере УВД</a><br>");
                    }

                    sw.WriteLine("<h3>Прием - передача файлов</h3>");
                    sw.WriteLine("<ul><li>Переданные:");
                    sw.WriteLine("<ul><li>Файл: " + Utils.cCurrStatus.sLastSendedName + "<li>Передан: " + Utils.cCurrStatus.sLastSendedTime + "</ul>");
                    sw.WriteLine("<li>Приятые:");
                    sw.WriteLine("<ul><li>Файл: " + Utils.cCurrStatus.sLastReciveName + "<li>Принят: " + Utils.cCurrStatus.sLastReciveTime + "</ul>");
                    sw.WriteLine("</ul><hr>");
                    sw.WriteLine("Уникальный номер станции: "+Utils.cCurrStatus.sDeviceSS_ID+".");
                    sw.WriteLine("<br><br>");
                    sw.WriteLine("Версии программ: ");
                    sw.WriteLine("<ul>");
                    sw.WriteLine("<li>Оболочка: SSLI.exe  Версия: " + Utils.sVersionSSLI);
                    for (int i = 0; i < Utils.strConfig.arstTasks.Length; i++)
                    {
                       sw.WriteLine("<li>Задача: " + Utils.strConfig.arstTasks[i].sNameTask + "  Версия: " + Utils.strConfig.arstTasks[i].sCurrentVersion);
                    }
                    sw.WriteLine("</ul>");
                    sw.WriteLine("<br>");
                    sw.WriteLine("<a href=\"" + "SSLI.txt" + "\">Протокол работы станции (для администратора)</a>");    //
                    sw.WriteLine("<br>");
                    sw.WriteLine("<a href=\"http://www.ambintech.ru\">Связь с разработчиками ООО \"АМБ ИнТех\"</a>");
                    sw.WriteLine("</body>");
                    sw.WriteLine("</html>");

                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                WriteDebugString("MakeHTMLFile:ERROR - " + ex.Message, 2);
            }
        }
    }
}
