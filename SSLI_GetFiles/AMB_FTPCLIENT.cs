using System;
using System.Net;
using System.IO;
using System.Text;
using System.Net.Sockets;

namespace AMB_FTPCLIENT
{
    public class FTPClient
    {

        private string remoteHost, remotePath, remoteUser, remotePass, mes;
        private int remotePort, bytes;
        private Socket clientSocket;

        private int retValue;
        private Boolean debug;
        private Boolean logined;
        private string reply;

        private const int BLOCK_SIZE = 512;            //было 8192
        private Byte[] buffer = new Byte[BLOCK_SIZE];   //сетевой буфер

        private const int iBuffSD_SIZE = 327680;
        private Byte[] buffSD = new byte[iBuffSD_SIZE];        //буфер для сброса на SD

        private Encoding ASCII = Encoding.ASCII;
        //private AsyncCallback acbNetRead;
        public bool bAbort = false;

        public FTPClient()
        {

            remoteHost = "localhost";
            remotePath = "/";
            remoteUser = "anonymous";
            remotePass = "test@ambintech.ru";
            remotePort = 21;
            debug = false;
            logined = false;
            //acbNetRead = new AsyncCallback(this.OnReadNetComplete);
        }

        #region Get_Set parametrs
        ///
        /// Set the name of the FTP server to connect to.
        ///
        /// Server name
        public void setRemoteHost(string remoteHost)
        {
            this.remoteHost = remoteHost;
        }

        ///
        /// Return the name of the current FTP server.
        ///
        /// Server name
        public string getRemoteHost()
        {
            return remoteHost;
        }

        ///
        /// Set the port number to use for FTP.
        ///
        /// Port number
        public void setRemotePort(int remotePort)
        {
            this.remotePort = remotePort;
        }

        ///
        /// Return the current port number.
        ///
        /// Current port number
        public int getRemotePort()
        {
            return remotePort;
        }

        ///
        /// Set the remote directory path.
        ///
        /// The remote directory path
        public void setRemotePath(string remotePath)
        {
            this.remotePath = remotePath;
        }

        ///
        /// Return the current remote directory path.
        ///
        /// The current remote directory path.
        public string getRemotePath()
        {
            return remotePath;
        }

        ///
        /// Set the user name to use for logging into the remote server.
        ///
        /// Username
        public void setRemoteUser(string remoteUser)
        {
            this.remoteUser = remoteUser;
        }

        ///
        /// Set the password to user for logging into the remote server.
        ///
        /// Password
        public void setRemotePass(string remotePass)
        {
            this.remotePass = remotePass;
        }

        ///
        /// Set debug mode.
        ///
        ///
        public void setDebug(Boolean debug)
        {
            this.debug = debug;
        }

        #endregion

        ///
        /// Return a string array containing the remote directory's file list.
        ///
        ///
        ///
        public string[] getFileList(string mask)
        {
            if (!logined)
            {
                login();
            }

            Socket cSocket;
            int iCount1 = 0;
            do
            {
                cSocket = createDataSocket();
                sendCommand("LIST " + mask); //NLST
                iCount1++;
                if (iCount1 > 50) break;
            }
            while (!(retValue == 150 || retValue == 125));
            if (!(retValue == 150 || retValue == 125))
            {
                throw new IOException(reply.Substring(0));
            }
            mes = "";
            while (true)
            {
                int bytes = cSocket.Receive(buffer, buffer.Length, 0);
                mes += ASCII.GetString(buffer, 0, bytes);
                mes = mes.Replace("\r", "");
                if (bytes < buffer.Length)
                {
                    System.Threading.Thread.Sleep(500);
                    if (cSocket.Available > 0) continue;
                    else break;
                }
            }

            char[] seperator = { '\n' };
            string[] mess = mes.Split(seperator);

            cSocket.Close();

            readReply();
            //возможен вылет с получением 550 вместо 226 если недочитали поток до конца
            if (retValue != 226)
            {
                //if (retValue != 550) 
                throw new IOException(reply.Substring(0));
            }
            return mess;
        }

        ///
        /// Return the size of a file.
        ///
        ///
        ///
        public long getFileSize(string fileName)
        {

            if (!logined)
            {
                login();
            }

            sendCommand("SIZE " + fileName);
            long size = 0;

            if (retValue == 213)
            {
                size = Int64.Parse(reply.Substring(4));
            }
            else
            {
                throw new IOException(reply.Substring(0));
            }

            return size;

        }

