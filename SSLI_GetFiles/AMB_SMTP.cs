using System;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Collections;

namespace AMB_MAIL
{
	/// <summary>
	/// Отправка почты, работет под СЕ
	/// </summary>
    public class AMB_SMTP
    {
        private int smtpPort = 25;
        private string smtpServer;
        /// <summary>
        /// Адрес SMTP севера
        /// </summary>
        public string SmtpServer
        {
            get
            {
                return this.smtpServer;
            }
            set
            {
                this.smtpServer = value;
            }
        }
        /// <summary>
        /// Порт smtp сервера по умолчаню 25
        /// </summary>
        public int SmtpPort
        {
            get
            {
                return this.smtpPort;
            }
            set
            {
                this.smtpPort = value;
            }
        }

        private enum SMTPResponse : int
        {
            CONNECT_SUCCESS = 220,
            GENERIC_SUCCESS = 250,
            DATA_SUCCESS = 354,
            QUIT_SUCCESS = 221

        }
        /// <summary>
        /// Отправка сообщения
        /// </summary>
        /// <param name="message">Соформированное сообщение</param>
        /// <returns>True если ОК</returns>
        public bool Send(MailMessage message)
        {
            string sFileName = null;
            if (message.Attachments.Count > 0)
            {
                sFileName = ((MailAttachment)message.Attachments[0]).Filename;
            }
            else return SendSmallMessage(message);

            if(sFileName!=null)
            {
                if(!File.Exists(sFileName)) return false;
            }
            FileInfo fi = new FileInfo(sFileName);
            if (fi.Length < 100000)
            {
                return SendSmallMessage(message);
            }
            else
            {
                return SendBigMessage(message);
            }

        }

