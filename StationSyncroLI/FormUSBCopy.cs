using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SSCE;
using System.IO;
using System.Threading;

namespace StationSyncroCE
{
    public partial class FormUSBCopy : Form
    {
        private Thread thCopy = null;
        private string sRootFolder = null;
        private int iPrBarAllValue = 0, iPrBarAllMax = 10;
        private int iPrBarCurValue = 0, iPrBarCurMax = 10;
        private string sNameCurFile = ".....";
        public bool bRequestReboot = false;
        private bool bVisibleLabelEnd = false;
        private string sTextLabelTop = "Идет копирование файлов. Ждите.";
        private bool bNeedRefreshBases = false;

        public FormUSBCopy(string _sRootFolder)
        {
            InitializeComponent();
            sRootFolder = _sRootFolder;
        }

        private void FormUSBCopy_Load(object sender, EventArgs e)
        {
            thCopy = new System.Threading.Thread(new System.Threading.ThreadStart(this.CopyAll));
            thCopy.Start();
            this.timer2.Enabled = true;
        }
        /// <summary>
        /// Копируем все с USB брелка и на него.
        /// </summary>
        private void CopyAll()
        {
            if (!Utils.bInizialised)
            {
                if (File.Exists("\\Documents and Settings\\SSCE" + "\\" + Utils.sFileNameInit + ".bak"))
                {
                    File.Delete("\\Documents and Settings\\SSCE" + "\\" + Utils.sFileNameInit + ".bak");
                }

                if (File.Exists(sRootFolder + "\\CurrentConfig\\" + Utils.GetDeviceID() + "\\" + Utils.sFileNameInit))
                {
                    File.Copy(sRootFolder + "\\CurrentConfig\\" + Utils.GetDeviceID() + "\\" + Utils.sFileNameInit, "\\Documents and Settings\\SSCE" + "\\" + Utils.sFileNameInit + ".bak");
                    bRequestReboot = true;
                    sNameCurFile = "Жду перезагрузку...";
                    bVisibleLabelEnd = true;
                    sTextLabelTop = "Файл конфигурации изменен";
                    return;
                }

                if (File.Exists(sRootFolder + "\\CurrentConfig" + "\\" + Utils.sFileNameInit))
                {
                    File.Copy(sRootFolder + "\\CurrentConfig" + "\\" + Utils.sFileNameInit, "\\Documents and Settings\\SSCE" + "\\" + Utils.sFileNameInit + ".bak");
                    bRequestReboot = true;
                    sNameCurFile = "Жду перезагрузку...";
                    bVisibleLabelEnd = true;
                    sTextLabelTop = "Файл конфигурации изменен";
                    return;
                }


                sNameCurFile = "Закрываюсь...";
                bVisibleLabelEnd = true;
                sTextLabelTop = "Файл конфигурации не найден";
                return;
            }
            WriteDebugString("CopyAll.start", 2);
            if (!Directory.Exists(sRootFolder + "\\FullBase")) Directory.CreateDirectory(sRootFolder + "\\FullBase");
            if (!Directory.Exists(sRootFolder + "\\NewSoft")) Directory.CreateDirectory(sRootFolder + "\\NewSoft");
            if (!Directory.Exists(sRootFolder + "\\NewSoft\\SS")) Directory.CreateDirectory(sRootFolder + "\\NewSoft\\SS");
            if (!Directory.Exists(sRootFolder + "\\NewSoft\\Terminal")) Directory.CreateDirectory(sRootFolder + "\\NewSoft\\Terminal");
            if (!Directory.Exists(sRootFolder + "\\OutBox")) Directory.CreateDirectory(sRootFolder + "\\OutBox");
            if (!Directory.Exists(sRootFolder + "\\Updates")) Directory.CreateDirectory(sRootFolder + "\\Updates");
            if (!Directory.Exists(sRootFolder + "\\Config")) Directory.CreateDirectory(sRootFolder + "\\Config");
            if (!Directory.Exists(sRootFolder + "\\CurrentConfig")) Directory.CreateDirectory(sRootFolder + "\\CurrentConfig");
            if (!Directory.Exists(sRootFolder + "\\CurrentConfig\\" + Utils.GetDeviceID())) Directory.CreateDirectory(sRootFolder + "\\CurrentConfig\\" + Utils.GetDeviceID());
            if (!Directory.Exists(sRootFolder + "\\Программы")) Directory.CreateDirectory(sRootFolder + "\\Программы");

            DirectoryInfo diFullBase = new DirectoryInfo(sRootFolder + "\\FullBase");
            DirectoryInfo diNewSoftSS = new DirectoryInfo(sRootFolder + "\\NewSoft\\SS");
            DirectoryInfo diNewSoftTerm = new DirectoryInfo(sRootFolder + "\\NewSoft\\Terminal");
            DirectoryInfo diUpdates = new DirectoryInfo(sRootFolder + "\\Updates");
            DirectoryInfo diConfig = new DirectoryInfo(sRootFolder + "\\Config");
            DirectoryInfo diOutBox = new DirectoryInfo(Utils.strConfig.sPathToOutBox);  //единственный на вывод.

            FileInfo[] fiFullBase = diFullBase.GetFiles();
            FileInfo[] fiNewSoftSS = diNewSoftSS.GetFiles();
            FileInfo[] fiNewSoftTerm = diNewSoftTerm.GetFiles();
            FileInfo[] fiUpdates = diUpdates.GetFiles();
            FileInfo[] fiConfig = diConfig.GetFiles();
            FileInfo[] fiOutBox = diOutBox.GetFiles();

            iPrBarAllMax = fiFullBase.Length + fiNewSoftSS.Length + fiNewSoftTerm.Length + fiUpdates.Length + fiConfig.Length + fiOutBox.Length;
            iPrBarAllValue = 0;

            try
            {
                if (File.Exists(Utils.strConfig.sPathToExecute + "\\" + Utils.sFileNameInit))   //копируем текущий конфиг в папки CurrentConfig и CurrentConfig\\Utils.GetDeviceID()
                {
                    if (File.Exists(sRootFolder + "\\CurrentConfig" + "\\" + Utils.sFileNameInit)) File.Delete(sRootFolder + "\\CurrentConfig" + "\\" + Utils.sFileNameInit);
                    File.Copy(Utils.strConfig.sPathToExecute + "\\" + Utils.sFileNameInit, sRootFolder + "\\CurrentConfig" + "\\" + Utils.sFileNameInit);

                    if (File.Exists(sRootFolder + "\\CurrentConfig\\" + Utils.GetDeviceID() + "\\" + Utils.sFileNameInit)) File.Delete(sRootFolder + "\\CurrentConfig\\" + Utils.GetDeviceID() + "\\" + Utils.sFileNameInit);
                    File.Copy(Utils.strConfig.sPathToExecute + "\\" + Utils.sFileNameInit, sRootFolder + "\\CurrentConfig\\" + Utils.GetDeviceID() + "\\" + Utils.sFileNameInit);
                }
                //DirectoryInfo di = new DirectoryInfo(Utils.strConfig.sPathToFullBase);
                //FileInfo[] fi = di.GetFiles("*.xml");
                //foreach (FileInfo f in fi)
                //{
                //    if (f.Exists)
                //    {
                //        if (File.Exists(sRootFolder + "\\CurrentConfig" + "\\" + f.Name)) File.Delete(sRootFolder + "\\CurrentConfig" + "\\" + f.Name);
                //        File.Copy(f.FullName, sRootFolder + "\\CurrentConfig" + "\\" + f.Name);

                //        if (File.Exists(sRootFolder + "\\CurrentConfig\\" + Utils.GetDeviceID() + "\\" + f.Name)) File.Delete(sRootFolder + "\\CurrentConfig\\" + Utils.GetDeviceID() + "\\" + f.Name);
                //        File.Copy(f.FullName, sRootFolder + "\\CurrentConfig\\" + Utils.GetDeviceID() + "\\" + f.Name);

                //    }
                //}

                if (File.Exists(Utils.strConfig.sPathToNewSoftSS + "\\" + Utils.sFileNameProgrammConfig))   //копируем программу для конфигурации станции. Если есть то из папки с новым софтом. Если нет то из рабочей папки
                {
                    if (File.Exists(sRootFolder + "\\Программы" + "\\" + Utils.sFileNameProgrammConfig)) File.Delete(sRootFolder + "\\Программы" + "\\" + Utils.sFileNameProgrammConfig);
                    File.Copy(Utils.strConfig.sPathToNewSoftSS + "\\" + Utils.sFileNameProgrammConfig, sRootFolder + "\\Программы" + "\\" + Utils.sFileNameProgrammConfig);
                }
                else if (File.Exists(Utils.strConfig.sPathToExecute + "\\" + Utils.sFileNameProgrammConfig))
                {
                    if (File.Exists(sRootFolder + "\\Программы" + "\\" + Utils.sFileNameProgrammConfig)) File.Delete(sRootFolder + "\\Программы" + "\\" + Utils.sFileNameProgrammConfig);
                    File.Copy(Utils.strConfig.sPathToExecute + "\\" + Utils.sFileNameProgrammConfig, sRootFolder + "\\Программы" + "\\" + Utils.sFileNameProgrammConfig);
                }

                if (File.Exists(Utils.strConfig.sPathToNewSoftSS + "\\" + Utils.sFileNameProgrammTransfer)) //копируем программу TransferFiles.exe. Если есть то из папки с новым софтом. Если нет то из рабочей папки
                {
                    if (File.Exists(sRootFolder + "\\Программы" + "\\" + Utils.sFileNameProgrammTransfer)) File.Delete(sRootFolder + "\\Программы" + "\\" + Utils.sFileNameProgrammTransfer);
                    File.Copy(Utils.strConfig.sPathToNewSoftSS + "\\" + Utils.sFileNameProgrammTransfer, sRootFolder + "\\Программы" + "\\" + Utils.sFileNameProgrammTransfer);
                }
                else if (File.Exists(Utils.strConfig.sPathToExecute + "\\" + Utils.sFileNameProgrammTransfer))
                {
                    if (File.Exists(sRootFolder + "\\Программы" + "\\" + Utils.sFileNameProgrammTransfer)) File.Delete(sRootFolder + "\\Программы" + "\\" + Utils.sFileNameProgrammTransfer);
                    File.Copy(Utils.strConfig.sPathToExecute + "\\" + Utils.sFileNameProgrammTransfer, sRootFolder + "\\Программы" + "\\" + Utils.sFileNameProgrammTransfer);
                }

                if (File.Exists(Utils.strConfig.sPathToExecute + "\\Ionic.Zip.dll"))    //библиотеки необходимые для работы программ
                {
                    if (!File.Exists(sRootFolder + "\\Программы" + "\\Ionic.Zip.dll")) 
                    File.Copy(Utils.strConfig.sPathToExecute + "\\Ionic.Zip.dll", sRootFolder + "\\Программы" + "\\Ionic.Zip.dll");
                }

            }
            catch (Exception ex)
            {
                WriteDebugString("CopyAll.1:ERROR - " + ex.Message, 1);
            }


            MyFileCopy FileCopy = new MyFileCopy();
            FileCopy.BufferLenghtEx = 65536;
            FileCopy.OnProgress += new MyFileCopy_Progress(FileCopy_OnProgress);
            foreach (FileInfo fi in fiFullBase) //копируем базы целиком - все что есть в папке
            {
                iPrBarCurMax = 101;
                iPrBarCurValue = 0;

                sNameCurFile = fi.Name;                
                try
                {
                    Utils.MarkedDirBusy(Utils.strConfig.sPathToFullBase);
                    if (File.Exists(Utils.strConfig.sPathToFullBase + "\\" + fi.Name)) File.Delete(Utils.strConfig.sPathToFullBase + "\\" + fi.Name);
                    FileCopy.CopyFile(fi.FullName, Utils.strConfig.sPathToFullBase + "\\" + fi.Name);
                    AddLastRecivedInfo(fi.Name);
                    bNeedRefreshBases = true;
                }
                catch (Exception ex)
                {
                    WriteDebugString("CopyAll.2:ERROR - " + ex.Message, 1);
                }
                Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToFullBase);
                //iPrBarCurValue++;
                iPrBarAllValue++;                
            }
            FileCopy = null;