        ///
        /// Login to the remote server.
        ///
        public void login()
        {

            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //IPAddress ipaTmp = null;
            //IPHostEntry hostInfo = Dns.GetHostEntry(remoteHost);
            //foreach (IPAddress ipa in hostInfo.AddressList)
            //{
            //    if (ipa.AddressFamily == AddressFamily.InterNetwork) ipaTmp = ipa;
            //}

            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(remoteHost), remotePort);

            try
            {
                clientSocket.Connect(ep);
            }
            catch (Exception)
            {
                throw new IOException("Couldn't connect to remote server");
            }

            readReply();
            if (retValue != 220)
            {
                close();
                throw new IOException(reply.Substring(0));
            }

            sendCommand("USER " + remoteUser);

            if (!(retValue == 331 || retValue == 230))
            {
                cleanup();
                throw new IOException(reply.Substring(0));
            }

            if (retValue != 230)
            {
                sendCommand("PASS " + remotePass);
                if (!(retValue == 230 || retValue == 202))
                {
                    cleanup();
                    throw new IOException(reply.Substring(0));
                }
            }

            logined = true;
            chdir(remotePath);
        }

        ///
        /// If the value of mode is true, set binary mode for downloads.
        /// Else, set Ascii mode.
        ///
        ///
        public void setBinaryMode(Boolean mode)
        {
            if (mode)
            {
                sendCommand("TYPE I");
            }
            else
            {
                sendCommand("TYPE A");
            }
            if (retValue != 200)
            {
                throw new IOException(reply.Substring(0));
            }
        }

        ///
        /// Download a file to the Assembly's local directory,
        /// keeping the same file name.
        ///
        ///
        public void download(string remFileName, ref int iCurrPosition)
        {
            download(remFileName, "", false, ref iCurrPosition);
        }

        ///
        /// Download a remote file to the Assembly's local directory,
        /// keeping the same file name, and set the resume flag.
        ///
        ///
        ///
        public void download(string remFileName, Boolean resume, ref int iCurrPosition)
        {
            download(remFileName, "", resume, ref iCurrPosition);
        }

        ///
        /// Download a remote file to a local file name which can include
        /// a path. The local file name will be created or overwritten,
        /// but the path must exist.
        ///
        ///
        ///
        public void download(string remFileName, string locFileName, ref int iCurrPosition)
        {
            download(remFileName, locFileName, false, ref iCurrPosition);
        }

        ///
        /// Download a remote file to a local file name which can include
        /// a path. The local file name will be created or overwritten,
        /// but the path must exist.
        ///
        ///
        ///
        public void download(string remFileName, string locFileName)
        {
            int i = 0;
            download(remFileName, locFileName, false, ref i);
        }

        ///
        /// Download a remote file to a local file name which can include
        /// a path, and set the resume flag. The local file name will be
        /// created or overwritten, but the path must exist.
        ///
        public void download(string remFileName, string locFileName, Boolean resume, ref int iCurrPosition)
        {
            iCurrPosition = 0;

            if (!logined)
            {
                login();
            }

            setBinaryMode(true);

            //Console.WriteLine("Downloading file " + remFileName + " from" + remoteHost + "/" + remotePath);

            if (locFileName.Equals(""))
            {
                locFileName = remFileName;
            }

            if (!File.Exists(locFileName))
            {
                Stream st = File.Create(locFileName);
                st.Close();
            }

            Socket cSocket = createDataSocket();

            FileStream output = new FileStream(locFileName, FileMode.Open);

            long offset = 0;

            if (resume)
            {
                offset = output.Length;
                if (offset > 0)
                {
                    sendCommand("REST " + offset);
                    if (retValue != 350)
                    {
                        //throw new IOException(reply.Substring(4));
                        //Some servers may not support resuming.
                        offset = 0;
                    }
                }

                if (offset > 0)
                {
                    if (debug)
                    {
                        //Console.WriteLine("seeking to " + offset);
                    }
                    long npos = output.Seek(offset, SeekOrigin.Begin);
                    //Console.WriteLine("new pos=" + npos);
                }
            }

            sendCommand("RETR " + remFileName);

            if (!(retValue == 150 || retValue == 125))
            {
                if (output != null)
                {
                    output.Close();
                }
                throw new IOException(reply.Substring(0));
            }

            int iCountRecived = 0;

            while (!bAbort)
            {
                iCountRecived = 0;
                do
                {
                    bytes = cSocket.Receive(buffSD, iCountRecived, iBuffSD_SIZE - iCountRecived, 0);
                    iCurrPosition += bytes;
                    iCountRecived = iCountRecived + bytes;
                    //System.Threading.Thread.Sleep(1);
                    if (bytes <= 0) break;
                } while (iCountRecived < iBuffSD_SIZE);

                try
                {
                    output.Write(buffSD, 0, iCountRecived);
                }
                catch (Exception ex)
                {
                    output.Close();
                    throw new IOException(ex.Message);
                }

                //bytes = cSocket.Receive(buffer, buffer.Length, 0);
                //output.Write(buffer, 0, bytes);
                if (bytes <= 0) break;
            }

            output.Close();
            if (cSocket.Connected)
            {
                cSocket.Close();
            }

            //Console.WriteLine("");

            readReply();

            if (!(retValue == 226 || retValue == 250))
            {
                throw new IOException(reply.Substring(0));
            }
        }

