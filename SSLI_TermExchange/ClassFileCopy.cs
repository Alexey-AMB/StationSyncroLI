using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Collections;
using System.Xml.Serialization;
using SSLI;

namespace T6.FileCopy
{
	public class ClassFileCopy
	{
        private string sPd = Path.DirectorySeparatorChar.ToString();
        private const string sRemoteNameFileT6Init = "T6Init.xml";
		private const string sErrorFileName = "T6Error.log";
		public const string sFileNameCurInit = "CurrT6Init.xml";
		public const string sFileNameCurUpdates = "CurrT6_updates.log";
		public const string sFileNameCurProtocol = "CurrT6_protocol.log";
		public const string sFileNameCurCommand = "CurrT6_command.log";
        private const string sProtocolFileName = "_protocol.log";
        private const string sUpdatesFileName = "_updates.log";
        private const string sComandFileName = "_command.log";
		private const string sNameRunUpd = "runUpdates";
		private const string sFileNameFlagBusy = "_busy_Flag";

        private const string sRebootCommand = "#REBOOT";
        private const string sSetTimeCommand = "#SETTIME ";

        private int iNumInDock = 0; //номер терминала в подставке от 0 до 4

		private SocketTransfer.SocketTransfer st = null;
		public TerminalStatus term = null;
		private string sStatPath = "";			//куда класть статистику
		private string sUpdPath = "";			//откуда копировать обновления
		public bool bFinal = false;
		private bool bSendFullBase = true;
		private string sPathToT6Init = null;
		public string sTextLabelWorkStatus;
		public string sTextLabelStatus;
		private ArrayList arFilesToSend = new ArrayList();
		private string sFolderLocalBase;
		private string sFolderNewSoft;
        private static string sPathToExecute = null;
		private static int iDebugLevel = 2;
        public bool bAbortThread
        {
            get
            {
                if (st != null) return st.bAbort_thread;
                return false;
            }
            set
            {
                if (st != null) st.bAbort_thread = value;
                if (value)
                {
                    Thread.Sleep(1000);
                    if (st != null)
                    {
                        if (st.client != null) st.client.Destroy();
                    }
                }
            }
        }
        private bool bRebootIsRequired = false; //были скопированы обновления софта, нужна перезагрузка терминала
        private int iError = 0;         //0- нет ошибок, 1- ошибка in, 2- ошибка analise, 3- ошибка out

