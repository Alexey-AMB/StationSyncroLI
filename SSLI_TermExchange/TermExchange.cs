using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Threading;

namespace SSCE
{
    public class TermExchange : SSCE.ClassAMBRenewedService
    {
        private const string sNameNDISCard = "USB80231";
        AMB_NDISNOTI.AMB_NDISNotification nn = null;
        T6.FileCopy.ClassFileCopy cfc = null;
        private bool bTermIsConnected = false;
        private int iDebugLevel = 2;
        private bool bAbort = false;
        private string sPathExecute = null;
        private string sPathToFullBase = null;
        private string sPathToNewSoftTerminal = null;
        private string sPathToUpdTerm = null;
        private string sPathToProtocol = null;

        private UInt32[] ardwIn = new UInt32[3];
        private UInt32[] ardwOut = new UInt32[3];

        private ArrayList arFoundTerm = new ArrayList();
        private ArrayList arUpdatedTerm = new ArrayList();

        private AMB_SPI.AMB_SPI spi = null;

        public override bool Init()
        {
            bool bRet = false;
            
            try
            {
                lock (Utils.oSyncroLoadSaveInit)
                {
                    sPathExecute = Utils.strConfig.sPathToExecute;
                    sPathToFullBase = Utils.strConfig.sPathToFullBase;
                    sPathToNewSoftTerminal = Utils.strConfig.sPathToNewSoftTerminal;
                    sPathToUpdTerm = Utils.strConfig.sPathToUpdTerm;
                    sPathToProtocol = Utils.strConfig.sPathToProtocol;
                }
                bRet = true;
            }
            catch (Exception ex)
            {
                WriteDebugString("Init:ERROR - " + ex.Message, 0);
                bRet = false;
            }

            try
            {
                WriteDebugString("---------------------------", 1);
                nn = new AMB_NDISNOTI.AMB_NDISNotification();
                AMB_NDISNOTI.AMB_NDISNotification.NewNotification += new AMB_NDISNOTI.AMB_NDISNotification.MyEv_NewNotifi(AMB_NDISNotification_NewNotification);
                if (nn.Init()) nn.Run();
                else
                {
                    WriteDebugString("Init:ERROR - Ошибка запуска контроля клиентов.", 0);
                    return false;
                }
            }
            catch (Exception ex)
            {
                WriteDebugString("Init.AMB_NDISNOTI:ERROR - " + ex.Message, 0);
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
                if (nn != null) nn.Dispose();
            }
            

            try
            {                
                cfc = new T6.FileCopy.ClassFileCopy();
            }
            catch (Exception ex)
            {
                WriteDebugString("Init.ClassFileCopy:ERROR - " + ex.Message, 0);
            }

            spi = new AMB_SPI.AMB_SPI();

            return bRet;
        }       

        public override void Start()
        {
            WriteDebugString("Start.Entrance:OK", 2);
            PicDisconnect();
            while (!bAbort)
            {
                Thread.Sleep(1000);
                try
                {
                    GetNextTerminal();
                }
                catch { }

            }
            WriteDebugString("Start.Exit:OK", 2);
            WriteDebugString("===========================", 1);
        }

