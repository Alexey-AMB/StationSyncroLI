using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using SSCE;

namespace StationSyncroCE
{
    public partial class FormMain : Form
    {
        private string sExecutePath = null;
        private ClassAMBRenewedService[] arRS = null;
        private Thread[] arThreadRS = null;

        AMB_USBFLASHNOTY.AMB_USBFlashNotification amUFN = null;
        private bool bFormCopyIsShow = false;
        private bool bUSBDiskIsInserted = false;
        private string sUSBDiskDeviceName = null;
        private bool bRebootNow = false;
        private int iCountReboot = 100;
        private DateTime dtSaveConfig = DateTime.Now.AddDays(7);

        public FormMain()
        {
            InitializeComponent();
            OnStart();
            this.timer4.Enabled = true;
            amUFN = new AMB_USBFLASHNOTY.AMB_USBFlashNotification();
            AMB_USBFLASHNOTY.AMB_USBFlashNotification.NewNotification += new AMB_USBFLASHNOTY.AMB_USBFlashNotification.MyEv_NewNotifi(AMB_USBFlashNotification_NewNotification);
            if (amUFN.Init()) amUFN.Run();
        }

        private void OnStart()
        {
            if (DateTime.Now < DateTime.Parse("01.01.2012"))
            {
                if (MessageBox.Show("Системное время не установлено.", "Установить время?", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.OK)
                {
                    Form_Settings formSysSet = new Form_Settings();
                    formSysSet.ShowDialog();
                }
            }

            sExecutePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);

            Utils.CutLogFile(sExecutePath + "//" + Utils.sFileNameLog, sExecutePath, 2000);

            //!!!if (!Directory.Exists("//Documents and Settings//SSCE")) Directory.CreateDirectory("//Documents and Settings//SSCE");

            if(FindConfigFile() != null) Utils.LoadInit(sExecutePath + "//" + Utils.sFileNameInit);
            Utils.sVersionSSCE = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            this.labelVersionOS.Text = "Ver. OS: " + new FileInfo("\\Windows\\nk.exe").CreationTime.ToString("ddMMyyyy");
            if (Utils.bInizialised)
            {
                try
                {
                    if (!Directory.Exists(Utils.strConfig.sPathToUpdTerm)) Directory.CreateDirectory(Utils.strConfig.sPathToUpdTerm);
                    if (!Directory.Exists(Utils.strConfig.sPathToNewSoftTerminal)) Directory.CreateDirectory(Utils.strConfig.sPathToNewSoftTerminal);
                    if (!Directory.Exists(Utils.strConfig.sPathToProtocol)) Directory.CreateDirectory(Utils.strConfig.sPathToProtocol);
                    if (!Directory.Exists(Utils.strConfig.sPathToFullBase)) Directory.CreateDirectory(Utils.strConfig.sPathToFullBase);
                    if (!Directory.Exists(Utils.strConfig.sPathToOutBox)) Directory.CreateDirectory(Utils.strConfig.sPathToOutBox);
                    if (!Directory.Exists(Utils.strConfig.sPathToNewSoftSS)) Directory.CreateDirectory(Utils.strConfig.sPathToNewSoftSS);
                    if (!Directory.Exists(Utils.strConfig.sPathToInBox)) Directory.CreateDirectory(Utils.strConfig.sPathToInBox);

                    //int result = Registry.CreateValueDWORD(Registry.HKLM, @"SOFTWARE\AMB InTech\AmbStarter", "AmbAngle", 180);
                    //result = Registry.CreateValueString(Registry.HKLM, @"SOFTWARE\AMB InTech\AmbStarter", "AutoRunFileName", sExecutePath + "\\" + "StationSyncroCE.exe");
                    //result = Registry.CreateKey(@"SOFTWARE\AMB InTech\SSCE");
                    //result = Registry.CreateValueString(Registry.HKLM, @"SOFTWARE\AMB InTech\SSCE", "PathToNewSoftSS", Utils.strConfig.sPathToNewSoftSS); //это потом читает стартер
                    //result = Registry.CreateValueString(Registry.HKLM, @"SOFTWARE\AMB InTech\SSCE", "VersionSSCE_Main", Utils.sVersionSSCE);
                    //result = Registry.CreateValueString(Registry.HKLM, @"SOFTWARE\AMB InTech\SSCE", "PathToExecute", sExecutePath);

                    //result = Registry.RegFlushKey(Registry.HKLM);
                }
                catch (Exception ex)
                {
                    Utils.WriteDebugString(sExecutePath, "- ONSTART - ERROR:" + ex.Message);
                }

                //если были закачаны новые версии файлов копируем их в рабочую папку с контролем версий
                DirectoryInfo di = new DirectoryInfo(Utils.strConfig.sPathToNewSoftSS);
                FileInfo[] arFi = di.GetFiles("*.*");
                foreach (FileInfo fi in arFi)
                {
                    try
                    {
                        if (SSCE.NativeFile.GetFileInfo(fi.FullName) >= SSCE.NativeFile.GetFileInfo(Utils.strConfig.sPathToExecute + "\\" + fi.Name))
                        {
                            if (File.Exists(Utils.strConfig.sPathToExecute + "\\" + fi.Name)) File.Delete(Utils.strConfig.sPathToExecute + "\\" + fi.Name);
                            File.Copy(fi.FullName, Utils.strConfig.sPathToExecute + "\\" + fi.Name);
                        }

                        File.Delete(fi.FullName);
                    }
                    catch { }
                }

                //Загружаем задачи
                arRS = new ClassAMBRenewedService[Utils.strConfig.arstTasks.Length];
                for (int i = 0; i < Utils.strConfig.arstTasks.Length; i++)
                {
                    arRS[i] = Utils.GetLastVersion(Utils.strConfig.arstTasks[i].sNameTask, ref Utils.strConfig.arstTasks[i].sCurrentVersion, sExecutePath, Utils.strConfig.sPathToNewSoftSS);
                }

                arThreadRS = new Thread[arRS.Length];

                for (int i = 0; i < arRS.Length; i++)
                {
                    if (arRS[i] != null)
                    {
                        if (arRS[i].Init()) //сначала все инициализируем
                        {
                            arThreadRS[i] = new System.Threading.Thread(new System.Threading.ThreadStart(arRS[i].Start));
                        }
                    }
                }

                Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToUpdTerm);
                Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToNewSoftTerminal);
                Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToProtocol);
                Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToFullBase);
                Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToOutBox);
                Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToNewSoftSS);
                Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToInBox);                

                //проверяем, что инициализированно, только потом запускаем
                for (int i = 0; i < arThreadRS.Length; i++)
                {
                    if (arThreadRS[i] != null) arThreadRS[i].Start();
                }

                Utils.strConfig.sTextLastCriticalError = "";
                Utils.strConfig.sTimeLastCriticalError = "";

                ShowPanel_System(false);
                ShowPanel_TermInMemory(false);
                ShowPanel_GetFiles(false);
                ShowPanel_Bases(false);
                ShowPanel_Settings(false);
                ShowPanel_DockStatus(true);
            }
            else
            {
                this.labelStatusMain.Text = "При запуске произошла фатальная ошибка. Файл конфигурации не считан. Установите флэш-накопитель для считывания резервной копии или обратитесь к разработчику ПО.";
            }
        }
        private void OnStop()
        {
            amUFN.Dispose();

            Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToUpdTerm);
            Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToNewSoftTerminal);
            Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToProtocol);
            Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToFullBase);
            Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToOutBox);
            Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToNewSoftSS);
            Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToInBox);

            Utils.SaveInit();
            Utils.SaveInit("\\Storage Card" + "\\" + Utils.sFileNameInit + ".bak");
            Utils.SaveInit("\\Documents and Settings\\SSCE" + "\\" + Utils.sFileNameInit + ".bak");

            this.timer1.Enabled = false;
            this.timer4.Enabled = false;
            for (int i = 0; i < arThreadRS.Length; i++)
            {
                if (arThreadRS[i] != null) arRS[i].Stop();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)    //Отображение статуса терминалов в подставке
        {
            if (Utils.cCurrStatus != null)
            {
                this.labelTName1.Text = Utils.cCurrStatus.arstTermInDock[0].sNameTerminal;
                this.labelTName2.Text = Utils.cCurrStatus.arstTermInDock[1].sNameTerminal;
                this.labelTName3.Text = Utils.cCurrStatus.arstTermInDock[2].sNameTerminal;
                this.labelTName4.Text = Utils.cCurrStatus.arstTermInDock[3].sNameTerminal;
                this.labelTName5.Text = Utils.cCurrStatus.arstTermInDock[4].sNameTerminal;

                this.labelTStat1.Text = Utils.cCurrStatus.arstTermInDock[0].sCurrStatus;
                this.labelTStat2.Text = Utils.cCurrStatus.arstTermInDock[1].sCurrStatus;
                this.labelTStat3.Text = Utils.cCurrStatus.arstTermInDock[2].sCurrStatus;
                this.labelTStat4.Text = Utils.cCurrStatus.arstTermInDock[3].sCurrStatus;
                this.labelTStat5.Text = Utils.cCurrStatus.arstTermInDock[4].sCurrStatus;

                this.labelTStat1.ForeColor = GetColorLabel(Utils.cCurrStatus.arstTermInDock[0].iColorLabelStatus);
                this.labelTStat2.ForeColor = GetColorLabel(Utils.cCurrStatus.arstTermInDock[1].iColorLabelStatus);
                this.labelTStat3.ForeColor = GetColorLabel(Utils.cCurrStatus.arstTermInDock[2].iColorLabelStatus);
                this.labelTStat4.ForeColor = GetColorLabel(Utils.cCurrStatus.arstTermInDock[3].iColorLabelStatus);
                this.labelTStat5.ForeColor = GetColorLabel(Utils.cCurrStatus.arstTermInDock[4].iColorLabelStatus);
            }
        }
        private void timer2_Tick(object sender, EventArgs e)    //Отображение статуса отправленных файлов и сети
        {
            if (Utils.cCurrStatus != null)
            {
                this.labelLastRecived.Text = Utils.cCurrStatus.sLastReciveName;
                this.labelLastRecivedTime.Text = Utils.cCurrStatus.sLastReciveTime;
                this.labelLastSended.Text = Utils.cCurrStatus.sLastSendedName;
                this.labelLastSendedTime.Text = Utils.cCurrStatus.sLastSendedTime;
            }

            if (Utils.IsNetPresented())
            {
                this.labelNetIs.Text = "ЕСТЬ";
                this.labelNetIs.ForeColor = Color.GreenYellow;
            }
            else
            {
                this.labelNetIs.Text = "НЕТ";
                this.labelNetIs.ForeColor = Color.Red;
            }
        }
        private void timer3_Tick(object sender, EventArgs e)    //Текущее время
        {
            this.labelDateTime.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
        }
        private void timer4_Tick(object sender, EventArgs e)    //таймер для контоля USB брелка
        {
            if ((bRebootNow)||(Utils.bRebootRequest))
            {
                if (iCountReboot == 100)
                {
                    if(Utils.bInizialised)  RebootNow();
                }

                this.panelBases.Visible = false;
                this.panelDockStatus.Visible = false;
                this.panelGetFiles.Visible = false;
                this.panelSettings.Visible = false;
                this.panelTermInMemory.Visible = false;
                this.panelSystem.Visible = false;

                this.labelStatusMain.Text = "Файл конфигурации был изменен. Устройство будет перезагружено через " + ((int)(iCountReboot / 10) + 1).ToString() + " секунд.";
                iCountReboot--;
                if (iCountReboot <= 0) Utils.SoftReset();
            }
            else
            {
                if (bUSBDiskIsInserted)
                {
                    bool bIsIncertedTerm = false;
                    foreach (stTermInDock tid in Utils.cCurrStatus.arstTermInDock)
                    {
                        if (tid.iServeStatus == 1) bIsIncertedTerm = true;
                    }
                    if (bIsIncertedTerm)
                    {
                        this.timer4.Enabled = false;
                        MessageBox.Show("Дождитесь окончания обмена с терминалами и повторите установку накопителя!");
                        this.timer4.Enabled = true;
                        return;
                    }
                    if (!bFormCopyIsShow)
                    {
                        bFormCopyIsShow = true;
                        FormUSBCopy fuc = new FormUSBCopy(sUSBDiskDeviceName);
                        fuc.ShowDialog();
                        if (fuc.bRequestReboot)
                        {
                            this.iCountReboot = 100;
                            bRebootNow = true;
                        }
                        fuc.Dispose();
                        fuc = null;
                    }
                }
            }

            if (DateTime.Now > dtSaveConfig)
            {
                dtSaveConfig = DateTime.Now.AddDays(7);
                Utils.SaveInit("\\Storage Card" + "\\" + Utils.sFileNameInit + ".bak");
                Utils.SaveInit("\\Documents and Settings\\SSCE" + "\\" + Utils.sFileNameInit + ".bak");
            }
        }

        private void ShowPanel_DockStatus(bool bShow)
        {
            if (bShow)
            {
                this.panelDockStatus.Left = 0;
                this.panelDockStatus.Top = 0;
                this.panelDockStatus.BringToFront();
                this.timer1.Enabled = true;
                this.labelVersion.Text = "v " + Utils.sVersionSSCE;
            }
            else
            {
                this.panelDockStatus.Left = 0;
                this.panelDockStatus.Top = 0;
                this.panelDockStatus.SendToBack();
                this.timer1.Enabled = false;
            }
        }
        private void ShowPanel_System(bool bShow)
        {
            if (bShow)
            {
                this.panelSystem.Left = 0;
                this.panelSystem.Top = 0;
                this.panelSystem.BringToFront();
                this.labelNamePodrazdelenie.Text = Utils.cCurrStatus.sNamePodrazdelenie;
                this.labelDevID.Text = Utils.cCurrStatus.sDeviceSS_ID;
                this.labelLastError.Text = Utils.strConfig.sTextLastCriticalError + " время: " + Utils.strConfig.sTimeLastCriticalError;
            }
            else
            {
                this.panelSystem.Left = 0;
                this.panelSystem.Top = 0;
                this.panelSystem.SendToBack();
            }
        }
        private void ShowPanel_TermInMemory(bool bShow)
        {
            if (bShow)
            {
                this.panelTermInMemory.Left = 0;
                this.panelTermInMemory.Top = 0;
                this.panelTermInMemory.BringToFront();

                this.listBoxTermInMem.Items.Clear();
                //Utils.cCurrStatus.UpdateTermInfo();
                if (Utils.cCurrStatus.arstTermInMemory == null)
                {
                    this.listBoxTermInMem.Items.Add("Нет данных");
                    return;
                }
                for (int i = 0; i < Utils.cCurrStatus.arstTermInMemory.Length; i++)
                {
                    this.listBoxTermInMem.Items.Add("Терминал: " + Utils.cCurrStatus.arstTermInMemory[i].sNameTerminal);
                    this.listBoxTermInMem.Items.Add(" - синхронизирован: " + Utils.cCurrStatus.arstTermInMemory[i].sLastSyncronized);
                    this.listBoxTermInMem.Items.Add(" - база на: " + Utils.cCurrStatus.arstTermInMemory[i].sLastUpdatesBaseLica);
                    this.listBoxTermInMem.Items.Add(" - проверок: " + Utils.cCurrStatus.arstTermInMemory[i].iCountSearchLica.ToString());
                    this.listBoxTermInMem.Items.Add("===========================");
                }
            }
            else
            {
                this.panelTermInMemory.Left = 0;
                this.panelTermInMemory.Top = 0;
                this.panelTermInMemory.SendToBack();
            }
        }
        private void ShowPanel_GetFiles(bool bShow)
        {
            if (bShow)
            {
                this.panelGetFiles.Left = 0;
                this.panelGetFiles.Top = 0;
                this.panelGetFiles.BringToFront();
                this.timer2.Enabled = true;
            }
            else
            {
                this.panelGetFiles.Left = 0;
                this.panelGetFiles.Top = 0;
                this.panelGetFiles.SendToBack();
                this.timer2.Enabled = false;
            }
        }
        private void ShowPanel_Bases(bool bShow)
        {
            if (bShow)
            {
                this.panelBases.Left = 0;
                this.panelBases.Top = 0;
                this.panelBases.BringToFront();

                this.listBoxBases.Items.Clear();
                //Utils.cCurrStatus.UpdateBaseInfo();
                if (Utils.cCurrStatus.arBaseInfo == null)
                {
                    this.listBoxBases.Items.Add("Нет данных");
                    return;
                }
                for (int i = 0; i < Utils.cCurrStatus.arBaseInfo.Length; i++)
                {
                    if (Utils.cCurrStatus.arBaseInfo[i].sName != null)
                    {
                        this.listBoxBases.Items.Add("База розыска: " + Utils.cCurrStatus.arBaseInfo[i].sName);
                        this.listBoxBases.Items.Add(" - база на: " + Utils.cCurrStatus.arBaseInfo[i].sDate);
                        this.listBoxBases.Items.Add(" - обновление: " + Utils.cCurrStatus.arBaseInfo[i].sLUpd);
                        this.listBoxBases.Items.Add("===========================");
                    }
                }
            }
            else
            {
                this.panelBases.Left = 0;
                this.panelBases.Top = 0;
                this.panelBases.SendToBack();
            }
        }
        private void ShowPanel_Settings(bool bShow)
        {
            if (bShow)
            {
                this.panelSettings.Left = 0;
                this.panelSettings.Top = 0;
                this.panelSettings.BringToFront();
                this.timer3.Enabled = true;
                uint uVal = 0;
                //int result = Registry.GetDWORDValue(Registry.HKCU, @"ControlPanel\BackLight", "ACBrightness", ref uVal);
                //if (result == 0) this.trackBar1.Value = (int)uVal;

                this.dateTimePicker1.Value = DateTime.Now;
                this.dateTimePicker2.Value = DateTime.Now;
            }
            else
            {
                this.panelSettings.Left = 0;
                this.panelSettings.Top = 0;
                this.panelSettings.SendToBack();
                this.timer3.Enabled = false;
            }
        }

        private void buttonNext1_Click(object sender, EventArgs e)
        {
            ShowPanel_DockStatus(false);
            ShowPanel_TermInMemory(true);
        }
        private void buttonNext2_Click(object sender, EventArgs e)
        {
            ShowPanel_TermInMemory(false);
            ShowPanel_Bases(true);
        }
        private void buttonNext5_Click(object sender, EventArgs e)
        {
            ShowPanel_Bases(false);
            ShowPanel_GetFiles(true);
        }
        private void buttonNext4_Click(object sender, EventArgs e)
        {
            ShowPanel_GetFiles(false);
            ShowPanel_System(true);
        }
        private void buttonNext3_Click(object sender, EventArgs e)
        {
            ShowPanel_System(false);
            ShowPanel_Settings(true);
        }
        private void buttonNext6_Click(object sender, EventArgs e)
        {
            ShowPanel_Settings(false);
            ShowPanel_DockStatus(true);
        }

        private void buttonApply_Click(object sender, EventArgs e)
        {
            DateTime dtNew = new DateTime(this.dateTimePicker1.Value.Year, this.dateTimePicker1.Value.Month, this.dateTimePicker1.Value.Day);

            Utils.SetSystemTime(dtNew);

            dtNew = new DateTime(this.dateTimePicker1.Value.Year, this.dateTimePicker1.Value.Month, this.dateTimePicker1.Value.Day, this.dateTimePicker2.Value.Hour,
                this.dateTimePicker2.Value.Minute, this.dateTimePicker2.Value.Second);

            Utils.SetSystemTime(dtNew);


            //int result = Registry.CreateValueDWORD(Registry.HKCU, @"ControlPanel\BackLight", "ACBrightness", (uint)this.trackBar1.Value);
            //result = Registry.RegFlushKey(Registry.HKCU);
        }

        private Color GetColorLabel(int iColor)
        {
            Color cRet = new Color();
            switch (iColor)
            {
                case -1:
                    cRet = Color.Red;
                    break;
                case 0:
                    cRet = Color.Black;
                    break;
                case 1:
                    cRet = Color.Magenta;
                    break;
                case 2:
                    cRet = Color.Green;
                    break;
            }
            return cRet;
        }

        void AMB_USBFlashNotification_NewNotification(string sNameDevice, bool bStatus)
        {
            if ((sNameDevice != "Жесткий диск") && (sNameDevice != "Hard Disk")) return;

            this.bUSBDiskIsInserted = bStatus;
            if (bUSBDiskIsInserted)
            {
                sUSBDiskDeviceName = sNameDevice;
            }
            else
            {
                bFormCopyIsShow = false;
                sUSBDiskDeviceName = null;
            }
        }

        private void RebootNow()
        {
            Utils.WriteDebugString(sExecutePath, " #RebootNow:WARNING - system shutdown. Reboot after 10 seconds.");

            amUFN.Dispose();

            Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToUpdTerm);
            Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToNewSoftTerminal);
            Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToProtocol);
            Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToFullBase);
            Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToOutBox);
            Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToNewSoftSS);
            Utils.DeleteMarkerDirBusy(Utils.strConfig.sPathToInBox);

            Utils.SaveInit("\\Storage Card" + "\\" + Utils.sFileNameInit + ".bak");
            Utils.SaveInit("\\Documents and Settings\\SSCE" + "\\" + Utils.sFileNameInit + ".bak");

            for (int i = 0; i < arThreadRS.Length; i++)
            {
                if (arThreadRS[i] != null) arRS[i].Stop();
            }
        }

        private string FindConfigFile()
        {
            try
            {
                string sRet = null;

                if (File.Exists(sExecutePath + "\\" + Utils.sFileNameInit))
                {
                    stSSConfig stC = new stSSConfig();
                    if (Utils.LoadInitFile(sExecutePath + "\\" + Utils.sFileNameInit, ref stC))
                    {
                        sRet = sExecutePath + "\\" + Utils.sFileNameInit;
                        Utils.WriteDebugString(sExecutePath, " #FindConfigFile: config use - " + sExecutePath + "\\" + Utils.sFileNameInit);
                        return sRet;
                    }
                }
                if (File.Exists("\\Storage Card" + "\\" + Utils.sFileNameInit + ".bak"))
                {
                    stSSConfig stC = new stSSConfig();
                    if (Utils.LoadInitFile("\\Storage Card" + "\\" + Utils.sFileNameInit + ".bak", ref stC))
                    {
                        if (File.Exists(sExecutePath + "\\" + Utils.sFileNameInit)) File.Delete(sExecutePath + "\\" + Utils.sFileNameInit);
                        File.Copy("\\Storage Card" + "\\" + Utils.sFileNameInit + ".bak", sExecutePath + "\\" + Utils.sFileNameInit);
                        sRet = sExecutePath + "\\" + Utils.sFileNameInit;
                        Utils.WriteDebugString(sExecutePath, " #FindConfigFile: config use - " + "\\Storage Card" + "\\" + Utils.sFileNameInit + ".bak");
                        return sRet;
                    }
                }
                if (File.Exists("\\Documents and Settings\\SSCE" + "\\" + Utils.sFileNameInit + ".bak"))
                {
                    stSSConfig stC = new stSSConfig();
                    if (Utils.LoadInitFile("\\Documents and Settings\\SSCE" + "\\" + Utils.sFileNameInit + ".bak", ref stC))
                    {
                        if (File.Exists(sExecutePath + "\\" + Utils.sFileNameInit)) File.Delete(sExecutePath + "\\" + Utils.sFileNameInit);
                        File.Copy("\\Documents and Settings\\SSCE" + "\\" + Utils.sFileNameInit + ".bak", sExecutePath + "\\" + Utils.sFileNameInit);
                        sRet = sExecutePath + "\\" + Utils.sFileNameInit;
                        Utils.WriteDebugString(sExecutePath, " #FindConfigFile: config use - " + "\\Documents and Settings\\SSCE" + "\\" + Utils.sFileNameInit + ".bak");
                        return sRet;
                    }
                }

                return sRet;
            }
            catch (Exception ex)
            {
                Utils.WriteDebugString(sExecutePath, " #FindConfigFile: ERROR - " + ex.Message);
                return null;
            }
        }

        private void buttonTouchCalibrate_Click(object sender, EventArgs e) //Калибровка экрана
        {
            Utils.TouchScreenCalibrate();

            //Registry.RegFlushKey(Registry.HKLM);
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)     //Изменение яркости
        {
            //int result = Registry.CreateValueDWORD(Registry.HKCU, @"ControlPanel\BackLight", "ACBrightness", (uint)this.trackBar1.Value);
           //result = Registry.RegFlushKey(Registry.HKCU);
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            OnStop();

            System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
            myProcess.StartInfo.FileName = "\\Windows\\explorer.exe";
            myProcess.Start();

            Application.Exit();
        }

        private void panel1_DoubleClick(object sender, EventArgs e)
        {
            //this.buttonClose.Visible = true;
            this.panel1.BackColor = Color.Red;
            this.panel1.Refresh();

            OnStop();

            System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
            myProcess.StartInfo.FileName = "\\Windows\\explorer.exe";
            myProcess.Start();

            Application.Exit();
        }
    }
}