        /*
        Socket cSocketDownload = null;
        FileStream outputDownload = null;
        bool bDownloadComplete = false;
        ///
        /// Download a remote file to a local file name which can include
        /// a path, and set the resume flag. The local file name will be
        /// created or overwritten, but the path must exist.
        ///
        public void download(string remFileName, string locFileName, Boolean resume)
        {
            if (!logined)
            {
                login();
            }

            setBinaryMode(true);

            //Console.WriteLine("Downloading file " + remFileName + " from" + remoteHost + "/" + remotePath);

            if (locFileName.Equals(""))
            {
                locFileName = remFileName;
            }

            if (!File.Exists(locFileName))
            {
                Stream st = File.Create(locFileName);
                st.Close();
            }

            outputDownload = new FileStream(locFileName, FileMode.Open);

            cSocketDownload = createDataSocket();

            long offset = 0;

            if (resume)
            {
                offset = outputDownload.Length;
                if (offset > 0)
                {
                    sendCommand("REST " + offset);
                    if (retValue != 350)
                    {
                        //throw new IOException(reply.Substring(4));
                        //Some servers may not support resuming.
                        offset = 0;
                    }
                }

                if (offset > 0)
                {
                    if (debug)
                    {
                        //Console.WriteLine("seeking to " + offset);
                    }
                    long npos = outputDownload.Seek(offset, SeekOrigin.Begin);
                    //Console.WriteLine("new pos=" + npos);
                }
            }

            sendCommand("RETR " + remFileName);

            if (!(retValue == 150 || retValue == 125))
            {
                throw new IOException(reply.Substring(4));
            }
            bDownloadComplete = false;
            cSocketDownload.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, acbNetRead, null);
            while (!bDownloadComplete) System.Threading.Thread.Sleep(10);
        }
        private void OnReadNetComplete(IAsyncResult ar)			// when called back by the read
        {
            int bytesRead;

            try
            {
                bytesRead = cSocketDownload.EndReceive(ar);

                if (bytesRead > 0)
                {
                    outputDownload.Write(buffer, 0, bytesRead);
                    cSocketDownload.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, acbNetRead, null);
                }
                else
                {
                    outputDownload.Close();
                    if (cSocketDownload.Connected)
                    {
                        cSocketDownload.Close();
                    }
                    bDownloadComplete = true;
                    //Console.WriteLine("");

                    readReply();

                    if (!(retValue == 226 || retValue == 250))
                    {
                        throw new IOException(reply.Substring(4));
                    }
                }
            }
            catch		// клиент отвалился
            {
                //Destroy();
            }
        }
         
         */

        ///
        /// Upload a file.
        ///
        ///
        public void upload(string fileName)
        {
            upload(fileName, false);
        }

