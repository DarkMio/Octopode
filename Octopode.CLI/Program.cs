using HidLibrary;
using Octopode.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Octopode.CLI {
    class Program {

        private static async void ReadLoop(HidDevice usbDevice) {
            HidDeviceData dataPoint = await usbDevice.ReadAsync();
            while (true) {
                dataPoint = await usbDevice.ReadAsync();
                if (dataPoint.Status != HidDeviceData.ReadStatus.Success) {
                    throw new InvalidOperationException($"Cannot read from USB device! State was {dataPoint.Status}");
                }

                foreach (var bit in dataPoint.Data) {
                    if(bit == 0) {
                        continue;
                    }
                    // Console.WriteLine(bit);
                }
            }
        }

        static void Main(string[] args) {
            var devices = new List<HidDevice>(DeviceEnumerator.EnumerateKrakenX62Devices());
            var tasks = new List<Task>();

            var deviceList = new List<HidDevice>(DeviceEnumerator.EnumerateKrakenX62Devices());
            if(deviceList.Count < 1) {
                Console.WriteLine("No Kraken X62 device found!");
                return;
            }
            var device = new KrakenDevice(deviceList[0]);
            // device.StartReading();
            Console.WriteLine($"Locking onto Kraken X62 {device.DeviceId} with firmware v{device.FirmwareVersion}");
            int counter = 0;
            // device.SetColor(ColorMode.Solid, 0xFF_FF_FF);
            int[] colors = new[] { 0x0000FF, 0x0, 0xFFFFFF, 0x0, 0xFF00FF, 0x0, 0x00FFFF, 0xFF0000, 0x0, 0xFFFF00, 0x0, 0x00FF00 };
            var colorPattern = new int[8, 20];
            for(var i = 0; i < 8; i++) {
                for(var j = 0; j < 20; j++) {
                    colorPattern[i, j] = colors[0];
                }
            }
            
            var z = 1;
            while(true) {
                // device.Read();
                while(device.LastStates.Count > 0) {
                    var state = device.LastStates.Dequeue();
                    Console.WriteLine($"{state.recordTime}:{state.recordTime.Millisecond:000}: {state.temperature}°C | Fan: {state.fanSpeed} | Pump: {state.pumpSpeed}");
                }
                var color = colors[z % colors.Length];
                device.SetColor(ColorMode.Solid, new ControlBlock(false, true, LightChannel.Both), new LEDConfiguration(0, 0, 4),
                                color, color, color, color, color, color, color, color, color);
                z += 1;
            }
        }
    }
}
