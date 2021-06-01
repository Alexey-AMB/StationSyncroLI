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
        //private const string sFullIpAddresMask = "172.16.223."; //??

        T6.FileCopy.ClassFileCopy cCFC = null;
        bool[] isFastCharge = new bool[5];

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

                    Utils.strConfig.sIdStation = Utils.GetDeviceID();
                    Utils.strConfig.sSerNumStation = Utils.GetDeviceSernum();
                    Utils.cCurrStatus.sDeviceSS_ID = Utils.strConfig.sIdStation;

                }
                else
                {
                    WriteDebugString("Init:COM.ERROR - Ошибка запуска контроля клиентов.", 0);
                    return false;
                }
            }
            else
            {
                WriteDebugString("Init:ERROR - Неполные данные настроек работы в реестре для COM.", 0);
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

            int iSlotNumber = 0;
            AnsStatus stPic = new AnsStatus();
            while (!bAbort)
            {
                //Thread.Sleep(1000);
                try
                {
                    if (GetStatus(iSlotNumber, ref stPic))
                    {
                        WorkWithSlot(iSlotNumber, ref stPic);
                        UpdateInfoTermInDock(iSlotNumber, ref stPic);
                    }

                    iSlotNumber++;
                    if (iSlotNumber > 4) iSlotNumber = 0;
                }
                catch (Exception ex)
                {
                    WriteDebugString("Start.MainLoop:ERROR - " + ex.Message, 2);
                }

            }
            WriteDebugString("Start.Exit:OK", 2);
            WriteDebugString("===========================", 1);
        }

        public override void Stop()
        {
            bAbort = true;
            if (cCFC != null) cCFC.bAbortThread = true;
            Utils.DeleteMarkerDirBusy(sPathToUpdTerm);
            Utils.DeleteMarkerDirBusy(sPathToNewSoftTerminal);
            Utils.DeleteMarkerDirBusy(sPathToProtocol);
            Utils.DeleteMarkerDirBusy(sPathToFullBase);
            ComPort.Close();
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

        private bool GetStatus(int iNumSlot, ref AnsStatus stPic)  // CMDRAS_GET_STATUS запрос от малины к станции
        {
            int iReadLenMessage = 0;
            bool bRet = false;

            Array.Clear(arbBuff, 0, arbBuff.Length);
            arbBuff[0] = (byte)iNumSlot;
            ComPort.CleanBytesReadPort();
            ComPort.SendMessage((byte)UsartCommand.CMDRAS_SLOT_PWRON, ref arbBuff, 1);
            ComPort.SendMessage((byte)UsartCommand.CMDRAS_GET_STATUS, ref arbBuff, 1);
            ComPort.GetMessage(ref arbBuff, ref iReadLenMessage);

            if (arbBuff[0] == (byte)UsartAnswer.ANS_STATUS)
            {
                stPic = WorkCom.ConvertBuffToAnsStat(arbBuff, 1);
                if (stPic.SerNum > 0)
                {
                    WriteDebugString("Found terminal №" + stPic.SerNum + " in slot " + iNumSlot.ToString(), 2);
                    WriteDebugString("SerNum = " + stPic.SerNum, 3);
                    WriteDebugString("V akk = " + stPic.uAkkmV, 3);
                    WriteDebugString("mode  = " + stPic.SC_mode, 3);
                    WriteDebugString("kv% akk = " + stPic.uAkkPrcnt, 3);
                    WriteDebugString("update = " + stPic.UpdateState, 3);
                    bRet = true;
                }
                else
                {
                    WriteDebugString("Slot " + iNumSlot.ToString() + " - terminal not found...", 2);
                    //WriteDebugString("GET_STATUS - ERROR", 0);
                    //ComPort.SendMessage((byte)UsartCommand.CMDRAS_SLOT_PWROFF, ref arbBuff, 1);
                    bRet = false;
                }
            }
            else
            {
                WriteDebugString("Slot " + iNumSlot.ToString() + " GET_STATUS - ERROR", 0);
                //ComPort.SendMessage((byte)UsartCommand.CMDRAS_SLOT_PWROFF, ref arbBuff, 1);
                bRet = false;
            }
            UpdateInfoTermInDock(iNumSlot, ref stPic);
            return bRet;
        }

        private void UpdateInfoTermInDock(int iNumSlot, ref AnsStatus stPic)
        {
                if ((stPic.SerNum == 0) && (stPic.uAkkmV == 0)) //слот пуст
                {
                    Utils.cCurrStatus.arstTermInDock[iNumSlot].bIsPresented = false;
                    Utils.cCurrStatus.arstTermInDock[iNumSlot].iColorLabelStatus = 0;
                    Utils.cCurrStatus.arstTermInDock[iNumSlot].iServeStatus = 0;
                    Utils.cCurrStatus.arstTermInDock[iNumSlot].sCurrStatus = "НЕ ОБРАБОТАН";
                    Utils.cCurrStatus.arstTermInDock[iNumSlot].sIDTerminal = "";
                    Utils.cCurrStatus.arstTermInDock[iNumSlot].sNameTerminal = "ПУСТО";
                    Utils.cCurrStatus.arstTermInDock[iNumSlot].iUpdateStatus = 0;
                    Utils.cCurrStatus.arstTermInDock[iNumSlot].iPercentAkk = 0;
                    Utils.cCurrStatus.arstTermInDock[iNumSlot].iChargeState = 0;
                }
                else
                {
                    if (WorkCom.ConvertByteArToID(stPic.sID) == Utils.cCurrStatus.arstTermInDock[iNumSlot].sIDTerminal)
                    {//тот же терминал
                        Utils.cCurrStatus.arstTermInDock[iNumSlot].bIsPresented = true;
                        Utils.cCurrStatus.arstTermInDock[iNumSlot].iPercentAkk = WorkCom.ConvertFAkkToPercent(stPic.uAkkmV);
                        Utils.cCurrStatus.arstTermInDock[iNumSlot].iChargeState = stPic.ChargeState;
                    }
                    else
                    {//другой
                        Utils.cCurrStatus.arstTermInDock[iNumSlot].bIsPresented = true;
                        Utils.cCurrStatus.arstTermInDock[iNumSlot].iColorLabelStatus = 0;
                        Utils.cCurrStatus.arstTermInDock[iNumSlot].iServeStatus = 0;
                        Utils.cCurrStatus.arstTermInDock[iNumSlot].sCurrStatus = "ИЗМЕНЕН";
                        Utils.cCurrStatus.arstTermInDock[iNumSlot].sIDTerminal = WorkCom.ConvertByteArToID(stPic.sID);
                        Utils.cCurrStatus.arstTermInDock[iNumSlot].sNameTerminal = WorkCom.ConvertByteArToNameTerm(stPic.SerNum);
                        Utils.cCurrStatus.arstTermInDock[iNumSlot].iUpdateStatus = 0;
                        Utils.cCurrStatus.arstTermInDock[iNumSlot].iPercentAkk = WorkCom.ConvertFAkkToPercent(stPic.uAkkmV);
                        Utils.cCurrStatus.arstTermInDock[iNumSlot].iChargeState = stPic.ChargeState;
                }
                }            
        }

        private void WorkWithSlot(int iNumSlot, ref AnsStatus stPic)
        {
            WriteDebugString("WorkWithSlot " + iNumSlot.ToString() + " ----------", 3);

            Array.Clear(arbBuff, 0, arbBuff.Length);
            arbBuff[0] = (byte)iNumSlot;
            ComPort.CleanBytesReadPort();

            if (stPic.uAkkmV > 3600)
            {
                if ((stPic.SC_mode != (byte)ScMode.WORK_MODE) && (!isFastCharge[iNumSlot]))
                {
                    ComPort.SendMessage((byte)UsartCommand.CMDRAS_SC_RUN, ref arbBuff, 1);
                    WriteDebugString("WorkWithSlot " + iNumSlot.ToString() + " включить!", 3);
                    Utils.cCurrStatus.arstTermInDock[iNumSlot].sCurrStatus = "Включить...";
                }
            }
            else
            {
                ComPort.SendMessage((byte)UsartCommand.CMDRAS_SLOT_PWRON, ref arbBuff, 1);
                WriteDebugString("WorkWithSlot " + iNumSlot.ToString() + " не заряжен.", 3);
                Utils.cCurrStatus.arstTermInDock[iNumSlot].sCurrStatus = "Не заряжен";
                return;
            }


            if (stPic.SC_mode != (byte)ScMode.WORK_MODE) return;

            switch (stPic.UpdateState)
            {
                case 0:
                    ComPort.CleanBytesReadPort();
                    //ComPort.SendMessage((byte)UsartCommand.CMDRAS_SLOT_PWROFF, ref arbBuff, 1);
                    //Thread.Sleep(5000);
                    ComPort.SendMessage((byte)UsartCommand.CMDRAS_CHRG_EN, ref arbBuff, 1);
                    ComPort.SendMessage((byte)UsartCommand.CMDRAS_USBOE_EN, ref arbBuff, 1);
                    ComPort.SendMessage((byte)UsartCommand.CMDRAS_LED_R_ON, ref arbBuff, 1);
                    ComPort.SendMessage((byte)UsartCommand.CMDRAS_LED_G_OFF, ref arbBuff, 1);
                    //TODO: зажигать и гасить лампочки в соответствии со stPic.uAkkPrcnt!
                    ComPort.SendMessage((byte)UsartCommand.CMDRAS_SLOT_PWRON, ref arbBuff, 1);

                    ComPort.SendMessage((byte)UsartCommand.CMDRAS_GET_AKKPRCNT, ref arbBuff, 1);
                    ComPort.SendMessage((byte)UsartCommand.CMDRAS_SET_UPDSTRT, ref arbBuff, 1);
                    Thread.Sleep(10000);
                    isFastCharge[iNumSlot] = false;
                    //TODO:  запускать передачу и уходить на круг неправильно! Надо ждать. 
                    WriteDebugString("WorkWithSlot " + iNumSlot.ToString() + " включить прием RNDIS.", 3);
                    Utils.cCurrStatus.arstTermInDock[iNumSlot].sCurrStatus = "Соединение...";
                    break;
                case 1:
                    WriteDebugString("WorkWithSlot " + iNumSlot.ToString() + " начало работы с RNDIS.", 3);

                    WorkWithTerminal(iNumSlot); // <--------

                    WriteDebugString("WorkWithSlot " + iNumSlot.ToString() + " завершено RNDIS.", 3);
                    break;
                case 2:
                    Utils.cCurrStatus.arstTermInDock[iNumSlot].sCurrStatus = "Обработка...";
                    break;
                case 3:
                    if (!isFastCharge[iNumSlot])
                    {
                        ComPort.CleanBytesReadPort();
                        ComPort.SendMessage((byte)UsartCommand.CMDRAS_SLOT_PWROFF, ref arbBuff, 1);
                        Thread.Sleep(10000);
                        ComPort.SendMessage((byte)UsartCommand.CMDRAS_CHRG_EN, ref arbBuff, 1);
                        ComPort.SendMessage((byte)UsartCommand.CMDRAS_USBOE_DIS, ref arbBuff, 1);
                        ComPort.SendMessage((byte)UsartCommand.CMDRAS_LED_R_ON, ref arbBuff, 1);
                        ComPort.SendMessage((byte)UsartCommand.CMDRAS_LED_G_OFF, ref arbBuff, 1);
                        //TODO: зажигать и гасить лампочки в соответствии со stPic.uAkkPrcnt!
                        ComPort.SendMessage((byte)UsartCommand.CMDRAS_SLOT_PWRON, ref arbBuff, 1);
                        isFastCharge[iNumSlot] = true;
                        WriteDebugString("WorkWithSlot " + iNumSlot.ToString() + " включить FAST_CHARGE.", 3);
                        Utils.cCurrStatus.arstTermInDock[iNumSlot].sCurrStatus = "Заряжается - " +
                            WorkCom.ConvertFAkkToPercent(stPic.uAkkmV) + "%";
                    }
                    else
                    {
                        if (stPic.uAkkPrcnt != 222)
                        {
                            ComPort.SendMessage((byte)UsartCommand.CMDRAS_GET_AKKPRCNT, ref arbBuff, 1);
                            if (stPic.uAkkPrcnt == 201)
                            {
                                ComPort.SendMessage((byte)UsartCommand.CMDRAS_LED_R_OFF, ref arbBuff, 1);
                                ComPort.SendMessage((byte)UsartCommand.CMDRAS_LED_G_ON, ref arbBuff, 1);
                                Utils.cCurrStatus.arstTermInDock[iNumSlot].sCurrStatus = "Готов";
                                break;
                            }
                            //TODO: зажигать и гасить лампочки в соответствии со stPic.uAkkPrcnt!
                        }
                        Utils.cCurrStatus.arstTermInDock[iNumSlot].sCurrStatus = "Заряжается - " +
                            WorkCom.ConvertFAkkToPercent(stPic.uAkkmV) + "%";
                    }
                    break;
            }
        }

        /// <summary>
        /// Работаем с подключенным по RNDIS терминалом.
        /// </summary>
        private void WorkWithTerminal(int iNumInDock)
        {
            if (cCFC == null)
            {
                cCFC = new T6.FileCopy.ClassFileCopy();
            }

            cCFC.Init(iNumInDock);

            bool bRet = cCFC.LoadDeviceInfo();

            if (cCFC != null) cCFC.bAbortThread = true;
            Thread.Sleep(500);
            cCFC = null;
            if (bRet) Utils.cCurrStatus.UpdateTermInfo();
        }

    }
}