        ///
        /// Upload a file and set the resume flag.
        ///
        ///
        ///
        public void upload(string fileName, Boolean resume)
        {
            if (!logined)
            {
                login();
            }

            Socket cSocket = createDataSocket();
            long offset = 0;

            if (resume)
            {
                try
                {

                    setBinaryMode(true);
                    offset = getFileSize(fileName);

                }
                catch (Exception)
                {
                    offset = 0;
                }
            }

            if (offset > 0)
            {
                sendCommand("REST " + offset);
                if (retValue != 350)
                {
                    //throw new IOException(reply.Substring(4));
                    //Remote server may not support resuming.
                    offset = 0;
                }
            }

            sendCommand("STOR " + Path.GetFileName(fileName));

            if (!(retValue == 125 || retValue == 150))
            {
                throw new IOException(reply.Substring(0));
            }

            // open input stream to read source file
            FileStream input = new FileStream(fileName, FileMode.Open);

            if (offset != 0)
            {

                if (debug)
                {
                    //Console.WriteLine("seeking to " + offset);
                }
                input.Seek(offset, SeekOrigin.Begin);
            }

            //Console.WriteLine("Uploading file " + fileName + " to " + remotePath);

            while ((bytes = input.Read(buffer, 0, buffer.Length)) > 0)
            {

                cSocket.Send(buffer, bytes, 0);

            }
            input.Close();

            //Console.WriteLine("");

            if (cSocket.Connected)
            {
                cSocket.Close();
            }

            readReply();
            if (!(retValue == 226 || retValue == 250))
            {
                throw new IOException(reply.Substring(0));
            }
        }

        ///
        /// Delete a file from the remote FTP server.
        ///
        ///
        public void deleteRemoteFile(string fileName)
        {
            if (!logined)
            {
                login();
            }

            sendCommand("DELE " + fileName);

            if (retValue != 250)
            {
                throw new IOException(reply.Substring(4));
            }
        }

        ///
        /// Rename a file on the remote FTP server.
        ///
        ///
        ///
        public void renameRemoteFile(string oldFileName, string newFileName)
        {
            if (!logined)
            {
                login();
            }

            sendCommand("RNFR " + oldFileName);

            if (retValue != 350)
            {
                throw new IOException(reply.Substring(4));
            }

            //  known problem
            //  rnto will not take care of existing file.
            //  i.e. It will overwrite if newFileName exist
            sendCommand("RNTO " + newFileName);
            if (retValue != 250)
            {
                throw new IOException(reply.Substring(4));
            }
        }

        ///
        /// Create a directory on the remote FTP server.
        ///
        ///
        public void mkdir(string dirName)
        {
            if (!logined)
            {
                login();
            }

            sendCommand("MKD " + dirName);

            if (retValue != 250)
            {
                throw new IOException(reply.Substring(4));
            }
        }

        ///
        /// Delete a directory on the remote FTP server.
        ///
        ///
        public void rmdir(string dirName)
        {
            if (!logined)
            {
                login();
            }

            sendCommand("RMD " + dirName);

            if (retValue != 250)
            {
                throw new IOException(reply.Substring(4));
            }
        }

        ///
        /// Change the current working directory on the remote FTP server.
        ///
        ///
        public void chdir(string dirName)
        {
            if (dirName.Equals("."))
            {
                return;
            }
            if (dirName.Equals(""))
            {
                return;
            }
            if (dirName.Equals(null))
            {
                return;
            }
            if (!logined)
            {
                login();
            }

            sendCommand("CWD " + dirName);

            if (retValue != 250)
            {
                throw new IOException(reply.Substring(0));
            }

            this.remotePath = dirName;

            //Console.WriteLine("Current directory is " + remotePath);
        }

        ///
        /// Close the FTP connection.
        ///
        public void close()
        {
            if (clientSocket != null)
            {
                try
                {
                    sendCommand("QUIT");
                }
                catch { }
            }

            cleanup();
            //Console.WriteLine("Closing...");
        }

        private void readReply()
        {
            mes = "";
            reply = readLine();
            //if (debug) WriteDebugString("from srv: " + reply);
            retValue = Int32.Parse(reply.Substring(0, 3));
        }

        private void cleanup()
        {
            if (clientSocket != null)
            {
                clientSocket.Close();
                clientSocket = null;
            }
            logined = false;
        }

