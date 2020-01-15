using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Xml.Serialization;
using System.Collections;
using AMB_FTPCLIENT;
using AMB_MAIL;
using Ionic.Zip;

namespace SSCE
{
    public class GetFiles : SSCE.ClassAMBRenewedService
    {
        private int iDebugLevel = 1;
        private bool bAbort = false;

        private string sPathExecute = null;
        private string sPathToOutBox = null;
        //private string sPathToInBox = null;
        private string sSMTPServer = null;
        //private string sPOP3Server = null;
        //private string sUserPOP3 = null;
        //private string sPasswordPOP3 = null;
        private string sNamePodrazdelenie = null;
        private string sPathToNewSoftSS = null;
        private string sPathToNewSoftTerminal = null;
        private string sPathToUpdTerm = null;
        private string sAdressMailServerUVD = null;
        private string sAdressMailLocal = null;

        private const string sFileNameResult = "SSStat_X.xml";

        private TimeSpan tsReciveInterval;
        private TimeSpan tsSendInterval;
        private DateTime dtLastRecive = DateTime.Now.AddHours(0); //было -1
        private DateTime dtLastSend = DateTime.Now.AddHours(0);
        //private POPClient popClient = new POPClient();

        //========== GFB - GetFullBases ================
        private string sPathToFullBase = null;
        private string sIPServerUVD = null;
        private string sPortServerUVD = null;

        private string sUserFTP = "anonymous";
        private string sPassFTP = "test@test.ru";

        private string sFileNameList = null;
        private string sPathOnServerToFullBase = null;
        private string sPathOnServerToUpdates = null;
        private string sPathOnServerToIncoming = null;

        private TimeSpan tsGFBReciveInterval;
        private DateTime dtGFBLastRecive = DateTime.Now.AddHours(0);
        private DateTime dtGFBNextRecive = DateTime.Now;
        private DateTime dtNewSoftNextRecive = DateTime.Now;
        private string sGFBHoursRecive = "01:00";
        private stOneBaseFull[] stRemoteFiles = null;

        public override bool Init()
        {
            bool bRet = false;
            try
            {
                lock (Utils.oSyncroLoadSaveInit)
                {
                    // ==== GFB ====
                    sPathToFullBase = Utils.strConfig.sPathToFullBase;
                    sIPServerUVD = Utils.strConfig.strGetFiles.sIPServerUVD;
                    sPortServerUVD = Utils.strConfig.strGetFiles.sPortServerUVD_FTP;
                    sUserFTP = Utils.strConfig.strGetFiles.sFTPUser;
                    sPassFTP = Utils.strConfig.strGetFiles.sFTPPass;

                    sFileNameList = Utils.strConfig.strGetFiles.sFileNameListOnServer;
                    sPathOnServerToFullBase = Utils.strConfig.strGetFiles.sPathOnServerToFullBase;
                    sPathOnServerToUpdates = Utils.strConfig.strGetFiles.sPathOnServerToUpdates;
                    if (Utils.strConfig.strGetFiles.sPathOnServerToIncoming == null) sPathOnServerToIncoming = "Incoming";
                    else sPathOnServerToIncoming = Utils.strConfig.strGetFiles.sPathOnServerToIncoming;
                    tsGFBReciveInterval = TimeSpan.FromDays(Utils.strConfig.strGetFiles.iReciveInterval);
                    sGFBHoursRecive = Utils.strConfig.strGetFiles.sHoursRecive;
                    dtGFBNextRecive = Convert.ToDateTime(Utils.strConfig.strGetFiles.sNextRecive);

                    // ==== MAIL ====
                    sPathToOutBox = Utils.strConfig.sPathToOutBox;
                    //sPathToInBox = Utils.strConfig.sPathToInBox;
                    sPathExecute = Utils.strConfig.sPathToExecute;
                    sNamePodrazdelenie = Utils.strConfig.sNamePodrazdelenie;
                    iDebugLevel = Utils.strConfig.iDebugLevel;

                    sPathToNewSoftSS = Utils.strConfig.sPathToNewSoftSS;
                    sPathToNewSoftTerminal = Utils.strConfig.sPathToNewSoftTerminal;
                    sPathToUpdTerm = Utils.strConfig.sPathToUpdTerm;

                    sSMTPServer = Utils.strConfig.strMail.sSMTPServer;
                    //sPOP3Server = Utils.strConfig.strMail.sPOP3Server;
                    //sUserPOP3 = Utils.strConfig.strMail.sUserPOP3;
                    //sPasswordPOP3 = Utils.strConfig.strMail.sPassPOP3;
                    sAdressMailServerUVD = Utils.strConfig.strMail.sAdresServerUVD;
                    sAdressMailLocal = Utils.strConfig.strMail.sAdressMailLocal;

                    tsReciveInterval = TimeSpan.FromMinutes(Utils.strConfig.strMail.iReciveInterval);
                    tsSendInterval = TimeSpan.FromMinutes(Utils.strConfig.strMail.iSendInterval);
                }
                WriteDebugString("---------------------------", 1);

                if ((sPathToOutBox != null) && (sPathExecute != null) && (sIPServerUVD != null) &&
                    (sPortServerUVD != null) && (sPathToFullBase != null) && (sNamePodrazdelenie != null) && (sSMTPServer != null)&&
                    (sAdressMailServerUVD != null))
                {
                    WriteDebugString("Init:OK", 2);
                    bRet = true;
                }
                else
                {
                    WriteDebugString("Init:ERROR - Неполные данные настроек работы в реестре.", 0);
                    bRet = false;
                }
            }
            catch (Exception ex)
            {
                WriteDebugString("Init:ERROR - " + ex.Message, 0);
                bRet = false;
            }

            try
            {
                int.Parse(sPortServerUVD);
            }
            catch (Exception ex)
            {
                WriteDebugString("Init.sPortServerUVD(=" + sPortServerUVD + "):ERROR - " + ex.Message, 1);
                sPortServerUVD = "21";
            }

            return bRet;
        }