        /// <summary>
        /// Инициализация класса. Ничего не делает, только читает переменные из Utils.strConfig
        /// </summary>
        /// <param name="iNum">Номер в доке, куда установлен терминал.</param>
		public void Init(int iNum)
		{

			this.sStatPath = Utils.strConfig.sPathToProtocol;
            this.sUpdPath = Utils.strConfig.sPathToUpdTerm;
            this.sFolderLocalBase = Utils.strConfig.sPathToFullBase;
            this.sFolderNewSoft = Utils.strConfig.sPathToNewSoftTerminal;
			this.bSendFullBase = true;
            sPathToExecute = Utils.strConfig.sPathToExecute;
            iDebugLevel = Utils.strConfig.iDebugLevel;
            iNumInDock = iNum;
		}
        /// <summary>
        /// Основная функция работы с терминалом. 
        /// Проверяет соединение с терминалом и ищет файл конфигурации терминала. 
        /// Сначала в папке Program Files\Terminal потом в Storage Card\Terminal. 
        /// Если нет ни там ни там ищет резервный .bak в корне Storage Card.
        /// </summary>
        /// <returns>TRUE - если терминал обнаружен.</returns>
		public void LoadDeviceInfoTh()
        {
            LoadDeviceInfo();
            Utils.cCurrStatus.UpdateTermInfo();
        }
        public bool LoadDeviceInfo()	//что подключили?
		{
			bool bIsBakInitFile = false;
			if (st == null) st = new SocketTransfer.SocketTransfer();
            bAbortThread = false;

            bool bConnected = st.InitClient(Utils.sIPLocalRNDIS, 30000);
            st.client.OnProgress += new SocketTransfer.SocketTransfer.STC_SendFileTo_Progress(client_OnProgress);
			if (!bConnected)	
			{
				WriteProtocol("st.InitClient:ERROR - Ошибка подключения.", 3);
                Utils.cCurrStatus.arstTermInDock[iNumInDock].iServeStatus = -1;
                Utils.cCurrStatus.arstTermInDock[iNumInDock].sCurrStatus = "ОШИБКА СОЕДИНЕНИЯ 0";
                Utils.cCurrStatus.arstTermInDock[iNumInDock].iColorLabelStatus = -1;
                Utils.cCurrStatus.arstTermInDock[iNumInDock].sIDTerminal = "НЕИЗВЕСТНО";
                Utils.cCurrStatus.arstTermInDock[iNumInDock].sNameTerminal = "НЕИЗВЕСТНО";
				return false;
			}
			else
			{
                sTextLabelStatus = "Устанавливаю соединение...";
                if (!st.client.SendMessage("Test connection"))  //если терминал не подключен
                {
                    sTextLabelStatus = "Терминал не найден";
                    WriteProtocol("st.Test_connection:ERROR - Ошибка подключения.", 3);
                    Utils.cCurrStatus.arstTermInDock[iNumInDock].iServeStatus = -1;
                    Utils.cCurrStatus.arstTermInDock[iNumInDock].sCurrStatus = "ОШИБКА СОЕДИНЕНИЯ 1";
                    Utils.cCurrStatus.arstTermInDock[iNumInDock].iColorLabelStatus = -1;
                    Utils.cCurrStatus.arstTermInDock[iNumInDock].sIDTerminal = "НЕИЗВЕСТНО";
                    Utils.cCurrStatus.arstTermInDock[iNumInDock].sNameTerminal = "НЕИЗВЕСТНО";
                    return false;
                }
				sTextLabelStatus = "Принимаю данные о терминале...";

				if (term == null) term = new TerminalStatus();
                //Сначала ищем в Program Files\Terminal
				bool bRet = st.client.FileExits("\\Program Files\\Terminal\\" + sRemoteNameFileT6Init);
                if (bRet) sPathToT6Init = "\\Program Files\\Terminal\\" + sRemoteNameFileT6Init;
                else
                {
                    //Потом в Storage Card\Files
                    bRet = st.client.FileExits("\\Storage Card\\Files\\" + sRemoteNameFileT6Init);
                    if (bRet) sPathToT6Init = "\\Storage Card\\Files\\" + sRemoteNameFileT6Init;
                    else //если нет ни там ни там ищем резервный файл.
                    {
                        bRet = st.client.FileExits("\\Storage Card\\" + sRemoteNameFileT6Init + ".bak");
                        if (bRet) sPathToT6Init = "\\Storage Card\\" + sRemoteNameFileT6Init + ".bak";
                        bIsBakInitFile = true;
                    }
                }
				if (!bRet)
				{
					sTextLabelStatus = "Файл конфигурации не найден";
					sTextLabelWorkStatus = "Ошибка. Синхронизация не выполнена";
					st.client.Destroy();

					WriteProtocol("Файл конфигурации не найден", 1);
					st = null;
					term = null;
                    Utils.cCurrStatus.arstTermInDock[iNumInDock].iServeStatus = -1;
                    Utils.cCurrStatus.arstTermInDock[iNumInDock].sCurrStatus = "ОШИБКА ПОДКЛЮЧЕНИЯ 3";
                    Utils.cCurrStatus.arstTermInDock[iNumInDock].iColorLabelStatus = -1;
                    Utils.cCurrStatus.arstTermInDock[iNumInDock].sIDTerminal = "НЕИЗВЕСТНО";
                    Utils.cCurrStatus.arstTermInDock[iNumInDock].sNameTerminal = "НЕИЗВЕСТНО";
					return false;
				}

                bRet = st.client.ReciveFileFrom(sPathToT6Init, sPathToExecute + sPd + sFileNameCurInit);
                term.LoadIniFile(sPathToExecute + sPd + sFileNameCurInit);

				if (bIsBakInitFile)		//если это бак-файл, то надо прочитать настоящий ини.
				{
					if (st.client.FileExits(term.stCurTerm.stPaths.sPathStorageFiles + sPd + sRemoteNameFileT6Init))
					{
						string sTmp = term.stCurTerm.stPaths.sPathStorageFiles + sPd + sRemoteNameFileT6Init;
                        bRet = st.client.ReciveFileFrom(sTmp, sPathToExecute + sPd + sFileNameCurInit);
                        term.LoadIniFile(sPathToExecute + sPd + sFileNameCurInit);
					}	//если настоящего ини нет, то работаем по баку
				}

				bFinal = false;
				RunTransfer(term.sTermName);
                if (iError == 0) return true;
                else return false;
			}
		}

        void client_OnProgress(int procent)
        {
            if (procent == 0) Utils.cCurrStatus.arstTermInDock[iNumInDock].sCurrStatus = "ПЕРЕДАЮ ОБНОВЛЕНИЯ";
            else Utils.cCurrStatus.arstTermInDock[iNumInDock].sCurrStatus = "ПЕРЕДАЮ ФАЙЛ " + procent.ToString() + "%";
        }

