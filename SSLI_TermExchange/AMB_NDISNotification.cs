using System;
using System.IO;
using System.Threading;

namespace AMB_NDISNOTI
{
    /// <summary>
    /// Класс оповещения об изменении статуса сетевых устройств
    /// сначала Init, потом Run.
    /// В конце Dispose (или его вызовет уборка мусора)
    /// </summary>
    public class AMB_NDISNotification : IDisposable
    {
        #region Объявления

        #endregion
        #region Структуры

        #endregion

        private bool bAbort = false;
        FileSystemWatcher wn = null;
        /// <summary>
        /// Событие при изменении статуса сетевых устройств
        /// </summary>
        /// <param name="sNameNDIS">Имя устройства вызвавшего событие</param>
        /// <param name="bStatus">TRUE - было подключено, FALSE - было отключено</param>
        /// <returns></returns>
        public delegate void MyEv_NewNotifi(string sNameNDIS, bool bStatus);
        public static event MyEv_NewNotifi NewNotification;
        /// <summary>
        /// Начало работы. Если все нормально возвращает TRUE.
        /// </summary>
        /// <returns></returns>
        public bool Init()
        {
            try
            {
                wn = new FileSystemWatcher("/sys/class/net"); //!! /sys/class/net/enp0s?/operstate == up or == down
                wn.NotifyFilter = NotifyFilters.DirectoryName;
                wn.IncludeSubdirectories = true;
                wn.Deleted += onChangeNet;
                wn.Created += onChangeNet;
                if (wn == null) return false;
                else return true;
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
            bAbort = false;
            wn.EnableRaisingEvents = true;
        }

        private static void onChangeNet(object source, FileSystemEventArgs e)
        {
            //Console.WriteLine($"Net: { e.FullPath} {e.ChangeType} ");
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
            bAbort = true;
            try
            {
                if (wn != null) wn.EnableRaisingEvents = false;
            }
            catch { }


            try
            {
                if (wn != null) wn.Dispose();
            }
            catch { }

            wn = null;
        }

    }

}