        private string readLine()
        {
            Encoding EncDef = Encoding.Default;

            bytes = 0;
            while (true)
            {
                int i = 0;
                do
                {
                    bytes += clientSocket.Receive(buffer, i, 1, 0);
                    i++;

                } while ((buffer[i - 1] != '\n') && (i < buffer.Length));

                mes += EncDef.GetString(buffer, 0, bytes);
                if (bytes < buffer.Length)
                {
                    break;
                }
            }
            //while (true)  //так было
            //{
            //    bytes = clientSocket.Receive(buffer, buffer.Length, 0);
            //    mes += EncDef.GetString(buffer, 0, bytes);
            //    if (bytes < buffer.Length)
            //    {
            //        break;
            //    }
            //}
            mes = mes.Replace("\r", "");
            char[] seperator = { '\n' };
            string[] mess = mes.Split(seperator);

            if (mess.Length >= 2)       //иногда читает слитно два ответа, чего делать - непонятно
            {
                mes = mess[mess.Length - 2];
            }
            else
            {
                mes = mess[0];
            }

            if (!mes.Substring(3, 1).Equals(" "))
            {
                mes = null;
                return readLine();
            }

            if (debug)
            {
                for (int k = 0; k < mess.Length - 1; k++)
                {
                    //Console.WriteLine(mess[k]);
                }
            }
            return mes;
        }

        private void sendCommand(String command)
        {
            //if (debug) WriteDebugString("to srv: " + command);
            Byte[] cmdBytes = Encoding.ASCII.GetBytes((command + "\r\n").ToCharArray());
            clientSocket.Send(cmdBytes, cmdBytes.Length, 0);
            readReply();
        }

        private Socket createDataSocket()
        {

            sendCommand("PASV");

            if (retValue != 227)
            {
                throw new IOException(reply.Substring(4));
            }

            int index1 = reply.IndexOf('(');
            int index2 = reply.IndexOf(')');
            string ipData = reply.Substring(index1 + 1, index2 - index1 - 1);
            int[] parts = new int[6];

            int len = ipData.Length;
            int partCount = 0;
            string buf = "";

            for (int i = 0; i < len && partCount <= 6; i++)
            {
                char ch = Convert.ToChar(ipData.Substring(i, 1));
                //char ch = Char.Parse(ipData.Substring(i, 1));
                if (Char.IsDigit(ch)) buf += ch;
                else if (ch != ',')
                {
                    throw new IOException("Malformed PASV reply: " + reply);
                }

                if (ch == ',' || i + 1 == len)
                {
                    try
                    {
                        parts[partCount++] = Int32.Parse(buf);
                        buf = "";
                    }
                    catch (Exception)
                    {
                        throw new IOException("Malformed PASV reply: " + reply);
                    }
                }
            }

            string ipAddress = parts[0] + "." + parts[1] + "." + parts[2] + "." + parts[3];

            int port = (parts[4] << 8) + parts[5];

            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //IPEndPoint ep = new IPEndPoint(Dns.GetHostEntry(ipAddress).AddressList[0], port);

            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            //s.ReceiveTimeout = 2000;
            //s.SendTimeout = 2000;

            try
            {
                s.Connect(ep);
            }
            catch (Exception)
            {
                throw new IOException("Can't connect to remote server");
            }

            return s;
        }

        private void WriteDebugString(string strWr)
        {
            string sPathExecute = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            if (sPathExecute.IndexOf("file:\\") == 0) sPathExecute = sPathExecute.Substring("file:\\".Length);

            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(sPathExecute + "\\" + "FTP.log", true);
                sw.WriteLine(DateTime.Now.ToLocalTime().ToString() + "   " + strWr);
            }
            catch //(Exception ex)
            {
            }
            finally
            {
                if (sw != null) sw.Close();
            }
        }
    }
}

/*Использование
public class Test 
{
   public static void Main() 
   {
     try 
     {
       ff = new FTPClient();
       //ff.setDebug(true);
       ff.setRemoteHost(this.textBoxServerName.Text);
       //ff.setRemoteUser(this.textBoxUserName.Text);
       //ff.setRemotePass(this.textBoxUserPass.Text);
       ff.login();
       ff.chdir("incoming");

       string[] fileNames = ff.getFileList("*.*");
       for(int i=0;i < fileNames.Length;i++) 
       {
         //Console.WriteLine(fileNames[i]);
       }
       ff.setBinaryMode(true);
       ff.upload("c:\jaimon\tmp\Webdunia.ttf");
       ff.download(this.textBoxFileName.Text, "\\storage card\\" + this.textBoxFileName.Text);
       ff.close();

     }
     catch(Exception e) 
     {
       //Console.WriteLine("Caught Error :" + e.Message);
     }
   }
}
*/