using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Xml.Serialization;
using System.Collections;

namespace SSLI
{
    public class Statistic : SSLI.ClassAMBRenewedService
    {
        private string sPd = Path.DirectorySeparatorChar.ToString();
        private string sPathToProtocol = null;
        private string sPathExecute = null;
        private string sPathToOutBox = null;
        private string sNamePodrazdelenie = null;
        private int iDebugLevel = 1;
        private DateTime dtNextSave = new DateTime();
        private DateTime dtPrevSave = new DateTime();
        private bool bAbort = false;
        private stOneTermStatistic[] arTermStat;
        private const string sProtocolFileName = "_protocol.log";
        private const string sRemoteNameFileT6Init = "T6Init.xml";

        private const string sFileNameCurInit = "SSCurrT6Init.xml";
        private const string sFileNameCurProtocol = "SSCurrT6_protocol.log";
        private const string sFileNameResult = "SSStat_X.xml";

        public override bool Init()
        {
            bool bRet = false;
            try
            {
                lock (Utils.oSyncroLoadSaveInit)
                {
                    sPathToProtocol = Utils.sFolderNameMain + sPd + Utils.strConfig.sPathToProtocol;
                    sPathExecute = Utils.strConfig.sPathToExecute;
                    sPathToOutBox = Utils.sFolderNameMain + sPd + Utils.strConfig.sPathToOutBox;
                    sNamePodrazdelenie = Utils.strConfig.sNamePodrazdelenie;
                    iDebugLevel = Utils.strConfig.iDebugLevel;
                    dtNextSave = Convert.ToDateTime(Utils.strConfig.strStatistic.sNextSave + " " + Utils.strConfig.strStatistic.sHoursSave);
                    dtPrevSave = Convert.ToDateTime(Utils.strConfig.strStatistic.sPrevionsSave + " " + Utils.strConfig.strStatistic.sHoursSave);
                }
                WriteDebugString("---------------------------", 1);

                if ((sPathToProtocol != null) && (sPathToOutBox != null) && (sPathExecute != null) &&
                    (sNamePodrazdelenie != null) && (Utils.strConfig.strStatistic.sNextSave != null) && (Utils.strConfig.strStatistic.sPrevionsSave != null))
                {
                    WriteDebugString("Init:OK", 2);
                    bRet = true;
                }
                else
                {
                    WriteDebugString("Init:ERROR - Missing parametrs.", 0);
                    bRet = false;
                }

            }
            catch (Exception ex)
            {
                WriteDebugString("Init:ERROR - " + ex.Message, 0);
                bRet = false;
            }
            return bRet;
        }
        public override void Start()
        {
            int i = 0;
            WriteDebugString("Start.Entrance:OK", 2);
            while (!bAbort)
            {
                Thread.Sleep(1000);
                if (DateTime.Now > dtNextSave)
                {
                    if (!Utils.IsFolderBusy(sPathToOutBox))
                    {
                        MakeStatFile();
                        WriteNewNextSave();
                    }
                    else
                    {
                        i++;
                        if (i >= 600)   //ждем 10 минут, потом снимаем маркер занятости папки. Ну что там можно столько времени делать?
                        {
                            i = 0;
                            Utils.DeleteMarkerDirBusy(sPathToOutBox);
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
            Utils.DeleteMarkerDirBusy(sPathToOutBox);
            WriteDebugString("Stop:OK", 1);
        }
        /// <summary>
        /// Запись в файл протокола ошибок
        /// </summary>
        /// <param name="strWr">Строка для записи</param>
        /// <param name="iLevelDebud">Уровень отладки: 0-важное, 1-некритичное, 2-нормальное выполнение</param>
        /// <returns></returns>
        private bool WriteDebugString(string strWr, int iLevelDebud)
        {
            bool bRet = true;
            if (iLevelDebud <= iDebugLevel)
            {
                try
                {
                    bRet = Utils.WriteDebugString(sPathExecute, " -StatisticService- " + strWr);
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

        private void WriteNewNextSave()
        {            
            try
            {
                lock (Utils.oSyncroLoadSaveInit)
                {
                    Utils.strConfig.strStatistic.sNextSave = DateTime.Now.AddDays(1).Date.ToString("dd.MM.yyyy");
                    Utils.strConfig.strStatistic.sPrevionsSave = DateTime.Now.Date.ToString("dd.MM.yyyy");
                    Utils.strConfig.strStatistic.sLastSended = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");

                    string sNextSave = Utils.strConfig.strStatistic.sNextSave;
                    string sHoursSave = Utils.strConfig.strStatistic.sHoursSave;

                    dtNextSave = Convert.ToDateTime(sNextSave + " " + sHoursSave);
                    dtPrevSave = DateTime.Now;
                }
                Utils.SaveInit();
                WriteDebugString("WriteNewNextSave:OK", 2);
            }
            catch (Exception ex)
            {
                WriteDebugString("WriteNewNextSave:ERROR - " + ex.Message, 0);
            }            
        }
        private void MakeStatFile()
        {
            try
            {
                WriteDebugString("MakeStatFile.Entrance:OK", 2);
                Utils.MarkedDirBusy(sPathToOutBox);

                string sTmpNamePodr = sNamePodrazdelenie.ToUpper();     //формируем удобочитаемое имя подразделения
                string[] sNotValid = { " ", @"\", "|", "_", "%", "`", "~", "=", "+", "*", ":", "!", "." };
                foreach (string s in sNotValid) sTmpNamePodr = sTmpNamePodr.Replace(s, "");
                if (iDebugLevel == 2)
                {
                    try
                    {
                        File.Copy(sPathExecute + sPd + Utils.sFileNameLog, sPathToOutBox + sPd + sTmpNamePodr + Utils.sFileNameLog);
                    }
                    catch { }
                }
                string sTmpFileRes = sFileNameResult.Replace("_X", "_" + sTmpNamePodr + "_" + DateTime.Now.ToString("ddMMyyyy")); //формируем имя файла

                DirectoryInfo diStat = new DirectoryInfo(sPathToOutBox);
                FileInfo[] fiStat = diStat.GetFiles("*" + sTmpNamePodr + "*");
                foreach (FileInfo fiTmp in fiStat)
                {
                    try
                    {
                        File.Delete(fiTmp.FullName);     //удаляем все старые файлы статистики
                    }
                    catch { }
                }

                DirectoryInfo diProtocol = new DirectoryInfo(sPathToProtocol);      //сканируем папки со статистикой с терминалов
                DirectoryInfo[] diCurrProtocol = diProtocol.GetDirectories();
                arTermStat = new stOneTermStatistic[diCurrProtocol.Length]; //создаем массив статистик по количеству терминалов
                int iCountTerm = 0;
                foreach (DirectoryInfo di in diCurrProtocol)    //побежали по всем папкам...
                {
                    try
                    {
                        if (File.Exists(sPathExecute + sPd + sFileNameCurProtocol)) File.Delete(sPathExecute + sPd + sFileNameCurProtocol);   //копируем файлы протокола и конфигурации из текущей папки в рабочую
                        if (File.Exists(sPathExecute + sPd + sFileNameCurInit)) File.Delete(sPathExecute + sPd + sFileNameCurInit);
                        FileInfo[] fiProt = di.GetFiles("*" + sProtocolFileName);
                        FileInfo[] fiInit = di.GetFiles(sRemoteNameFileT6Init);
                        if (fiProt.Length > 0) File.Copy(fiProt[0].FullName, sPathExecute + sPd + sFileNameCurProtocol);
                        if (fiInit.Length > 0)
                        {
                            File.Copy(fiInit[0].FullName, sPathExecute + sPd + sFileNameCurInit);
                            DateTime dtChangeIni = fiInit[0].LastWriteTime;
                            stOneTermStatistic stOTSCurr = WorkWithFiles(sPathExecute + sPd + sFileNameCurInit, sPathExecute + sPd + sFileNameCurProtocol, dtChangeIni.ToString("dd.MM.yyyy HH:mm:ss"));
                            if (stOTSCurr.sDeviceID != null)
                            {
                                arTermStat[iCountTerm] = stOTSCurr;
                                iCountTerm++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteDebugString("MakeStatFile.WorkWithFiles:ERROR - " + ex.Message, 1);
                    }
                }
                SaveStatFile(sPathToOutBox + sPd + sTmpFileRes);
                if (File.Exists(sPathExecute + sPd + sFileNameCurProtocol)) File.Delete(sPathExecute + sPd + sFileNameCurProtocol);   //удаляем рабочие файлы
                if (File.Exists(sPathExecute + sPd + sFileNameCurInit)) File.Delete(sPathExecute + sPd + sFileNameCurInit);
                Utils.DeleteMarkerDirBusy(sPathToOutBox);
                WriteDebugString("MakeStatFile.Exit:OK", 2);
            }
            catch (Exception ex)
            {
                WriteDebugString("MakeStatFile:ERROR - " + ex.Message, 1);
            }
        }
        /// <summary>
        /// Создание структуры статистики проверок по одному терминалу
        /// </summary>
        /// <param name="sIniFileName">Полное имя файла конфигурации терминала</param>
        /// <param name="sProtFileName">Полное имя файла протокола проверок терминала</param>
        /// <param name="sChangeIniFile">Дата последнего изменения файла конфигурации</param>
        /// <returns>Структура проверок одного терминала</returns>
        private stOneTermStatistic WorkWithFiles(string sIniFileName, string sProtFileName, string sChangeIniFile)
        {
            WriteDebugString("WorkWithFiles.Entrance:OK", 2);
            stOneTermStatistic ots = new stOneTermStatistic();
            stIni ini;
            ArrayList arSearchTmp = new ArrayList();
            DateTime dtLastDate;

            ini = LoadT6IniFile(sIniFileName);
            dtLastDate = DateTime.Now.AddDays(-30); //за последние 30 дней
            ots.sPodrazdelenieID = Utils.GetDeviceID();
            ots.sNamePodrazdelenie = sNamePodrazdelenie;
            ots.sDeviceID = ini.sDeviceID;
            ots.sNameTerminal = ini.sNameTerminal;
            ots.stVersions = ini.stVersions;
            ots.sDateTimeLastSyncronisation = sChangeIniFile;
            ots.arBaseStat = new stOneBaseStat[ini.iCountBases];
            ots.iCountBases = ini.iCountBases;
            for (int i = 0; i < ini.iCountBases; i++)
            {
                ots.arBaseStat[i].bCheckBase = ini.arBases[i].bCheckBase;
                ots.arBaseStat[i].bIsBase = ini.arBases[i].bIsBase;
                ots.arBaseStat[i].sComment = ini.arBases[i].sComment;
                if (ini.arBases[i].sDateLastUpdates != null)
                {
                    if (ini.arBases[i].sDateLastUpdates.Length >= 7) ots.arBaseStat[i].sDateLastUpdates = ini.arBases[i].sDateLastUpdates;
                    else ots.arBaseStat[i].sDateLastUpdates = "01.01.2001";
                }
                else ots.arBaseStat[i].sDateLastUpdates = "01.01.2001";
                ots.arBaseStat[i].sTypeBase = ini.arBases[i].sTypeBase;
                ots.arBaseStat[i].sVerFileDll = ini.arBases[i].sVerFileDll;
            }
            if (ots.sNameTerminal != null)
            {
                WriteDebugString("WorkWithFiles.ReadInitXML:OK - " + ots.sNameTerminal, 2);
            }

            StreamReader sr = null;
            string readStr = null;
            string sTmpTermName = null;
            string sTmpTermID = null;
            string sDateTimeTmp = null;

            try
            {
                if (File.Exists(sProtFileName))
                {
                    sr = File.OpenText(sProtFileName);
                    while ((readStr = sr.ReadLine()) != null)
                    {
                        if (readStr.IndexOf("TERMNAME=") >= 0) sTmpTermName = readStr.Substring(readStr.IndexOf("=") + 1);
                        if (readStr.IndexOf("TERMID=") >= 0) sTmpTermID = readStr.Substring(readStr.IndexOf("=") + 1);
                        //имя_базы | дата_время | задержан | параметры_поиска
                        int[] iRasd = new int[5];
                        if (readStr.IndexOf(";") != 0)
                        {
                            for (int iN = 1; iN < 5; iN++)
                            {
                                iRasd[iN] = readStr.IndexOf("|", iRasd[iN - 1] + 1);
                                if (iRasd[iN] < 0) break;
                            }
                        }
                        if (iRasd[3] > 0)
                        {
                            stOneSearch stOS = new stOneSearch();
                            stOS.sComment = readStr.Substring(iRasd[0], iRasd[1] - iRasd[0]);
                            sDateTimeTmp = readStr.Substring(iRasd[1] + 1, iRasd[3] - iRasd[1] - 1);
                            stOS.sDateTime = sDateTimeTmp.Replace("|", " ");
                            stOS.bResult = Convert.ToBoolean(readStr.Substring(iRasd[3] + 1, iRasd[4] - iRasd[3] - 1));
                            stOS.sParamSearch = readStr.Substring(iRasd[4] + 1);

                            DateTime dtCurStr = Convert.ToDateTime(stOS.sDateTime);
                            if (dtLastDate < dtCurStr) arSearchTmp.Add(stOS);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteDebugString("WorkWithFiles.ReadProtocol:ERROR - " + ex.Message, 1);
            }
            finally
            {
                if (sr != null) sr.Close();
            }

            ots.arSearch = new stOneSearch[arSearchTmp.Count];
            ots.iCountSearch = arSearchTmp.Count;
            int iC = 0;
            foreach (stOneSearch stOs in arSearchTmp)
            {
                ots.arSearch[iC] = stOs;
                iC++;
            }
            WriteDebugString("WorkWithFiles.Exit:OK", 2);
            return ots;
        }
        private stIni LoadT6IniFile(string sFullIniFileName)
        {
            FileStream fs2 = null;
            stIni stCurTerm = new stIni();
            try
            {
                fs2 = new FileStream(sFullIniFileName, FileMode.Open);
                XmlSerializer sr2 = new XmlSerializer(typeof(stIni));
                stCurTerm = (stIni)sr2.Deserialize(fs2);
                fs2.Close();
            }
            catch (Exception ex)
            {
                if (fs2 != null) fs2.Close();
                WriteDebugString("LoadT6IniFile:ERROR - " + ex.Message, 1);
            }
            return stCurTerm;
        }
        private bool SaveStatFile(string sFileName)
        {
            FileStream fs = null;
            try
            {
                if (sPathToOutBox != null)
                {
                    fs = new FileStream(sFileName, FileMode.Create);
                    XmlSerializer sr = new XmlSerializer(typeof(stOneTermStatistic[]));
                    sr.Serialize(fs, arTermStat);
                    fs.Close();
                    WriteDebugString("SaveStatFile:OK", 2);
                    return true;
                }
                else
                {
                    WriteDebugString("SaveStatFile:ERROR - sPathToOutBox==null", 0);
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (fs != null) fs.Close();
                if (File.Exists(sFileName)) File.Delete(sFileName);
                WriteDebugString("SaveStatFile:ERROR - " + ex.Message, 0);
                return false;
            }
        }		
    }
}