            iPrBarCurMax = fiNewSoftSS.Length;
            iPrBarCurValue = 0;
            foreach (FileInfo fi in fiNewSoftSS)
            {
                sNameCurFile = fi.Name;
                try
                {
                    if (File.Exists(Utils.strConfig.sPathToNewSoftSS + "\\" + fi.Name)) File.Delete(Utils.strConfig.sPathToNewSoftSS + "\\" + fi.Name);
                    File.Copy(fi.FullName, Utils.strConfig.sPathToNewSoftSS + "\\" + fi.Name);
                    AddLastRecivedInfo(fi.Name);
                }
                catch (Exception ex)
                {
                    WriteDebugString("CopyAll.3:ERROR - " + ex.Message, 1);
                }
                iPrBarCurValue++;
                iPrBarAllValue++;
            }

            iPrBarCurMax = fiNewSoftTerm.Length;
            iPrBarCurValue = 0;
            foreach (FileInfo fi in fiNewSoftTerm)
            {
                sNameCurFile = fi.Name;
                try
                {
                    if (File.Exists(Utils.strConfig.sPathToNewSoftTerminal + "\\" + fi.Name)) File.Delete(Utils.strConfig.sPathToNewSoftTerminal + "\\" + fi.Name);
                    File.Copy(fi.FullName, Utils.strConfig.sPathToNewSoftTerminal + "\\" + fi.Name);
                    AddLastRecivedInfo(fi.Name);
                }
                catch (Exception ex)
                {
                    WriteDebugString("CopyAll.4:ERROR - " + ex.Message, 1);
                }
                iPrBarCurValue++;
                iPrBarAllValue++;
            }

