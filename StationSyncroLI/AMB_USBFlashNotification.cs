using System;
using System.IO;
using System.Threading;

namespace AMB_USBFLASHNOTY
{
    /// <summary>
    /// Класс оповещения об изменении статуса устройств USB 
    /// сначала Init, потом Run.
    /// В конце Dispose (или его вызовет уборка мусора)
    /// </summary>
    public class AMB_USBFlashNotification : IDisposable
    {
        //#region Объявления

        //#endregion

        //#region Структуры

        //#endregion

        FileSystemWatcher w = null;
        public delegate void MyEv_NewNotifi(string sNameDevice, bool bStatus);
        public static event MyEv_NewNotifi NewNotification;
        /// <summary>
        /// Начало работы. Если все нормально возвращает TRUE.
        /// </summary>
        /// <returns></returns>
        public bool Init()
        {
            try
            {
                w = new FileSystemWatcher("/media");
                w.NotifyFilter = NotifyFilters.DirectoryName;
                w.IncludeSubdirectories = true;
                w.Deleted += onChange;
                w.Created += onChange;
                if (w == null) return false;
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Запуск слушателя
        /// </summary>
        public void Run()
        {
            if (w != null) w.EnableRaisingEvents = true;
        }

        private static void onChange(object source, FileSystemEventArgs e)
        {
            //Console.WriteLine($"File: { e.FullPath} {e.ChangeType} ");
            bool bStat = false;
            //if (File.Exists(e.FullPath)) bStat = true;
            if (e.ChangeType == WatcherChangeTypes.Created) bStat = true;
            NewNotification(e.Name, bStat);
        }



        /// <summary>
        /// Завершение слушателя
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (w != null) w.EnableRaisingEvents = false;
            }
            catch { }


            try
            {
                if (w != null) w.Dispose();
            }
            catch { }

            w = null;
        }
    }
}
