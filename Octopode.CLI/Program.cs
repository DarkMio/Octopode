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

            var messages = KrakenDevice.GenerateCoolingMessage(false, true, 25,
                                                               25,
                                                               25,
                                                               25,
                                                               25,
                                                               25,
                                                               25,
                                                               25,
                                                               35,
                                                               45,
                                                               55,
                                                               75,
                                                               100,
                                                               100,
                                                               100,
                                                               100,
                                                               100,
                                                               100,
                                                               100,
                                                               100,
                                                               100);
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach(var message in messages) {
                device.Write(message);
            }
            stopwatch.Start();
            Console.WriteLine($"Time taken: {stopwatch.ElapsedMilliseconds}ms - Avg: {stopwatch.ElapsedMilliseconds / (float) messages.Length}ms");
        }
    }
}