            iPrBarCurMax = fiUpdates.Length;
            iPrBarCurValue = 0;
            foreach (FileInfo fi in fiUpdates)
            {
                sNameCurFile = fi.Name;
                Utils.MarkedDirBusy(Utils.strConfig.sPathToUpdTerm);
                try
                {
                    if (File.Exists(Utils.strConfig.sPathToUpdTerm + "\\" + fi.Name)) File.Delete(Utils.strConfig.sPathToUpdTerm + "\\" + fi.Name);
                    File.Copy(fi.FullName, Utils.strConfig.sPathToUpdTerm + "\\" + fi.Name);
                    AddLastRecivedInfo(fi.Name);
                    bNeedRefreshBases = true;
                }
                catch (Exception ex)
                {
                    WriteDebugString("CopyAll.5:ERROR - " + ex.Message, 1);
                }
                Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToUpdTerm);
                iPrBarCurValue++;
                iPrBarAllValue++;
            }

            iPrBarCurMax = fiConfig.Length;
            iPrBarCurValue = 0;
            foreach (FileInfo fi in fiConfig)
            {
                sNameCurFile = fi.Name;
                if (fi.Name == Utils.sFileNameInit)
                {
                    try
                    {
                        WriteDebugString("CopyAll:WARNING - found CONFIG file", 1);
                        stSSConfig stC = new stSSConfig();
                        if(Utils.LoadInitFile(fi.FullName, ref stC))
                        {
                            //int result = Registry.CreateValueString(Registry.HKLM, @"Comm\SMSC91181\Parms\TcpIp", "IpAddress", stC.strNet.sIPAdressLocal);
                            //result = Registry.CreateValueString(Registry.HKLM, @"Comm\SMSC91181\Parms\TcpIp", "Subnetmask", stC.strNet.sMask);
                            //result = Registry.CreateValueString(Registry.HKLM, @"Comm\SMSC91181\Parms\TcpIp", "DefaultGateway", stC.strNet.sGateway);
                            //result = Registry.CreateValueDWORD(Registry.HKLM, @"Comm\SMSC91181\Parms", "MacAddressHi", Convert.ToUInt32(stC.strNet.sMAC_hi));
                            //result = Registry.CreateValueDWORD(Registry.HKLM, @"Comm\SMSC91181\Parms", "MacAddressLo", Convert.ToUInt32(stC.strNet.sMAC_lo));
                            //result = Registry.RegFlushKey(Registry.HKLM);

                            if (File.Exists(Utils.strConfig.sPathToExecute + "\\" + fi.Name)) File.Delete(Utils.strConfig.sPathToExecute + "\\" + fi.Name);
                            File.Move(fi.FullName, Utils.strConfig.sPathToExecute + "\\" + fi.Name);
                            WriteDebugString("CopyAll:CONFIG file changed FROM:" + fi.FullName + "  TO:" + Utils.strConfig.sPathToExecute + "\\" + fi.Name, 1);
                            bRequestReboot = true;
                            AddLastRecivedInfo(fi.Name);                            
                        }
                    }
                    catch (Exception ex)
                    {
                        Utils.WriteDebugString(Utils.strConfig.sPathToExecute, "FormUsbCopy - ERROR: write registry - " + ex.Message);
                    }                    
                }
                else
                {
                    try
                    {
                        if (File.Exists(Utils.strConfig.sPathToExecute + "\\" + fi.Name)) File.Delete(Utils.strConfig.sPathToExecute + "\\" + fi.Name);
                        File.Move(fi.FullName, Utils.strConfig.sPathToExecute + "\\" + fi.Name);
                        AddLastRecivedInfo(fi.Name);
                    }
                    catch (Exception ex)
                    {
                        WriteDebugString("CopyAll.6:ERROR - " + ex.Message, 1);
                    }
                }
                iPrBarCurValue++;
                iPrBarAllValue++;                
            }

