using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HidLibrary;

namespace Octopode.Core {
    public class KrakenState {
        public float temperature;
        public int fanSpeed;
        public int pumpSpeed;
        public DateTime recordTime;

        public KrakenState(float temperature, int fanSpeed, int pumpSpeed, DateTime recordTime) {
            this.temperature = temperature;
            this.fanSpeed = fanSpeed;
            this.pumpSpeed = pumpSpeed;
            this.recordTime = recordTime;
        }
    }

    /// <summary>
    /// WARNING: Before changing, this is the definition for old ProtocolBytes!
    /// CAM assemblies have two different setups
    /// </summary>
    public enum ColorMode : byte {
        Fixed           = 0x00,
        Fading          = 0x01,
        SpectrumWave    = 0x02,
        Marquee         = 0x03,
        CoveringMarquee = 0x04,
        Alternating     = 0x05,
        Breathing       = 0x06,
        Pulse           = 0x07,
        TaiChi          = 0x08,
        WaterCooler     = 0x09,
        Loading         = 0x0A,
        RPM             = 0x0B,
        Wings           = 0x0C,
        Wave            = 0x0D,
        Audio           = 0x0E,
        Halt            = 0x1E
    }

    public enum AnimationSpeed : byte {
        Slowest = 0x00,
        Slow    = 0x01,
        Normal  = 0x02,
        Fast    = 0x03,
        Fastest = 0x04
    }

    public enum LightChannel : byte {
        Rim = 0x2,
        Logo = 0x1,
        Both = 0x0
    }

    public class ControlBlock {
        public readonly bool direction;
        public readonly bool alternatingMoving;
        public readonly LightChannel channelMode;

        private ControlBlock() { }
        public ControlBlock(bool forwardDirection, bool alternateMoving, LightChannel channels) {
            direction = forwardDirection;
            alternatingMoving = alternateMoving;
            channelMode = channels;
        }

        public static implicit operator byte(ControlBlock cb) {
            var alternate = cb.alternatingMoving ? 0x10 : 0x00;
            var direction = cb.direction ? 0x08 : 0x00;
            var light = (byte) cb.channelMode;
            return (byte) (alternate | direction | light);
        }
    }

    public class LEDConfiguration {
        public readonly byte lightIndex;     //  [0, 8]
        public readonly byte ledGroupSize;   //  [0, 3]
        public readonly AnimationSpeed animationSpeed; //  [0, 4]
        
        private LEDConfiguration() { }

        public LEDConfiguration(byte lightIndex, byte ledGroupSize, AnimationSpeed animationSpeed) {
            this.lightIndex = lightIndex;
            this.ledGroupSize = ledGroupSize;
            this.animationSpeed = animationSpeed;
        }

        public static implicit operator byte(LEDConfiguration config) {
            return (byte) ((config.lightIndex << 5) | (config.ledGroupSize << 3) | ((byte) config.animationSpeed));
        }
    }

    public class KrakenDevice {
        private const byte lowerSpeedLimit = 30;
        private const byte upperSpeedLimit = 100;
        private byte deviceId;
        private int firmwareVersion;

        public HidDevice usbDevice;

        private bool keepReading;
        private Task readTask;
        private CancellationTokenSource tokenSource;
        private bool readAnyBytes;

        public Queue<KrakenState> LastStates { get; }

        public int DeviceId {
            get {
                if(!readAnyBytes) {
                    var package = usbDevice.Read();
                    GetDeviceId(package);
                }

                return deviceId;
            }
        }

        public int FirmwareVersion {
            get {
                if(!readAnyBytes) {
                    var package = usbDevice.Read();
                    GetDeviceId(package);
                }

                return deviceId;
            }
        }

        public KrakenDevice(HidDevice usbDevice) {
            LastStates = new Queue<KrakenState>();
            this.usbDevice = usbDevice;
            tokenSource = new CancellationTokenSource();
            // StartReading();
        }

        public void StartReading() {
            if(readTask != null && readTask.Status == TaskStatus.Running) {
                throw new NotSupportedException("Read Task is already reading, cannot read twice.");
            }

            keepReading = true;
            readTask = Task.Factory.StartNew(ReadLoop, tokenSource.Token);
        }

        public void StopReading() {
            if(readTask == null) {
                throw new NotSupportedException("There exists no read task to stop.");
            }

            switch(readTask.Status) {
                case TaskStatus.Running:
                case TaskStatus.WaitingForActivation:
                case TaskStatus.WaitingForChildrenToComplete:
                case TaskStatus.WaitingToRun:
                    keepReading = false;
                    break;
                default:
                    throw new NotSupportedException("The read task is likely already cancelled.");
            }
        }

        public void SetFanSpeed(byte percentage) {
            percentage = Math.Min(lowerSpeedLimit, Math.Max(upperSpeedLimit, percentage));
            usbDevice.Write(new byte[] { 0x02, 0x4d, 0x00, 0x00, percentage });
        }

        public void SetPumpSpeed(byte percentage) {
            percentage = Math.Min(lowerSpeedLimit, Math.Max(upperSpeedLimit, percentage));
            usbDevice.Write(new byte[] { 0x02, 0x4d, 0x40, 0x00, percentage });
        }


