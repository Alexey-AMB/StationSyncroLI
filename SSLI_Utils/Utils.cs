using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections;
using Ionic.Zip;
using System.IO.Ports;
using System.Threading;

namespace SSLI
{
    using DWORD = System.UInt32;
    public class ClassAMBRenewedService
    {
        public virtual bool Init()
        {
            return true;
        }
        public virtual void Start()
        {

        }
        public virtual void Stop()
        {

        }
    }
    public class MEMORYSTATUS
    {
        public MEMORYSTATUS()
        {
            dwLength = (uint)Marshal.SizeOf(this);
        }
        public uint dwLength;
        public uint dwMemoryLoad;
        public uint dwTotalPhys;
        public uint dwAvailPhys;
        public uint dwTotalPageFile;
        public uint dwAvailPageFile;
        public uint dwTotalVirtual;
        public uint dwAvailVirtual;
    }
    #region Структуры
    /// <summary>
    /// Описание состояния терминала
    /// </summary>
    public struct stIni
    {
        public int iVersionIniFile;
        public string Copyright;
        public string sNameTerminal;
        public bool bAutoLoadUpdates;
        public bool bCheckAdminPass;
        public string sAdminPasswrd;
        public string sHashCode;
        public string sDeviceID;
        public int iShowFoundInBase;
        public string sLastSyncronisation;

        public stVersions stVersions;
        public stPaths stPaths;
        public stBlueT stBlueT;
        public int iCountBases;
        public stOneBase[] arBases;
    }
    public struct stPaths
    {
        public string sPathToBase;
        public string sPathExecute;
        public string sPathUpdates;
        public string sPathNewSoft;
        public string sPathStorageFiles;
    }
    public struct stVersions
    {
        /// <summary>
        /// Версия запускающей программы
        /// </summary>
        public string sVersionStarter;
        /// <summary>
        /// Версия основной программы терминала
        /// </summary>
        public string sVersionTerminal;
    }
    public struct stBlueT
    {
        public bool bBlueToothSupported;
        public bool bUseBlueTooth;
        public string sLocalAdrBT;
        public string sLocalNameBT;
        public string sServerAdrBT;
        public string sServerNameBT;
        public string sServiceGUIDBT;
        public string sPassBT;
    }
    public struct stOneBase
    {
        /// <summary>
        /// тип базы (кодовое имя)
        /// </summary>
        public string sTypeBase;
        /// <summary>
        /// имя файла базы
        /// </summary>
        public string sNameFileBase;			//
        /// <summary>
        /// имя таблицы
        /// </summary>
        public string sNameTable;		//
        /// <summary>
        /// комментарий
        /// </summary>
        public string sComment;			//
        /// <summary>
        /// имя кнопки
        /// </summary>
        public string sNameButton;
        /// <summary>
        /// первые буквы файла обновления
        /// </summary>
        public string sPrefUpdates;		//
        /// <summary>
        /// количество букв префикса файла обновления
        /// </summary>
        public int iNumCharPref;		//
        /// <summary>
        /// расширение файла обновления
        /// </summary>
        public string sExtUpdates;		//
        /// <summary>
        /// есть ли такая база
        /// </summary>
        public bool bIsBase;			//
        /// <summary>
        /// проверять по этой базе?
        /// </summary>
        public bool bCheckBase;		//
        /// <summary>
        /// имя последнего обновления
        /// </summary>
        public string sNameLastUpdates;		//
        /// <summary>
        /// дата последнего обновления в формате "01.02.2008"
        /// </summary>
        public string sDateLastUpdates;
        /// <summary>
        /// версия данного файла
        /// </summary>
        public string sVersion;		//
        /// <summary>
        /// рекомендуемый порядковый номер кнопки
        /// </summary>
        public int iPriority;			//
        /// <summary>
        /// Тип обновления базы "UPDATE" - обновлениями или "FULL" - полная заливка
        /// </summary>
        public string sTypeUpdate;
        /// <summary>
        /// тип загрузки формы - целиком "FORM", Loadable "XML", нет видимой кнопки "NONE"
        /// </summary>
        public string sTypeForm;		//
        /// <summary>
        /// ассемблерное имя файла DLL
        /// </summary>
        public string sNameAsmDll;		//
        /// <summary>
        /// файловое имя файла DLL
        /// </summary>
        public string sNameFileDll;		//
        /// <summary>
        /// текущая версия файла DLL
        /// </summary>
        public string sVerFileDll;		//
        /// <summary>
        /// имя файла XML если форма типа Loadable или нуль
        /// </summary>
        public string sNameFileLoadXML;	//
        /// <summary>
        /// текущая версия файла XML
        /// </summary>
        public string sVerFileLoadXML;	//
    }
    public struct stOneTermStatistic
    {
        /// <summary>
        /// ID станции сформировавшей статистику
        /// </summary>
        public string sPodrazdelenieID;
        /// <summary>
        /// имя подразделения
        /// </summary>
        public string sNamePodrazdelenie;
        /// <summary>
        /// имя терминала
        /// </summary>
        public string sNameTerminal;
        /// <summary>
        /// уникальный номер терминала
        /// </summary>
        public string sDeviceID;
        /// <summary>
        /// Последнее время синхронизации
        /// </summary>
        public string sDateTimeLastSyncronisation;
        /// <summary>
        /// количество баз общее
        /// </summary>
        public int iCountBases;
        /// <summary>
        /// количество записей проверок
        /// </summary>
        public int iCountSearch;
        /// <summary>
        /// версии основных программ
        /// </summary>
        public stVersions stVersions;
        /// <summary>
        /// массив записей проверок
        /// </summary>
        public stOneSearch[] arSearch;
        /// <summary>
        /// информация по базам
        /// </summary>
        public stOneBaseStat[] arBaseStat;
    }
    public struct stOneSearch
    {
        /// <summary>
        /// Комментарий базы
        /// </summary>
        public string sComment;
        /// <summary>
        /// дата_время
        /// </summary>
        public string sDateTime;
        /// <summary>
        /// задержан 
        /// </summary>
        public bool bResult;
        /// <summary>
        /// параметры_поиска
        /// </summary>
        public string sParamSearch;
    }
    public struct stOneBaseStat
    {
        /// <summary>
        /// тип базы (кодовое имя)
        /// </summary>
        public string sTypeBase;
        /// <summary>
        /// комментарий
        /// </summary>
        public string sComment;
        /// <summary>
        /// есть ли такая база
        /// </summary>
        public bool bIsBase;
        /// <summary>
        /// проверять по этой базе?
        /// </summary>
        public bool bCheckBase;
        /// <summary>
        /// Актуальность последнего обновления
        /// </summary>
        public string sDateLastUpdates;
        /// <summary>
        /// текущая версия файла DLL
        /// </summary>
        public string sVerFileDll;
    }
    public struct stOneBaseFull
    {
        /// <summary>
        /// тип базы (кодовое имя)
        /// </summary>
        public string sTypeBase;
        /// <summary>
        /// комментарий
        /// </summary>
        public string sComment;
        /// <summary>
        /// дата создания
        /// </summary>
        public string sCreateDate;
        /// <summary>
        /// имя файла базы
        /// </summary>
        public string sNameFileBase;
        /// <summary>
        /// имя файла описания базы
        /// </summary>
        public string sNameFileXML;
        /// <summary>
        /// откуда можно скачать файл (полный путь без имени файла)
        /// </summary>
        public string sPathToDownload;
        /// <summary>
        /// имя сжатого файла (база + файл описания)
        /// </summary>
        public string sNameFileZipBase;
        /// <summary>
        /// откуда можно скачать сжатый файл (полный путь без имени файла)
        /// </summary>
        public string sPathToDownloadZip;
    }

    /// <summary>
    /// Инициализация и параметры станции
    /// </summary>
    public struct stSSConfig
    {
        /// <summary>
        /// Имя подразделения
        /// </summary>
        public string sNamePodrazdelenie;
        /// <summary>
        /// Уровень отладки 2 - все сообщения, 1 - важные, 0 - критические
        /// </summary>
        public int iDebugLevel;

        /// <summary>
        /// Текст последней критической ошибки
        /// </summary>
        public string sTextLastCriticalError;
        /// <summary>
        /// Время последней критической ошибки
        /// </summary>
        public string sTimeLastCriticalError;

        /// <summary>
        /// Путь к исполняемым файлам
        /// </summary>
        public string sPathToExecute;
        /// <summary>
        /// Путь к розыскным базам
        /// </summary>
        public string sPathToFullBase;
        /// <summary>
        /// Путь к входящим файлам (Почта)
        /// </summary>
        public string sPathToInBox;
        /// <summary>
        /// Путь к обновлениям софта станции
        /// </summary>
        public string sPathToNewSoftSS;
        /// <summary>
        /// Путь к обновлениям софта терминалов
        /// </summary>
        public string sPathToNewSoftTerminal;
        /// <summary>
        /// Путь к файлам на отправку (Почта)
        /// </summary>
        public string sPathToOutBox;
        /// <summary>
        /// Путь к файлам конфигурации терминалов, когда-либо подключавшихся к станции
        /// </summary>
        public string sPathToProtocol;
        /// <summary>
        /// Путь к обновлениям баз
        /// </summary>
        public string sPathToUpdTerm;
        /// <summary>
        /// Путь по которому подключается USB флэшка.
        /// </summary>
        public string sPathToRemovableFlash;
        /// <summary>
        /// Версия ПО.
        /// </summary>
        public string sCurrentVersionSS;
        /// <summary>
        /// Текущий тип ПО станции. "SSLI".
        /// </summary>
        public string sCurrentTypeSS;

        /// <summary>
        /// Список баз с которыми должна работать станция
        /// </summary>
        public string[] arsSupportedBases;

        /// <summary>
        /// Список задач станции
        /// </summary>
        public stSSTasks[] arstTasks;
        /// <summary>
        /// Конфигурация почты
        /// </summary>
        public stSSMail strMail;
        /// <summary>
        /// Конфигурация статистики
        /// </summary>
        public stSSStatistic strStatistic;
        /// <summary>
        /// Конфигурация задачи получения файлов
        /// </summary>
        public stSSGetFiles strGetFiles;
        /// <summary>
        /// Конфигурация WEB-сервера
        /// </summary>
        public stSSWEBServer strWebServer;
        /// <summary>
        /// Конфигурация сетевых настроек станции
        /// </summary>
        public stSSNet strNet;
        /// <summary>
        /// Конфигурация для обмена с терминалами в подставке.
        /// </summary>
        public stSSTermExchange strTermExch;