            iPrBarCurMax = fiOutBox.Length;
            iPrBarCurValue = 0;
            foreach (FileInfo fi in fiOutBox)
            {
                sNameCurFile = fi.Name;
                try
                {
                    if (File.Exists(sRootFolder + "\\OutBox" + "\\" + fi.Name)) File.Delete(sRootFolder + "\\OutBox" + "\\" + fi.Name);
                    File.Copy(fi.FullName, sRootFolder + "\\OutBox" + "\\" + fi.Name);
                    AddLastSendedInfo(fi.Name);
                }
                catch { }
                iPrBarCurValue++;
                iPrBarAllValue++;
            }            

            sTextLabelTop = "Копирование завершено.";
            bVisibleLabelEnd = true;
            WriteDebugString("CopyAll.exit", 2);
        }

        void FileCopy_OnProgress(string message, int procent)
        {
            iPrBarCurValue = procent;
        }

        private void timer3_Tick(object sender, EventArgs e)    //таймер закрытия
        {
            if (bNeedRefreshBases) Utils.cCurrStatus.UpdateBaseInfo();

            this.timer2.Enabled = false;
            this.timer3.Enabled = false;

            this.Close();
        }

        private void timer2_Tick(object sender, EventArgs e)    //таймер прогрессбаров
        {
            this.progressBarAll.Maximum = iPrBarAllMax;
            this.progressBarAll.Value = iPrBarAllValue;
            this.progressBarCurr.Maximum = iPrBarCurMax;
            this.progressBarCurr.Value = iPrBarCurValue;

            this.labelFileName.Text = sNameCurFile;

            if (bVisibleLabelEnd)
            {
                this.labelEnd.Visible = true;
                this.timer3.Enabled = true;
                this.labelFileName.Text = "Завершено.";
            }
            this.labelTop.Text = sTextLabelTop;
        }

        private bool WriteDebugString(string strWr, int iLevelDebud)
        {
            bool bRet = true;
            if (iLevelDebud <= Utils.strConfig.iDebugLevel)
            {
                try
                {
                    bRet = Utils.WriteDebugString(Utils.strConfig.sPathToExecute, " -FormUSBCopy- " + strWr);
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
                    if(!bRequestReboot) Utils.SaveInit();
                }
                catch //(Exception ex)
                {
                    bRet = false;
                }
            }
            return bRet;
        }
        /// <summary>
        /// Добавить информацию о принятых файлах.
        /// </summary>
        /// <param name="sFileName">Имя принятого файла.</param>
        private void AddLastRecivedInfo(string sFileName)
        {
            lock (Utils.oSyncroLoadSaveInit)
            {
                try
                {
                    Utils.strConfig.strMail.sLastRecivedTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                    Utils.cCurrStatus.sLastReciveTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                    Utils.cCurrStatus.sLastReciveName = sFileName;
                    if (!bRequestReboot) Utils.SaveInit();
                    WriteDebugString("AddLastRecivedInfo:OK - " + sFileName, 2);
                }
                catch (Exception ex)
                {
                    WriteDebugString("AddLastRecivedInfo:ERROR - " + ex.Message, 1);
                }
            }
        }
        /// <summary>
        /// Добавить информацию о переданных файлах.
        /// </summary>
        /// <param name="sFileName">Имя переданного файла.</param>
        private void AddLastSendedInfo(string sFileName)
        {
            lock (Utils.oSyncroLoadSaveInit)
            {
                try
                {
                    Utils.strConfig.strMail.sLastSendedTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                    Utils.cCurrStatus.sLastSendedTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                    Utils.cCurrStatus.sLastSendedName = sFileName;
                    if (!bRequestReboot) Utils.SaveInit();
                    WriteDebugString("AddLastSendedInfo:OK - " + sFileName, 2);
                }
                catch (Exception ex)
                {
                    WriteDebugString("AddLastSendedInfo:ERROR - " + ex.Message, 1);
                }
            }
        }
    }
}