        public void SetColorPattern(ColorMode mode, ControlBlock block, LEDConfiguration ledConfig, int[][] colorPattern) {
            for(byte i = 0; i < colorPattern.Length && i < 8; i++) {
                var correctConfig = new LEDConfiguration(i, ledConfig.ledGroupSize, ledConfig.animationSpeed);
                var message = GenerateMessage(mode, block, correctConfig, colorPattern[i]);
                usbDevice.Write(message, 1050);
            }
        }

        public void SetColor(ColorMode mode, ControlBlock block, LEDConfiguration ledConfig, params int[] colorPattern) {
            var message = GenerateMessage(mode, block, ledConfig, colorPattern);
            usbDevice.Write(message, 1050);
        }

        private static byte[] GenerateMessage(ColorMode mode, ControlBlock block, LEDConfiguration ledConfig,
                                              int[] colorPattern) {
            // a message is constructed as following: 
            // 0x02  ; control command
            // 0x4c  ; light control, which begs to wonder what 0x4a is
            // 0x13  ; 0b0001_0_0_00 is directional parameter
            //       ; 0b0000_1_0_00 is a binary option solely for "alternating" light option
            //       ; 0b0000_0_0_11 is 'iChannelMode'; controls logo or rim settings:
            //       ;                   - 0 - apply this set for rim and logo
            //       ;                   - 1 - apply this set for logo only
            //       ;                   - 2 - apply this set for rim only
            // 0x06  ; pulse mode, see light mode table
            // 0x25  ; 0b111_00_000 is the light index number (for multiple colored light)
            //       ; 0b000_11_000 is the LED group size, which exists only for marquee width
            //       ; 0b000_00_111 is the speed of animation between 0-4
            // 0xFF  ; Green Color for Text
            // 0xFF  ; Red Color for Text
            // 0xFF  ; Blue Color for Text
            // 0xFF  ; Red Color for 1st LED
            // 0xFF  ; Green Color for 1st LED
            // 0xFF  ; Blue Color for 1st LED
            if(colorPattern.Length < 1) {
                throw new ArgumentException("Color Pattern needs to have at least 1 color!");
            }

            byte[] message = new byte[32];
            message[0] = 0x02; // control message
            message[1] = 0x4c; // lightning control message
            message[2] = block;
            message[3] = (byte) mode;
            message[4] = ledConfig;
            message[5] = (byte) ((colorPattern[0] & 0x00_FF_00) >> 8);
            message[6] = (byte) ((colorPattern[0] & 0xFF_00_00) >> 16);
            message[7] = (byte) ((colorPattern[0] & 0x00_00_FF) >> 0);
            for(byte i = 1; i < 9 && i < colorPattern.Length; i++) {
                message[i * 3 + 5] = (byte) ((colorPattern[i] & 0xFF_00_00) >> 16);
                message[i * 3 + 6] = (byte) ((colorPattern[i] & 0x00_FF_00) >> 8);
                message[i * 3 + 7] = (byte) ((colorPattern[i] & 0x00_00_FF) >> 0);
            }
            

            return message;
        }

        public void AbortReading() {
            tokenSource.Cancel();
        }

        private async void ReadLoop() {
            HidDeviceData dataPoint = await usbDevice.ReadAsync();
            GetDeviceId(dataPoint);
            while(keepReading) {
                dataPoint = await usbDevice.ReadAsync();
                if(dataPoint.Status != HidDeviceData.ReadStatus.Success) {
                    throw new InvalidOperationException($"Cannot read from USB device! State was {dataPoint.Status}");
                }

                // this is the same behaviour as the CAM software.
                // Somehow it sometimes spits random, fixed values and nobody really knows why.
                if(dataPoint.Data[10] != deviceId) {
                    // Console.WriteLine("Disposed");
                    continue;
                }

                if(dataPoint.Data[0] == 4) {
                    LastStates.Enqueue(ParseDataPackage(dataPoint));
                }

                while(LastStates.Count > 255) {
                    LastStates.Dequeue();
                }
            }
        }

        public void Read() {
            HidDeviceData dataPoint = usbDevice.Read();
            if(dataPoint.Status != HidDeviceData.ReadStatus.Success) {
                throw new InvalidOperationException($"Cannot read from USB device! State was {dataPoint.Status}");
            }
            // this is the same behaviour as the CAM software.
            // Somehow it sometimes spits random, fixed values and nobody really knows why.
            if(dataPoint.Data[10] != deviceId) {
                return;
            }

            if(dataPoint.Data[0] == 4) {
                LastStates.Enqueue(ParseDataPackage(dataPoint));
            }

            while(LastStates.Count > 255) {
                LastStates.Dequeue();
            }
        }
        
        private KrakenState ParseDataPackage(HidDeviceData data) {
            var bytes = data.Data;
            var decimalTemperature = (float) bytes[2];
            while(decimalTemperature > 1) {
                decimalTemperature /= 10f;
            }

            return new KrakenState(bytes[1] + decimalTemperature,
                                   bytes[3] << 8 | bytes[4],
                                   bytes[5] << 8 | bytes[6],
                                   DateTime.Now);
        }

        private void GetDeviceId(HidDeviceData data) {
            // is there actually a likelyhood to hit the one case, where it breaks by getting the garbled
            // packet with the unidentified data?
            readAnyBytes = true;
            deviceId = data.Data[10];
            firmwareVersion = data.Data[11] * 1000 + data.Data[12] * 100 + data.Data[13] * 10 + data.Data[14];
        }
    }
}