        private bool SendSmallMessage(MailMessage message)
        {
            //IPHostEntry IPhst = Dns.GetHostEntry(SmtpServer);
            //IPEndPoint endPt = new IPEndPoint(IPhst.AddressList[0], 25);

            IPEndPoint endPt = new IPEndPoint(IPAddress.Parse(SmtpServer), 25);
            Socket s = new Socket(endPt.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            s.Connect(endPt);

            if (!Check_Response(s, SMTPResponse.CONNECT_SUCCESS))
            {
                s.Close();
                return false;
            }

            Senddata(s, string.Format("HELO {0}\r\n", Dns.GetHostName()));
            if (!Check_Response(s, SMTPResponse.GENERIC_SUCCESS))
            {
                s.Close();
                return false;
            }

            Senddata(s, string.Format("MAIL FROM: <{0}>\r\n", message.From));
            if (!Check_Response(s, SMTPResponse.GENERIC_SUCCESS))
            {

                s.Close();
                return false;
            }

            string _To = message.To;
            string[] Tos = _To.Split(new char[] { ';' });
            foreach (string To in Tos)
            {
                Senddata(s, string.Format("RCPT TO: <{0}>\r\n", To));
                if (!Check_Response(s, SMTPResponse.GENERIC_SUCCESS))
                {

                    s.Close();
                    return false;
                }
            }

            if (message.Cc != null)
            {
                Tos = message.Cc.Split(new char[] { ';' });
                foreach (string To in Tos)
                {
                    Senddata(s, string.Format("RCPT TO: <{0}>\r\n", To));
                    if (!Check_Response(s, SMTPResponse.GENERIC_SUCCESS))
                    {
                        s.Close();
                        return false;
                    }
                }
            }

            StringBuilder Header = new StringBuilder();
            Header.Append("From: " + message.From + "\r\n");
            Tos = message.To.Split(new char[] { ';' });
            Header.Append("To: ");
            for (int i = 0; i < Tos.Length; i++)
            {
                Header.Append(i > 0 ? "," : "");
                Header.Append(Tos[i]);
            }
            Header.Append("\r\n");
            if (message.Cc != null)
            {
                Tos = message.Cc.Split(new char[] { ';' });
                Header.Append("Cc: ");
                for (int i = 0; i < Tos.Length; i++)
                {
                    Header.Append(i > 0 ? "," : "");
                    Header.Append(Tos[i]);
                }
                Header.Append("\r\n");
            }
            Header.Append("Date: ");
            //Date: Mon, 30 Mar 2009 08:37:41 +0400
            Header.Append(DateTime.Now.ToString("ddd, d MMM yyyy H:m:s z"));
            Header.Append("\r\n");
            Header.Append("Subject: " + "=?" + message.BodyEncoding.WebName + "?B?" + System.Convert.ToBase64String(message.BodyEncoding.GetBytes(message.Subject)) + "?=" + "\r\n");
            Header.Append("X-Mailer: AMB_SMTP_smallmess v1\r\n");
            string MsgBody = System.Convert.ToBase64String(message.BodyEncoding.GetBytes(message.Body));
            if (!MsgBody.EndsWith("\r\n")) MsgBody += "\r\n";

            StringBuilder sb = new StringBuilder();
            if (message.Attachments.Count > 0)
            {
                Header.Append("MIME-Version: 1.0\r\n");
                Header.Append("Content-Type: multipart/mixed; boundary=unique-boundary-1\r\n");
                Header.Append("\r\n");
                Header.Append("This is a multi-part message in MIME format.\r\n");

                sb.Append("--unique-boundary-1\r\n");
            }
            sb.Append("Content-Type: text/plain; ");
            sb.Append("charset=\"" + message.BodyEncoding.WebName + "\"\r\n");
            sb.Append("Content-Transfer-Encoding: base64\r\n");
            sb.Append("\r\n");
            sb.Append(MsgBody + "\r\n");
            sb.Append("\r\n");
            if (message.Attachments.Count > 0)
            {
                foreach (object o in message.Attachments)
                {
                    MailAttachment a = o as MailAttachment;
                    byte[] binaryData;
                    if (a != null)
                    {
                        FileInfo f = new FileInfo(a.Filename);
                        sb.Append("--unique-boundary-1\r\n");
                        sb.Append("Content-Type: application/octet-stream; file=" + "\"" + "=?" + message.BodyEncoding.WebName + "?B?" + System.Convert.ToBase64String(message.BodyEncoding.GetBytes(f.Name)) + "?=" + "\"" + "\r\n");
                        sb.Append("Content-Transfer-Encoding: base64\r\n");
                        sb.Append("Content-Disposition: attachment; filename=" + "\"" + "=?" + message.BodyEncoding.WebName + "?B?" + System.Convert.ToBase64String(message.BodyEncoding.GetBytes(f.Name)) + "?=" + "\"" + "\r\n");
                        sb.Append("\r\n");
                        FileStream fs = f.OpenRead();
                        binaryData = new Byte[57];
                        int iBytesRead = 0;
                        do
                        {
                            iBytesRead = fs.Read(binaryData, 0, 57);
                            string sTemp = System.Convert.ToBase64String(binaryData, 0, iBytesRead);
                            sb.Append(System.Convert.ToBase64String(binaryData, 0, iBytesRead));
                            sb.Append("\r\n");
                        } while (iBytesRead > 0);
                        fs.Close();
                        fs = null;

                        sb.Append("--unique-boundary-1--\r\n"); ///;;;                       
                    }
                }
                MsgBody = sb.ToString();
                sb = null;
            }

            Senddata(s, ("DATA\r\n"));
            if (!Check_Response(s, SMTPResponse.DATA_SUCCESS))
            {

                s.Close();
                return false;
            }

            Header.Capacity = Header.Capacity + MsgBody.Length + 8;
            Header.Append("\r\n");

            Header.Append(MsgBody);
            Header.Append(".\r\n");
            Header.Append("\r\n");
            Header.Append("\r\n");
            int iNumBlok = (int)(Header.Length / 10000);
            for (int iSendCount = 0; iSendCount < iNumBlok; iSendCount++)
            {
                Senddata(s, Header.ToString(iSendCount * 10000, 10000));
            }
            Senddata(s, Header.ToString(iNumBlok * 10000, Header.Length - iNumBlok * 10000));
            if (!Check_Response(s, SMTPResponse.GENERIC_SUCCESS))
            {

                s.Close();
                return false;
            }

            Senddata(s, "QUIT\r\n");
            //if (Check_Response(s, SMTPResponse.QUIT_SUCCESS))
            //{
            //    s.Close();
            //    return false;
            //}
            return true;
        }

        private bool SendBigMessage(MailMessage message)
        {
            //IPHostEntry IPhst = Dns.GetHostEntry(SmtpServer);
            //IPEndPoint endPt = new IPEndPoint(IPhst.AddressList[0], 25);

            IPEndPoint endPt = new IPEndPoint(IPAddress.Parse(SmtpServer), 25);
            Socket s = new Socket(endPt.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            s.Connect(endPt);

            if (!Check_Response(s, SMTPResponse.CONNECT_SUCCESS))
            {
                s.Close();
                return false;
            }

            Senddata(s, string.Format("HELO {0}\r\n", Dns.GetHostName()));
            if (!Check_Response(s, SMTPResponse.GENERIC_SUCCESS))
            {
                s.Close();
                return false;
            }

            Senddata(s, string.Format("MAIL From: <{0}>\r\n", message.From));
            if (!Check_Response(s, SMTPResponse.GENERIC_SUCCESS))
            {
                s.Close();
                return false;
            }

            string _To = message.To;
            string[] Tos = _To.Split(new char[] { ';' });
            foreach (string To in Tos)
            {
                Senddata(s, string.Format("RCPT TO: <{0}>\r\n", To));
                if (!Check_Response(s, SMTPResponse.GENERIC_SUCCESS))
                {
                    s.Close();
                    return false;
                }
            }

            if (message.Cc != null)
            {
                Tos = message.Cc.Split(new char[] { ';' });
                foreach (string To in Tos)
                {
                    Senddata(s, string.Format("RCPT TO: <{0}>\r\n", To));
                    if (!Check_Response(s, SMTPResponse.GENERIC_SUCCESS))
                    {
                        s.Close();
                        return false;
                    }
                }
            }

            StringBuilder Header = new StringBuilder();
            Header.Append("From: " + message.From + "\r\n");
            Tos = message.To.Split(new char[] { ';' });
            Header.Append("To: ");
            for (int i = 0; i < Tos.Length; i++)
            {
                Header.Append(i > 0 ? "," : "");
                Header.Append(Tos[i]);
            }
            Header.Append("\r\n");
            if (message.Cc != null)
            {
                Tos = message.Cc.Split(new char[] { ';' });
                Header.Append("Cc: ");
                for (int i = 0; i < Tos.Length; i++)
                {
                    Header.Append(i > 0 ? "," : "");
                    Header.Append(Tos[i]);
                }
                Header.Append("\r\n");
            }
            Header.Append("Date: ");
            Header.Append(DateTime.Now.ToString("ddd, d MMM yyyy H:m:s z"));
            Header.Append("\r\n");
            Header.Append("Subject: " + "=?" + message.BodyEncoding.WebName + "?B?" + System.Convert.ToBase64String(message.BodyEncoding.GetBytes(message.Subject)) + "?=" + "\r\n");
            Header.Append("X-Mailer: AMB_SMTP_bigmess v1\r\n");
            string MsgBody = System.Convert.ToBase64String(message.BodyEncoding.GetBytes(message.Body));
            if (!MsgBody.EndsWith("\r\n")) MsgBody += "\r\n";

            Senddata(s, ("DATA\r\n"));
            if (!Check_Response(s, SMTPResponse.DATA_SUCCESS))
            {
                s.Close();
                return false;
            }

            //Header.Append("\r\n");

            Senddata(s, Header.ToString());
            Header.Remove(0, Header.Length);

            if (message.Attachments.Count > 0)
            {
                Header.Append("MIME-Version: 1.0\r\n");
                Header.Append("Content-Type: multipart/mixed; boundary=unique-boundary-1\r\n");
                Header.Append("\r\n");
                Header.Append("This is a multi-part message in MIME format.\r\n");
                Header.Append("\r\n");//;;;

                Senddata(s, Header.ToString());
                Header.Remove(0, Header.Length);

                StringBuilder sb = new StringBuilder();
                sb.Append("--unique-boundary-1\r\n");
                sb.Append("Content-Type: text/plain; ");
                sb.Append("charset=\"" + message.BodyEncoding.WebName + "\"\r\n");
                sb.Append("Content-Transfer-Encoding: base64\r\n");
                sb.Append("\r\n");
                sb.Append(MsgBody + "\r\n");
                sb.Append("\r\n");

                Senddata(s, sb.ToString());
                sb.Remove(0, sb.Length);

                foreach (object o in message.Attachments)
                {
                    MailAttachment a = o as MailAttachment;
                    byte[] binaryData;
                    if (a != null)
                    {
                        FileInfo f = new FileInfo(a.Filename);
                        sb.Append("--unique-boundary-1\r\n");
                        sb.Append("Content-Type: application/octet-stream; file=" + "\"" + "=?" + message.BodyEncoding.WebName + "?B?" + System.Convert.ToBase64String(message.BodyEncoding.GetBytes(f.Name)) + "?=" + "\"" + "\r\n");
                        sb.Append("Content-Transfer-Encoding: base64\r\n");
                        sb.Append("Content-Disposition: attachment; filename=" + "\"" + "=?" + message.BodyEncoding.WebName + "?B?" + System.Convert.ToBase64String(message.BodyEncoding.GetBytes(f.Name)) + "?=" + "\"" + "\r\n");
                        sb.Append("\r\n");

                        Senddata(s, sb.ToString());
                        sb.Remove(0, sb.Length);

                        FileStream fs = f.OpenRead();
                        binaryData = new Byte[57];
                        int iBytesRead = 0;
                        do
                        {
                            iBytesRead = fs.Read(binaryData, 0, 57);
                            string sTemp = System.Convert.ToBase64String(binaryData, 0, iBytesRead);
                            sb.Append(System.Convert.ToBase64String(binaryData, 0, iBytesRead));
                            sb.Append("\r\n");

                            Senddata(s, sb.ToString());
                            sb.Remove(0, sb.Length);

                        } while (iBytesRead > 0);
                        fs.Close();
                        fs = null;

                        Senddata(s, sb.ToString());
                        sb.Remove(0, sb.Length);

                        //sb.Append("\r\n");
                        sb.Append("--unique-boundary-1--\r\n"); ///;;; 
                        ///
                        Senddata(s, sb.ToString());
                        sb.Remove(0, sb.Length);
                    }
                }
                //MsgBody = sb.ToString();
                sb = null;
            }


            Header.Append(".\r\n");
            Header.Append("\r\n");
            Header.Append("\r\n");
            Senddata(s, Header.ToString());
            if (!Check_Response(s, SMTPResponse.GENERIC_SUCCESS))
            {

                s.Close();
                return false;
            }

            Senddata(s, "QUIT\r\n");
            //if (Check_Response(s, SMTPResponse.QUIT_SUCCESS))
            //{
            //    s.Close();
            //    return false;
            //}
            return true;
        }

        private void Senddata(Socket s, string msg)
        {

            byte[] _msg = Encoding.ASCII.GetBytes(msg);
            s.Send(_msg, 0, _msg.Length, SocketFlags.None);
        }

        private bool Check_Response(Socket s, SMTPResponse response_expected)
        {
            string sResponse;
            int iCount = 0;
            int response;
            byte[] bytes = new byte[1024];
            while (s.Available == 0)
            {
                System.Threading.Thread.Sleep(100);
                iCount++;
                if (iCount > 300) return false;
            }

            s.Receive(bytes, 0, s.Available, SocketFlags.None);
            sResponse = Encoding.ASCII.GetString(bytes, 0, (int)bytes.Length);
            response = Convert.ToInt32(sResponse.Substring(0, 3));
            if (response != (int)response_expected)
                return false;
            return true;
        }
    }
    // Использование
    //private void buttonSend_Click(object sender, EventArgs e)
    //    {
    //        AMB_SMTP smtp = new AMB_SMTP();
    //        smtp.SmtpServer = "87.249.14.90";
    //        MailMessage message = new MailMessage();
    //        message.Body = "Mesbody: Превед, susleg!";
    //        message.From = "alexey@ambintech.ru";
    //        message.To = "test_UVD@ambintech.ru";
    //        message.Subject = "Test email";

    //        string sFileAttName = @"\Storage Card\adress.zip";   //OldBuster.txt
    //        if (File.Exists(sFileAttName))
    //        {
    //            MailAttachment attachment = new MailAttachment(sFileAttName);
    //            message.Attachments.Add(attachment);
    //        }
    //        if (smtp.Send(message))
    //        {
    //            MessageBox.Show("Sent OK");
    //        }
    //        else
    //        {
    //            MessageBox.Show("Something BAD Happened!");
    //        }
    //    }

    /// <summary>
    /// Summary description for MailAttachment.
    /// </summary>
    public class MailAttachment
    {
        #region Fields

        private Encoding encoding;
        private string filename;

        #endregion

        #region Constructors

        public MailAttachment(string filename)
        {
            this.filename = filename;
            this.encoding = Encoding.Default;
            CheckFile(filename);
        }

        public MailAttachment(string filename, Encoding encoding)
        {
            this.filename = filename;
            this.encoding = encoding;
            CheckFile(filename);
        }

        #endregion

        #region properties


        public string Filename
        {
            get
            {
                return this.filename;
            }
        }

        public Encoding Encoding
        {
            get
            {
                return this.encoding;
            }
        }

        #endregion

        #region helper methods


        private void CheckFile(string filename)
        {
            try
            {
                // Verify if we can open the file for reading
                File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read).Close();
            }
            catch //(Exception e) 
            {
                throw new ArgumentException("Bad attachment", filename);
            }

        }

        #endregion
    }
    /// <summary>
    /// Summary description for MailMessage.
    /// </summary>
    public class MailMessage
    {
        #region Fields

        private string to;
        private string from;
        private string body;
        private string subject;
        private string cc;
        private string bcc;
        private Encoding bodyEncoding;
        private IList attachments;

        #endregion

        #region Constructors

        public MailMessage()
        {
            attachments = new ArrayList();
        }

        #endregion

        #region properties

        public string To
        {
            get
            {
                return this.to;
            }
            set
            {

                this.to = value;
            }
        }

        public string From
        {
            get
            {
                return this.from;
            }
            set
            {

                this.from = value;
            }
        }

        public string Body
        {
            get
            {
                return this.body;
            }
            set
            {
                this.body = value;
            }
        }

        public string Subject
        {
            get
            {
                return this.subject;
            }
            set
            {
                this.subject = value;
            }
        }

        public string Cc
        {
            get
            {
                return this.cc;
            }
            set
            {

                this.cc = value;
            }
        }

        public string Bcc
        {
            get
            {
                return this.bcc;
            }
            set
            {

                this.bcc = value;
            }
        }

        public Encoding BodyEncoding
        {
            get
            {
                return this.bodyEncoding;
            }
            set
            {
                this.bodyEncoding = value;
            }
        }

        public IList Attachments
        {
            get
            {
                return this.attachments;
            }
        }


        #endregion

    }
}

