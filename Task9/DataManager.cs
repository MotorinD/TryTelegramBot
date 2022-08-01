using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Task9
{
    /// <summary>
    /// Сервис для доступа к данным
    /// </summary>
    internal class DataManager
    {
        string catImageUrl = @"https://theoldreader.com/kittens/600/400/";
        string saveCatImagePath = @"1\{0}_{1}.jpg";

        /// <summary>
        /// Загружает со стороннего ресурса изображение кота. 
        /// </summary>
        /// <returns>Путь к сохраненному файлу</returns>
        public string DownloadCatImage(long userId)
        {
            var dateTimeNow = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss-ffff");
            var saveFilePath = string.Format(this.saveCatImagePath, userId, dateTimeNow);

            using (var client = new WebClient())
                client.DownloadFile(this.catImageUrl, saveFilePath);

            return saveFilePath;
        }
    }
}
