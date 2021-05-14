using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace SSLI
{
    public class TermExchange : SSLI.ClassAMBRenewedService
    {
        private string sPd = System.IO.Path.DirectorySeparatorChar.ToString();
        private const string sFullIpAddresMask = "172.16.223."; //??

        Thread tThredsCfc = null;
        T6.FileCopy.ClassFileCopy cCfc = new T6.FileCopy.ClassFileCopy();

        private int iDebugLevel = 2;
        private bool bAbort = false;
        private string sPathExecute = null;
        private string sPathToFullBase = null;
        private string sPathToNewSoftTerminal = null;
        private string sPathToUpdTerm = null;
        private string sPathToProtocol = null;

        private byte[] arbBuff;

        public override bool Init()
        {
            bool bRet = false;
            
            try
            {
                lock (Utils.oSyncroLoadSaveInit)
                {
                    sPathExecute = Utils.strConfig.sPathToExecute;
                    sPathToFullBase = Utils.sFolderNameMain + sPd + Utils.strConfig.sPathToFullBase;
                    sPathToNewSoftTerminal = Utils.sFolderNameMain + sPd + Utils.strConfig.sPathToNewSoftTerminal;
                    sPathToUpdTerm = Utils.sFolderNameMain + sPd + Utils.strConfig.sPathToUpdTerm;
                    sPathToProtocol = Utils.sFolderNameMain + sPd + Utils.strConfig.sPathToProtocol;
                }
                bRet = true;
            }
            catch (Exception ex)
            {
                WriteDebugString("Init:ERROR - " + ex.Message, 0);
                bRet = false;
            }

            WriteDebugString("---------------------------", 1);
            if ((Utils.strConfig.strTermExch.sComPortName!=null)&&(Utils.strConfig.strTermExch.iComPortSpeed !=0))
            {
                if(ComPort.Init(Utils.strConfig.strTermExch.sComPortName, Utils.strConfig.strTermExch.iComPortSpeed))
                {
                    WriteDebugString("Init:COM.OK - Запуск контроля клиентов.", 2);
                }
                else
                {
                    WriteDebugString("Init:COM.ERROR - Ошибка запуска контроля клиентов.", 0);
                    return false;
                }
            }
            else
            {
                WriteDebugString("Init:ERROR - Неполные данные настроек работы в реестре.", 0);
                return false;
            }

            if ((sPathExecute != null) && (sPathToFullBase != null) && (sPathToNewSoftTerminal != null) && (sPathToUpdTerm != null) &&
                    (sPathToProtocol != null))
            {
                WriteDebugString("Init:OK", 2);
                bRet = true;
            }
            else
            {
                WriteDebugString("Init:ERROR - Неполные данные настроек работы в реестре.", 0);
                bRet = false;
            }

            return bRet;
        }

        public override void Start()
        {
            arbBuff = new byte[300];
            int iReadLenMessage = 0;
            AnsStatus stFromPic;

            WriteDebugString("Start.Entrance:OK", 2);

            Array.Clear(arbBuff, 0, arbBuff.Length);
            ComPort.CleanBytesReadPort();
            ComPort.SendMessage((byte)UsartCommand.CMD_TEST, ref arbBuff, 0);
            ComPort.GetMessage(ref arbBuff, ref iReadLenMessage);
            if (iReadLenMessage > 0)
            {
                if (arbBuff[0] == (byte)UsartAnswer.ANS_OK)
                {
                    WriteDebugString("Start.COM:OK - pic answer ANS_OK", 2);
                }
            }
            else
            {
                WriteDebugString("Start.COM:ERROR - pic NOT answer ANS_OK", 0);
                bAbort = true;
            }

            while (!bAbort)
            {
                Thread.Sleep(1000);
                try
                {
                    //GetNextTerminal();

                    Array.Clear(arbBuff, 0, arbBuff.Length);
                    ComPort.CleanBytesReadPort();
                    //send com get_status
                    ComPort.SendMessage((byte)UsartCommand.CMDRAS_GET_STATUS, ref arbBuff, 0); //надо опрашивать по очереди!!!
                    //wait arStatus
                    ComPort.GetMessage(ref arbBuff, ref iReadLenMessage);
                    if (iReadLenMessage == 0) continue; //такого быть не должно. Это ошибка!

                    //update infopanel
                    if (arbBuff[0] == (byte)UsartAnswer.ANS_STATUS)
                    {
                        stFromPic = WorkCom.ConvertBuffToAnsStat(arbBuff, 1);
                    }
                    else continue;

                    //UpdateInfoTermInDock(stFromPic);

                    // test_only!!! ConnectNewTermIndock(); .. обновить файл mycommands !!!
                }
                catch { }

            }
            WriteDebugString("Start.Exit:OK", 2);
            WriteDebugString("===========================", 1);
        }

        public override void Stop()
        {
            ComPort.Close();
            bAbort = true;
            foreach(T6.FileCopy.ClassFileCopy cfc in arCfc) if (cfc != null) cfc.bAbortThread = true;
            Utils.DeleteMarkerDirBusy(sPathToUpdTerm);
            Utils.DeleteMarkerDirBusy(sPathToNewSoftTerminal);
            Utils.DeleteMarkerDirBusy(sPathToProtocol);
            Utils.DeleteMarkerDirBusy(sPathToFullBase);

            WriteDebugString("Stop:OK", 1);
        }

        private bool WriteDebugString(string strWr, int iLevelDebud)
        {
            bool bRet = true;
            if (iLevelDebud <= iDebugLevel)
            {
                try
                {
                    bRet = Utils.WriteDebugString(sPathExecute, " -TermExchange- " + strWr);
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

        private void UpdateInfoTermInDock(AnsStatus[] arSt)
        {
            for(int i=0;i<5;i++)
            {
                if ((arSt[i].SerNum == 0) && (arSt[i].uAkkmV == 0)) //слот пуст
                {
                    Utils.cCurrStatus.arstTermInDock[i].bIsPresented = false;
                    Utils.cCurrStatus.arstTermInDock[i].iColorLabelStatus = 0;
                    Utils.cCurrStatus.arstTermInDock[i].iServeStatus = 0;
                    Utils.cCurrStatus.arstTermInDock[i].sCurrStatus = "НЕ ОБРАБОТАН";
                    Utils.cCurrStatus.arstTermInDock[i].sIDTerminal = "";
                    Utils.cCurrStatus.arstTermInDock[i].sNameTerminal = "ПУСТО";
                    Utils.cCurrStatus.arstTermInDock[i].iConectStatus = 0;
                    Utils.cCurrStatus.arstTermInDock[i].iIPaddrClient = (byte)(i + 1);
                    Utils.cCurrStatus.arstTermInDock[i].iIPaddrServer = (byte)(i + 101);
                    Utils.cCurrStatus.arstTermInDock[i].iPercentAkk = 0;
                    Utils.cCurrStatus.arstTermInDock[i].iChargeState = 0;
                }
                else
                {
                    if (WorkCom.ConvertByteArToID(arSt[i].sID) == Utils.cCurrStatus.arstTermInDock[i].sIDTerminal)
                    {//тот же терминал
                        Utils.cCurrStatus.arstTermInDock[i].bIsPresented = true;
                        Utils.cCurrStatus.arstTermInDock[i].iPercentAkk = WorkCom.ConvertFAkkToPercent(arSt[i].uAkkmV);
                        Utils.cCurrStatus.arstTermInDock[i].iChargeState = arSt[i].ChargeState;
                    }
                    else
                    {//другой
                        Utils.cCurrStatus.arstTermInDock[i].bIsPresented = true;
                        Utils.cCurrStatus.arstTermInDock[i].iColorLabelStatus = 0;
                        Utils.cCurrStatus.arstTermInDock[i].iServeStatus = 0;
                        Utils.cCurrStatus.arstTermInDock[i].sCurrStatus = "ИЗМЕНЕН";
                        Utils.cCurrStatus.arstTermInDock[i].sIDTerminal = WorkCom.ConvertByteArToID(arSt[i].sID);
                        Utils.cCurrStatus.arstTermInDock[i].sNameTerminal = WorkCom.ConvertByteArToNameTerm(arSt[i].SerNum);
                        Utils.cCurrStatus.arstTermInDock[i].iConectStatus = 0;
                        Utils.cCurrStatus.arstTermInDock[i].iIPaddrClient = (byte)(i + 1);
                        Utils.cCurrStatus.arstTermInDock[i].iIPaddrServer = (byte)(i + 100);
                        Utils.cCurrStatus.arstTermInDock[i].iPercentAkk = WorkCom.ConvertFAkkToPercent(arSt[i].uAkkmV);
                        Utils.cCurrStatus.arstTermInDock[i].iChargeState = arSt[i].ChargeState;
                    }
                }
            }
        }

        private void ConnectNewTermIndock()
        {
            string sNameNewIface = null;
            for (int i = 0; i < 5; i++)
            {
                if (Utils.cCurrStatus.arstTermInDock[i].bIsPresented)
                {
                    if (Utils.cCurrStatus.arstTermInDock[i].iSC_mode < 1) continue; //модуль выключен. Должен влючиться сам если акк заряжен.
                    if (Utils.cCurrStatus.arstTermInDock[i].iServeStatus <= 0)
                    {
                        WriteDebugString("ConnectNewTermIndock:OK found terminal " +
                            Utils.cCurrStatus.arstTermInDock[i].sIDTerminal + " in dock №" + i.ToString(), 3);
                        if (Utils.cCurrStatus.arstTermInDock[i].iConectStatus == 0)
                        {
                            ArrayList arlOldIface = new ArrayList();
                            DirectoryInfo diNet = new DirectoryInfo("/sys/class/net");
                            DirectoryInfo[] ardi = diNet.GetDirectories("USB*");
                            foreach (DirectoryInfo d in ardi) arlOldIface.Add(d.Name);
                            //получили список всех сетевых интерфейсов ДО подключения нового терминала
                            Array.Clear(arbBuff, 0, arbBuff.Length);
                            //ComPort.SendMessage((byte)UsartCommand.CMD_USBA_DIS, ref arbBuff, 0);
                            arbBuff[0] = (byte)i;//slot number
                            arbBuff[1] = Utils.cCurrStatus.arstTermInDock[i].iIPaddrClient; //i.e. 1 (if i=1)
                            arbBuff[2] = Utils.cCurrStatus.arstTermInDock[i].iIPaddrServer; //i.e. 101 (if i=1)
                            ComPort.SendMessage((byte)UsartCommand.CMDRAS_SET_IP, ref arbBuff, 3);

                            sNameNewIface = null;
                            for (int n = 0; n < 20; n++)
                            {
                                ardi = diNet.GetDirectories("USB*");
                                if (ardi.Length > arlOldIface.Count)
                                {//появился новый интерфейс
                                    foreach (DirectoryInfo dn in ardi)
                                    {
                                        if (!arlOldIface.Contains(dn.Name))
                                        {
                                            sNameNewIface = dn.Name;
                                            break;
                                        }
                                    }
                                    break;
                                }
                                Thread.Sleep(500);
                            }
                            // ip up
                            if (sNameNewIface != null)
                            {
                                NetInterfaceUp(sNameNewIface, Utils.cCurrStatus.arstTermInDock[i].iIPaddrClient);
                                Thread.Sleep(1000);     //ждем пока запустится сервер на терминале
                                WorkWithTerminal(i);
                            }
                            else
                            {
                                Utils.cCurrStatus.arstTermInDock[i].iColorLabelStatus = -1;
                                Utils.cCurrStatus.arstTermInDock[i].sCurrStatus = "НЕТ СОЕДИНЕНИЯ";
                                                        
                                WriteDebugString("ConnectNewTermIndock:ERROR not found net interface in dock №" + i.ToString(), 2);
                            }
                        }
                    }
                }
            }
        }

        private void NetInterfaceUp(string sNameIface, int iIpClient) //станция это клиент, сервер на терминале!
        {
            string sShellCommand = "-c ", sOut = "";

            Process proc = new Process();
            proc.StartInfo.FileName = "/bin/bash";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;

            sShellCommand += "ifconfig " + sNameIface + " " + sFullIpAddresMask + iIpClient.ToString() +
                "netmask 255.255.255.0 up";

            proc.StartInfo.Arguments = sShellCommand;
            proc.Start();

            //while (!proc.StandardOutput.EndOfStream)
            //{
            //    sOut += proc.StandardOutput.ReadToEnd();
            //}
        }

        /// <summary>
        /// Работаем с подключенным по RNDIS терминалом.
        /// </summary>
        private void WorkWithTerminal(int iNumInDock)
        {
            if (arCfc[iNumInDock] != null)
            {
                arCfc[iNumInDock].bAbortThread = true;
                Thread.Sleep(500);
            }

            if(arThredsCfc[iNumInDock] != null)
            {
                arThredsCfc[iNumInDock].Abort();
                Thread.Sleep(500);
            }

            arCfc[iNumInDock] = new T6.FileCopy.ClassFileCopy();
            arThredsCfc[iNumInDock] = new Thread(new ThreadStart(arCfc[iNumInDock].LoadDeviceInfoTh));

            arCfc[iNumInDock].Init(iNumInDock);
            arThredsCfc[iNumInDock].Start();

            //bool bRet = arCfc[iNumInDock].LoadDeviceInfo();

            //if (arCfc[iNumInDock] != null) arCfc[iNumInDock].bAbortThread = true;
            //Thread.Sleep(500);
            //arCfc[iNumInDock] = null;
            ////if (bRet) 
            //Utils.cCurrStatus.UpdateTermInfo();
        }

    }
}