        /// <summary>
        /// Пароль администратора станции
        /// </summary>
        public string sPassword;
    }
    public struct stSSTasks
    {
        /// <summary>
        /// Порядковый номер запуска задачи
        /// </summary>
        public int iNumStart;
        /// <summary>
        /// Имя задачи !строгое!
        /// </summary>
        public string sNameTask;
        /// <summary>
        /// Текущая версия задачи. 
        /// </summary>
        public string sCurrentVersion;
    }
    public struct stSSMail
    {
        /// <summary>
        /// Адрес откуда забирать почту
        /// </summary>
        public string sAdressMailLocal;
        /// <summary>
        /// IP адрес сервера УВД в виде "192.168.0.100"
        /// </summary>
        public string sAdresServerUVD;
        /// <summary>
        /// IP адрес сервера POP3
        /// </summary>
        public string sPOP3Server;
        /// <summary>
        /// Имя пользователя на сервере РОР3
        /// </summary>
        public string sUserPOP3;
        /// <summary>
        /// Пароль на сервере РОР3
        /// </summary>
        public string sPassPOP3;
        /// <summary>
        /// IP адрес сервера SMTP
        /// </summary>
        public string sSMTPServer;
        /// <summary>
        /// Последнее время приема файлов
        /// </summary>
        public string sLastRecivedTime;
        /// <summary>
        /// Последнее время передачи файлов
        /// </summary>
        public string sLastSendedTime;
        /// <summary>
        /// Интервал приема файлов в минутах (обновления только. Базы целиком в stSSGetFiles)
        /// </summary>
        public int iReciveInterval;
        /// <summary>
        /// Интервал отправки файлов (Почта) в минутах
        /// </summary>
        public int iSendInterval;
    }
    public struct stSSStatistic
    {
        /// <summary>
        /// Время дня в которое выплняется задача
        /// </summary>
        public string sHoursSave;
        /// <summary>
        /// Когда была отправлена последняя статистика
        /// </summary>
        public string sLastSended;
        /// <summary>
        /// Когда отправлять следующую статистику
        /// </summary>
        public string sNextSave;
        /// <summary>
        /// Когда была отправлена предыдущая
        /// </summary>
        public string sPrevionsSave;
    }
    public struct stSSGetFiles
    {
        /// <summary>
        /// Время дня в которое получать базы целиком
        /// </summary>
        public string sHoursRecive;
        /// <summary>
        /// IP адрес сервера УВД
        /// </summary>
        public string sIPServerUVD;
        /// <summary>
        /// Порт по которому обращаться к серверу по FTP
        /// </summary>
        public string sPortServerUVD_FTP;
        /// <summary>
        /// Порт по которому обращаться к серверу по HTTP
        /// </summary>
        public string sPortServerUVD_HTTP;
        /// <summary>
        /// Имя пользователя FTP
        /// </summary>
        public string sFTPUser;
        /// <summary>
        /// Пароль ползователя FTP
        /// </summary>
        public string sFTPPass;
        /// <summary>
        /// Имя файла со списком и описаниями баз целиком на сервере УВД в виде "List.xml"
        /// </summary>
        public string sFileNameListOnServer;
        /// <summary>
        /// Путь на сервере к базам целиком. Там должен лежать файл с именем из параметра sFileNameListOnServer
        /// </summary>
        public string sPathOnServerToFullBase;
        /// <summary>
        /// Путь на сервере к обновлениям баз
        /// </summary>
        public string sPathOnServerToUpdates;
        /// <summary>
        /// Путь на сервере к обновлениям софта станции
        /// </summary>
        public string sPathOnServerToNewSoftSS;
        /// <summary>
        /// Путь на сервере к обновлениям софта терминалов
        /// </summary>
        public string sPathOnServerToNewSoftTerm;
        /// <summary>
        /// Путь на сервере к папке входящих
        /// </summary>
        public string sPathOnServerToIncoming;
        /// <summary>
        /// Время последнего приема баз целиком
        /// </summary>
        public string sLastRecive;
        /// <summary>
        /// Время следующего приема баз целиком
        /// </summary>
        public string sNextRecive;
        /// <summary>
        /// Интервал приема баз целиком в днях
        /// </summary>
        public int iReciveInterval;
        /// <summary>
        /// Протокол приема файлов. HTTP или FTP.
        /// </summary>
        public string sNameProtocol;
    }
    public struct stSSWEBServer
    {
        /// <summary>
        /// Корневая папка WEB-сервера. Обычно в ОЗУ - например Temp
        /// </summary>
        public string sWWWrootDir;
        /// <summary>
        /// Порт на котором отвечает WEB-сервер
        /// </summary>
        public int iPortWWWserver;
    }
    public struct stSSNet
    {
        /// <summary>
        /// Локальный сетевой адрес станции в виде "192.168.0.118"
        /// </summary>
        public string sIPAdressLocal;
        /// <summary>
        /// Маска локального сетевого адреса станции в виде "255.255.255.0"
        /// </summary>
        public string sMask;
        /// <summary>
        /// Сетевой шлюз по умолчанию в виде "192.168.0.1"
        /// </summary>
        public string sGateway;
        /// <summary>
        /// Старший DWORD MAC-адреса станции
        /// </summary>
        public string sMAC_hi;
        /// <summary>
        /// Младший DWORD MAC-адреса станции
        /// </summary>
        public string sMAC_lo;
    }
    public struct stSSTermExchange
    {
        /// <summary>
        /// The name of the COM port.
        /// </summary>
        public string sComPortName;
        /// <summary>
        /// The COM port speed.
        /// </summary>
        public int iComPortSpeed;
    }

    public struct stTermInMem
    {
        /// <summary>
        /// Имя терминала
        /// </summary>
        public string sNameTerminal;
        /// <summary>
        /// Уникальный номер терминала
        /// </summary>
        public string sIDTerminal;
        /// <summary>
        /// Дата последней синхронизации со станцией
        /// </summary>
        public string sLastSyncronized;
        /// <summary>
        /// Дата актуальности базы розыск лиц
        /// </summary>
        public string sLastUpdatesBaseLica;
        /// <summary>
        /// Путь к файлу конфигурации терминала
        /// </summary>
        public string sPathToTermIniFile;
        /// <summary>
        /// Количество проверок по базе розыск лиц с момента последней синхронизации
        /// </summary>
        public int iCountSearchLica;
    }
    public struct stTermInDock
    {
        /// <summary>
        /// имя терминала
        /// </summary>
        public string sNameTerminal;
        /// <summary>
        /// ИД терминала
        /// </summary>
        public string sIDTerminal;
        /// <summary>
        /// Текуший статус словами: работаю, обработан, и тд.
        /// </summary>
        public string sCurrStatus;
        /// <summary>
        /// Цвет, каким выделять надпись на основной форме
        /// -1 - red
        /// 0 - black
        /// 1 - yellow
        /// 2 - green
        /// </summary>
        public int iColorLabelStatus;
        /// <summary>
        /// Числовой статус -1 - была ошибка, 0 - не обслуживался, 1 - работаю, 2 - был обработан.
        /// </summary>
        public int iServeStatus;
        /// <summary>
        /// Есть в доке или нет
        /// </summary>
        public bool bIsPresented;
        /// <summary>
        /// Послений байт ИП адреса сервера (сторона на терминале)
        /// </summary>
        public byte iIPaddrServer;
        /// <summary>
        /// Последний байт ИП адреса клиента (сторона на станции)
        /// </summary>
        public byte iIPaddrClient;
        /// <summary>
        /// Статус состояния обновления: 0-не начиналось, 1-готов к приему, 2-в процессе работы, 3-завершено
        /// </summary>
        public int iUpdateStatus;
        /// <summary>
        /// Процент заряда аккумулатора терминала
        /// </summary>
        public int iPercentAkk;
        /// <summary>
        /// Статус зарядки аккумулятора с лапки модуля 0-заряжается, 1-зарядка завершена
        /// </summary>
        public int iChargeState;
        /// <summary>
        /// Текущий режим модуля:
        /// 0 - выключен
        /// 1 - экран погашен, но не выключен
        /// 2 - активен
        /// </summary>
        public int iSC_mode;
    }

    public struct stBaseInfo
    {
        /// <summary>
        /// Комментарий базы
        /// </summary>
        public string sName;
        /// <summary>
        /// Дата базы в виде строки
        /// </summary>
        public string sDate;
        /// <summary>
        /// Тоже в др. формате
        /// </summary>
        public DateTime dtDate;
        /// <summary>
        /// Имя файла имеющегося последнего обновления по этой базе
        /// </summary>
        public string sLUpd;
        /// <summary>
        /// Имя обновления переведенное в дт. формат
        /// </summary>
        public DateTime dtLUpd;
        /// <summary>
        /// Тип базы (кодовое имя)
        /// </summary>
        public string sTypeBase;
        /// <summary>
        /// Имя файла, содержащего базу
        /// </summary>
        public string sNameFileBase;
    }

    public struct DEVICE_ID
    {
        public uint dwSize;
        public uint dwPresetIDOffset;
        public uint dwPresetIDBytes;
        public uint dwPlatformIDOffset;
        public uint dwPlatformIDBytes;
        public byte u1;
        public byte u2;
        public byte u3;
        public byte u4;
        public byte u5;
        public byte u6;
        public byte u7;
        public byte u8;
        public byte u9;
        public byte u10;
        public byte u11;
        public byte u12;
        public byte u13;
        public byte u14;
        public byte u15;
        public byte u16;
        public byte u17;
        public byte u18;
        public byte u19;
        public byte u20;
        public byte u21;
        public byte u22;
        public byte u23;
        public byte u24;
    };
    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEMTIME
    {
        [MarshalAs(UnmanagedType.U2)]
        public short Year;
        [MarshalAs(UnmanagedType.U2)]
        public short Month;
        [MarshalAs(UnmanagedType.U2)]
        public short DayOfWeek;
        [MarshalAs(UnmanagedType.U2)]
        public short Day;
        [MarshalAs(UnmanagedType.U2)]
        public short Hour;
        [MarshalAs(UnmanagedType.U2)]
        public short Minute;
        [MarshalAs(UnmanagedType.U2)]
        public short Second;
        [MarshalAs(UnmanagedType.U2)]
        public short Milliseconds;
    }
    #endregion
    public static class Utils
    {
        #region Импорт Dll
        [DllImport("coredll.dll")]
        private static extern bool KernelIoControl(Int32 IoControlCode, IntPtr
            InputBuffer, Int32 InputBufferSize, ref DEVICE_ID OutputBuffer, Int32
            OutputBufferSize, ref Int32 BytesReturned);

