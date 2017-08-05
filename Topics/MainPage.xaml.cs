using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
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
using Windows.Devices.Gpio;
using System.Threading;
using System.Threading.Tasks;
using displayI2C;
using Windows.UI.Core;
using Windows.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windows.Storage.Search;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;



//空白頁項目範本收錄在 http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Topics
{
    /// <summary>
    /// 可以在本身使用或巡覽至框架內的空白頁面。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public class Record //紀錄
        {
            public String time { get; set; }//時間
            public float temperature { get; set; }//溫度
            public float humidity { get; set; }//濕度
            public float soilMoisture { get; set; }//泥土濕度
            public float brightness { get; set; }//亮度

        }
        public class ButtonRecord
        {
            public IList<string> wateringButton { get; set; }//澆水按鈕
            public IList<string> pesticideButton { get; set; }//農藥噴灑按鈕
            public IList<string> illuminationButton { get; set; }//光照按鈕
        }

        private void appBarButton_Click(object sender, RoutedEventArgs e)
        {
            homeGrid.Visibility = Visibility.Collapsed;
            recordGrid.Visibility = Visibility.Visible;
        }

        private async void timerControlPanel()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                string formatTime = "HH:mm";
                string formatDate = "MM/dd/yy";
                ControlPanelTime.Text = DateTime.Now.ToString(formatTime);
                ControlPanelDate.Text = DateTime.Now.ToString(formatDate);
            });
        }

        public MainPage()
        {                       
            Debug.WriteLine("787");
            this.InitializeComponent();
            recordGrid.Visibility = Visibility.Collapsed;
            a1 = new mcp3008();
            a2 = new DHT11.MainPage();
            
            a2.Page_Loaded();
            Unloaded += a1.MainPage_Unloaded;
            //ntrolPanelTimer = new Timer(timerControlPanel, this, 0, 1000);
            var u1Timer = new DispatcherTimer();
            u1Timer.Interval = TimeSpan.FromMinutes(60);
            u1Timer.Tick += timer_Tick;
            u1Timer.Start();
            archives();

            rotationTime = new DispatcherTimer();
            rotationTime.Interval = TimeSpan.FromMilliseconds(10000);
            rotationTime.Tick += rotationTime_Tick;
            rotationTime.Start();
            rotation = 0;            
            InitGpio();

            currentTime = new DispatcherTimer();
            currentTime.Interval = TimeSpan.FromMilliseconds(1000);
            currentTime.Tick += currentTime_Tick;
            currentTime.Start();

            InitMcp3008();
            InitLCD();
            AutomaticTimer();
            u1();
            mqtt();

            _mqttTimer = new DispatcherTimer();
            _mqttTimer.Interval = TimeSpan.FromMilliseconds(500);
            _mqttTimer.Tick += _mqttTimer_Tick;
            _mqttTimer.Start();

            Watering_mq = 0;
            illumination_mq = 0;
            WateringAoto_mq = 0;
            illuminationAoto_mq = 0;

        }

        private async void archives()
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile sampleFile = await folder.CreateFileAsync("buttonRecord.json", CreationCollisionOption.ReplaceExisting);
            ButtonRecord butt = new ButtonRecord
            {
                pesticideButton = new List<string> { },
                wateringButton = new List<string> { },
                illuminationButton = new List<string> { }    
            };
            string json = JsonConvert.SerializeObject(butt);
            await FileIO.WriteTextAsync(sampleFile, json);

        }

        private async void recordControl(string types)
        {
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            StorageFile sampleFile = await storageFolder.GetFileAsync("buttonRecord.json");
            if (types== "watering")
            {
                try
                {
                    string text = await FileIO.ReadTextAsync(sampleFile);
                    ButtonRecord person = JsonConvert.DeserializeObject<ButtonRecord>(text);
                    person.wateringButton.Add(DateTime.Now.ToString());
                    string json = JsonConvert.SerializeObject(person);
                    await FileIO.WriteTextAsync(sampleFile,json);
                }
                catch (Exception)
                {
                    ButtonRecord butt = new ButtonRecord
                    {
                        wateringButton = new List<string> { }

                    };
                    butt.wateringButton.Add(DateTime.Now.ToString());
                    string json = JsonConvert.SerializeObject(butt);
                    await FileIO.WriteTextAsync(sampleFile, json);

                }
            }
            else if(types == "illumination")
            {
                try
                {
                    string text = await FileIO.ReadTextAsync(sampleFile);
                    ButtonRecord person = JsonConvert.DeserializeObject<ButtonRecord>(text);
                    person.illuminationButton.Add(DateTime.Now.ToString());
                    string json = JsonConvert.SerializeObject(person);
                    await FileIO.WriteTextAsync(sampleFile, json);
                }
                catch (Exception)
                {
                    ButtonRecord butt = new ButtonRecord
                    {
                        illuminationButton = new List<string> { }

                    };
                    butt.illuminationButton.Add(DateTime.Now.ToString());
                    string json = JsonConvert.SerializeObject(butt);
                    await FileIO.WriteTextAsync(sampleFile, json);

                }
            }
            else if (types == "pesticide")
            {
                try
                {
                    string text = await FileIO.ReadTextAsync(sampleFile);
                    ButtonRecord person = JsonConvert.DeserializeObject<ButtonRecord>(text);
                    person.pesticideButton.Add(DateTime.Now.ToString());
                    string json = JsonConvert.SerializeObject(person);
                    await FileIO.WriteTextAsync(sampleFile, json);
                }
                catch (Exception)
                {
                    ButtonRecord butt = new ButtonRecord
                    {
                        pesticideButton = new List<string> { }

                    };
                    butt.pesticideButton.Add(DateTime.Now.ToString());
                    string json = JsonConvert.SerializeObject(butt);
                    await FileIO.WriteTextAsync(sampleFile, json);

                }
            }
        }

        private void _mqttTimer_Tick(object sender, object e)
        {
            if (Watering_mq==0)
            {

            }
            else if (Watering_mq == 1)
            {
                Watering_mq = 0;
                Watering_p();
            }
            
            //
            if (illumination_mq == 0)
            {

            }
            else if (illumination_mq == 1)
            {
                illumination_mq = 0;
                illumination_p();
            }
            
            //
            if (WateringAoto_mq == 0)
            {
                Watering.IsOn = false;
            }
            else if (WateringAoto_mq == 1)
            {
                Watering.IsOn = true;
            }
            else if (WateringAoto_mq == 2)
            {

            }
            //
            if (illuminationAoto_mq == 0)
            {
                illumination.IsOn = false;
            }
            else if (illuminationAoto_mq == 1)
            {
                illumination.IsOn = true;
            }
            else if (illuminationAoto_mq == 2)
            {

            }
            //
            if(pesticide_mq==0)
            {
                

            }
            else if (pesticide_mq==1)
            {
                pesticide_mq = 0;
                pesticideStart();
                pesticideStop.Start();
            }

            if (recordControl_mq==1)
            {
                recordControl_mq = 0;
                recordControl_mqtt();

            }

        }

        private async void recordControl_mqtt()
        {
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            StorageFile sampleFile = await storageFolder.GetFileAsync("buttonRecord.json");
            string text = await Windows.Storage.FileIO.ReadTextAsync(sampleFile);
            ushort msgId = client.Publish("tw/recordControl_qa/", // topic
                                          Encoding.UTF8.GetBytes(text), // message body
                                          MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, // QoS level
                                          false); // retained

        }

        private void currentTime_Tick(object sender, object e)
        {
            timerControlPanel();
        }


        private void rotationTime_Tick(object sender, object e)
        {
            if (rotation==0)
            {
                rotation = 1;
            }
            else
            {
                rotation = 0;
            }
        }

        private void AutomaticTimer()
        {
            wateringDetermination = new DispatcherTimer();
            wateringDetermination.Interval = TimeSpan.FromMinutes(10);
            wateringDetermination.Tick += wateringDetermination_Tick;

            wateringStop = new DispatcherTimer();
            wateringStop.Interval = TimeSpan.FromMilliseconds(3000);
            wateringStop.Tick += wateringStop_Tick;

            illuminationDetermination = new DispatcherTimer();
            illuminationDetermination.Interval = TimeSpan.FromMilliseconds(500);
            illuminationDetermination.Tick += illuminationDetermination_Tick;

            pesticideStop = new DispatcherTimer();
            pesticideStop.Interval = TimeSpan.FromMilliseconds(3000);
            pesticideStop.Tick += pesticideStop_Tick;

            

        }

        private void illuminationStop()
        {
            pin13.Write(GpioPinValue.Low);
            pin2 = 0;
        }

        private void illuminationStart()
        {
            pin13.Write(GpioPinValue.High);
            pin2 = 1;
        }

        private void illuminationDetermination_Tick(object sender, object e)
        {
            
            if (1024-Lumi < 500)
            {
                illuminationStart();
            }
            else
            {
                illuminationStop();
            }
        }

        private void wateringStop_Tick(object sender, object e)
        {
            
            pin26.Write(GpioPinValue.Low);
            pin = 0;
            Watering.IsEnabled = true;
            WateringButton.IsEnabled = true;
            wateringStop.Stop();

        }

        private void wateringStart()
        {
            Watering.IsEnabled = false;
            WateringButton.IsEnabled = false;
            pin26.Write(GpioPinValue.High);
            pin = 1;
        }

        private void pesticideStop_Tick(object sender, object e)
        {

            pin19.Write(GpioPinValue.Low);
            pin3 = 0;
            pesticide.IsEnabled = true;
            pesticideStop.Stop();
            Debug.WriteLine("pesticideStop_Tick");

        }

        private void pesticideStart()
        {
            pesticide.IsEnabled = false;
            pin19.Write(GpioPinValue.High);
            pin3 = 1;
        }

        private void wateringDetermination_Tick(object sender, object e)
        {
            if (Soil * 0.01 < 3)
            {
                wateringStart();
                wateringStop.Start();
            }
        }

        private async void InitMcp3008()
        {
            try
            {
                await a1.InitSPI();
            }
            catch (Exception ex)
            {
                throw new Exception("InitMcp3008失敗", ex);

            }
            periodicTimer = new Timer(this.Timer_Tick, null, 0, 1000);
        }

        private void Timer_Tick(object state)
        {
            Channel = 0;
            for (int i = 0; i < 2; i++)
            {
                Channel = i;
                a1.async((byte)Channel);
                if (Channel == 0)
                {
                    luminosity = "B:" + ((1024 - a1.adcValue) * 0.01).ToString("0.0").ToString();
                    

                    var task = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        textBlock6.Text = ((1024 - a1.adcValue) * 0.01).ToString("0.0").ToString();         /*亮度       */
                        Lumi = a1.adcValue;
                        ushort msgId = client.Publish("tw/luminosity/", // topic
                                          Encoding.UTF8.GetBytes(((1024 - a1.adcValue) * 0.01).ToString("0.0").ToString()), // message body
                                          MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, // QoS level
                                          false); // retained

                    });
                }
                else
                {
                    soilMoisture = "S:" + (a1.adcValue * 0.01).ToString("0.0").ToString();
                    

                    var task2 = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        textBlock7.Text = (a1.adcValue * 0.01).ToString("0.0").ToString();         /*泥土濕度  */
                        Soil = a1.adcValue;
                        ushort msgId = client.Publish("tw/soilMoisture/", // topic
                                          Encoding.UTF8.GetBytes((a1.adcValue * 0.01).ToString("0.0").ToString()), // message body
                                          MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, // QoS level
                                          false); // retained

                    });
                }
                //lcd.prints((a1.adcValue).ToString());
                var task3 = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    textBlock1.Text = (a2.Temperature()).ToString() + "℃";            /*溫度*/
                    textBlock5.Text = (a2.Humidity()).ToString() + "% RH";           /*濕度*/
                    temperature = "T:" + (a2.Temperature()).ToString() + "C"; 
                    humidity = "H:" + (a2.Humidity()).ToString() + "% RH";
                    Temp= a2.Temperature();
                    Humi = a2.Humidity();
                    ushort msgId = client.Publish("tw/Temperature/", // topic
                                          Encoding.UTF8.GetBytes((a2.Temperature()).ToString()), // message body
                                          MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, // QoS level
                                          false); // retained
                    ushort msgId2 = client.Publish("tw/Humidity/", // topic
                                          Encoding.UTF8.GetBytes((a2.Humidity()).ToString()), // message body
                                          MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, // QoS level
                                          false); // retained




                });



            }

            
        }

        private void InitGpio()
        {
            var gpio = GpioController.GetDefault();
            if (gpio == null)
            {
                throw new Exception("此設備上沒有GPIO控制器");
            }
            else
            {
                pin26 = gpio.OpenPin(26);
                pin26.SetDriveMode(GpioPinDriveMode.Output);
                pin26.Write(GpioPinValue.Low);
                pin = 0;
                pin13 = gpio.OpenPin(13);
                pin13.SetDriveMode(GpioPinDriveMode.Output);
                pin13.Write(GpioPinValue.Low);
                pin2 = 0;
                pin19 = gpio.OpenPin(19);
                pin19.SetDriveMode(GpioPinDriveMode.Output);
                pin19.Write(GpioPinValue.Low);
                pin3 = 0;
            }
            

        }

        private void InitLCD()
        {

            displayI2CLCD lcd = new displayI2CLCD(DEVICE_I2C_ADDRESS, I2C_CONTROLLER_NAME, RS, RW, EN, D4, D5, D6, D7, BL);
            lcd.init();
            lcd.createSymbol(new byte[] { 0x00, 0x00, 0x0A, 0x00, 0x11, 0x0E, 0x00, 0x00 }, 0x00);

            var t = Task.Run(async delegate
            {
                while (true)
                {


                    if (rotation == 0)
                    {
                        await Task.Delay(1000);
                        lcd.init();
                        //lcd.createSymbol(new byte[] { 0x00, 0x00, 0x0A, 0x00, 0x11, 0x0E, 0x00, 0x00 }, 0x00);
                        lcd.gotoxy(0, 0);
                        lcd.prints(soilMoisture);
                        lcd.gotoxy(0, 1);
                        lcd.prints(luminosity);
                    }
                    else
                    {
                        await Task.Delay(1000);
                        lcd.init();
                        //lcd.createSymbol(new byte[] { 0x00, 0x00, 0x0A, 0x00, 0x11, 0x0E, 0x00, 0x00 }, 0x00);
                        lcd.gotoxy(0, 0);
                        lcd.prints(temperature);
                        lcd.gotoxy(0, 1);
                        lcd.prints(humidity);
                    }
                }

            });

        }
        private void u1()
        {

            List<Record> hourData = new List<Record>();
            hourData.Add(new Record()
            {
                time = DateTime.Now.ToString(),
                /*
                temperature = 20.0f,
                humidity = 70.5f,
                SoilMoisture = 90.8f,
                brightness = 40.8f*/
                
                temperature =(float)Temp,
                humidity = (float)Humi,
                soilMoisture = (float)Soil,
                brightness = (float)Lumi//((1024 - Lumi) * 0.1)
                

            });
            //textBlock9.Text = "456";//((float)Temp).ToString();


            string json_data = JsonConvert.SerializeObject(hourData, Formatting.Indented);
            
            Archive(json_data);
        }
        private void timer_Tick(object sender, object e)
        {
            
            u1();
            

        }
        private async void Archive(String hourData)
        {
            try
            {
                //string tr = DateTime.Now.Ticks.ToString() + ".txt";
                string tr = DateTime.Now.Ticks.ToString() + ".txt";
                //StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                StorageFolder storageFolder = ApplicationData.Current.TemporaryFolder;
                StorageFile sampleFile = await storageFolder.CreateFileAsync(tr, CreationCollisionOption.ReplaceExisting);
                var stream = await sampleFile.OpenAsync(FileAccessMode.ReadWrite);
                using (var outputStream = stream.GetOutputStreamAt(0))
                {
                    // We'll add more code here in the next step.
                    using (var dataWriter = new Windows.Storage.Streams.DataWriter(outputStream))
                    {
                        dataWriter.WriteString(hourData);
                        await dataWriter.StoreAsync();
                        await outputStream.FlushAsync();
                    }
                }
                stream.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Archive: " + ex);
            }
        }
        private void Watering_Toggled(object sender, RoutedEventArgs e)//澆水
        {
            Watering_Toggled();
        }

        private void Watering_Toggled()
        {
            if (Watering.IsOn == true)//開
            {
                WateringAoto_mq = 1;
                if (a1.adcValue * 0.01 < 3)
                {
                    wateringStart();
                    wateringStop.Start();
                }
                wateringDetermination.Start();
                ushort msgId = client.Publish("tw/WateringAoto_qa/", // topic
                                          Encoding.UTF8.GetBytes("1"), // message body
                                          MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, // QoS level
                                          false); // retained

            }
            else//關
            {
                WateringAoto_mq = 0;
                wateringStop.Stop();
                ushort msgId = client.Publish("tw/WateringAoto_qa/", // topic
                                          Encoding.UTF8.GetBytes("0"), // message body
                                          MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, // QoS level
                                          false); // retained
            }
        }
        private void WateringButton_Click(object sender, RoutedEventArgs e)
        {
            Watering_p();
        }

        private void Watering_p()
        {
            recordControl("watering");
            wateringStart();
            wateringStop.Start();
        }

        private void illumination_Toggled(object sender, RoutedEventArgs e)
        {
            illumination_Toggled_p();
        }

        private void illumination_Toggled_p()
        {
            if (illumination.IsOn == true)
            {
                illuminationAoto_mq = 1;
                illuminationDetermination.Start();
                illuminationButton.Content = "手動光照";
                //illuminationStop();
                illBut = 0;

                ushort msgId = client.Publish("tw/illuminationAoto_qa/", // topic
                                          Encoding.UTF8.GetBytes("1"), // message body
                                          MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, // QoS level
                                          false); // retained
            }
            else
            {
                illuminationAoto_mq = 0;
                illuminationDetermination.Stop();

                ushort msgId = client.Publish("tw/illuminationAoto_qa/", // topic
                                          Encoding.UTF8.GetBytes("0"), // message body
                                          MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, // QoS level
                                          false); // retained
            }
        }

        private void illuminationButton_Click(object sender, RoutedEventArgs e)
        {
            illumination_p();
        }

        private void illumination_p()
        {
            recordControl("illumination");
            if (illBut == 0)
            {
                illumination.IsOn = false;
                illuminationButton.Content = "手動關閉光照";
                illuminationStart();
                illBut = 1;
            }
            else
            {
                illuminationButton.Content = "手動光照";
                illuminationStop();
                illBut = 0;
            }
        }

        private void pesticide_Click(object sender, RoutedEventArgs e)//農藥噴灑
        {
            recordControl("pesticide");
            pesticideStart();
            pesticideStop.Start();
        }



        private void theDayBeforeYesterdayButton_Click(object sender, RoutedEventArgs e)
        {
            Read(-2);
        }

        private void yesterdayButton_Click(object sender, RoutedEventArgs e)
        {
            Read(-1);
            
        }

        private void dayButton_Click(object sender, RoutedEventArgs e)
        {
            Read(0);
            
        }

        private async void Read(int p)
        {
            StorageFolder storageFolder = ApplicationData.Current.TemporaryFolder;
            IReadOnlyList<StorageFile> fileList = await storageFolder.GetFilesAsync();
            List<Record> chart_Data = new List<Record>();
            foreach (StorageFile folder in fileList)
            {
                char delimiterChars = '.';
                string[] words = folder.Name.Split(delimiterChars);
                long fileName = long.Parse(words[0]);
                long p1 = DateTime.Today.AddDays(p).Ticks;
                long p2 = DateTime.Today.AddDays(p).AddHours(24).AddMilliseconds(-1).Ticks;
                if (fileName > p1)
                {
                    if (fileName < p2)
                    {
                        StorageFile sampleFile = await storageFolder.GetFileAsync(folder.Name);
                        string text = await FileIO.ReadTextAsync(sampleFile);
                        var readLine = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            try
                            {

                                Record[] json1 = JsonConvert.DeserializeObject<Record[]>(text);
                                Record firstObject = json1[0];
                                textBlock9.Text = firstObject.time;

                                chart_Data.Add(new Record()
                                {
                                    time = firstObject.time,
                                    temperature = (float)firstObject.temperature,
                                    humidity = (float)firstObject.humidity,
                                    soilMoisture = (float)firstObject.soilMoisture,
                                    brightness = (float)firstObject.brightness,
                                });


                            }
                            catch (Exception ex)
                            {
                                string yi = ex.ToString();

                            }



                            //string json_data = JsonConvert.SerializeObject(json);
                        });
                    }
                }
            }
            (temperatureChart.Series[0] as LineSeries).ItemsSource = chart_Data;
            (humidityChart.Series[0] as LineSeries).ItemsSource = chart_Data;
            (SoilMoistureChart.Series[0] as LineSeries).ItemsSource = chart_Data;
            (brightnessChart.Series[0] as LineSeries).ItemsSource = chart_Data;
        }

        private void appBarButton1_Click(object sender, RoutedEventArgs e)
        {
            homeGrid.Visibility = Visibility.Visible;
            recordGrid.Visibility = Visibility.Collapsed;
        }

        

        private void mqtt()
        {
            client = new MqttClient("iot.eclipse.org");
            byte code = client.Connect(Guid.NewGuid().ToString());             

            ushort msgId = client.Subscribe(PublishTopic,
                new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE,MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE
                ,MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE
            });
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            
            Debug.WriteLine("77");

            client.ConnectionClosed += Client_ConnectionClosed;
        }

        private static void Client_ConnectionClosed(object sender, EventArgs e)
        {
            Debug.WriteLine("Client_ConnectionClosed");
        }

        private  void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            Debug.WriteLine("Received = " + Encoding.UTF8.GetString(e.Message) + " on topic " + e.Topic);

            if (e.Topic.ToString() == "tw/Watering/")
            {
                Debug.WriteLine("if_Watering");
                Watering_mq = Int32.Parse(Encoding.UTF8.GetString(e.Message));
                

            }
            else if (e.Topic.ToString() == "tw/illumination/")
            {
                Debug.WriteLine("if_illumination");
                illumination_mq = Int32.Parse(Encoding.UTF8.GetString(e.Message));

            }
            else if (e.Topic.ToString() == "tw/WateringAoto/")
            {
                Debug.WriteLine("if_WateringAoto");
                WateringAoto_mq = Int32.Parse(Encoding.UTF8.GetString(e.Message));
            }
            else if (e.Topic.ToString() == "tw/illuminationAoto/")
            {
                Debug.WriteLine("if_illuminationAoto");
                illuminationAoto_mq = Int32.Parse(Encoding.UTF8.GetString(e.Message));
            }
            else if(e.Topic.ToString() == "tw/pesticide/")
            {
                Debug.WriteLine("if_pesticide");
                pesticide_mq = Int32.Parse(Encoding.UTF8.GetString(e.Message));
            }
            else if (e.Topic.ToString() == "tw/recordControl/")
            {
                recordControl_mq = 1;
            }
            else
            {
                Debug.WriteLine("if_recordControl");
                Debug.WriteLine("if_" + e.Topic);
                
            }

        }



        private mcp3008 a1;
        private DHT11.MainPage a2;
        private int Channel;
        private Timer periodicTimer;        
        private displayI2CLCD lcd;
        private String soilMoisture = " ";
        private String luminosity = " ";
        private String temperature = " ";
        private String humidity = " ";
        public Timer controlPanelTimer;
        private int pin;
        private GpioPin pin26;//澆水
        private int pin2;
        private GpioPin pin13;//光罩
        private GpioPin pin19;//農藥噴灑
        private int pin3;
        private Double illBut;
        private double Lumi;//亮度
        private double Soil;//泥土濕度
        private double Temp;//溫度
        private double Humi;//濕度

        public static MqttClient client;
        public static string[] PublishTopic = new string[] { "tw/Watering/", "tw/illumination/", "tw/WateringAoto/",
            "tw/illuminationAoto/","tw/pesticide/","tw/recordControl/" };
        private int Watering_mq;
        private int illumination_mq;
        private int WateringAoto_mq;
        private int illuminationAoto_mq;
        private int pesticide_mq;
        private int recordControl_mq;






        private DispatcherTimer wateringDetermination;
        private DispatcherTimer wateringStop;
        private DispatcherTimer pesticideStop;
        private DispatcherTimer illuminationDetermination;
        private DispatcherTimer rotationTime;
        private DispatcherTimer currentTime;
        private DispatcherTimer _mqttTimer;
        private int rotation;

        private List<string> k1;
        private List<float> k2;
        private List<float> k3;
        private List<float> k4;
        private List<float> k5;
        private int pa1;




        //LCD
        //設置位置
        private const string I2C_CONTROLLER_NAME = "I2C1"; //use for RPI2
        private const byte DEVICE_I2C_ADDRESS = 0x27; // 7-bit I2C address of the port expander

        //設置pin
        private const byte EN = 0x02;
        private const byte RW = 0x01;
        private const byte RS = 0x00;
        private const byte D4 = 0x04;
        private const byte D5 = 0x05;
        private const byte D6 = 0x06;
        private const byte D7 = 0x07;
        private const byte BL = 0x03;

        
    }
}





