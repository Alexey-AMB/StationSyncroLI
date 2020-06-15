using System;
using System.Runtime.InteropServices;

namespace SSLI
{
    enum UsartCommand
    {                       //длинна данных, описание
        CMD_NONE = 0,       //0, не используется  
        CMD_TEST,           //0, ответ "ANS_OK"
        CMD_CHRG_EN,        //0, включить зарядку
        CMD_CHRG_DIS,       //0, выключить зарядку
        CMD_USBA_EN,        //0, включить USB
        CMD_USBA_DIS,       //0, выключить USB
        CMD_SC_SLEEP,       //0, отправить в сон, если включен, если уже сон или выключен - ничего
        CMD_SC_RUN,         //0, включить если уже не включен
        CMD_SC_DOWN,        //0, полностью выключить модуль - долго - 8 сек
        CMD_SC_REBOOT,      //0, выключить и включить - долго - 40 сек
        CMD_LED_R_ON,       //0, зажечь красный
        CMD_LED_R_OFF,      //0, погасить красный
        CMD_LED_G_ON,       //0, зажечь зеленый
        CMD_LED_G_OFF,      //0, погасить зеленый
        CMD_SOFT_UPD_PROGRESS, //0, сообщение, что прогр. верх. уровня начала обновление баз
        CMD_SOFT_UPD_DONE,  //0, сообщение, что прогр. верх. уровня завершила обновление баз
        CMD_GET_SERNUM,     //0, запрос сер. номера, ответ типа "ANS_SERNUM"
        CMD_GET_ID,         //0, запрос уникального номера, ответ типа "ANS_ID"
        CMD_GET_AKKVOLT,    //0, запрос напряжения аккумулятора  ответ "ANS_AKKVOLT"
        CMD_GET_STATUS,     //0, запрос текущего статуса, ответ типа "ANS_STATUS"
        CMD_SET_SERNUM,     //2, передача серийного номера, для проверки считать
        CMD_SET_ID,         //16, передача уникального ID,  для проверки считать
        CMD_SET_IP,         //2, передача последнего байта IP адресов станции и терминала
        CMDRAS_GET_STATUS,  //0, запрос текущих статусов терминалов - только по USART1
        CMDRAS_SET_IP       //3, передача последнего байта IP адресов станции и терминала на указанный слот (слот, станция, терм) - только по USART1
    }


    enum UsartAnswer
    {
        ANS_NONE = 100, //0, не используется
        ANS_OK,         //0
        ANS_ERROR,      //0
        ANS_SERNUM,     //2
        ANS_ID,         //16
        ANS_AKKVOLT,    //4
        ANS_STATUS,     //25 = AnsStatus
        ANS_ARSTAT      //5*AnsStatus
    }

    [StructLayout(LayoutKind.Sequential, /*Size = 25,*/ Pack = 1)]
    public struct AnsStatus
    {
        [MarshalAs(UnmanagedType.U2)]
        public ushort SerNum;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public ushort[] sID;  //8*2
        [MarshalAs(UnmanagedType.U4)]
        public UInt32 uAkkmV; //in mV
        [MarshalAs(UnmanagedType.U1)]
        public byte SC_mode; //0-down, 1-sleep, 2-work
        [MarshalAs(UnmanagedType.U1)]
        public byte UpdateState; //0-not start, 1-in progress, 2-done
        [MarshalAs(UnmanagedType.U1)]
        public byte ChargeState; //0-none, 1-in progress
    }

    public static class WorkCom
    {
        /// <summary>
        /// Converts the buff to struct AnsStatus
        /// </summary>
        /// <returns>The buff to ans stat.</returns>
        /// <param name="buff">Принятый из порта буфер.</param>
        /// <param name="iStart">Оффсет в буфере, обычно 1.</param>
        /// <param name="coutnStruct">Ожидаемое количество структур, обычно 5.</param>
        public static AnsStatus[] ConvertBuffToAnsStat(byte[] buff, int iStart, int coutnStruct)
        {
            AnsStatus[] stret = new AnsStatus[coutnStruct];
            int an_size = Marshal.SizeOf(typeof(AnsStatus));
            IntPtr pt = IntPtr.Zero;
            for (int i = 0; i < coutnStruct; i++)
            {
                pt = Marshal.AllocHGlobal(an_size);
                Marshal.Copy(buff, iStart + i * an_size, pt, an_size);
                stret[i] = (AnsStatus)Marshal.PtrToStructure(pt, typeof(AnsStatus));
                Marshal.FreeHGlobal(pt);
            }
            return stret;
        }
        /// <summary>
        /// Возвращает ID терминала в виде строки
        /// </summary>
        /// <returns>The byte ar to identifier.</returns>
        /// <param name="arb">Массив из 8 двубайтовых элементов.</param>
        public static string ConvertByteArToID(UInt16[] arb)
        {
            string sRet = null;
            for (int i = 0; i < arb.Length; i++)
            {
                byte[] bytes = BitConverter.GetBytes(arb[i]);
                sRet += bytes[1].ToString("X2");
                sRet += bytes[0].ToString("X2");
            }
            return sRet;
        }
        /// <summary>
        /// Возвращает сериный номер терминала в виде строки типа 1234-03, где 1234-номер комплекта, а 03-номер терминала в комплекте
        /// </summary>
        /// <returns>The byte ar to name term.</returns>
        /// <param name="ui">Номер терминала в формате двубайтового числа.</param>
        public static string ConvertByteArToNameTerm(UInt16 ui)
        {
            string sRet = null;

            sRet = ui.ToString();
            sRet = sRet.Insert(sRet.Length - 1, "-0");
            return sRet;
        }
        /// <summary>
        /// Возвращает примерный уровень заряда аккумулятора в процентах
        /// </summary>
        /// <returns>The FA kk to percent.</returns>
        /// <param name="ua">Напряжение аккумулятора в миливольтах.</param>
        public static int ConvertFAkkToPercent(UInt32 ua)
        {
            if (ua < 3000) return 0;
            if (ua < 3600) return 10;
            if (ua < 3800) return 30;
            if (ua < 3900) return 50;
            if (ua < 4100) return 75;
            if (ua < 4300) return 90;
            return 100;
        }

        public static byte[] ConvertAnsStatToBuff(AnsStatus anss)
        {
            int size = Marshal.SizeOf(anss);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(anss, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }
    }
}