        public override void Start()
        {
            WriteDebugString("Start.Entrance:OK", 2);

            while (!bAbort)
            {
                Thread.Sleep(950);

                if (Utils.IsNetPresented())     //если нет сети, то что тут делать?
                {
                    if (DateTime.Now > (dtLastRecive + tsReciveInterval))
                    {                                                       //пришло время забирать файлы
                        GetUpdatesFTP();

                        if (DateTime.Now > dtNewSoftNextRecive)
                        {
                            GetNewSoftFTP();
                            dtNewSoftNextRecive.AddDays(1);             //обновления софта забираются раз в день
                        }
                        dtLastRecive = DateTime.Now;
                    }
                    if (DateTime.Now > (dtLastSend + tsSendInterval))
                    {                                                       //пришло время файлы разбрасывать
                        PrepareAndSend();
                    }

                    if (DateTime.Now > dtGFBNextRecive)
                    {                                                       //забираем базу целиком
                        if (!Utils.bRebootRequest)
                        {
                            GetFullBase();
                            Utils.SaveInit("\\Storage Card" + "\\" + Utils.sFileNameInit + ".bak");
                            Utils.SaveInit("\\Documents and Settings\\SSCE" + "\\" + Utils.sFileNameInit + ".bak");
                        }
                    }
                }
            }
            WriteDebugString("Start.Exit:OK", 2);
            WriteDebugString("===========================", 1);
        }
               
        public override void Stop()
        {
            bAbort = true;
            Utils.DeleteMarkerDirBusy(sPathToUpdTerm);
            Utils.DeleteMarkerDirBusy(sPathToNewSoftTerminal);
            Utils.DeleteMarkerDirBusy(sPathToNewSoftSS);
            //Utils.DeleteMarkerDirBusy(sPathToInBox);
            WriteDebugString("Stop:OK", 1);
        }

