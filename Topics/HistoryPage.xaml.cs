using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading;
using Windows.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windows.Storage.Search;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;

// 空白頁項目範本已記錄在 http://go.microsoft.com/fwlink/?LinkId=234238

namespace Topics
{
    /// <summary>
    /// 可以在本身使用或巡覽至框架內的空白頁面。
    /// </summary>
    public sealed partial class HistoryPage : Page
    {
        public class Record //紀錄
        {
            public String time { get; set; }//時間
            public Double temperature { get; set; }//溫度
            public Double humidity { get; set; }//濕度
            public Double SoilMoisture { get; set; }//泥土濕度
            public Double brightness { get; set; }//亮度

        }
        

        public HistoryPage()
        {
            try
            {
                this.InitializeComponent();
            }
            catch (Exception ex)
            {
                
                //showMessageBox("", ex.Message.ToString());
                throw;
            }
            
        }

        /*
        private async void Read(int p)
        {

            //string tr = DateTime.Now.Ticks.ToString() + ".txt"; 
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;

            StorageFolderQueryResult queryResult = storageFolder.CreateFolderQuery();
            IReadOnlyList<StorageFolder> folderList = await queryResult.GetFoldersAsync();

            foreach (StorageFolder folder in folderList)
            {
                IReadOnlyList<StorageFile> fileList = await folder.GetFilesAsync();
                foreach (StorageFile file in fileList)
                {
                    char delimiterChars = '.';
                    string[] words = file.Name.Split(delimiterChars);
                    long fileName = long.Parse(words[0]);
                    long a1 = DateTime.Today.AddDays(p).Ticks;
                    long a2 = DateTime.Today.AddDays(p).Ticks + 999999999;

                    if (fileName > a1)
                    {
                        if (fileName > a2)
                        {
                            String name = fileName.ToString() + ".txt";
                            StorageFile sampleFile = await storageFolder.GetFileAsync(name);
                            var stream = await sampleFile.OpenAsync(FileAccessMode.ReadWrite);
                            ulong size = stream.Size;
                            using (var inputStream = stream.GetInputStreamAt(0))
                            {
                                using (var dataReader = new Windows.Storage.Streams.DataReader(inputStream))
                                {
                                    uint numBytesLoaded = await dataReader.LoadAsync((uint)size);
                                    string text = dataReader.ReadString(numBytesLoaded);
                                    dynamic json = JValue.Parse(text);
                                    List<Record> chart_Data = new List<Record>();
                                    chart_Data.Add(new Record()
                                    {
                                        time = json.time,
                                        temperature = json.temperature,
                                        humidity = json.humidity,
                                        SoilMoisture = json.SoilMoisture,
                                        brightness = json.brightness,
                                    });
                                    (temperature.Series[0] as LineSeries).ItemsSource = chart_Data;
                                    (humidity.Series[0] as LineSeries).ItemsSource = chart_Data;
                                    (SoilMoisture.Series[0] as LineSeries).ItemsSource = chart_Data;
                                    (brightness.Series[0] as LineSeries).ItemsSource = chart_Data;



                                }
                            }

                        }
                    }
                }
            }

        }
        */

        private void theDayBeforeYesterdayButton_Click(object sender, RoutedEventArgs e)
        {
            //Read(-2);
        }

        private void yesterdayButton_Click(object sender, RoutedEventArgs e)
        {
            //Read(-1);
        }

        private void dayButton_Click(object sender, RoutedEventArgs e)
        {
            //Read(0);
        }


        private DHT11.MainPage a2;
        private mcp3008 a1;

       
    }
}
