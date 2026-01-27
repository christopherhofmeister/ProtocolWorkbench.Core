using FTD2XX_NET;
using ProtocolWorkbench.Core.Services.UartDevice;
using ProtocolWorkBench.Core;
using ProtocolWorkBench.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ProtocolWorkbench.Core.Services.UartManager
{
    public class UartManager : IUartManager
    {
        private int MAX_NUMBER_COMS = 8;

        public List<IUartDevice> SerialPorts { get; set; }

        private List<int> MockComPorts;

        private bool debugMode { get; set; }
        public bool DebugMode
        {
            get => debugMode;
            set
            {
                debugMode = value;
                DebugService.OutputDebug = value;
            }
        }

        public UartManager()
        {
            SerialPorts = new List<IUartDevice>();
            MockComPorts = new List<int>();
        }

        /// <summary>
        /// Get names of COM Port on device, for example COM1, COM2
        /// </summary>
        /// <param name="ftdiOnly">Return only FTDI COM Ports</param>
        /// <param name="closeComPorts">Close Any Open Com Ports</param>
        /// <param name="mockNumber">Optional Param for Mock Service</param>
        /// <param name="mockStartEnum">Optional Parm for Mock Service</param>
        /// <returns></returns>
        public List<FtdiDevice> GetComPorts(bool ftdiOnly, bool closeComPorts, int maxNumber = 0, int mockStartEnum = 0)
        {
            List<FtdiDevice> coms = new List<FtdiDevice>();
            if (closeComPorts)
            {
                CloseComPorts();
                SerialPorts = new List<IUartDevice>();
            }
#if MOCK
            coms = GetComPortsMock(ftdiOnly, mockNumber, mockStartEnum);
#else
            coms = GetComPortsReal(ftdiOnly, (UInt32)maxNumber);
#endif
            foreach (FtdiDevice com in coms)
            {
                // Add Com Port if it doesn't exist in SerialPorts
                if (SerialPorts.Where(x => x.PortName == com.ComPort).Count() == 0)
                {
#if MOCK
                    SerialPorts.Add(new MockUartDevice { PortName = com.ComPort, FtdiDeviceType = com.FtdiDeviceType, FtdiSerialNumber = com.SerialNumber });
#else
                    SerialPorts.Add(new UartDevice.UartDevice { PortName = com.ComPort, FtdiDeviceType = com.FtdiDeviceType, FtdiSerialNumber = com.SerialNumber });
#endif
                }
            }

            return coms;
        }

        /// <summary>
        /// Close any Com Port that is currently open.
        /// </summary>
        public void CloseComPorts()
        {
            if (SerialPorts != null)
            {
                for (int i = 0; i < SerialPorts.Count(); i++)
                {
                    if (SerialPorts[i] != null)
                    {
                        SerialPorts[i].CloseSerialPort();
                    }
                }
            }
            SerialPorts = new List<IUartDevice>();
            MockComPorts = new List<int>();
        }

        /// <summary>
        /// Clear out any user attributes on open Com Ports
        /// </summary>
        public void ClearUserAttributes()
        {
            if (SerialPorts != null)
            {
                for (int i = 0; i < SerialPorts.Count(); i++)
                {
                    if (SerialPorts[i] != null)
                    {
                        SerialPorts[i].UserAttribute1 = null;
                        SerialPorts[i].UserAttribute2 = null;
                    }
                }
            }
        }

        /// <summary>
        /// Get an instance to the Uart Device based on the Com Port.
        /// </summary>
        /// <param name="comPort"></param>
        /// <returns>True if Com Port found, and an instance to it.</returns>
        public (bool Success, IUartDevice Uart) GetUartByComPort(string comPort)
        {
            bool success = false;
            IUartDevice uart = null;
            if (SerialPorts != null && SerialPorts.Count > 0)
            {
                List<IUartDevice> uarts = SerialPorts.Where(x => x.PortName == comPort).ToList();
                if (uarts.Count == 1)
                {
                    success = true;
                    return (success, uarts[0]);
                }
            }
            return (success, uart);
        }

        public void CloseComPort(string comPort)
        {
            var result = GetUartByComPort(comPort);
            if (result.Success)
            {
                result.Uart.CloseSerialPort();
                SerialPorts.Remove(result.Uart);
            }
        }

        private List<FtdiDevice> GetComPortsReal(bool FTDIOnly, UInt32 numberOfNodes = 0)
        {
            List<FtdiDevice> comPorts = new List<FtdiDevice>();

            if (FTDIOnly)
            {
                UInt32 ftdiDeviceCount = 0;
                FTDI ftdiLib = new FTDI();
                FTDI.FT_STATUS ftStatus = ftdiLib.GetNumberOfDevices(ref ftdiDeviceCount);
                if (ftStatus == FTDI.FT_STATUS.FT_OK)
                {
                    if (numberOfNodes != 0)
                    {
                        ftdiDeviceCount = numberOfNodes;
                    }

                    FTDI.FT_DEVICE_INFO_NODE[] ftdiDeviceList = new FTDI.FT_DEVICE_INFO_NODE[ftdiDeviceCount];
                    ftStatus = ftdiLib.GetDeviceList(ftdiDeviceList);
                    if (ftStatus == FTDI.FT_STATUS.FT_OK)
                    {
                        for (UInt32 i = 0; i < ftdiDeviceCount; i++)
                        {
                            FTDI lib1 = new FTDI();
                            ftStatus = lib1.OpenByIndex(i);
                            if (ftStatus != FTDI.FT_STATUS.FT_OK)
                            {
                                byte lineStatus = 0;
                                lib1.GetLineStatus(ref lineStatus);
                                ftStatus = lib1.RestartInTask();
                                ftStatus = lib1.ResetPort();
                                ftStatus = lib1.CyclePort();
                                continue;
                            }
                            string comPort = null;
                            string serialNumber = null;
                            FTDI.FT_DEVICE deviceType = FTDI.FT_DEVICE.FT_DEVICE_UNKNOWN;
                            ftStatus = lib1.GetCOMPort(out comPort);
                            if (ftStatus == FTDI.FT_STATUS.FT_OK)
                            {
                                ftStatus= lib1.GetSerialNumber(out serialNumber);
                                if (ftStatus == FTDI.FT_STATUS.FT_OK)
                                {
                                    lib1.GetDeviceType(ref deviceType);
                                }
                                comPorts.Add(new FtdiDevice {ComPort = comPort, SerialNumber = serialNumber, FtdiDeviceType = deviceType });
                            }
                            lib1.Close();
                        }
                    }
                }
                ftdiLib.Close();
            }
            else
            {
                string[] comPortNames = System.IO.Ports.SerialPort.GetPortNames();
                foreach (string cpn in comPortNames)
                {
                    comPorts.Add(new FtdiDevice { ComPort = cpn });
                }
            }

            return comPorts;
        }

        private List<FtdiDevice> GetComPortsMock(bool FTDIOnly, int number, int mockStartEnum)
        {
            List<FtdiDevice> devices = new List<FtdiDevice>();
            for (int i = 0; i < number; i++)
            {
                if (mockStartEnum == 0)
                {
                    devices.Add(new FtdiDevice { ComPort = $"COM{MockComPorts.Count() + 10}", FtdiDeviceType = FTDI.FT_DEVICE.FT_DEVICE_232R, SerialNumber = "abc123" + MockComPorts.Count() });
                }
                else
                {
                    devices.Add(new FtdiDevice { ComPort = $"COM{mockStartEnum}", FtdiDeviceType = FTDI.FT_DEVICE.FT_DEVICE_232R, SerialNumber = "abc123" + mockStartEnum });
                }
                MockComPorts.Add(MockComPorts.Count());
                mockStartEnum++;
            }
            return devices;
        }

        public bool FtdiPortsSetAsyncBitBangMode()
        {
            bool success = true;
            // get fresh list of com ports and ensure they are closed
            GetComPorts(true, true, MAX_NUMBER_COMS);
            foreach (IUartDevice uart in SerialPorts)
            {
                var result = uart.FtdiToAsyncBitBangMode(0);
                if (!result.Success)
                {
                    success = false;
                }
                Debug.WriteLine($"{uart.PortName} Set Async BB Mode result = {result.Success}.  Error code = {result.Error}");
            }
            return success;
        }

        public bool FtdiPortsResetAsyncBitBangMode()
        {
            bool success = true;
            // get fresh list of com ports and ensure they are closed
            GetComPorts(true, true, MAX_NUMBER_COMS);
            foreach (IUartDevice uart in SerialPorts)
            {
                var result = uart.FtdiBitBangModeReset();
                if (!result.Success)
                {
                    success = false;
                }
                Debug.WriteLine($"{uart.PortName} Reset Async BB Mode result = {result.Success}.  Error code = {result.Error}");
            }
            return success;
        }
    }
}