        public override void Stop()
        {
            if (nn != null) nn.Dispose();
            bAbort = true;
            if (cfc != null) cfc.bAbortThread = true;
            Utils.DeleteMarkerDirBusy(sPathToUpdTerm);
            Utils.DeleteMarkerDirBusy(sPathToNewSoftTerminal);
            Utils.DeleteMarkerDirBusy(sPathToProtocol);
            Utils.DeleteMarkerDirBusy(sPathToFullBase);
            PicDisconnect();
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

        void AMB_NDISNotification_NewNotification(string sNameNDIS, bool bStatus)
        {
            if (sNameNDIS == sNameNDISCard)
            {
                bTermIsConnected = bStatus;
                if (!bStatus)
                {
                    if (cfc != null) cfc.bAbortThread = true;
                }
            }
        }
        /// <summary>
        /// Работаем с ПИКом. Если есть необслуженный терминал, подключаем его.
        /// </summary>
        private void GetNextTerminal()
        {
            int iCountWaitPIC = 10000;
            while (!PicRead())
            {
                Thread.Sleep(10);
                iCountWaitPIC--;
                if (iCountWaitPIC == 0)
                {
                    WriteDebugString("GetNextTerminal:ERROR - PIC not responded after 100 sec.", 1);
                    return;
                }
            }
            //Есть ли в очереди?
            //byte bNum = 0;  //порядковый номер начиная с 1    !!!! не 00000100
            for (byte bNum = 0; bNum < 5; bNum++)
            {
                if (Utils.cCurrStatus.arstTermInDock[bNum].bIsPresented)
                {
                    if (Utils.cCurrStatus.arstTermInDock[bNum].iServeStatus <= 0)
                    {
                        //если есть - Пику - включить.
                        WriteDebugString("GetNextTerminal:OK found terminal in dock №" + ((byte)(bNum + 1)).ToString(), 3);
                        Thread.Sleep(10);
                        PicConnect(bNum);
                        int iCount = 200;   //было 1000
                        while (!bTermIsConnected)
                        {
                            Thread.Sleep(10);
                            iCount--;       //ждем 10 секунд пока подключится
                            if (iCount == 0)
                            {
                                Utils.cCurrStatus.arstTermInDock[bNum].iColorLabelStatus = -1;
                                Utils.cCurrStatus.arstTermInDock[bNum].sCurrStatus = "НЕТ СОЕДИНЕНИЯ";
                                break;
                            }
                        }
                        if (bTermIsConnected)
                        {
                            Thread.Sleep(1000);     //ждем пока запустится сервер на терминале
                            WorkWithTerminal(bNum);
                        }
                        Thread.Sleep(10);
                        PicDisconnect();        //Пику - отключить все.
                    }
                }
            }
            
        }
        /// <summary>
        /// Работаем с подключенным по RNDIS терминалом.
        /// </summary>
        private void WorkWithTerminal(int iNumInDock)
        {
            if (cfc == null)
            {
                cfc = new T6.FileCopy.ClassFileCopy();
            }

            cfc.Init(iNumInDock);

            bool bRet = cfc.LoadDeviceInfo();

            if (cfc != null) cfc.bAbortThread = true;
            Thread.Sleep(500);
            cfc = null;
            if (bRet) Utils.cCurrStatus.UpdateTermInfo();
        }
        /// <summary>
        /// Читаем три байта с SPI и обрабатываем.
        /// </summary>
        /// <returns>TRUE если CRC = OK.</returns>
        private bool PicRead()
        {
            ardwOut[0] = 0;
            if (!SPIExchange()) return false;

            //пока заглушка ========
            //ardwIn[0] что сейчас в подставке: если есть, то в бите 1
            //ardwIn[1] что было изменено с последнего чтения: было изменение в бите 1
            //ardwIn[2] контрольная суммк двух первых байт
            //======================

            if ((ardwIn[0] + ardwIn[1]) == ardwIn[2])   //если совпала контрольная сумма
            {
                byte bMask = 1;
                ardwIn[0] = ardwIn[0] & 31; //только первые пять бит
                for (int i = 0; i < 5; i++)
                {
                    if ((ardwIn[0] & (bMask << i)) == (bMask << i))
                    {
                        Utils.cCurrStatus.arstTermInDock[i].bIsPresented = true;
                        if(Utils.cCurrStatus.arstTermInDock[i].iServeStatus < 1) Utils.cCurrStatus.arstTermInDock[i].sCurrStatus = "ЖДУ ОЧЕРЕДИ";
                    }
                    else
                    {
                        Utils.cCurrStatus.arstTermInDock[i].bIsPresented = false;
                        Utils.cCurrStatus.arstTermInDock[i].iColorLabelStatus = 0;
                        Utils.cCurrStatus.arstTermInDock[i].iServeStatus = 0;
                        Utils.cCurrStatus.arstTermInDock[i].sCurrStatus = "НЕ ОБРАБОТАН";
                        Utils.cCurrStatus.arstTermInDock[i].sIDTerminal = "";
                        Utils.cCurrStatus.arstTermInDock[i].sNameTerminal = "ПУСТО";
                    }

                    if ((ardwIn[1] & (bMask << i)) == (bMask << i))
                    {
                        Utils.cCurrStatus.arstTermInDock[i].iServeStatus = 0;
                        Utils.cCurrStatus.arstTermInDock[i].iColorLabelStatus = 0;
                        Utils.cCurrStatus.arstTermInDock[i].sCurrStatus = "НЕ ОБРАБОТАН";
                        if (Utils.cCurrStatus.arstTermInDock[i].bIsPresented) Utils.cCurrStatus.arstTermInDock[i].sCurrStatus = "ЖДУ ОЧЕРЕДИ";
                        Utils.cCurrStatus.arstTermInDock[i].sIDTerminal = "";
                        Utils.cCurrStatus.arstTermInDock[i].sNameTerminal = "ИЗМЕНЁН";
                    }
                }
            }
            else
            {
                WriteDebugString("ReadPic:ERROR CRC", 1);

                UInt32[] ardwInErrCrc = new UInt32[1];
                UInt32[] ardwOutErrCrc = new UInt32[1];
                ardwOutErrCrc[0] = 0;
                bool bRet = false;
                try
                {
                    bRet = spi.Init(8, 100);
                    if (bRet)
                    {
                        bRet = spi.GetSendBuffer(ref ardwInErrCrc, ref ardwOutErrCrc);
                    }
                    else WriteDebugString("SPIExchangeErrCrc.spi.Init:ERROR", 3);
                }
                catch (Exception ex)
                {
                    WriteDebugString("SPIExchangeErrCrc:ERROR - " + ex.Message, 1);
                    bRet = false;
                }
                finally
                {
                    spi.Close();
                }

                return false;
            }
            return true;
        }
        /// <summary>
        /// Подключаем указанный слот.
        /// </summary>
        /// <param name="bNumSlot">Номер слота начиная с 0.</param>
        private void PicConnect(byte bNumSlot)
        {
            UInt32 bBt = 1;
            ardwOut[0] = 0;
            for (int i = 0; i < (bNumSlot + 1); i++) ardwOut[0] = bBt << i;
            ardwOut[0] = ardwOut[0] | 128;
            SPIExchange();
        }
        /// <summary>
        /// Работаем с ПИКом. Отключаем все.
        /// </summary>
        private void PicDisconnect()
        {
            ardwOut[0] = 128; //b'10000000' - все выключить
            SPIExchange();
        }
        /// <summary>
        /// Обмен с ПИКом по SPI. На момент обмена arbOut[1] должен быть выставлен правильно.
        /// </summary>
        /// <returns>TRUE - все ОК.</returns>
        private bool SPIExchange()
        {
            //всего передается 3 байта
            //информационный только первый байт
            //если 7 бит первого байта == 1 то биты с 0 по 4 будут выставлены на порту А
            //если == 0 то просто как команда чтения текущего статуса

            //ardwIn[0] что сейчас в подставке: если есть, то в бите 1
            //ardwIn[1] что было изменено с последнего чтения: было изменение в бите 1
            //ardwIn[2] контрольная сумма двух первых байт


            bool bRet = true;

            ardwOut[1] = 170;   //Просто так, что бы неслать одни нули.
            ardwOut[2] = ardwOut[0] + ardwOut[1];

            if (spi == null) return false;
            try
            {
                //;;;;;test
                bRet = spi.Init(8, 100); 

                if (bRet)
                {
                    bRet = spi.GetSendBuffer(ref ardwIn, ref ardwOut);
                }
                else WriteDebugString("SPIExchange.spi.Init:ERROR", 3);
                //;;;;;test
            }
            catch (Exception ex)
            {
                WriteDebugString("SPIExchange:ERROR - " + ex.Message, 1);
                bRet = false;
            }
            finally
            {
                spi.Close();
            }

            WriteDebugString("SPIExchange:" + bRet.ToString() + " to SPI: " + ardwOut[0].ToString() + " " + ardwOut[1].ToString() + " " + ardwOut[2].ToString() + " from SPI: " +
                ardwIn[0].ToString() + " " + ardwIn[1].ToString() + " " + ardwIn[2].ToString(), 4);

            //ardwIn[0] = 2;      //test only
            //ardwIn[1] = 2;
            //ardwIn[2] = 4;

            return bRet;
        }
    }
}
