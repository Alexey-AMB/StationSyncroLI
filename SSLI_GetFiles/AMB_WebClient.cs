using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace AMB_WEBCLIENT
{
    public class WebClient
    {
        /// <summary>
        /// Закачка файла по протоколу HTTP.
        /// </summary>
        /// <param name="url">URL-путь к файлу. Типа: @"http://192.168.0.101:80/SUVDWEB/FullBases/List.xml"</param>
        /// <param name="destination">Полное имя файла для сохранения.</param>
        /// <returns>TRUE если все ОК.</returns>
        public bool DownloadFile(string url, string destination)
        {
            bool success = false;

            System.Net.HttpWebRequest request = null;
            System.Net.WebResponse response = null;
            Stream responseStream = null;
            FileStream fileStream = null;

            try
            {
                request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = 100000; // 100 seconds
                response = request.GetResponse();

                responseStream = response.GetResponseStream();

                fileStream = File.Open(destination, FileMode.Create, FileAccess.Write, FileShare.None);

                int maxRead = 2048; //10240
                byte[] buffer = new byte[maxRead];
                int bytesRead = 0;
                int totalBytesRead = 0;

                while ((bytesRead = responseStream.Read(buffer, 0, maxRead)) > 0)
                {
                    totalBytesRead += bytesRead;
                    fileStream.Write(buffer, 0, bytesRead);
                    System.Threading.Thread.Sleep(1);
                }
                success = true;
            }
            catch //(Exception exp)
            {
                success = false;
                //Debug.WriteLine(exp);
            }
            finally
            {
                if (null != responseStream) responseStream.Close();
                if (null != response) response.Close();
                if (null != fileStream) fileStream.Close();
            }

            // При обрыве связи удаляем кусок закачанного
            try
            {
                if (!success && File.Exists(destination)) File.Delete(destination);
            }
            catch { }

            return success;
        }
        /// <summary>
        /// Открытие потока из файла на сервере на чтение.
        /// </summary>
        /// <param name="url">URL-путь к файлу. Типа: @"http://192.168.0.101:80/SUVDWEB/FullBases/List.xml"</param>
        /// <returns>Открытый поток если все ОК или null при ошибке.</returns>
        public Stream OpenRead(string url)
        {
            System.Net.HttpWebRequest request = null;
            System.Net.WebResponse response = null;
            Stream responseStream = null;

            try
            {
                request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = 100000; // 100 seconds
                response = request.GetResponse();

                responseStream = response.GetResponseStream();
            }
            catch
            {
                if (responseStream != null) responseStream.Close();
                return null;
            }
            return responseStream;
        }
    }
}