        private bool WriteDebugString(string strWr, int iLevelDebud)
        {
            bool bRet = true;
            if (iLevelDebud <= iDebugLevel)
            {
                try
                {
                    bRet = Utils.WriteDebugString(sPathExecute, " -GetFilesService- " + strWr);
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

        private void PrepareAndSend()
        {
            if (Utils.IsFolderBusy(sPathToOutBox)) return;

            if (!SendFtpOrMail(sPathOnServerToIncoming))
            {                                                   //отправка по почте
                #region send_from_mail
                string sFrom = sNamePodrazdelenie;
                if (sAdressMailLocal != null) sFrom = sAdressMailLocal;
                string sBodyMail = "Отправлено автоматической службой AMBInTech_MailService со станции синхронизации " + sNamePodrazdelenie + ".\r\n"
                    + "Не отвечайте на это письмо. Если Вы получили его по ошибке свяжитесь с разработчиками по адресу info@ambintech.ru \r\n"
                    + "STATION_NAME=" + sNamePodrazdelenie + ".\r\n" + "STATION_VERSION=" + System.Reflection.Assembly.GetExecutingAssembly().FullName;

                if (Utils.IsFolderBusy(sPathToOutBox))
                {
                    WriteDebugString("PrepareAndSendMail.Send:OK - Directory is busy", 2);
                    dtLastSend = DateTime.Now;
                    return;
                }

                DirectoryInfo di = new DirectoryInfo(sPathToOutBox);
                FileInfo[] fiOut = di.GetFiles("*.*");
                int i = 1;
                AMB_SMTP clientSMTP = new AMB_SMTP();
                clientSMTP.SmtpServer = sSMTPServer;

                Utils.MarkedDirBusy(sPathToOutBox);

                foreach (FileInfo fi in fiOut)
                {
                    bool bSendOk = false;
                    MailMessage message = new MailMessage();
                    message.From = sFrom;
                    message.To = sAdressMailServerUVD;
                    message.Subject = GetSubjectByName(fi.Name);
                    message.Body = sBodyMail;
                    message.BodyEncoding = Encoding.GetEncoding(1251);
                    try
                    {
                        if (fi.Name != Utils.sFileNameFlagBusy)
                        {
                            MailAttachment att = new MailAttachment(fi.FullName);
                            message.Attachments.Add(att);
                            bSendOk = clientSMTP.Send(message);
                            if (bSendOk)
                            {
                                WriteDebugString("PrepareAndSendMail.Send(" + i.ToString() + "):OK - file: " + fi.Name, 2);
                                AddLastSendedInfo(fi.Name);
                            }
                            else WriteDebugString("PrepareAndSendMail.Send(" + i.ToString() + "):FALSE - file: " + fi.Name, 2);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteDebugString("PrepareSendingMail.Send(" + i.ToString() + "):ERROR - " + ex.Message, 1);
                        bSendOk = false;
                    }

                    try
                    {
                        if (bSendOk)
                        {
                            File.Delete(fi.FullName);
                            WriteDebugString("PrepareAndSendMail.Delete(" + i.ToString() + "):OK - file: " + fi.FullName, 2);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteDebugString("PrepareSendingMail.Delete(" + i.ToString() + "):ERROR - " + ex.Message, 1);
                    }
                    i++;
                }
                Utils.DeleteMarkerDirBusy(sPathToOutBox);
                dtLastSend = DateTime.Now;
                #endregion
            }
            else
            {                                                   //отправка по FTP
                #region senf_from_ftp                

                Utils.MarkedDirBusy(sPathToOutBox);
                DirectoryInfo di = new DirectoryInfo(sPathToOutBox);
                FileInfo[] fiOut = di.GetFiles("*.*");

                int i = 1;
                bool bZipOk;
                foreach (FileInfo fi in fiOut)
                {
                    if (fi.Name != Utils.sFileNameFlagBusy)
                    {
                        bZipOk = Utils.ZipFile(fi.FullName, sPathToOutBox + "\\" + Utils.GetDeviceID() + "_" + DateTime.Now.ToString("ddMMyyyy") + "_" + i.ToString() + ".zip", GetSubjectByName(fi.Name));
                        i++;
                        try
                        {
                            if (bZipOk)
                            {
                                File.Delete(fi.FullName);
                                WriteDebugString("PrepareSendingFTP.Delete(" + i.ToString() + "):OK - file: " + fi.FullName, 2);
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteDebugString("PrepareSendingFTP.Delete(" + i.ToString() + "):ERROR - " + ex.Message, 1);
                        }
                    }
                }

                FileInfo[] fiOutZip = di.GetFiles("*.zip");
                if (fiOutZip.Length > 0)
                {
                    WriteDebugString("PrepareSendingFTP.Entrance:OK", 2);
                    AMB_FTPCLIENT.FTPClient ff = new FTPClient();
                    ff.setRemoteHost(sIPServerUVD);
                    ff.setRemoteUser(sUserFTP);
                    ff.setRemotePass(sPassFTP);
                    ff.setRemotePort(int.Parse(sPortServerUVD));

                    try
                    {
                        ff.login();
                        ff.chdir("\\");
                        ff.chdir(sPathOnServerToIncoming);
                    }
                    catch (Exception ex)
                    {
                        WriteDebugString("PrepareSendingFTP.Login:ERROR - " + ex.Message, 1);
                    }

                    i = 1;

                    foreach (FileInfo fi in fiOutZip)
                    {
                        bool bSendOk = false;
                        try
                        {
                            try
                            {

                                ff.setBinaryMode(true);
                                ff.upload(fi.FullName);
                                ff.setBinaryMode(false);
                                bSendOk = true;
                                WriteDebugString("PrepareSendingFTP.Send(" + i.ToString() + "):OK - file: " + fi.Name, 2);
                                AddLastSendedInfo(fi.Name);
                            }
                            catch (Exception ex)
                            {
                                WriteDebugString("PrepareSendingFTP.Send(" + i.ToString() + "):FALSE - file: " + fi.Name + " ERROR - " + ex.Message, 2);
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteDebugString("PrepareSendingFTP.Send(" + i.ToString() + "):ERROR - " + ex.Message, 1);
                            bSendOk = false;
                        }

                        try
                        {
                            if (bSendOk)
                            {
                                File.Delete(fi.FullName);
                                WriteDebugString("PrepareSendingFTP.Delete(" + i.ToString() + "):OK - file: " + fi.FullName, 2);
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteDebugString("PrepareSendingFTP.Delete(" + i.ToString() + "):ERROR - " + ex.Message, 1);
                        }
                        i++;
                    }
                    if (ff != null) ff.close();
                }

                Utils.DeleteMarkerDirBusy(sPathToOutBox);
                dtLastSend = DateTime.Now;                

                #endregion
            }
        }
        private string GetSubjectByName(string sName)
        {
            string sRet = "UNKNOWN";
            if (sName.IndexOf(sFileNameResult.Substring(0, 6)) == 0) sRet = "TERM_STATISTIC";
            if (sName.IndexOf(".log") >= 0) sRet = "SSSERVICE_LOG";
            return sRet;
        }
        private void AddLastSendedInfo(string sFileName)
        {
            lock (Utils.oSyncroLoadSaveInit)
            {
                try
                {
                    Utils.strConfig.strMail.sLastSendedTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                    Utils.cCurrStatus.sLastSendedTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                    Utils.cCurrStatus.sLastSendedName = sFileName;
                    Utils.SaveInit();
                    WriteDebugString("AddLastSendedInfo:OK", 2);
                }
                catch (Exception ex)
                {
                    WriteDebugString("AddLastSendedInfo:ERROR - " + ex.Message, 1);
                }
            }
        }
        private bool SendFtpOrMail(string sFolderFtpIncom)
        {
            bool bRet = false;

            WriteDebugString("SendFtpOrMail.Entrance:OK", 2);
            AMB_FTPCLIENT.FTPClient ff = new FTPClient();
            ff.setRemoteHost(sIPServerUVD);
            ff.setRemoteUser(sUserFTP);
            ff.setRemotePass(sPassFTP);
            ff.setRemotePort(int.Parse(sPortServerUVD));

            try
            {
                ff.login();
                ff.chdir(sFolderFtpIncom);
                bRet = true;
            }
            catch(Exception ex)
            {
                WriteDebugString("SendFtpOrMail:ERROR - " + ex.Message, 2);
                bRet = false;
            }

            if (bRet)WriteDebugString("SendFtpOrMail.Exit - Папка входящих FTP обнаружена.", 2);
            else WriteDebugString("SendFtpOrMail.Exit - Папка входящих FTP не найдена.", 2);
            return bRet;
        }

        private void GetFullBase()
        {
            if (Utils.IsFolderBusy(sPathToFullBase))
            {
                WriteDebugString("GetFullBase:OK  - Directory is busy", 2);
                Thread.Sleep(10000);
                return;
            }

            stRemoteFiles = LoadRemoteListFileFTP();

            if (stRemoteFiles != null)
            {
                for (int i = 0; i < stRemoteFiles.Length; i++)
                {
                    bool bIsUse = false;
                    lock (Utils.oSyncroLoadSaveInit)
                    {
                        foreach (string sSuppTypeBase in Utils.strConfig.arsSupportedBases)
                        {
                            if (stRemoteFiles[i].sTypeBase == sSuppTypeBase) bIsUse = true;
                        }
                    }
                    if (!bIsUse) stRemoteFiles[i].sPathToDownload = null;
                    else
                    {
                        if ((stRemoteFiles[i].sPathToDownload.IndexOf("http") >= 0)) stRemoteFiles[i].sPathToDownload = stRemoteFiles[i].sPathToDownload.Substring(stRemoteFiles[i].sPathToDownload.LastIndexOf(@"/") + 1);
                    }
                }
                foreach (stOneBaseFull ob in stRemoteFiles)
                {
                    if ((ob.sTypeBase != null) && (ob.sPathToDownload != null))
                    {
                        LoadFullBaseFTP(ob);
                    }
                }
                WriteNewNextSaveGFB();
            }
        }
        private stOneBaseFull[] LoadRemoteListFileFTP()
        {
            AMB_FTPCLIENT.FTPClient ff = new FTPClient();
            ff.setRemoteHost(sIPServerUVD);
            ff.setRemoteUser(sUserFTP);
            ff.setRemotePass(sPassFTP);
            ff.setRemotePort(int.Parse(sPortServerUVD));
            try
            {
                ff.login();
                ff.chdir(sPathOnServerToFullBase);
                ff.setBinaryMode(true);
                if (File.Exists("\\Temp\\List.xml")) File.Delete("\\Temp\\List.xml");
                ff.download(sFileNameList, "\\Temp\\List.xml");
            }
            catch (Exception ex)
            {
                WriteDebugString("LoadRemoteListFile.FTP:ERROR - " + ex.Message, 1);
            }
            finally
            {
                if (ff != null) ff.close();
            }

            Stream fs = null;
            stOneBaseFull[] stRemoteBase = null;
            try
            {
                fs = new FileStream("\\Temp\\List.xml", FileMode.Open);
                XmlSerializer sr = new XmlSerializer(typeof(stOneBaseFull[]));
                stRemoteBase = (stOneBaseFull[])sr.Deserialize(fs);
                fs.Close();
                WriteDebugString("LoadRemoteListFile:OK", 2);
            }
            catch (Exception ex)
            {
                if (fs != null) fs.Close();
                WriteDebugString("LoadRemoteListFile:ERROR - " + ex.Message, 1);
                dtGFBNextRecive = DateTime.Now.AddHours(24);    //повторим через сутки
            }
            try
            {
                if (File.Exists("\\Temp\\List.xml")) File.Delete("\\Temp\\List.xml");
            }
            catch (Exception ex)
            {
                WriteDebugString("LoadRemoteListFile.FileListDelete:ERROR - " + ex.Message, 1);
            }
            return stRemoteBase;
        }
        private void LoadFullBaseFTP(stOneBaseFull ob)
        {
            WriteDebugString("LoadFullBase.Entrance:OK", 2);
            AMB_FTPCLIENT.FTPClient ff = new FTPClient();
            ff.setRemoteHost(sIPServerUVD);
            ff.setRemoteUser(sUserFTP);
            ff.setRemotePass(sPassFTP);
            ff.setRemotePort(int.Parse(sPortServerUVD));
            DateTime dtCurFile = new DateTime(2001, 01, 01);
            bool bErrorBase = false;
            bool bErrorXml = false;
            dtCurFile = Utils.cCurrStatus.GetNewestBaseDate(ob.sTypeBase);
            try
            {
                if (Convert.ToDateTime(ob.sCreateDate) > dtCurFile)
                {
                    Utils.MarkedDirBusy(sPathToFullBase);
                    if (ob.sNameFileZipBase != null)    //если есть сжатый файл качаем его
                    {
                        if (File.Exists(sPathToFullBase + "\\" + ob.sNameFileZipBase + "_old")) File.Delete(sPathToFullBase + "\\" + ob.sNameFileZipBase + "_old");
                        if (File.Exists(sPathToFullBase + "\\" + ob.sNameFileZipBase)) File.Move(sPathToFullBase + "\\" + ob.sNameFileZipBase, sPathToFullBase + "\\" + ob.sNameFileZipBase + "_old");

                        try
                        {
                            ff.login();
                            ff.chdir(ob.sPathToDownloadZip);
                            ff.download(ob.sNameFileZipBase, sPathToFullBase + "\\" + ob.sNameFileZipBase);
                            ff.chdir("\\");
                            WriteDebugString("LoadFullBase:OK  - " + ob.sPathToDownloadZip + @"\" + ob.sNameFileZipBase, 2);
                            AddLastRecivedInfo(ob.sNameFileZipBase);

                            //если перекачали сжатую базу, то удаляем XML
                            if (File.Exists(sPathToFullBase + "\\" + ob.sNameFileBase + "_old")) File.Delete(sPathToFullBase + "\\" + ob.sNameFileBase + "_old");
                            if (File.Exists(sPathToFullBase + "\\" + ob.sNameFileBase)) File.Move(sPathToFullBase + "\\" + ob.sNameFileBase, sPathToFullBase + "\\" + ob.sNameFileBase + "_old");
                            if (File.Exists(sPathToFullBase + "\\" + ob.sNameFileXML + "_old")) File.Delete(sPathToFullBase + "\\" + ob.sNameFileXML + "_old");
                            if (File.Exists(sPathToFullBase + "\\" + ob.sNameFileXML)) File.Move(sPathToFullBase + "\\" + ob.sNameFileXML, sPathToFullBase + "\\" + ob.sNameFileXML + "_old");
                        }
                        catch (Exception ex)
                        {
                            WriteDebugString("LoadFullBase.FTP:ERROR - " + ex.Message, 1);
                            bErrorBase = true;
                        }
                        finally
                        {
                            if (ff != null) ff.close();
                        }
                    }
                    else
                    {                                   //если нет, качаем не сжатый
                        if (File.Exists(sPathToFullBase + "\\" + ob.sNameFileBase + "_old")) File.Delete(sPathToFullBase + "\\" + ob.sNameFileBase + "_old");
                        if (File.Exists(sPathToFullBase + "\\" + ob.sNameFileBase)) File.Move(sPathToFullBase + "\\" + ob.sNameFileBase, sPathToFullBase + "\\" + ob.sNameFileBase + "_old");

                        try
                        {
                            ff.login();
                            ff.chdir(ob.sPathToDownload);
                            ff.download(ob.sNameFileBase, sPathToFullBase + "\\" + ob.sNameFileBase);
                            ff.chdir("\\");
                            WriteDebugString("LoadFullBase:OK  - " + ob.sPathToDownload + @"/" + ob.sNameFileBase, 2);
                            AddLastRecivedInfo(ob.sNameFileBase);
                        }
                        catch (Exception ex)
                        {
                            WriteDebugString("LoadFullBase.FTP:ERROR - " + ex.Message, 1);
                            bErrorBase = true;
                        }
                        finally
                        {
                            if (ff != null) ff.close();
                        }

                        if (bErrorBase) throw new Exception("Error on download file");

                        if (File.Exists(sPathToFullBase + "\\" + ob.sNameFileXML + "_old")) File.Delete(sPathToFullBase + "\\" + ob.sNameFileXML + "_old");
                        if (File.Exists(sPathToFullBase + "\\" + ob.sNameFileXML)) File.Move(sPathToFullBase + "\\" + ob.sNameFileXML, sPathToFullBase + "\\" + ob.sNameFileXML + "_old");

                        try
                        {
                            ff.login();
                            ff.chdir(ob.sPathToDownload);
                            ff.setBinaryMode(true);
                            ff.download(ob.sNameFileXML, sPathToFullBase + "\\" + ob.sNameFileXML);
                            WriteDebugString("LoadFullBase:OK  - " + ob.sPathToDownload + @"/" + ob.sNameFileXML, 2);
                            AddLastRecivedInfo(ob.sNameFileXML);
                        }
                        catch (Exception ex)
                        {
                            WriteDebugString("LoadFullBase.FTP:ERROR - " + ex.Message, 1);
                            bErrorXml = true;
                        }
                        finally
                        {
                            if (ff != null) ff.close();
                        }
                    }

                    Utils.DeleteMarkerDirBusy(sPathToFullBase);

                    if (bErrorXml) throw new Exception("Error on download xml-file");
                }
                else
                {
                    WriteDebugString("LoadFullBase(" + ob.sPathToDownload + @" TYPE: " + ob.sComment + "):не загружена, т.к. локальный файл новее - " + dtCurFile.ToString(), 1);
                }
            }
            catch (Exception ex)
            {                
                WriteDebugString("LoadFullBase(" + ob.sPathToDownload + @" TYPE: " + ob.sComment + "):ERROR - " + ex.Message, 1);
                try
                {
                    if (ex.Message == "Error on download file")
                    {
                        if (File.Exists(sPathToFullBase + "\\" + ob.sNameFileBase)) File.Delete(sPathToFullBase + "\\" + ob.sNameFileBase);
                        if (File.Exists(sPathToFullBase + "\\" + ob.sNameFileBase + "_old")) File.Move(sPathToFullBase + "\\" + ob.sNameFileBase + "_old", sPathToFullBase + "\\" + ob.sNameFileBase);
                    }
                    if (ex.Message == "Error on download xml-file")
                    {
                        if (File.Exists(sPathToFullBase + "\\" + ob.sNameFileXML)) File.Delete(sPathToFullBase + "\\" + ob.sNameFileXML);
                        if (File.Exists(sPathToFullBase + "\\" + ob.sNameFileXML + "_old")) File.Move(sPathToFullBase + "\\" + ob.sNameFileXML + "_old", sPathToFullBase + "\\" + ob.sNameFileXML);
                    }
                    Utils.DeleteMarkerDirBusy(sPathToFullBase);
                }
                catch
                {
                    WriteDebugString("При загрузке базы произошло необратимое провреждение файла:" + ob.sComment, 0);
                }
                Thread.Sleep(1000);
            }

            try
            {
                Utils.cCurrStatus.UpdateBaseInfo();
            }
            catch { }

            WriteDebugString("LoadFullBase.Exit:OK", 2);
        }
        private void WriteNewNextSaveGFB()
        {
            try
            {
                //throw new Exception("The method or operation is not implemented.");
                dtGFBLastRecive = DateTime.Now;
                dtGFBNextRecive = Convert.ToDateTime((((DateTime)(dtGFBLastRecive + tsGFBReciveInterval)).ToString("dd.MM.yyyy") + " " + sGFBHoursRecive));

                lock (Utils.oSyncroLoadSaveInit)
                {
                    Utils.strConfig.strGetFiles.sNextRecive = dtGFBNextRecive.ToString();
                    Utils.strConfig.strGetFiles.sLastRecive = DateTime.Now.ToString();
                }
                Utils.SaveInit();
                WriteDebugString("WriteNewNextSaveGFB:OK", 2);
            }
            catch (Exception ex)
            {
                WriteDebugString("WriteNewNextSaveGFB:ERROR - " + ex.Message, 0);
            }
        }

        private void GetUpdatesFTP()
        {
            WriteDebugString("GetUpdates.Entrance:OK", 2);
            AMB_FTPCLIENT.FTPClient ff = new FTPClient();
            ff.setRemoteHost(sIPServerUVD);
            ff.setRemoteUser(sUserFTP);
            ff.setRemotePass(sPassFTP);
            ff.setRemotePort(int.Parse(sPortServerUVD));
            try
            {
                ff.login();
                ff.chdir(sPathOnServerToUpdates);
            }
            catch (Exception ex)
            {
                WriteDebugString("GetUpdates.FTP.Connect:ERROR - " + ex.Message, 1);
                return;
            }

            ArrayList arNamesUpdates = new ArrayList();

            DirectoryInfo di = new DirectoryInfo(sPathToFullBase);
            FileInfo[] fiAll = di.GetFiles("*.xml");
            foreach (FileInfo fi in fiAll)          //сначала по xml
            {
                stOneBase stOb = GetLocalBaseInfo(fi.FullName);
                string[] arsFtpFileName = null;
                try
                {
                    arsFtpFileName = ff.getFileList(stOb.sPrefUpdates + "*." + stOb.sExtUpdates);
                    foreach (string sFtpFL in arsFtpFileName)
                    {
                        string sFN = sFtpFL.Substring(sFtpFL.LastIndexOf(" ") + 1);
                        if (sFN.Length < 8) continue;
                        if (!File.Exists(sPathToUpdTerm + "\\" + sFN))  //если файл не существует
                        {
                            string tmpstr = sFN;
                            string tmpdFile = tmpstr.Substring(stOb.iNumCharPref, 2);
                            string tmpmFile = tmpstr.Substring(stOb.iNumCharPref + 2, 2);
                            string tmpyFile = tmpstr.Substring(stOb.iNumCharPref + 4, 4);                           
                            DateTime dtFileDate = new DateTime(Convert.ToInt32(tmpyFile), Convert.ToInt32(tmpmFile), Convert.ToInt32(tmpdFile));
                            if (dtFileDate >= Utils.cCurrStatus.GetNewestBaseDate(stOb.sTypeBase))
                            {
                                if (sFN.Length >= 1) arNamesUpdates.Add(sFN);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteDebugString("GetUpdates.FTP.GetFileList:ERROR - " + ex.Message, 1);
                }
            }

            FileInfo[] fiAllZip = di.GetFiles("*.zip");
            foreach (FileInfo fi in fiAllZip)          //потом по сжатым базам
            {
                stOneBase stOb = GetLocalBaseInfoZip(fi.FullName);
                string[] arsFtpFileName = null;
                try
                {
                    arsFtpFileName = ff.getFileList(stOb.sPrefUpdates + "*." + stOb.sExtUpdates);
                    foreach (string sFtpFL in arsFtpFileName)
                    {
                        string sFN = sFtpFL.Substring(sFtpFL.LastIndexOf(" ") + 1);
                        if (sFN.Length < 8) continue;
                        if (!File.Exists(sPathToUpdTerm + "\\" + sFN))  //если файл не существует
                        {
                            string tmpstr = sFN;
                            string tmpdFile = tmpstr.Substring(stOb.iNumCharPref, 2);
                            string tmpmFile = tmpstr.Substring(stOb.iNumCharPref + 2, 2);
                            string tmpyFile = tmpstr.Substring(stOb.iNumCharPref + 4, 4);
                            DateTime dtFileDate = new DateTime(Convert.ToInt32(tmpyFile), Convert.ToInt32(tmpmFile), Convert.ToInt32(tmpdFile));
                            if (dtFileDate >= Utils.cCurrStatus.GetNewestBaseDate(stOb.sTypeBase))
                            {
                                if (sFN.Length >= 1) arNamesUpdates.Add(sFN);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteDebugString("GetUpdates.FTP.GetFileListZip:ERROR - " + ex.Message, 1);
                }
            }

            //получили все имена файлов для скачивания
            ff.setBinaryMode(true);
            #region Folder busy
            int iCount = 0;
            while (Utils.IsFolderBusy(sPathToUpdTerm))
            {
                Thread.Sleep(1000);
                iCount++;
                if (iCount > 30)
                {
                    WriteDebugString("GetUpdates:ERROR - Directory is busy: " + sPathToUpdTerm, 1);
                    return;
                }
            }
            #endregion
            Utils.MarkedDirBusy(sPathToUpdTerm);
            foreach (string sN in arNamesUpdates)
            {
                iCount = 0;
                bool bDownOk = false;
                while (iCount < 3)      //делаем 3 попытки закачать файл
                {
                    try
                    {
                        ff.download(sN, sPathToUpdTerm + "\\" + sN);
                        iCount = 3;
                        bDownOk = true;
                        WriteDebugString("GetUpdates.FTP.DownLoad:OK  - File: " + sN, 2);
                        AddLastRecivedInfo(sN);
                    }
                    catch (Exception ex)
                    {
                        iCount++;
                        WriteDebugString("GetUpdates.FTP.Dwld:ERROR - " + ex.Message + " -"+ iCount.ToString() +"- At file: " + sN, 2);
                    }
                }
                if (!bDownOk)
                    WriteDebugString("GetUpdates.FTP.DownLoad:ERROR after 3 stage - At file: " + sN, 1);
            }

            ff.close();
            Utils.DeleteMarkerDirBusy(sPathToUpdTerm);
            WriteDebugString("GetUpdates.Exit:OK", 2);
        }
        private stOneBase GetLocalBaseInfo(string sFullNameXmlFile)
        {
            Stream fs = null;
            stOneBase stOneBase = new stOneBase();
            if (!File.Exists(sFullNameXmlFile))
            {
                WriteDebugString("GetLocalBaseInfo:ERROR - file not found:" + sFullNameXmlFile, 1);
                return stOneBase;
            }
            try
            {
                fs = new FileStream(sFullNameXmlFile, FileMode.Open);
                XmlSerializer sr = new XmlSerializer(typeof(stOneBase));
                stOneBase = (stOneBase)sr.Deserialize(fs);
                fs.Close();
                WriteDebugString("GetLocalBaseInfo:OK - " + stOneBase.sNameFileBase + " " + stOneBase.sDateLastUpdates, 2);
            }
            catch (Exception ex)
            {
                if (fs != null) fs.Close();
                WriteDebugString("GetCreationBaseTime.LoadXml:ERROR - " + ex.Message, 1);
            }
                return stOneBase;
        }
        private stOneBase GetLocalBaseInfoZip(string sFullNameZipFile)
        {
            stOneBase stOneBase = new stOneBase();
            if (!File.Exists(sFullNameZipFile))
            {
                WriteDebugString("GetLocalBaseInfoZip:ERROR - file not found:" + sFullNameZipFile, 1);
                return stOneBase;
            }
            try
            {
                string sTmp2 = null;
                using (ZipFile zip1 = new ZipFile(sFullNameZipFile, Encoding.UTF8))
                {
                    sTmp2 = zip1.Comment.Substring(zip1.Comment.IndexOf("XML:") + "XML:".Length);
                }
                XmlSerializer dsr = new XmlSerializer(typeof(stOneBase));
                TextReader tr = new StringReader(sTmp2);
                stOneBase = (stOneBase)dsr.Deserialize(tr);
                tr.Close();

                WriteDebugString("GetLocalBaseInfoZip:OK - " + stOneBase.sNameFileBase + " " + stOneBase.sDateLastUpdates, 2);
            }
            catch (Exception ex)
            {
                WriteDebugString("GetLocalBaseInfoZip.LoadXml:ERROR - " + ex.Message, 1);
            }
            return stOneBase;
        }
        private void AddLastRecivedInfo(string sFileName)
        {
            lock (Utils.oSyncroLoadSaveInit)
            {
                try
                {
                    Utils.strConfig.strMail.sLastRecivedTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                    Utils.cCurrStatus.sLastReciveTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                    Utils.cCurrStatus.sLastReciveName = sFileName;
                    Utils.SaveInit();
                    WriteDebugString("AddLastRecivedInfo:OK", 2);
                }
                catch (Exception ex)
                {
                    WriteDebugString("AddLastRecivedInfo:ERROR - " + ex.Message, 1);
                }
            }
        }

        private void GetNewSoftFTP()
        {
            int iRet = 0;
            WriteDebugString("GetNewSoft.Entrance:OK", 2);
            iRet = GetAllFromFTPFolder(Utils.strConfig.strGetFiles.sPathOnServerToNewSoftTerm, Utils.strConfig.sPathToNewSoftTerminal, "*.*");
            WriteDebugString("GetNewSoft.NewSoftTerm:find " + (iRet-1).ToString() + " files.", 2);
            iRet = GetAllFromFTPFolder(Utils.strConfig.strGetFiles.sPathOnServerToNewSoftSS, Utils.strConfig.sPathToNewSoftSS, "*.*");
            WriteDebugString("GetNewSoft.NewSoftSS:find " + (iRet-1).ToString() + " files.", 2);
            if (iRet > 0)
            {
               // Пока отключим Utils.bRebootRequest = true;
            }
            WriteDebugString("GetNewSoft.Exit:OK", 2);
        }
        private int GetAllFromFTPFolder(string sFolderOnFTP, string sFolderLocal, string sMaskFiles)
        {
            int iRet = 0;
            WriteDebugString("GetAllFromFTPFolder.Entrance:OK", 2);
            AMB_FTPCLIENT.FTPClient ff = new FTPClient();
            ff.setRemoteHost(sIPServerUVD);
            ff.setRemoteUser(sUserFTP);
            ff.setRemotePass(sPassFTP);
            ff.setRemotePort(int.Parse(sPortServerUVD));
            try
            {
                ff.login();
                ff.chdir("\\");
                ff.chdir(sFolderOnFTP);
            }
            catch (Exception ex)
            {
                WriteDebugString("GetAllFromFTPFolder.FTP.Connect:ERROR - " + ex.Message, 1);
                return -1;
            }

            string[] arsFtpFileName = null;
            try
            {
                arsFtpFileName = ff.getFileList(sMaskFiles);
                for (int iC = 0; iC < arsFtpFileName.Length; iC++)
                {
                    arsFtpFileName[iC] = arsFtpFileName[iC].Substring(arsFtpFileName[iC].LastIndexOf(" ") + 1);
                }
            }
            catch (Exception ex)
            {
                WriteDebugString("GetAllFromFTPFolder.FTP.GetFileList:ERROR - " + ex.Message, 1);
                iRet = 0;
            }

            //получили все имена файлов для скачивания
            int iCount = 0;
            while (Utils.IsFolderBusy(sFolderLocal))
            {
                Thread.Sleep(1000);
                iCount++;
                if (iCount > 30)
                {
                    WriteDebugString("GetAllFromFTPFolder:ERROR - Directory is busy: " + sPathToUpdTerm, 1);
                    return -2;
                }
            }
            if (arsFtpFileName == null)
            {
                ff.close();
                Utils.DeleteMarkerDirBusy(sFolderLocal);
                WriteDebugString("GetAllFromFTPFolder.Exit:OK - found 0 files.", 2);
                return 0;
            }
            Utils.MarkedDirBusy(sFolderLocal);
            NewSoftUpdates nsu = new NewSoftUpdates();
            foreach (string sN in arsFtpFileName)
            {
                try
                {
                    if (sN != "")
                    {
                        if (File.Exists(sFolderLocal + "\\" + sN)) File.Delete(sFolderLocal + "\\" + sN);
                        ff.download(sN, sFolderLocal + "\\" + sN);
                        WriteDebugString("GetAllFromFTPFolder.FTP.DownLoad:OK  - File: " + sN, 2);
                        AddLastRecivedInfo(sN);
                        nsu.AddNewUpdates(sFolderLocal + "\\" + sN);
                    }
                    iRet++;
                }
                catch (Exception ex)
                {
                    WriteDebugString("GetAllFromFTPFolder.FTP.DownLoad:ERROR - " + ex.Message + " --- At file: " + sN, 1);
                }
            }
            if (nsu != null) nsu.Close();
            ff.close();
            Utils.DeleteMarkerDirBusy(sFolderLocal);
            WriteDebugString("GetAllFromFTPFolder.Exit:OK", 2);
            return iRet;
        }
    }
}
