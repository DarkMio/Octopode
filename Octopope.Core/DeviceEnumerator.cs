using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HidLibrary;

namespace Octopode.Core {
    public class DeviceEnumerator {

        public static IEnumerable<HidDevice> EnumerateAllDevices() {
            return HidDevices.Enumerate();
        }

        public static IEnumerable<HidDevice> EnumerateKrakenDevices() {
            return HidDevices.Enumerate(0x1E71);
        }

        public static IEnumerable<HidDevice> EnumerateKrakenX62Devices() {
            foreach(var device in EnumerateKrakenDevices()) {
                if(device.Attributes.ProductId == 0x170E) {
                    yield return device;
                }
            }
        }
    }
}