        [DllImport("coredll.dll")]
        private static extern bool KernelIoControl(Int32 IoControlCode, ref UInt32
            InputBuffer, Int32 InputBufferSize, ref Guid OutputBuffer, Int32
            OutputBufferSize, ref UInt32 BytesReturned);

        [DllImport("CoreDll.dll")]
        private static extern void GlobalMemoryStatus(MEMORYSTATUS lpBuffer);

        const int POWER_FORCE = 4096;
        const int POWER_STATE_RESET = 0x00800000;

        [DllImport("coredll.dll", SetLastError = true)]
        static extern int SetSystemPowerState(string psState, int StateFlags, int Options);

        [DllImport("coredll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetLocalTime([In] ref SYSTEMTIME lpLocalTime);

        [DllImport("coredll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool TouchCalibrate();

        #endregion
        /// <summary>
        /// Версия файла SSLI.exe
        /// </summary>
        public static string sVersionSSLI = "0.0.0.0";
        /// <summary>
        /// Имя файла обозначающего, что данная директория занята монопольно
        /// </summary>
        public const string sFileNameFlagBusy = "_busy_Flag";
        /// <summary>
        /// Имя основного лог-файла службы
        /// </summary>
        public const string sFileNameLog = "StationSyncroLI.log";
        /// <summary>
        /// Имя файла конфигурации станции
        /// </summary>
        public const string sFileNameInit = "StationSyncroLI.xml";

        public const string sFolderNameMain = "Storage Card";   //!!
        public const string sFolderNameSecond = "DaS_SSLI";

        /// <summary>
        /// Имя программы для настроек станции на ПК
        /// </summary>
        public const string sFileNameProgrammConfig = "Config_SSLI.exe";
        /// <summary>
        /// Имя файла программы для обмена с сервером с ПК
        /// </summary>
        public const string sFileNameProgrammTransfer = "TransferFiles_SSLI.exe";
        public const string sIPLocalRNDIS = "192.168.42.129";
        public const int iLocalRNDISPort = 30000;
        /// <summary>
        /// Объект для синхронизации работы с файлом конфигурации.
        /// </summary>
        public static Object oSyncroLoadSaveInit = new object();
        private static bool bInitOK = false;
        /// <summary>
        /// класс инициализирован
        /// </summary>
        public static bool bInizialised
        {
            get
            {
                return bInitOK;
            }
        }
        /// <summary>
        /// Кому-то требуется перезагрузить всю систему.
        /// </summary>
        public static bool bRebootRequest = false;
        /// <summary>
        /// Основной класс конфигурации системы. Все настройки тут.
        /// </summary>
        public static stSSConfig strConfig;
        /// <summary>
        /// Класс текущего состояния системы.
        /// </summary>
        public static cSSCurrStatus cCurrStatus = null;

        /// <summary>
        /// Запись в лог файл
        /// </summary>
        /// <param name="sPathExecute">папка куда писать</param>
        /// <param name="strWr">строка чего писать</param>
        /// <returns>true если все нормально</returns>
        public static bool WriteDebugString(string sPathExecute, string strWr)
        {
            Object ob = new object();
            lock (ob)
            {
                bool bRet = true;
                StreamWriter sw = null;

                try
                {
                    sw = new StreamWriter(sPathExecute + Path.DirectorySeparatorChar.ToString() + sFileNameLog, true);
                    sw.WriteLine(DateTime.Now.ToLocalTime().ToString() + strWr);
                    bRet = true;
                }
                catch
                {
                    bRet = false;
                }
                finally
                {
                    if (sw != null) sw.Close();
                }

                return bRet;
            }
        }
        /// <summary>
        /// Удаление лишних строк из лог-файла
        /// </summary>
        /// <param name="fileName">Имя лог-файла</param>
        /// <param name="sPathExecute">Путь к директории исполнения</param>
        /// <param name="rowsCountMax">Сколько строк оставить</param>
        public static void CutLogFile(string fileName, string sPathExecute, int rowsCountMax)
        {
            int rowCount = 0, countTmp = 0;
            string readStr;
            StreamReader sr = null;
            StreamWriter sw = null;
            if (File.Exists(fileName))
            {
                try
                {
                    sr = File.OpenText(fileName);
                    do
                    {
                        readStr = sr.ReadLine();
                        rowCount++;
                    } while (readStr != null);
                    sr.Close();
                    if (rowCount <= rowsCountMax) return;

                    sr = File.OpenText(fileName);
                    while (countTmp < (rowCount - rowsCountMax - 1))
                    {
                        readStr = sr.ReadLine();
                        if (readStr == null) break;
                        countTmp++;
                    }

                    sw = File.CreateText(sPathExecute + Path.DirectorySeparatorChar.ToString() + "TempFile.tmp");
                    do
                    {
                        readStr = sr.ReadLine();
                        if (readStr == null) break;
                        sw.WriteLine(readStr);
                    } while (readStr != null);
                    sr.Close();
                    sw.Close();

                    File.Delete(fileName);
                    FileInfo fi = new FileInfo(sPathExecute + Path.DirectorySeparatorChar.ToString() + "TempFile.tmp");
                    fi.MoveTo(fileName);
                    WriteDebugString(sPathExecute, " #CutLogFile(" + fileName + "):OK");
                }
                catch (Exception ex)
                {
                    if (sr != null) sr.Close();
                    if (sw != null) sw.Close();
                    WriteDebugString(sPathExecute, " #CutLogFile:ERROR - " + ex.Message);
                }
            }
        }
        /// <summary>
        /// Пометить директорию как используемую монопольно
        /// </summary>
        /// <param name="sNameFolder">имя директории</param>
        public static void MarkedDirBusy(string sNameFolder)
        {
            Object ob = new object();
            lock (ob)
            {
                try
                {
                    if (!File.Exists(sNameFolder + Path.DirectorySeparatorChar.ToString() + sFileNameFlagBusy))
                    {
                        StreamWriter sw = File.CreateText(sNameFolder + Path.DirectorySeparatorChar.ToString() + sFileNameFlagBusy);
                        if (sw != null) sw.Close();
                    }
                }
                catch { }
            }
        }
        /// <summary>
        /// проверка используется ли директория другими процессами
        /// </summary>
        /// <param name="sNameFolder">имя директории</param>
        /// <returns>true - если используется, false - если директория свободна</returns>
        public static bool IsFolderBusy(string sNameFolder)
        {
            Object ob = new object();
            lock (ob)
            {
                try
                {
                    if (File.Exists(sNameFolder + Path.DirectorySeparatorChar.ToString() + sFileNameFlagBusy)) return true;
                    else return false;
                }
                catch { return true; }
            }
        }
        /// <summary>
        /// снять маркер монопольного использования директории
        /// </summary>
        /// <param name="sNameFolder">имя директории</param>
        public static void DeleteMarkerDirBusy(string sNameFolder)
        {
            Object ob = new object();
            lock (ob)
            {
                try
                {
                    if (File.Exists(sNameFolder + Path.DirectorySeparatorChar.ToString() + sFileNameFlagBusy)) 
                        File.Delete(sNameFolder + Path.DirectorySeparatorChar.ToString() + sFileNameFlagBusy);
                }
                catch { }
            }
        }
        /// <summary>
        /// Инициализация - чтение файла конфигурации. После выполнения проверить bInizialised.
        /// </summary>
        /// <param name="sPathToiniFile">Путь с именем файла конфигурации</param>
        public static void LoadInit(string sFullPathToiniFile)
        {
            lock (oSyncroLoadSaveInit)
            {
                bool bRet = false;
                strConfig = new stSSConfig();
                FileStream fs = null;
                try
                {
                    fs = new FileStream(sFullPathToiniFile, FileMode.Open);
                    XmlSerializer sr = new XmlSerializer(typeof(stSSConfig));
                    strConfig = (stSSConfig)sr.Deserialize(fs);
                    fs.Close();
                    bRet = true;
                }
                catch (Exception ex)
                {
                    if (fs != null) fs.Close();
                    if (Directory.Exists(Path.GetDirectoryName(sFullPathToiniFile)))
                        WriteDebugString(Path.GetDirectoryName(sFullPathToiniFile), (" #LoadInit:ERROR - " + ex.Message));
                }
                bInitOK = bRet;
            }
            cCurrStatus = new cSSCurrStatus();
        }
        /// <summary>
        /// Чтение файла конфигурации
        /// </summary>
        /// <param name="sFullPathToiniFile">Полный путь к файлу</param>
        /// <param name="stRet">Структура типа stSSConfig</param>
        /// <returns>TRUE если все ОК, или FALSE при ошибке.</returns>
        public static bool LoadInitFile(string sFullPathToiniFile, ref stSSConfig stRet)
        {
            bool bRet = false;
            lock (oSyncroLoadSaveInit)
            {
                FileStream fs = null;
                try
                {
                    fs = new FileStream(sFullPathToiniFile, FileMode.Open);
                    XmlSerializer sr = new XmlSerializer(typeof(stSSConfig));
                    stRet = (stSSConfig)sr.Deserialize(fs);
                    fs.Close();
                    bRet = true;
                }
                catch (Exception ex)
                {
                    bRet = false;
                    if (fs != null) fs.Close();
                    if (Directory.Exists(Path.GetDirectoryName(sFullPathToiniFile)))
                        WriteDebugString(Path.GetDirectoryName(sFullPathToiniFile), (" #LoadIniFile:ERROR - " + ex.Message));
                }
            }
            return bRet;
        }
        /// <summary>
        /// Сохранение файла конфигурации по указанному пути.
        /// </summary>
        /// <param name="sFullPathToiniFile">Полный путь с именем файла и расширением.</param>
        /// <returns>True - все ОК.</returns>
        public static bool SaveInit(string sFullPathToiniFile)
        {
            strConfig.sCurrentTypeSS = "SSLI";
            strConfig.sCurrentVersionSS = sVersionSSLI;

            lock (oSyncroLoadSaveInit)
            {
                FileStream fs = null;
                try
                {
                    fs = new FileStream(sFullPathToiniFile, FileMode.Create);
                    XmlSerializer sr = new XmlSerializer(typeof(stSSConfig));
                    sr.Serialize(fs, strConfig);
                    fs.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    if (fs != null) fs.Close();
                    if (Directory.Exists(Path.GetDirectoryName(sFullPathToiniFile)))
                        WriteDebugString(Path.GetDirectoryName(sFullPathToiniFile), (" #SaveIniFile:ERROR - " + ex.Message));
                    return false;
                }
            }
        }
        /// <summary>
        /// Сохранение файла конфигурации по фиксированному пути: папка исполнения + Utils.sFileNameInit
        /// </summary>
        /// <returns>True - все ОК.</returns>
        public static bool SaveInit()
        {
            return SaveInit(strConfig.sPathToExecute + Path.DirectorySeparatorChar.ToString() + sFileNameInit);
        }
        /// <summary>
        /// Получение серийника девайса. Вроде как уникальный.
        /// </summary>
        /// <returns>Строка с серийником.</returns>
        public static string GetDeviceID()  
        {
            int iReadLenMessage = 0;
            UInt16[] ints = null;
            byte [] arbBuff = new byte[300];

            Array.Clear(arbBuff, 0, arbBuff.Length);
            ComPort.CleanBytesReadPort();
            //send com get_status
            ComPort.SendMessage((byte)UsartCommand.CMD_GET_ID, ref arbBuff, 0);
            //wait arStatus
            ComPort.GetMessage(ref arbBuff, ref iReadLenMessage);
            if (iReadLenMessage > 0)
            {
                if (arbBuff[0] == (byte)UsartAnswer.ANS_ID)
                {
                    int size = 8;
                    ints = new UInt16[size];
                    for (int index = 0; index < size; index++)
                    {
                        ints[index] = BitConverter.ToUInt16(arbBuff, 1 + index * sizeof(UInt16));
                    }
                    return WorkCom.ConvertByteArToID(ints);
                }
            }

            //string sRet = "";
            //string sNameFile = "/proc/cpuinfo";
            //if (File.Exists(sNameFile))
            //{
            //    string[] lines = File.ReadAllLines(sNameFile);
            //    foreach (string s in lines)
            //    {
            //        if(s.IndexOf("Serial") >= 0)
            //        {
            //            sRet = "0000000000000000" + s.Substring(10, 16);
            //            return sRet;
            //        }
            //    }
            //}

            return "01020304050607080102030405060708";
        }

        public static string GetDeviceSernum()
        {
            int iReadLenMessage = 0;
            UInt16 uRet = 0;
            byte[] arbBuff = new byte[300];

            Array.Clear(arbBuff, 0, arbBuff.Length);
            ComPort.CleanBytesReadPort();
            //send com get_status
            ComPort.SendMessage((byte)UsartCommand.CMD_GET_SERNUM, ref arbBuff, 0);
            //wait arStatus
            ComPort.GetMessage(ref arbBuff, ref iReadLenMessage);
            if (iReadLenMessage > 0)
            {
                if (arbBuff[0] == (byte)UsartAnswer.ANS_SERNUM)
                {
                    uRet = BitConverter.ToUInt16(arbBuff, 1);
                    return WorkCom.ConvertByteArToNameTerm(uRet);
                }
            }
            return "0000-00";
        }

        /// <summary>
        /// Получение статуса памяти устройства
        /// </summary>
        /// <param name="iTotal">Сколько памяти всего</param>
        /// <param name="iAvaib">Сколько памяти доступно для программы</param>
        public static void GetMemoryStatus(ref int iTotal, ref int iAvaib)
        {
            // /proc/meminfo
            MEMORYSTATUS memStatus = new MEMORYSTATUS();
            GlobalMemoryStatus(memStatus);

            iTotal = (int)(memStatus.dwTotalPhys / (1024));
            iAvaib = (int)(memStatus.dwAvailPhys / (1024));

        }
        /// <summary>
        /// Программный сброс устройства
        /// </summary>
        /// <returns>Если сработает, то уже не важно что он вернет.</returns>
        public static int SoftReset()
        {
            // sudo /sbin/reboot
            return SetSystemPowerState(null, POWER_STATE_RESET, POWER_FORCE);
        }
        /// <summary>
        /// Получаем последние версии файлов.
        /// </summary>
        /// <param name="sNameDll">Имя загружаемой библиотеки без пути. Расширение dll добавляется автоматически.</param>
        /// <param name="sCurrVersion">Текущая версия библиотеки.</param>
        /// <param name="sExecutePath">Путь откуда запускается библиотека.</param>
        /// <param name="sNewSoftPath">Где искать новые версии библиотеки.</param>
        /// <returns>Загруженный класс типа "ClassAMBRenewedService".</returns>
        public static ClassAMBRenewedService GetLastVersion(string sNameDll, ref string sCurrVersion, string sExecutePath, string sNewSoftPath)
        {
            try
            {
                string sPd = Path.DirectorySeparatorChar.ToString();
                Version vCurrVersion = null, vNewVersion = null;
                Assembly asmCurr = null;
                string sNameNewFile = sNewSoftPath + sPd + sNameDll + ".dll";
                string sNameCurrFile = sExecutePath + sPd + sNameDll + ".dll";

                vNewVersion = NativeFile.GetFileInfo(sNameNewFile);
                vCurrVersion = NativeFile.GetFileInfo(sNameCurrFile);

                if (!File.Exists(sNameCurrFile))
                {
                    if (File.Exists(sNameNewFile))
                    {
                        File.Copy(sNameNewFile, sNameCurrFile);
                        File.Delete(sNameNewFile);
                    }
                    else return null;
                }
                if ((vCurrVersion != null) && (vNewVersion != null))
                {
                    if (vNewVersion > vCurrVersion)
                    {
                        if (File.Exists(sNameCurrFile + "_old")) File.Delete(sNameCurrFile + "_old");
                        File.Move(sNameCurrFile, sNameCurrFile + "_old");
                        File.Copy(sNameNewFile, sNameCurrFile);
                    }
                    try
                    {
                        if(File.Exists(sNameNewFile)) File.Delete(sNameNewFile);
                    }
                    catch (Exception ex)
                    {  }
                }
                //с версиями разобрались

                asmCurr = Assembly.LoadFrom(sNameCurrFile);
                sCurrVersion = asmCurr.GetName().Version.ToString();
                ClassAMBRenewedService cRSP = null;
                try
                {
                    cRSP = (ClassAMBRenewedService)asmCurr.CreateInstance(sNameDll);
                }
                catch (Exception e)
                {
                    string s = e.Message;
                }
                return cRSP;
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// Установка системного времени.
        /// </summary>
        /// <param name="newTime">Дата-время в формате DateTime</param>
        /// <returns>TRUE - если все ОК.</returns>
        public static bool SetSystemTime(DateTime newTime)
        {
            bool bret;

            SYSTEMTIME st = new SYSTEMTIME();
            st.Year = (short)newTime.Year;
            st.Month = (short)newTime.Month;
            st.Day = (short)newTime.Day;
            st.Hour = (short)newTime.Hour;
            st.Minute = (short)newTime.Minute;
            st.Second = (short)newTime.Second;
            bret = SetLocalTime(ref st);
            return bret;
        }
        /// <summary>
        /// Проверка в сети или нет. К сети относится и ActiveSync.
        /// </summary>
        /// <returns>TRUE если есть хоть какая сеть.</returns>
        public static bool IsNetPresented()
        {
            try
            {
                string sHostName = System.Net.Dns.GetHostName();
                System.Net.IPHostEntry ipThisHost = System.Net.Dns.GetHostEntry(sHostName);
                string sThisIpAddr = ipThisHost.AddressList[0].ToString();
                if (sThisIpAddr != System.Net.IPAddress.Parse("127.0.0.1").ToString())
                {
                    if (sThisIpAddr != System.Net.IPAddress.Parse(sIPLocalRNDIS).ToString()) return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Вызов калибровки сенсорного экрана.
        /// </summary>
        /// <returns>The function returns TRUE if it succeeds and FALSE if it fails.</returns>
        public static bool TouchScreenCalibrate()
        {
            return TouchCalibrate();
        }

        /// <summary>
        /// Сжатие (архивирование) файла
        /// </summary>
        /// <param name="sFullFileName">Полное имя файла для сжатия</param>
        /// <param name="sFullZipName">Полное имя файла архива</param>
        /// <param name="sZipComment">Комментарий внутри архива</param>
        /// <returns>TRUE если все ОК</returns>
        public static bool ZipFile(string sFullFileName, string sFullZipName, string sZipComment)
        {
            bool bRet = false;
            try
            {
                using ( ZipFile zip = new ZipFile(Encoding.UTF8))
                {
                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestSpeed;
                    ZipEntry ze1 = zip.AddFile(sFullFileName, "");
                    zip.Comment = sZipComment;
                    zip.TempFileFolder = sFolderNameMain +  Path.DirectorySeparatorChar.ToString() + "Temp";
                    zip.Save(sFullZipName);
                    bRet = true;
                }
            }
            catch (Exception ex)
            {
                WriteDebugString(strConfig.sPathToExecute, ("#ZipFile (" + sFullFileName + "):ERROR - " + ex.Message));
                bRet = false;
            }
            return bRet;
        }
        /// <summary>
        /// Распаковка сжатых файлов
        /// </summary>
        /// <param name="sFullZipName">Полное имя файла архива</param>
        /// <param name="sPathToUnzip">Путь куда распаковывать</param>
        /// <returns>TRUE если все ОК</returns>
        public static bool UnZipFile(string sFullZipName, string sPathToUnzip)
        {
            bool bRet = false;
            WriteDebugString(strConfig.sPathToExecute, "#UnZipFile (" + sFullZipName + "):Entrance...");
            try
            {
                using (ZipFile zip1 = new ZipFile(sFullZipName, Encoding.UTF8))
                {
                    zip1.ExtractAll(sPathToUnzip, ExtractExistingFileAction.OverwriteSilently);
                }
                bRet = true;
            }
            catch (Exception ex)
            {
                WriteDebugString(strConfig.sPathToExecute, "#UnZipFile (" + sFullZipName + "):ERROR - " + ex.Message);
                bRet = false;
            }
            WriteDebugString(strConfig.sPathToExecute, "#UnZipFile (" + sFullZipName + "):Exit.");
            return bRet;
        }

    }

    public static class ComPort
    {
        private static SerialPort sp = null;
        public static bool bIntrUsart1 = false;
        private static byte rbyte1 = 0;
        private static byte iRecived1 = 0;
        private static byte iExpectedLen1 = 0;
        public static byte[] arRecivBuff1 = new byte[200];
        private static byte[] arSendBuff1 = new byte[32];

        /// <summary>
        /// Init the specified sPortName and iPortSpeed.
        /// </summary>
        /// <returns>The init.</returns>
        /// <param name="sPortName">Имя порта. ("/dev/ttyUSB0") </param>
        /// <param name="iPortSpeed">Скорость. (9600)</param>
        public static bool Init(string sPortName, int iPortSpeed)
        {
            bool bRet = false;

            sp = new SerialPort(sPortName, iPortSpeed, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
            sp.Handshake = Handshake.None;
            sp.ReadTimeout = 100;
            sp.DtrEnable = true;
            try
            {
                sp.Open();
            }
            catch (Exception ex)
            {

            }
            if (sp.IsOpen)
            {
                bRet = true;
            }

            return bRet;
        }

        public static void Close()
        {
            if (sp != null)
            {
                if (sp.IsOpen) sp.Close();
                sp.Dispose();
            }
            sp = null;
        }
        /// <summary>
        /// Очистить входной буфер порта.
        /// </summary>
        public static void CleanBytesReadPort()
        {
            if (sp != null)
            {
                if (sp.IsOpen)
                {
                    while (sp.BytesToRead > 0) sp.ReadByte();
                }
            }
        }
        /// <summary>
        /// Получение сообщения из порта. Блокирует поток!
        /// </summary>
        /// <param name="buff">Буфер, куда записать принятое.</param>
        /// <param name="iReadlen">Сколько было записано в буфер.</param>
        public static void GetMessage(ref byte[] buff, ref int iReadlen)
        {

            Array.Clear(buff, 0, buff.Length);
            iReadlen = 0;
            int iCnt = 0;
            for (iCnt = 0; iCnt < 200; iCnt++) //до 1 сек ожидание ответа
            {
                if (sp.BytesToRead > 0) break;
                Thread.Sleep(10);
            }

            if (sp.BytesToRead == 0) return;

            while (!bIntrUsart1)
            {
                try
                {
                    if (sp.BytesToRead == 0) System.Threading.Thread.Sleep(10);
                    iReadlen = UsartGetBlock();
                }
                catch (TimeoutException)
                {
                    iReadlen = 0;
                    break;
                }
            }
            if (bIntrUsart1 && (iReadlen > 0))
            {
                Array.Copy(arRecivBuff1, buff, iReadlen);
            }
        }

        private static int UsartGetBlock()
        {
            int iRet = 0;
            while (/*(sp.BytesToRead > 0) && */(!bIntrUsart1))
            {
                rbyte1 = (byte)sp.ReadByte();
                if (iRecived1 == 0)
                {
                    if (rbyte1 != 0x0A) break; //continue
                    else iRecived1++;
                }
                else
                {
                    if (iRecived1 == 1)
                    {
                        iExpectedLen1 = rbyte1;
                        Array.Clear(arRecivBuff1, 0, arRecivBuff1.Length);
                        if (iExpectedLen1 > arRecivBuff1.Length)
                        {
                            iRecived1 = 0;
                            iExpectedLen1 = 0;
                            //sendErr()
                            break;
                        }
                    }
                    if ((iRecived1 > 1) && (iRecived1 < iExpectedLen1 - 1))
                    {
                        arRecivBuff1[iRecived1 - 2] = rbyte1;
                    }
                    if (iRecived1 >= iExpectedLen1 - 1)
                    {
                        if (GetCRC8(arRecivBuff1, iRecived1, 0) == rbyte1)
                        {
                            bIntrUsart1 = true;
                            iRet = iExpectedLen1;
                            //sendOk()
                        }
                        else
                        {
                            //error crc - sendErr()
                        }
                        iRecived1 = 0;
                        iExpectedLen1 = 0;
                        break;
                    }
                    iRecived1++;
                }
            }
            return iRet;
        }
        /// <summary>
        /// Отправить сообщение через ком-порт.
        /// </summary>
        /// <param name="ans">Команда сообщения типа UsartCommand.</param>
        /// <param name="data">Данные для команды.</param>
        /// <param name="lendata">Длинна данных в буфере.</param>
        public static void SendMessage(byte ans, ref byte[] data, int lendata)
        {
            int lenmess = lendata + 4;
            Array.Clear(arSendBuff1, 0, lenmess);

            arSendBuff1[0] = 0x0A;
            arSendBuff1[1] = (byte)lenmess;
            arSendBuff1[2] = ans;
            Array.Copy(data, 0, arSendBuff1, 3, lendata);
            arSendBuff1[lendata + 3] = GetCRC8(arSendBuff1, lendata + 1, 2);
            sp.Write(arSendBuff1, 0, lenmess);
            bIntrUsart1 = false;

            Thread.Sleep(10);
        }

        private static byte GetCRC8(byte[] buf, int lenbuf, int ofset)
        {
            int i;
            byte crc = 0;

            for (i = ofset; i < (lenbuf + ofset); i++) crc += (byte)(buf[i]);

            return crc;
        }
    }


    /// <summary>
    /// Вся информация о текущем статусе станции. Динамическая! Не сохраняется!
    /// </summary>
    public class cSSCurrStatus
    {
        private const string sRemoteNameFileT6Init = "T6Init.xml";
        private const string sProtocolFileName = "_protocol.log";
        private string sPd = Path.DirectorySeparatorChar.ToString();
        /// <summary>
        /// Имя подразделения
        /// </summary>
        public string sNamePodrazdelenie;
        /// <summary>
        /// Уникальный номер станции
        /// </summary>
        public string sDeviceSS_ID;

        /// <summary>
        /// Имя последнего принятого файла
        /// </summary>
        public string sLastReciveName;
        /// <summary>
        /// Время получения последнего принятого файла
        /// </summary>
        public string sLastReciveTime;
        /// <summary>
        /// Имя последнего отправленного файла
        /// </summary>
        public string sLastSendedName;
        /// <summary>
        /// Время отправки последнего файла
        /// </summary>
        public string sLastSendedTime;

        /// <summary>
        /// Массив розыскных баз на станции
        /// </summary>
        public stBaseInfo[] arBaseInfo = null;
        /// <summary>
        /// Массив информации о терминалах с которыми когда-то работали
        /// </summary>
        public stTermInMem[] arstTermInMemory = null;
        /// <summary>
        /// Массив информации о терминалах сейчас в подставке
        /// </summary>
        public stTermInDock[] arstTermInDock = null;

        public cSSCurrStatus()
        {
            sNamePodrazdelenie = Utils.strConfig.sNamePodrazdelenie;
            sDeviceSS_ID = Utils.GetDeviceID();
            sLastReciveName = "нет данных";
            sLastReciveTime = Utils.strConfig.strMail.sLastRecivedTime;
            sLastSendedName = "нет данных";
            sLastSendedTime = Utils.strConfig.strMail.sLastSendedTime;

            arstTermInDock = new stTermInDock[5];
            for (int i = 0; i < arstTermInDock.Length; i++)
            {
                arstTermInDock[i].bIsPresented = false;
                arstTermInDock[i].iColorLabelStatus = 0;
                arstTermInDock[i].iServeStatus = 0;
                arstTermInDock[i].sCurrStatus = "НЕ ОБРАБОТАН";
                arstTermInDock[i].sIDTerminal = "";
                arstTermInDock[i].sNameTerminal = "ПУСТО";
            }
            UpdateBaseInfo();
            UpdateTermInfo();
        }
        /// <summary>
        /// Обновить информацию в arBaseInfo
        /// </summary>
        /// <returns>TRUE - все ОК.</returns>
        public bool UpdateBaseInfo()
        {
            bool bRet = true;

            if (!Directory.Exists(Utils.sFolderNameMain + sPd + Utils.strConfig.sPathToFullBase)) return false;
            if (!Directory.Exists(Utils.sFolderNameMain + sPd + Utils.strConfig.sPathToUpdTerm)) return false;
            if (Utils.IsFolderBusy(Utils.sFolderNameMain + sPd + Utils.strConfig.sPathToFullBase)) return false;

            DirectoryInfo di = new DirectoryInfo(Utils.sFolderNameMain + sPd + Utils.strConfig.sPathToFullBase);
            FileInfo[] fiXml = di.GetFiles("*.xml");
            FileInfo[] fiZip = di.GetFiles("*.zip");
            if ((fiXml.Length == 0)&&(fiZip.Length == 0)) return false;
            arBaseInfo = new stBaseInfo[fiXml.Length + fiZip.Length];
            int iCountBase = 0;
            foreach (FileInfo fi in fiXml)      //сначала пробегаем по xml
            {
                try
                {
                    arBaseInfo[iCountBase] = WorkWithXmlBaseFile(fi.FullName);
                    iCountBase++;
                }
                catch { }
            }
            foreach (FileInfo fi in fiZip)      //потом по zip
            {
                try
                {
                    stBaseInfo stBI = WorkWithZipBaseFile(fi.FullName);
                    bool bIsFound = false;
                    for (int iCntBase = 0; iCntBase < fiXml.Length + fiZip.Length; iCntBase++)
                    {
                        if (arBaseInfo[iCntBase].sTypeBase == stBI.sTypeBase)       //если zip и xml дублируются
                        {
                            if (stBI.dtDate > arBaseInfo[iCntBase].dtDate)
                            {
                                arBaseInfo[iCntBase] = stBI;        //выбираем новейшую
                            }
                            bIsFound = true;
                            break;
                        }
                    }
                    if (!bIsFound)
                    {
                        arBaseInfo[iCountBase] = stBI;
                        iCountBase++;
                    }
                }
                catch { }
            }
            return bRet;
        }
        private stBaseInfo WorkWithZipBaseFile(string sNameFileZip)
        {
            stBaseInfo stbiRet = new stBaseInfo();
            string sTmp2 = null;
            Utils.MarkedDirBusy(Path.GetDirectoryName(sNameFileZip));
            using (ZipFile zip1 = new ZipFile(sNameFileZip, Encoding.UTF8))
            {
                sTmp2 = zip1.Comment.Substring(zip1.Comment.IndexOf("XML:") + "XML:".Length);
            }
            XmlSerializer dsr = new XmlSerializer(typeof(stOneBase));
            TextReader tr = new StringReader(sTmp2);
            stOneBase stOb = (stOneBase)dsr.Deserialize(tr);
            tr.Close();
            Utils.DeleteMarkerDirBusy(Path.GetDirectoryName(sNameFileZip));
            try
            {
                if (stOb.sNameFileBase != null)
                {
                    stbiRet.sName = stOb.sComment;
                    stbiRet.sDate = stOb.sDateLastUpdates;
                    stbiRet.dtDate = Convert.ToDateTime(stOb.sDateLastUpdates);
                    stbiRet.sTypeBase = stOb.sTypeBase;
                    stbiRet.sNameFileBase = Path.GetFileName(sNameFileZip);

                    if (stOb.sTypeUpdate == "UPDATE")
                    {
                        //получить дату самого свежего обновления
                        ArrayList arFoundFiles = new ArrayList();
                        DirectoryInfo updDir = new DirectoryInfo(Utils.sFolderNameMain + sPd + Utils.strConfig.sPathToUpdTerm);
                        FileInfo[] updFilesNotSort = updDir.GetFiles(stOb.sPrefUpdates + "*." + stOb.sExtUpdates);
                        foreach (FileInfo ufns in updFilesNotSort)
                        {
                            string tmpstr = "";
                            if (ufns.Length > 10)
                            {
                                MyInfoFile inffile = new MyInfoFile();
                                tmpstr = ufns.Name;
                                string tmpdFile = tmpstr.Substring(stOb.iNumCharPref, 2);
                                string tmpmFile = tmpstr.Substring(stOb.iNumCharPref + 2, 2);
                                string tmpyFile = tmpstr.Substring(stOb.iNumCharPref + 4, 4);
                                inffile.fileName = tmpstr;
                                inffile.fileNumdate = Convert.ToInt32(tmpyFile + tmpmFile + tmpdFile); //(int) YYYYMMDD
                                inffile.fileNameFull = ufns.FullName;
                                inffile.dtFileDate = new DateTime(Convert.ToInt32(tmpyFile), Convert.ToInt32(tmpmFile), Convert.ToInt32(tmpdFile));
                                arFoundFiles.Add(inffile);
                            }
                        }
                        if (arFoundFiles.Count > 0)
                        {
                            arFoundFiles.Sort();
                            stbiRet.sLUpd = ((MyInfoFile)arFoundFiles[arFoundFiles.Count - 1]).fileName;
                            stbiRet.dtLUpd = ((MyInfoFile)arFoundFiles[arFoundFiles.Count - 1]).dtFileDate;
                            if ((arFoundFiles.Count > 15) && (!Utils.IsFolderBusy(Utils.sFolderNameMain + sPd + Utils.strConfig.sPathToUpdTerm)))    //удаляем лишние файлы обновлений
                            {
                                Utils.MarkedDirBusy(Utils.sFolderNameMain + sPd + Utils.strConfig.sPathToUpdTerm);
                                for (int i = 0; i < arFoundFiles.Count - 10; i++)
                                {
                                    if (((MyInfoFile)arFoundFiles[i]).dtFileDate.AddDays(10) < DateTime.Parse(stOb.sDateLastUpdates))    //удаляем те, которые на 10 дней старше базы
                                    {
                                        try
                                        {
                                            File.Delete(((MyInfoFile)arFoundFiles[i]).fileNameFull);
                                        }
                                        catch { }
                                    }
                                }
                                Utils.DeleteMarkerDirBusy(Utils.sFolderNameMain + sPd + Utils.strConfig.sPathToUpdTerm);
                            }
                            return stbiRet;
                        }
                    }
                    stbiRet.sLUpd = "НЕТ";
                }
            }
            catch { }
            return stbiRet;
        }        
        private stBaseInfo WorkWithXmlBaseFile(string sNameFileXml)
        {
            FileStream fs = null;
            stOneBase stOb = new stOneBase();
            stBaseInfo stbiRet = new stBaseInfo();
            string sErr = null;
            if (Utils.IsFolderBusy(Path.GetDirectoryName(sNameFileXml))) return stbiRet;
            try
            {
                fs = new FileStream(sNameFileXml, FileMode.Open);
                XmlSerializer sr = new XmlSerializer(typeof(stOneBase));
                stOb = (stOneBase)sr.Deserialize(fs);
                fs.Close();
            }
            catch (Exception ex)
            {
                sErr = ex.Message;
            }
            finally
            {
                if (fs != null) fs.Close();
            }

            try
            {
                if (stOb.sNameFileBase != null)
                {
                    stbiRet.sName = stOb.sComment;
                    stbiRet.sDate = stOb.sDateLastUpdates;
                    stbiRet.dtDate = Convert.ToDateTime(stOb.sDateLastUpdates);
                    stbiRet.sTypeBase = stOb.sTypeBase;
                    stbiRet.sNameFileBase = stOb.sNameFileBase;

                    if (stOb.sTypeUpdate == "UPDATE")
                    {
                        //получить дату самого свежего обновления
                        ArrayList arFoundFiles = new ArrayList();
                        DirectoryInfo updDir = new DirectoryInfo(Utils.sFolderNameMain + sPd + Utils.strConfig.sPathToUpdTerm);
                        FileInfo[] updFilesNotSort = updDir.GetFiles(stOb.sPrefUpdates + "*." + stOb.sExtUpdates);
                        foreach (FileInfo ufns in updFilesNotSort)
                        {
                            string tmpstr = "";
                            if (ufns.Length > 10)
                            {
                                MyInfoFile inffile = new MyInfoFile();
                                tmpstr = ufns.Name;
                                string tmpdFile = tmpstr.Substring(stOb.iNumCharPref, 2);
                                string tmpmFile = tmpstr.Substring(stOb.iNumCharPref + 2, 2);
                                string tmpyFile = tmpstr.Substring(stOb.iNumCharPref + 4, 4);
                                inffile.fileName = tmpstr;
                                inffile.fileNumdate = Convert.ToInt32(tmpyFile + tmpmFile + tmpdFile); //(int) YYYYMMDD
                                inffile.fileNameFull = ufns.FullName;
                                inffile.dtFileDate = new DateTime(Convert.ToInt32(tmpyFile), Convert.ToInt32(tmpmFile), Convert.ToInt32(tmpdFile));
                                arFoundFiles.Add(inffile);
                            }
                        }
                        if (arFoundFiles.Count > 0)
                        {
                            arFoundFiles.Sort();
                            stbiRet.sLUpd = ((MyInfoFile)arFoundFiles[arFoundFiles.Count - 1]).fileName;
                            stbiRet.dtLUpd = ((MyInfoFile)arFoundFiles[arFoundFiles.Count - 1]).dtFileDate;
                            if ((arFoundFiles.Count > 15) && (!Utils.IsFolderBusy(Utils.sFolderNameMain + sPd + Utils.strConfig.sPathToUpdTerm)))    //удаляем лишние файлы обновлений
                            {
                                Utils.MarkedDirBusy(Utils.sFolderNameMain + sPd + Utils.strConfig.sPathToUpdTerm);
                                for (int i = 0; i < arFoundFiles.Count - 10; i++)
                                {
                                    if (((MyInfoFile)arFoundFiles[i]).dtFileDate.AddDays(10) < DateTime.Parse(stOb.sDateLastUpdates))    //удаляем те, которые на 10 дней старше базы
                                    {
                                        try
                                        {
                                            File.Delete(((MyInfoFile)arFoundFiles[i]).fileNameFull);
                                        }
                                        catch { }
                                    }
                                }
                                Utils.DeleteMarkerDirBusy(Utils.sFolderNameMain + sPd + Utils.strConfig.sPathToUpdTerm);
                            }
                            return stbiRet;
                        }
                    }
                    stbiRet.sLUpd = "НЕТ";
                }
            }
            catch { }
            return stbiRet;
        }
        /// <summary>
        /// Обновить информацию в arstTermInMemory.
        /// </summary>
        /// <returns>TRUE - все ОК.</returns>
        public bool UpdateTermInfo()
        {
            bool bRet = true;

            if (Utils.sFolderNameMain + sPd + Utils.strConfig.sPathToProtocol == null) return false;

            DirectoryInfo diProtocol = new DirectoryInfo(Utils.sFolderNameMain + sPd + Utils.strConfig.sPathToProtocol);
            if (diProtocol.Exists)
            {
                DirectoryInfo[] diCurrProtocol = diProtocol.GetDirectories();
                arstTermInMemory = new stTermInMem[diCurrProtocol.Length];
                int iCountTerm = 0;
                foreach (DirectoryInfo di in diCurrProtocol)
                {
                    try
                    {
                        FileInfo[] fiInit = di.GetFiles(sRemoteNameFileT6Init);
                        if (fiInit != null)
                        {
                            stIni stI = LoadT6IniFile(fiInit[0].FullName);
                            arstTermInMemory[iCountTerm].sIDTerminal = stI.sDeviceID;
                            arstTermInMemory[iCountTerm].sNameTerminal = stI.sNameTerminal;
                            arstTermInMemory[iCountTerm].sLastSyncronized = stI.sLastSyncronisation;
                            arstTermInMemory[iCountTerm].sLastUpdatesBaseLica = stI.arBases[0].sNameLastUpdates;
                            arstTermInMemory[iCountTerm].sPathToTermIniFile = fiInit[0].FullName;
                            FileInfo[] fiProt = di.GetFiles("*" + sProtocolFileName);
                            if (fiProt != null)
                            {
                                arstTermInMemory[iCountTerm].iCountSearchLica = GetCountSearch(fiProt[0].FullName);
                            }
                        }                        
                    }
                    catch { }
                    iCountTerm++;
                }
            }
            return bRet;
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
            catch //(Exception ex)
            {
                if (fs2 != null) fs2.Close();
                //WriteDebugString("LoadT6IniFile:ERROR - " + ex.Message, 1);
            }
            return stCurTerm;
        }
        private int GetCountSearch(string sProtocolFileName)
        {
            int iRet = 0;
            StreamReader sr = null;
            string readStr = null;

            try
            {
                if (File.Exists(sProtocolFileName))
                {
                    sr = File.OpenText(sProtocolFileName);
                    while ((readStr = sr.ReadLine()) != null)
                    {
                        if (readStr.IndexOf(";") != 0)
                        {
                            if(readStr.IndexOf("TERM") != 0) iRet++;
                        }                        
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.WriteDebugString(Utils.strConfig.sPathToExecute, " #GetCountSearch ERROR:file - " + sProtocolFileName + ". Message - " + ex.Message);
            }
            finally
            {
                if (sr != null) sr.Close();
            }
            return iRet;
        }
        /// <summary>
        /// Найти на станции новейшую базу по типу
        /// </summary>
        /// <param name="sTypeBase">Тип базы</param>
        /// <returns>Имя файла содержащего базу</returns>
        public string GetNewestBaseFileName(string sTypeBase)
        {
            foreach (stBaseInfo stB in arBaseInfo)
            {
                if (stB.sTypeBase == sTypeBase) return stB.sNameFileBase;
            }
            return null;
        }
        /// <summary>
        /// Найти на станции новейшую базу по типу
        /// </summary>
        /// <param name="sTypeBase">Тип базы</param>
        /// <returns>Дата актуальности</returns>
        public DateTime GetNewestBaseDate(string sTypeBase)
        {
            foreach (stBaseInfo stB in arBaseInfo)
            {
                if (stB.sTypeBase == sTypeBase) return stB.dtDate;
            }
            return new DateTime();
        }

    }
    public class MyInfoFile : IComparable
    {
        public string fileNameFull;
        public string fileName;
        public int fileNumdate;
        public DateTime dtFileDate;

        public MyInfoFile()
        {
            fileNameFull = null;
            fileName = null;
            fileNumdate = 0;
            dtFileDate = new DateTime();
        }

        int IComparable.CompareTo(object o)
        {
            MyInfoFile tmpMIF = (MyInfoFile)(o);
            if (this.fileNumdate > tmpMIF.fileNumdate) return 1;
            if (this.fileNumdate < tmpMIF.fileNumdate) return -1;
            else return 0;
        }
    }
    public class NewSoftUpdates
    {
        private static stOneRow[] arstUpdates = null;
        private static Object oSyncro = new object();
        private const string sNameFileBaseNewSoftUpdates = "NewSoftUpdates.xml";

        /// <summary>
        /// Конструктор. Если есть файл протокола, читет его
        /// </summary>
        public NewSoftUpdates()
        {
            if(!LoadIni(Utils.strConfig.sPathToExecute + Path.DirectorySeparatorChar.ToString() + sNameFileBaseNewSoftUpdates)) arstUpdates = new stOneRow[1];
        }
        public void Close()
        {
            SaveIni(Utils.strConfig.sPathToExecute + Path.DirectorySeparatorChar.ToString() + sNameFileBaseNewSoftUpdates);
        }
        /// <summary>
        /// Проверить передавалось ли данное обновление данному терминалу.
        /// </summary>
        /// <param name="sNameFile">Полное имя файла обновления.</param>
        /// <param name="sIDTerm">Уникальный номер терминала.</param>
        /// <param name="sCurVersion">Текущая версия софта.</param>
        /// <returns>TRUE если обновление было передано.</returns>
        public bool CheckUpdates(string sFullNameFile, string sIDTerm, string sCurVersion)
        {
            Version vCurrVersion = new Version(sCurVersion), vNewVersion = null;
            lock (oSyncro)
            {
                FileInfo fi = new FileInfo(sFullNameFile);
                bool bRet = false;
                int iIndex = 0;
                bool bIs = false;
                try
                {
                    foreach (stOneRow or in arstUpdates)
                    {
                        if (or.sNameFile == fi.Name)
                        {
                            bIs = true;
                            break;
                        }
                        iIndex++;
                    }
                    if (bIs)
                    {
                        vNewVersion = new Version(arstUpdates[iIndex].sVersion);
                        if (vCurrVersion > vNewVersion)
                        {
                            return true;
                        }

                        if (arstUpdates[iIndex].arsIDTerm == null) return false;
                        
                        foreach (string s in arstUpdates[iIndex].arsIDTerm)
                        {
                            if (s == sIDTerm)
                            {
                                bRet = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                catch { }
                return bRet;
            }
        }
        /// <summary>
        /// Добавить новый файл обновления в список. При этом обнуляется список передач этого обновления на терминалы.
        /// </summary>
        /// <param name="sNameFile">Полное имя файла обновления.</param>
        public void AddNewUpdates(string sFullNameFile)
        {
            lock (oSyncro)
            {
                FileInfo fi = new FileInfo(sFullNameFile);
                Version vNewVersion = new Version("0.0.0.0");
                Version vOldVersion = new Version("0.0.0.0");
                if (fi.Exists)
                {
                    try
                    {
                        string sTempNF = System.IO.Path.GetTempFileName();
                        if (File.Exists(sTempNF)) File.Delete(sTempNF);
                        File.Copy(fi.FullName, sTempNF);
                        vNewVersion = System.Reflection.Assembly.LoadFrom(sTempNF).GetName().Version;
                    }
                    catch { }
                }

                int iIndex = 0;
                bool bIs = false;
                try
                {
                    foreach (stOneRow or in arstUpdates)
                    {
                        if (or.sNameFile == fi.Name)
                        {
                            vOldVersion = new Version(or.sVersion);
                            if (vNewVersion > vOldVersion)
                            {
                                bIs = true;
                                break;
                            }
                            else return;
                        }
                        iIndex++;
                    }
                    if (bIs)
                    {
                        arstUpdates[iIndex].arsIDTerm = null;
                        arstUpdates[iIndex].sVersion = vNewVersion.ToString();
                    }
                    else
                    {
                        if (arstUpdates[0].sNameFile == null)
                        {
                            arstUpdates[0].sNameFile = fi.Name;
                            arstUpdates[0].sVersion = vNewVersion.ToString();
                            arstUpdates[0].arsIDTerm = null;
                        }
                        else
                        {
                            stOneRow[] arstUpdatesNew = new stOneRow[arstUpdates.Length + 1];
                            for (int i = 0; i < arstUpdates.Length; i++) arstUpdatesNew[i] = arstUpdates[i];
                            arstUpdatesNew[arstUpdates.Length].sNameFile = fi.Name;
                            arstUpdatesNew[arstUpdates.Length].sVersion = vNewVersion.ToString();
                            arstUpdatesNew[arstUpdates.Length].arsIDTerm = null;
                            arstUpdates = arstUpdatesNew;
                        }
                    }
                }
                catch { }
            }
        }
        /// <summary>
        /// Добавить информацию в список. На данный терминал было передано данное обновление.
        /// </summary>
        /// <param name="sNameFile">Полное имя файла обновления.</param>
        /// <param name="sIDTerm">Уникальный номер терминала.</param>
        public void AddNewTerminal(string sFullNameFile, string sIDTerm)
        {
            lock (oSyncro)
            {
                FileInfo fi = new FileInfo(sFullNameFile);
                int iIndex = 0;
                bool bIs = false;
                try
                {
                    foreach (stOneRow or in arstUpdates)
                    {
                        if (or.sNameFile == fi.Name)
                        {
                            bIs = true;
                            break;
                        }
                        iIndex++;
                    }
                    if (!bIs)
                    {
                        iIndex = 0;
                        AddNewUpdates(sFullNameFile);
                        foreach (stOneRow or in arstUpdates)
                        {
                            if (or.sNameFile == fi.Name)
                            {
                                bIs = true;
                                break;
                            }
                            iIndex++;
                        }
                    }

                    if (arstUpdates[iIndex].arsIDTerm == null)
                    {
                        arstUpdates[iIndex].arsIDTerm = new string[1];
                        arstUpdates[iIndex].arsIDTerm[0] = sIDTerm;
                        return;
                    }
                    int iCurCountTerm = arstUpdates[iIndex].arsIDTerm.Length;
                    string[] arsNew = new string[iCurCountTerm + 1];
                    for (int i = 0; i < iCurCountTerm; i++)
                    {
                        arsNew[i] = arstUpdates[iIndex].arsIDTerm[i];
                    }
                    arsNew[iCurCountTerm] = sIDTerm;
                    arstUpdates[iIndex].arsIDTerm = arsNew;
                }
                catch { }
            }
        }

        private bool LoadIni(string sFullNameIni)
        {
            bool bRet = false;
            lock (oSyncro)
            {
                FileStream fs = null;
                try
                {
                    fs = new FileStream(sFullNameIni, FileMode.Open);
                    XmlSerializer sr = new XmlSerializer(typeof(stOneRow[]));
                    arstUpdates = (stOneRow[])sr.Deserialize(fs);
                    fs.Close();
                    bRet = true;
                }
                catch (Exception ex)
                {
                    if (fs != null) fs.Close();
                    if (Directory.Exists(Path.GetDirectoryName(sFullNameIni)))
                        Utils.WriteDebugString(Path.GetDirectoryName(sFullNameIni), (" NewSoftUpdates - LoadIni:ERROR - " + ex.Message));
                }
            }
            return bRet;
        }
        private bool SaveIni(string sFullNameIni)
        {
            lock (oSyncro)
            {
                FileStream fs = null;
                try
                {
                    fs = new FileStream(sFullNameIni, FileMode.Create);
                    XmlSerializer sr = new XmlSerializer(typeof(stOneRow[]));
                    sr.Serialize(fs, arstUpdates);
                    fs.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    if (fs != null) fs.Close();
                    if (Directory.Exists(Path.GetDirectoryName(sFullNameIni)))
                        Utils.WriteDebugString(Path.GetDirectoryName(sFullNameIni), (" NewSoftUpdates - SaveIni:ERROR - " + ex.Message));
                    return false;
                }
            }
        }        
        /// <summary>
        /// Одна строка в базе. Соответствует одному имени файла обновления.
        /// </summary>
        public struct stOneRow
        {
            /// <summary>
            /// Имя файла обновления.
            /// </summary>
            public string sNameFile;
            /// <summary>
            /// Версия файла.
            /// </summary>
            public string sVersion;
            /// <summary>
            /// Массив уникальных номеров терминалов на которые обновление было передано.
            /// </summary>
            public string[] arsIDTerm;
        }
    }

    public delegate void MyFileCopy_Complet(bool ifComplete);
    public delegate void MyFileCopy_Progress(string message, int procent);
    public class MyFileCopy
    {
        /// <summary>
        /// Событие на завершение копирования файла
        /// </summary>
        public event MyFileCopy_Complet OnComplete;
        /// <summary>
        /// Событие во время копирования
        /// </summary>
        public event MyFileCopy_Progress OnProgress;

        /// <summary>
        /// Размер буфера в байтах
        /// </summary>
        private int BufferLenght;

        public int BufferLenghtEx
        {
            get { return BufferLenght; }
            set { BufferLenght = value; }
        }

        private int PercentDone;

        public int PercentDoneEx
        {
            get { return PercentDone; }
        }

        public MyFileCopy()
        {
            //задаем размер буфера
            BufferLenght = 1024;
        }

        /// <summary>
        /// Копирование файла
        /// </summary>
        /// <param name="sourceFile">Путь к исходному файлу</param>
        /// <param name="destinationFile">Путь к целевому файлу</param>
        public void CopyFile(string sourceFile, string destinationFile)
        {
            try
            {
                //Создаем буфер по размеру исходного файла
                //В буфер будем записывать информацию из файла
                Byte[] streamBuffer = new Byte[BufferLenght];
                //Общее количество считанных байт
                long totalBytesRead = 0;
                //Количество считываний
                //Используется для задания периода отправки сообщений
                int numReads = 0;

                //Готовим поток для исходного файла
                using (FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
                {
                    //Получаем длину исходного файла
                    long sLenght = sourceStream.Length;
                    //Готовим поток для целевого файла
                    using (FileStream destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write))
                    {
                        //Читаем из буфера и записываем в целевой файл
                        while (true) //Из цикла выйдем по окончанию копирования файла
                        {
                            //Увеличиваем на единицу количество считываний
                            numReads++;
                            //Записываем в буфер streamBuffer BufferLenght байт
                            //bytesRead содержит количество записанных байт
                            //это количество не может быть больше заданного BufferLenght
                            int bytesRead = sourceStream.Read(streamBuffer, 0, BufferLenght);

                            //Если ничего не было считано
                            if (bytesRead == 0)
                            {
                                //Записываем информацию о процессе
                                getInfo(sLenght, sLenght);
                                //и выходим из цикла
                                break;
                            }

                            //Записываем данные буфера streamBuffer в целевой файл
                            destinationStream.Write(streamBuffer, 0, bytesRead);
                            //Для статистики запоминаем сколько уже байт записали
                            totalBytesRead += bytesRead;

                            //Если количество считываний кратно 10
                            if (numReads % 10 == 0)
                            {
                                //Записываем информацию о процессе
                                getInfo(totalBytesRead, sLenght);
                            }

                            //Если количество считанных байт меньше буфера
                            //Значит это конец
                            if (bytesRead < BufferLenght)
                            {
                                //Записываем информацию о процессе
                                getInfo(totalBytesRead, sLenght);
                                break;
                            }
                        }
                    }
                }

                //Отправляем сообщение что процесс копирования закончен удачно
                if (OnComplete != null)
                    OnComplete(true);
            }
            catch //(Exception e)
            {
                //System.Windows.Forms.MessageBox.Show("Возникла следующая ошибка при копировании:\n" + e.Message);
                //Отправляем сообщение что процесс копирования закончен неудачно
                if (OnComplete != null)
                    OnComplete(false);
            }
        }

        /// <summary>
        /// Задаем информацию о процессе копирования
        /// </summary>
        /// <param name="totalBytesRead">Всего байт прочитано</param>
        /// <param name="sLenght">Размер файла</param>
        private void getInfo(long totalBytesRead, long sLenght)
        {
            //Формируем сообщение
            string message = string.Empty;
            double pctDone = (double)((double)totalBytesRead / (double)sLenght);
            PercentDone = (int)(pctDone * 100);
            message = string.Format("Считано: {0} из {1}. Всего {2}%",
                     totalBytesRead,
                     sLenght,
                     (int)(pctDone * 100));
            //Отправляем сообщение подписавшимя на него
            if (OnProgress != null && !double.IsNaN(pctDone))
                OnProgress(message, (int)(pctDone * 100));
        }
    }

    /// <summary>
    /// Читаем версию файла без его загрузки
    /// </summary>
    public static class NativeFile
    {

        public unsafe static Version GetFileInfo(string FullPathToFile)
        {
            Version retV = new Version("0.0.0.0");
            if (!File.Exists(FullPathToFile)) return retV;
            System.Diagnostics.FileVersionInfo fvi;
            fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(FullPathToFile);
            retV = new Version(fvi.ProductVersion);
            return retV;
        }

        /*
        public struct NativeFileInfo
        {
            public Version Version;
            public System.Collections.Specialized.NameValueCollection StringTable;
        }

        public unsafe static Version GetFileInfo(string path)
        {
            if (!File.Exists(path))
            {
                return new Version("0.0.0.0");
            }

            IntPtr handle, buffer = IntPtr.Zero; 

            try
            {
                int size = GetFileVersionInfoSize(path, out handle);
                buffer = Marshal.AllocHGlobal(size);

                if (!GetFileVersionInfo(path, handle, size, buffer))
                {
                    throw new FileNotFoundException();
                }

                IntPtr pVersion;
                int versionLength;
                VerQueryValue(buffer, Path.DirectorySeparatorChar.ToString(), out pVersion, out versionLength);

                VS_FIXEDFILEINFO versionInfo = (VS_FIXEDFILEINFO)Marshal.PtrToStructure(pVersion, typeof(VS_FIXEDFILEINFO));

                Version version = new Version((int)versionInfo.dwFileVersionMS >> 16,
                                          (int)versionInfo.dwFileVersionMS & 0xFFFF,
                                          (int)versionInfo.dwFileVersionLS >> 16,
                                          (int)versionInfo.dwFileVersionLS & 0xFFFF);

                // move to the string table and parse
                //byte* pStringTable = ((byte*)pVersion.ToPointer()) + versionLength;
                //System.Collections.Specialized.NameValueCollection strings = ParseStringTable(pStringTable, size - versionLength);

                //NativeFileInfo nfi = new NativeFileInfo();
                //nfi.Version = version;
                //nfi.StringTable = strings;

                return version;
            }
            catch (Exception ex)
            {

            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            return new Version("0.0.0.0");
        }

        private unsafe static System.Collections.Specialized.NameValueCollection ParseStringTable(byte* pStringTable, int length)
        {
            System.Collections.Specialized.NameValueCollection nvc = new System.Collections.Specialized.NameValueCollection();

            byte* p = pStringTable;
            short stringFileInfoLength = (short)*p;
            byte* end = pStringTable + length;

            p += (2 + 2 + 2); // length + valuelength + type
            // verify key
            string key = Marshal.PtrToStringUni(new IntPtr(p), 14);
            if (key != "StringFileInfo") throw new ArgumentException();

            // move past the key to the first string table
            p += 30;
            short stringTableLength = (short)*p;
            p += (2 + 2 + 2); // length + valuelength + type
            // get locale info
            key = Marshal.PtrToStringUni(new IntPtr(p), 8);

            // move to the first string
            p += 18;

            while (p < end)
            {
                short stringLength = (short)*p;
                p += 2;
                short valueChars = (short)*p;
                p += 2;
                short type = (short)*p;
                p += 2;

                if (stringLength == 0) break;

                if ((valueChars == 0) || (type != 1))
                {
                    p += stringLength;
                    continue;
                }

                int keyLength = stringLength - (valueChars * 2) - 6;
                key = Marshal.PtrToStringUni(new IntPtr(p), keyLength / 2).TrimEnd('\0');
                p += keyLength;
                string value = Marshal.PtrToStringUni(new IntPtr(p), valueChars).TrimEnd('\0');
                p += valueChars * 2;

                if ((int)p % 4 != 0) p += 2;

                nvc.Add(key, value);
            }
            return nvc;
        }

        private const string COREDLL = "coredll.dll";

        [DllImport(COREDLL, SetLastError = true)]
        private static extern int GetFileVersionInfoSize(string lptstrFilename, out IntPtr lpdwHandle);

        [DllImport(COREDLL, SetLastError = true)]
        private static extern bool GetFileVersionInfo(string lptstrFilename, IntPtr dwHandle, int dwLen, IntPtr lpData);

        [DllImport(COREDLL, SetLastError = true)]
        private static extern bool VerQueryValue(IntPtr pBlock, string lpSubBlock, out IntPtr lplpBuffer, out int puLen);

        [StructLayout(LayoutKind.Sequential)]
        private struct VS_FIXEDFILEINFO
        {
            public DWORD dwSignature;
            public DWORD dwStrucVersion;
            public DWORD dwFileVersionMS;
            public DWORD dwFileVersionLS;
            public DWORD dwProductVersionMS;
            public DWORD dwProductVersionLS;
            public DWORD dwFileFlagsMask;
            public DWORD dwFileFlags;
            public FileOS dwFileOS;
            public FileType dwFileType;
            public DWORD dwFileSubtype;
            public DWORD dwFileDateMS;
            public DWORD dwFileDateLS;
        };

        public enum FileOS : uint
        {
            Unknown = 0x00000000,
            DOS = 0x00010000,
            OS2_16 = 0x00020000,
            OS2_32 = 0x00030000,
            NT = 0x00040000,
            WindowsCE = 0x00050000,
        }

        public enum FileType : uint
        {
            Unknown = 0x00,
            Application = 0x01,
            DLL = 0x02,
            Driver = 0x03,
            Font = 0x04,
            VXD = 0x05,
            StaticLib = 0x07
        }
        */
    }
}
