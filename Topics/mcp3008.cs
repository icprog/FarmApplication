using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using Windows.Devices.Enumeration;

namespace Topics
{

    public class mcp3008 
    {
        
        

        public async Task InitSPI()
        {
            try
            {
                var settings = new SpiConnectionSettings(SPI_CHIP_SELECT_LINE);
                settings.ClockFrequency = 500000;   /* 0.5MHz clock rate                                        */
                settings.Mode = SpiMode.Mode0;      /* The ADC expects idle-low clock polarity so we use Mode0  */


                string spiAs = SpiDevice.GetDeviceSelector("SPI0");
                var deviceinfo = await DeviceInformation.FindAllAsync(spiAs);
                SpiADC = await SpiDevice.FromIdAsync(deviceinfo[0].Id, settings);

            }
            catch (Exception ex)
            {

                throw new Exception("SPI初始化失敗" , ex);
            }
        }

        public void  async(byte whichChannel)//SPI取值
        {
            //whichChannel=0
            byte command = whichChannel;
            command |= 0x08;//command跟0x08(00001000)進行OR運算
            command <<= 4;//向左移位4位元
            byte[] readBuffer = new byte[3];
            byte[] writeBuffer = new byte[3] { 0x01, command, 0x00 };

            SpiADC.TransferFullDuplex(writeBuffer, readBuffer);//rpi3和mcp3008傳輸(全雙工)

            adcValue =convertToInt(readBuffer);
                

        }

        private int convertToInt(byte[] data)
        {
            int result = 0;

            result = data[1] & 0x0F;
            result <<= 8;
            result += data[2];
            return result;
        }



        public void MainPage_Unloaded(object sender, object args)
        {
            if (SpiADC != null)
            {
                SpiADC.Dispose();
            }
        }

        private const string SPI_CONTROLLER_NAME = "SPI0";
        private const Int32 SPI_CHIP_SELECT_LINE = 0;
        private  SpiDevice SpiADC;
        public int adcValue;

        //public int adcValue;


    }
    


}