		private void RunTransfer(string sNameTerm)	//копируем все нужное
		{
            Utils.cCurrStatus.arstTermInDock[iNumInDock].sIDTerminal = term.stCurTerm.sDeviceID;
            Utils.cCurrStatus.arstTermInDock[iNumInDock].sNameTerminal = term.stCurTerm.sNameTerminal;

            if (bAbortThread)
            {
                WriteProtocol("==== RNDIS ==== Aborted1 " + sNameTerm, 1);
                return;
            }
			WriteProtocol("==== RNDIS ==== Работаю с терминалом: " + sNameTerm, 2);
            Utils.cCurrStatus.arstTermInDock[iNumInDock].iServeStatus = 1;
            Utils.cCurrStatus.arstTermInDock[iNumInDock].sCurrStatus = "ПРИНИМАЮ ФАЙЛЫ";
            Utils.cCurrStatus.arstTermInDock[iNumInDock].iColorLabelStatus = 1;
            try
            {
                TransferFrom();
            }
            catch (Exception ex)
            {
                WriteProtocol("==== RNDIS ==== TransferFrom:ERROR - " + ex.Message, 1);
            }
            if (bAbortThread)
            {
                WriteProtocol("==== RNDIS ==== Aborted2 " + sNameTerm, 1);
                return;
            }
            Utils.cCurrStatus.arstTermInDock[iNumInDock].sCurrStatus = "АНАЛИЗИРУЮ ФАЙЛЫ";
            try
            {
                AnalisRecivedFiles();
            }
            catch (Exception ex)
            {
                WriteProtocol("==== RNDIS ==== AnalisRecivedFiles:ERROR - " + ex.Message, 1);
            }
            if (bAbortThread)
            {
                WriteProtocol("==== RNDIS ==== Aborted3 " + sNameTerm, 1);
                return;
            }
            Utils.cCurrStatus.arstTermInDock[iNumInDock].sCurrStatus = "ПЕРЕДАЮ ОБНОВЛЕНИЯ";
            try
            {
                iError = TransferTo();
            }
            catch (Exception ex)
            {
                WriteProtocol("==== RNDIS ==== TransferTo:ERROR - " + ex.Message, 1);
            }

            //если нужно передаем команды
            st.client.SendMessage(sSetTimeCommand + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
            if (bRebootIsRequired)
            {
                Thread.Sleep(1000);
                st.client.SendMessage(sRebootCommand);
                WriteProtocol("==== RNDIS ==== " + sNameTerm + " - Отправлена команда перезагрузки.", 2);
            }            
            st.client.CloseServer();
			st.client.Destroy();

			WriteProtocol("==== RNDIS ==== " + sNameTerm + " - Обмен завершен.", 2);
			try
			{
				if (File.Exists(sPathToExecute + sPd + sFileNameCurInit)) File.Delete(sPathToExecute + sPd + sFileNameCurInit);
				if (File.Exists(sPathToExecute + sPd + sFileNameCurUpdates)) File.Delete(sPathToExecute + sPd + sFileNameCurUpdates);
				if (File.Exists(sPathToExecute + sPd + sFileNameCurProtocol)) File.Delete(sPathToExecute + sPd + sFileNameCurProtocol);
				if (File.Exists(sPathToExecute + sPd + sFileNameCurCommand)) File.Delete(sPathToExecute + sPd + sFileNameCurCommand);
			}
			catch (Exception ex)
			{
				WriteProtocol(" Final:ERROR " + ex.Message, 1);
			}
			st = null;
			term = null;
			bFinal = true;
            if (iError == 0)
            {
                Utils.cCurrStatus.arstTermInDock[iNumInDock].iServeStatus = 2;
                Utils.cCurrStatus.arstTermInDock[iNumInDock].sCurrStatus = "ОБРАБОТАН, ЗАРЯЖАЕТСЯ";
                Utils.cCurrStatus.arstTermInDock[iNumInDock].iColorLabelStatus = 2;
            }
            if (iError != 0)
            {
                Utils.cCurrStatus.arstTermInDock[iNumInDock].iServeStatus = 2;
                Utils.cCurrStatus.arstTermInDock[iNumInDock].sCurrStatus = "ОБРАБОТАН НО С ОШИБКОЙ";
                Utils.cCurrStatus.arstTermInDock[iNumInDock].iColorLabelStatus = 2;
            }
		}

        private int TransferTo()	//копируем все нужное на КПК
		{
			bool bRet;
            int iRet = 0;

			sTextLabelWorkStatus = "Передаю файлы на терминал";

			string sCommandFileName = term.stCurTerm.stPaths.sPathStorageFiles + sPd + term.stCurTerm.sNameTerminal + sComandFileName;
			try
			{
				if (st.client.FileExits(sCommandFileName)) st.client.DeleteFile(sCommandFileName);
			}
			catch (Exception ex)
			{
				WriteProtocol(" TransferTo.DeleteCommandFile:ERROR - " + ex.Message, 1);
			}

			foreach (stFileUpd sFileNames in arFilesToSend)
			{
				int iTmp1 = 0;
				while (Utils.IsFolderBusy(Path.GetDirectoryName(sFileNames.sFileSource)))
				{
					WriteProtocol(" TransferTo.Directory is busy (" + Path.GetDirectoryName(sFileNames.sFileSource) + ")", 1);
					Thread.Sleep(1000);
					iTmp1++;			//ждем 5 min потом снимаем маркер
					if (iTmp1 > 300) Utils.DeleteMarkerDirBusy(Path.GetDirectoryName(sFileNames.sFileSource));
				}
				if (File.Exists(sFileNames.sFileSource))
				{
					try
					{
                        if (bAbortThread)
                        {
                            WriteProtocol("==== RNDIS.TransferTo ==== Aborted4 (не дождавшись окончания передачи вынули из станции) - " + term.stCurTerm.sNameTerminal, 1);
                            return 3;
                        }
						bRet = st.client.SendFileTo(sFileNames.sFileSource, sFileNames.sFileDest);
                        if (!bRet) iRet = 3;
						WriteProtocol("Передан файл(" + bRet.ToString() + "): из " + sFileNames.sFileSource + " в " + sFileNames.sFileDest, 2);
						if ((bRet) && (sFileNames.bChangeConfigFiles))
						{
							term.ChangeLastUpdatesByTypeBase(sFileNames.sTypeBase, "FULL" + sFileNames.sDateAktual.Replace(".", "") + ".dat", sFileNames.sDateAktual);
							term.SaveIniFile(sPathToExecute + sPd + sFileNameCurInit);
						}
					}
					catch (Exception ex)
					{
						WriteProtocol(" TransferTo.CopyFileToPocketPC(" + sFileNames.sFileSource + "):ERROR - " + ex.Message, 1);
                        iRet = 3;
					}
                    Thread.Sleep(500);
				}
			}


            //bRet = st.client.ReciveFileFrom(sPathToT6Init, sPathToExecute + "\\" + sFileNameCurInit);
            //term.LoadIniFile(sPathToExecute + "\\" + sFileNameCurInit);  TEST ONLY

			//без этого файла обновление на терминала не начнется
			try
			{
				if (!File.Exists(sPathToExecute + sPd + sNameRunUpd))
				{
					FileStream fs = File.Create(sPathToExecute + sPd + sNameRunUpd);
					if (fs != null) fs.Close();
				}
				bRet = st.client.SendFileTo(sPathToExecute + sPd + sNameRunUpd, term.stCurTerm.stPaths.sPathUpdates + sPd + sNameRunUpd);
                if (!bRet) iRet = 3;
				WriteProtocol("Передан файл(" + bRet.ToString() + "): завершающий файл пакета обновлений.", 2);
			}
			catch (Exception ex)
			{
				WriteProtocol(" TransferTo.CopyFileToPocketPC(" + sNameRunUpd + "):ERROR - " + ex.Message, 1);
                iRet = 3;
			}
            return iRet;
		}

        private int TransferFrom()	//копируем все нужное с КПК на PC
		{
            int iRet = 0;
			if (term.stCurTerm.sNameTerminal == null) return 1;

			sTextLabelWorkStatus = "Копирую файлы с терминала";

			string[] protName = 
			    {
					sRemoteNameFileT6Init,
					sErrorFileName,
					term.stCurTerm.sNameTerminal + sProtocolFileName,
					term.stCurTerm.sNameTerminal + sUpdatesFileName,
					term.stCurTerm.sNameTerminal + sComandFileName,
				};


			try
			{
				if (!Directory.Exists(sStatPath + sPd + term.stCurTerm.sDeviceID)) Directory.CreateDirectory(sStatPath + sPd + term.stCurTerm.sDeviceID);
			}
			catch { }

			//очистить текущую папку
            DirectoryInfo di = new DirectoryInfo(sStatPath + sPd + term.stCurTerm.sDeviceID);
			if (di.Exists)
			{
				//if (Convert.ToDateTime(term.stCurTerm.sLastSyncronisation) < DateTime.Today)
				{
					FileInfo[] fiAll = di.GetFiles("*.*");
					foreach (FileInfo fi in fiAll)
					{
						try
						{
							File.Delete(fi.FullName);
						}
						catch { }
					}
				}
			}

			foreach (string pn in protName)
			{
                try
                {
                    //if (Convert.ToDateTime(term.stCurTerm.sLastSyncronisation) < DateTime.Today)
                    {
                        if (bAbortThread)
                        {
                            WriteProtocol("==== RNDIS.TransferFrom ==== Aborted5 " + term.stCurTerm.sNameTerminal, 1);
                            return 0;
                        }
                        bool bR = st.client.ReciveFileFrom(term.stCurTerm.stPaths.sPathStorageFiles + sPd + pn, sStatPath + sPd + term.stCurTerm.sDeviceID + sPd + pn);
                        if (bR) WriteProtocol("Принят файл: " + sStatPath + sPd + term.stCurTerm.sDeviceID + sPd + pn, 2);
                        else
                        {
                            WriteProtocol("Ошибка приема файла: " + sStatPath + sPd + term.stCurTerm.sDeviceID + sPd + pn, 2);
                            iRet = 1;
                        }
                    }
                    Thread.Sleep(500);
                }
                catch (Exception exception)
                {
                    WriteProtocol("Ошибка приема файла: " + sStatPath + sPd + term.stCurTerm.sDeviceID + sPd + pn + " -ERROR- " + exception.Message, 2);
                    iRet = 1;
                }				
			}
            return iRet;
		}

        private void AnalisRecivedFiles()
		{
			sTextLabelWorkStatus = "Анализирую файлы с терминала";
			arFilesToSend.Clear();
            bool bBaseAdded = false;
            foreach (stOneBase ob in term.stCurTerm.arBases)
            {
                try
                {
                    bBaseAdded = false;
                    if (ob.sTypeBase != null)
                    {
                        if ((FullBaseIsBetter(ob.sDateLastUpdates, ob.sTypeBase)) && (ob.sTypeUpdate != "FULL") && (File.Exists(sFolderLocalBase + sPd + Utils.cCurrStatus.GetNewestBaseFileName(ob.sTypeBase))) && bSendFullBase)
                        {	//отправляем базу целиком
                            stFileUpd fu = new stFileUpd();
                            fu.sFileSource = sFolderLocalBase + sPd + Utils.cCurrStatus.GetNewestBaseFileName(ob.sTypeBase);
                            fu.sFileDest = term.stCurTerm.stPaths.sPathToBase + sPd + Utils.cCurrStatus.GetNewestBaseFileName(ob.sTypeBase);
                            fu.bChangeConfigFiles = true;
                            fu.sTypeBase = ob.sTypeBase;
                            fu.sDateAktual = Utils.cCurrStatus.GetNewestBaseDate(ob.sTypeBase).ToString("dd.MM.yyyy");
                            arFilesToSend.Add(fu);
                            bBaseAdded = true;
                        }

                        //отправляем обновления
                        DirectoryInfo dirInf = new DirectoryInfo(sUpdPath);
                        FileInfo[] updFiles = dirInf.GetFiles(ob.sPrefUpdates + "*." + ob.sExtUpdates);
                        foreach (FileInfo sUpdFileName in updFiles)
                        {
                            stFileUpd fu = new stFileUpd();
                            fu.sFileSource = sUpdFileName.FullName;
                            fu.sFileDest = term.stCurTerm.stPaths.sPathUpdates + sPd + sUpdFileName.Name;
                            fu.bChangeConfigFiles = false;
                            if (bBaseAdded)
                            {   //если базу передаем, то те обновления, которые новее переданной базы
                                if (IsUpdateNew(Utils.cCurrStatus.GetNewestBaseDate(ob.sTypeBase).ToString("dd.MM.yyyy"), sUpdFileName.Name)) arFilesToSend.Add(fu);
                            }
                            else
                            {   //только те которые после последнего обновления на терминале                                    
                                if (IsUpdateNew(ob.sDateLastUpdates, sUpdFileName.Name)) arFilesToSend.Add(fu);
                            }
                        }

                        //потом базы целиком
                        if (ob.sTypeUpdate == "FULL")
                        {
                            if (File.Exists(sFolderLocalBase + sPd + Utils.cCurrStatus.GetNewestBaseFileName(ob.sTypeBase)))
                            {
                                stFileUpd fu = new stFileUpd();
                                fu.sFileSource = sFolderLocalBase + sPd + Utils.cCurrStatus.GetNewestBaseFileName(ob.sTypeBase);
                                fu.sFileDest = term.stCurTerm.stPaths.sPathToBase + sPd + Utils.cCurrStatus.GetNewestBaseFileName(ob.sTypeBase);
                                fu.bChangeConfigFiles = true;
                                fu.sTypeBase = ob.sTypeBase;
                                fu.sDateAktual = Utils.cCurrStatus.GetNewestBaseDate(ob.sTypeBase).ToString("dd.MM.yyyy");
                                //проверить новая ли это база или таже что и на терминале
                                if (IsUpdateNew(ob.sDateLastUpdates, Utils.cCurrStatus.GetNewestBaseDate(ob.sTypeBase)))
                                {//изменить файл конфига терминала, добавить запись в файл списка обновлений
                                    arFilesToSend.Add(fu);
                                    bBaseAdded = true;
                                }

                            }
                        }
                    }
                }
                catch { }
            }

			//копируем базы целиком по запросу терминала
			StreamReader sr = null;
            if ((File.Exists(sStatPath + sPd + term.stCurTerm.sDeviceID + sPd + term.sTermName + sComandFileName)) && bSendFullBase)
			{//"GET_FULL_BASE " с пробелом перед именем файла базы
				string readStr;
				ArrayList arGetFile = new ArrayList();
				try
				{
                    sr = File.OpenText(sStatPath + sPd + term.stCurTerm.sDeviceID + sPd + term.sTermName + sComandFileName);
					while ((readStr = sr.ReadLine()) != null)
					{
						if (readStr.IndexOf("GET_FULL_BASE ") == 0)
						{
                            string sType = term.GetTypeBaseByNameFileBase(readStr.Substring(readStr.IndexOf(" ") + 1));
                            WriteProtocol(" TransferTo.AnalisRecivedFiles:OK - запрос новой базы типа: " + sType + " .", 2);
                            arGetFile.Add(sType);
						}
					}
					for (int i = 0; i<arGetFile.Count;i++)
					{
						string sTmp = (string)arGetFile[i];
						for (int n = i +1; n < arGetFile.Count; n++) if ((string)arGetFile[n] == sTmp) arGetFile[n] = "";
					}
					foreach (string s in arGetFile)
					{
						if (s == "") continue;
						stFileUpd fu = new stFileUpd();
                        fu.sFileSource = sFolderLocalBase + sPd + Utils.cCurrStatus.GetNewestBaseFileName(s);
                        fu.sFileDest = term.stCurTerm.stPaths.sPathToBase + sPd + Utils.cCurrStatus.GetNewestBaseFileName(s);
						fu.bChangeConfigFiles = true;	//изменить файл конфига терминала, добавить запись в файл списка обновлений
						fu.sTypeBase = s;
                        fu.sDateAktual = Utils.cCurrStatus.GetNewestBaseDate(s).ToString("dd.MM.yyyy");
                        if (File.Exists(fu.sFileSource))
                        {
                            arFilesToSend.Add(fu);
                            bBaseAdded = true;
                        }
					}
				}
				catch (Exception ex)
				{
					WriteProtocol(" GET_FULL_BASE:ERROR - " + ex.Message, 1);
				}
				finally
				{
					if (sr != null) sr.Close();
				}
			}

            DirectoryInfo dirInfXML = new DirectoryInfo(sFolderLocalBase);		// XML - файлы описания баз для терминала
            if (dirInfXML.Exists)
            {
                FileInfo[] updFilesXML = dirInfXML.GetFiles("T6.Base.*.xml");
                foreach (FileInfo sXMLFileName in updFilesXML)
                {
                    stFileUpd fu = new stFileUpd();
                    fu.sFileSource = sXMLFileName.FullName;
                    fu.sFileDest = term.stCurTerm.stPaths.sPathToBase + sPd + sXMLFileName.Name;
                    fu.bChangeConfigFiles = false;
                    if (bBaseAdded) arFilesToSend.Add(fu);	//если была передана база
                }
            }
            else WriteProtocol(" AnalisRecivedFiles:ERROR - Папка '" + dirInfXML.FullName + "' не существует. Измените файл конфигурации.", 1);

			//а еще обновления софта
			DirectoryInfo dirInfNS = new DirectoryInfo(sFolderNewSoft);
            NewSoftUpdates nsu = new NewSoftUpdates();
			if (dirInfNS.Exists)
			{
				FileInfo[] updFilesExe = dirInfNS.GetFiles("*.exe");
				foreach (FileInfo sNSFileName in updFilesExe)
				{
					try
					{
                        Version vCurrVersion = new Version(0, 0, 0, 0); // vNewVersion = new Version(0, 0, 0, 0);
						stFileUpd fu = new stFileUpd();
						fu.sFileSource = sNSFileName.FullName;
						fu.sFileDest = term.stCurTerm.stPaths.sPathNewSoft + sPd + sNSFileName.Name;
						fu.bChangeConfigFiles = false;
                        if (sNSFileName.Name == "Terminal.exe") vCurrVersion = new Version(term.stCurTerm.stVersions.sVersionTerminal);
                        if (sNSFileName.Name == "Starter.exe") vCurrVersion = new Version(term.stCurTerm.stVersions.sVersionStarter);

                        //System.Reflection.Assembly asmCurr = System.Reflection.Assembly.LoadFrom(sNSFileName.FullName);
                        //vNewVersion = asmCurr.GetName().Version;
                        //if (vNewVersion > vCurrVersion)
                        //{
                        if (!nsu.CheckUpdates(sNSFileName.FullName, term.stCurTerm.sDeviceID, vCurrVersion.ToString()))
                            {
                                arFilesToSend.Add(fu);
                                WriteProtocol(" TransferTo.AnalisRecivedFiles:OK - добавлен новый софт: " + sNSFileName.Name + " .", 2);
                                bRebootIsRequired = true;
                                nsu.AddNewTerminal(sNSFileName.FullName, term.stCurTerm.sDeviceID);
                            }
                        //}
					}
					catch (Exception ex)
					{
						WriteProtocol("NewVersion.exe:ERROR - " + ex.Message, 1);
					}
				}


				FileInfo[] updFilesDll = dirInfNS.GetFiles("*.dll");
				foreach (FileInfo sNSFileName in updFilesDll)
				{
					try
					{
                        //Version vCurrVersion = new Version(0, 0, 0, 0), vNewVersion = new Version(0, 0, 0, 0);
						stFileUpd fu = new stFileUpd();
						fu.sFileSource = sNSFileName.FullName;
						fu.sFileDest = term.stCurTerm.stPaths.sPathNewSoft + sPd + sNSFileName.Name;
						fu.bChangeConfigFiles = false;
                        //bool bFoundInArBases = false;
                        //foreach (stOneBase ob in term.stCurTerm.arBases)
                        //{
                        //    if (ob.sNameFileDll == sNSFileName.Name)
                        //    {
                        //        vCurrVersion = new Version(ob.sVerFileDll);
                        //        bFoundInArBases = true;
                        //        break;
                        //    }
                        //}
                        //System.Reflection.Assembly asmCurr = System.Reflection.Assembly.LoadFrom(sNSFileName.FullName);
                        //vNewVersion = asmCurr.GetName().Version;
                        //if (vNewVersion > vCurrVersion)
                        //{
                            if (!nsu.CheckUpdates(sNSFileName.FullName, term.stCurTerm.sDeviceID, "0.0.0.0"))
                            {
                                arFilesToSend.Add(fu);
                                //if (bFoundInArBases) bRebootIsRequired = true;
                                nsu.AddNewTerminal(sNSFileName.FullName, term.stCurTerm.sDeviceID);
                            }
                        //}
					}
					catch (Exception ex)
					{
						WriteProtocol("NewVersion.dll:ERROR - " + ex.Message , 1);
					}
				}
			}            
			else WriteProtocol(" AnalisRecivedFiles:ERROR - Папка '" + dirInfNS.FullName + "' не существует. Измените файл конфигурации.", 1);
            if (nsu != null) nsu.Close();

            //добавить файл конфига в список отправляемых
            stFileUpd fu1 = new stFileUpd();
            fu1.bChangeConfigFiles = false;
            fu1.sFileSource = sPathToExecute + sPd + sFileNameCurInit;
            fu1.sFileDest = term.stCurTerm.stPaths.sPathStorageFiles + sPd + sRemoteNameFileT6Init;
            arFilesToSend.Add(fu1);

            //записать инфу о последней синхронизации
            term.stCurTerm.sLastSyncronisation = DateTime.Now.ToString("dd.MM.yyyy");
            term.SaveIniFile(sPathToExecute + sPd + sFileNameCurInit);
		}
        /// <summary>
        /// Запись в файл протокола работы.
        /// </summary>
        /// <param name="strWr">Строка с записью.</param>
        /// <param name="iLevelDebud">Уровень важности записи.</param>
        /// <returns></returns>
        public static bool WriteProtocol(string strWr, int iLevelDebud)
		{
            bool bRet = true;
            if (iLevelDebud <= iDebugLevel)
            {
                try
                {
                    bRet = Utils.WriteDebugString(sPathToExecute, " -ClassFileCopy- " + strWr);
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
		private bool IsUpdateNew(string sCurUpdDate, string sNameNewUpd)
		{
			bool bRet = false;
			try
			{				
				DateTime dtCur = Convert.ToDateTime(sCurUpdDate);
				if (sNameNewUpd.IndexOf(".") > 8) sNameNewUpd = sNameNewUpd.Substring(0, sNameNewUpd.IndexOf("."));
				sNameNewUpd = sNameNewUpd.Substring(sNameNewUpd.Length - 8, 8);
				DateTime dtNew = new DateTime(Convert.ToInt32(sNameNewUpd.Substring(4, 4)), Convert.ToInt32(sNameNewUpd.Substring(2, 2)), Convert.ToInt32(sNameNewUpd.Substring(0, 2)));
				if (dtNew > dtCur) bRet = true;
			}
			catch (Exception ex)
			{
				bRet = true;
				WriteProtocol(" IsUpdateNew:ERROR - " + ex.Message, 1);
			}
			return bRet;
		}
		private bool IsUpdateNew(string sCurUpd, DateTime dtNewUpd)
		{
			bool bRet = false;
			try
			{
				DateTime dtCur = Convert.ToDateTime(sCurUpd);
				if (dtNewUpd > dtCur) bRet = true;
			}
			catch (Exception ex)
			{
				bRet = true;
				WriteProtocol(" IsUpdateNewDT:ERROR - " + ex.Message, 1);
			}
			return bRet;
		}
		private bool FullBaseIsBetter(string sDateLastUpd, string sTypeBase)
		{
			bool bRet = false;
            DateTime dtBase = Utils.cCurrStatus.GetNewestBaseDate(sTypeBase);
            WriteProtocol(" TransferTo.FullBaseIsBetter:OK - база типа " + sTypeBase + " на станции: " + dtBase.ToString("dd.MM.yyyy"), 2);
			try
			{
				DateTime dtCur = Convert.ToDateTime(sDateLastUpd);
                WriteProtocol(" TransferTo.FullBaseIsBetter:OK - база типа " + sTypeBase + " на терминале: " + dtCur.ToString("dd.MM.yyyy"), 2);
				if (dtCur < dtBase) bRet = true;
			}
			catch (Exception ex)
			{
				bRet = true;
				WriteProtocol(" FullBaseIsBetter:ERROR - " + ex.Message, 1);
			}
			return bRet;
		}
		private DateTime GetCreationBaseTime(string sFullNameXmlFile)
		{
			Stream fs = null;
			stOneBase stOneBase = new stOneBase();
			DateTime dtRet = new DateTime(2001, 1, 1);
			if (!File.Exists(sFullNameXmlFile))
			{
				WriteProtocol("GetCreationBaseTime.LoadXml:ERROR - file not found:" + sFullNameXmlFile, 1);
				return dtRet;
			}
			try
			{
				fs = new FileStream(sFullNameXmlFile, FileMode.Open);
				XmlSerializer sr = new XmlSerializer(typeof(stOneBase));
				stOneBase = (stOneBase)sr.Deserialize(fs);
				fs.Close();
				//WriteProtocol("GetCreationBaseTime:OK");
			}
			catch (Exception ex)
			{
				if (fs != null) fs.Close();
				WriteProtocol("GetCreationBaseTime.LoadXml:ERROR - " + ex.Message, 1);
			}

			if (stOneBase.sDateLastUpdates != null)
			{
				try
				{
					dtRet = Convert.ToDateTime(stOneBase.sDateLastUpdates);
				}
				catch (Exception ex)
				{
					WriteProtocol("GetCreationBaseTime.ParseNameUpd:ERROR - " + ex.Message, 1);
				}
			}
			return dtRet;
		}        
	}
	public struct stFileUpd
	{
		/// <summary>
		/// Путь к локальному файлу
		/// </summary>
		public string sFileSource;
		/// <summary>
		/// Путь к файлу на КПК
		/// </summary>
		public string sFileDest;
		/// <summary>
		/// Нужно ли изменять файл конфига терминала
		/// </summary>
		public bool bChangeConfigFiles;
		/// <summary>
		/// Тип базы
		/// </summary>
		public string sTypeBase;
		/// <summary>
		/// Дата актуальности передаваемого файла
		/// </summary>
		public string sDateAktual;
	}

}
