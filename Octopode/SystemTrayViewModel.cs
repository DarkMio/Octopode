using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Caliburn.Micro;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using Octopode.Core;

namespace Octopode {
    public class SystemTrayViewModel : Screen {
        public bool isTrue { get; set; }
        private string _activeIcon;
        private KrakenDevice device;
        private string _tooltipText = "N/A";
        private string temperatureText = "N/A";
        private string fanSpeedText = "N/A";
        private string pumpSpeedText = "N/A";

        private int pumpRpm, fanRpm;
        private float temperature;
        private int minPumpRPM = int.MaxValue;
        private int maxPumpRPM = int.MinValue;
        private int minFanRPM = int.MaxValue;
        private int maxFanRPM = int.MinValue;
        private float minTemperature;
        private float maxTemperature;
        private readonly RegistryKey appKey;

        public readonly LightningManager logoManager;
        public readonly LightningManager rimManager;

        private readonly CommandController commander;
        private MenuItem previousSpeedBox;


        public string TooltipText {
            get { return _tooltipText; }
            set {
                _tooltipText = value;
                NotifyOfPropertyChange();
            }
        }

        public int PumpRpm {
            get { return pumpRpm; }
            set {
                if(minPumpRPM < int.MaxValue && maxPumpRPM > int.MinValue) {
                    float percentile = ((value * 100f) / (maxPumpRPM));
                    PumpSpeedText = $"Pump: {value}RPM ({percentile:##.#}%)";
                } else {
                    PumpSpeedText = $"Pump: {value}RPM";
                }

                if(value < minPumpRPM) {
                    appKey.SetValue(Constants.RegistryMinPumpRPM, value);
                    minPumpRPM = value;
                }

                if(value > maxPumpRPM) {
                    appKey.SetValue(Constants.RegistryMaxPumpRPM, value);
                    maxPumpRPM = value;
                }

                pumpRpm = value;
            }
        }

        public int FanRpm {
            get { return fanRpm; }
            set {
                if(minFanRPM < int.MaxValue && maxFanRPM > int.MinValue) {
                    var percentile = ((value * 100f) / (maxFanRPM));
                    FanSpeedText = $"Fan: {value}RPM ({percentile:##.#}%)";
                } else {
                    FanSpeedText = $"Fan: {value}RPM";
                }

                if(value < minFanRPM) {
                    appKey.SetValue(Constants.RegistryMinFanRPM, value);
                    minFanRPM = value;
                }

                if(value > maxFanRPM) {
                    appKey.SetValue(Constants.RegistryMaxFanRPM, value);
                    maxFanRPM = value;
                }

                fanRpm = value;
            }
        }

        public float Temperature {
            get { return temperature; }
            set {
                TemperatureText = $"Temperature: {value}°C";
                minTemperature = Math.Min(value, minTemperature);
                maxTemperature = Math.Max(value, maxTemperature);
                temperature = value;
            }
        }

        public string TemperatureText {
            get { return temperatureText; }
            set {
                temperatureText = value;
                NotifyOfPropertyChange();
            }
        }

        public string FanSpeedText {
            get { return fanSpeedText; }
            set {
                fanSpeedText = value;
                NotifyOfPropertyChange();
            }
        }

        public string PumpSpeedText {
            get { return pumpSpeedText; }
            set {
                pumpSpeedText = value;
                NotifyOfPropertyChange();
            }
        }

        public SystemTrayViewModel() {
            ActiveIcon = "Resources/octopode_icon.ico";
            device = new KrakenDevice(DeviceEnumerator.EnumerateKrakenX62Devices().First());
            Task.Run(() => RefreshControllerState());
            device.StartReading();


            var key = Registry.CurrentUser.OpenSubKey("Software", true);
            if(key == null) {
                Console.Error.WriteLine("Could not open Registry Key");
                return;
            }

            appKey = key.OpenSubKey("Octopode", true);
            if(appKey == null) {
                appKey = key.CreateSubKey("Octopode", true, RegistryOptions.None);
            }

            LoadProfile();

            logoManager = new LightningManager(LightChannel.Logo);
            rimManager = new LightningManager(LightChannel.Rim);

            logoManager.OnNewLightSetting += LightingManagerCallback;
            rimManager.OnNewLightSetting += LightingManagerCallback;

            commander = new CommandController(device.usbDevice);
        }

        private void LightingManagerCallback(LightningManager sender, LightSetting setting) {
            commander.AddCommand(KrakenDevice.GenerateLightMessage(setting.mode,
                                                              new ControlBlock(false, false, sender.lightChannel),
                                                              new LEDConfiguration(0, 0, AnimationSpeed.Normal),
                                                              new[] {
                                                                  0xFFFFFF, 0xFFFFFF, 0xFFFFFF, 0xFFFFFF, 0xFFFFFF,
                                                                  0xFFFFFF, 0xFFFFFF, 0xFFFFFF, 0xFFFFFF, 0xFFFFFF
                                                              }));
        }


        public string ActiveIcon {
            get { return _activeIcon; }
            set {
                _activeIcon = value;
                NotifyOfPropertyChange();
            }
        }

        private void LoadProfile() {
            minPumpRPM = (int) appKey.GetValue(Constants.RegistryMinPumpRPM, int.MaxValue);
            maxPumpRPM = (int) appKey.GetValue(Constants.RegistryMaxPumpRPM, int.MinValue);

            minFanRPM = (int) appKey.GetValue(Constants.RegistryMinFanRPM, int.MaxValue);
            maxFanRPM = (int) appKey.GetValue(Constants.RegistryMaxFanRPM, int.MinValue);
        }

        private void ReadState() {
            KrakenState lastState = null;
            while(device.LastStates.Count > 0) {
                lastState = device.LastStates.Dequeue();
            }

            if(lastState != null) {
                TooltipText =
                    $"{lastState.temperature}°C\nFan: {lastState.fanSpeed}RPM\nPump: {lastState.pumpSpeed}RPM";
                PumpRpm = lastState.pumpSpeed;
                FanRpm = lastState.fanSpeed;
                Temperature = lastState.temperature;
            }

            NotifyOfPropertyChange();
        }

        private void RefreshControllerState() {
            while(true) {
                ReadState();
                Thread.Sleep(1000);
            }
        }

        private void UpdateBenchmark() {
            System.Console.WriteLine("Starting staged Performance Benchmark:");
            Console.WriteLine("- Stage 1: Slow");
            for(var i = 0; i < 10; i++) {
                commander.AddCommand(KrakenDevice.GenerateCoolingMessage(true, false, 35));
                commander.AddCommand(KrakenDevice.GenerateCoolingMessage(false, false, 35));
                Thread.Sleep(1000);
            }

            Console.WriteLine("- Stage 2: Full");
            for(var i = 0; i < 10; i++) {
                commander.AddCommand(KrakenDevice.GenerateCoolingMessage(true, false, 100));
                commander.AddCommand(KrakenDevice.GenerateCoolingMessage(false, false, 100));
                Thread.Sleep(1000);
            }

            Console.WriteLine("- Stage 3: Reset");
            commander.AddCommand(KrakenDevice.GenerateCoolingMessage(true, false, 40));
            commander.AddCommand(KrakenDevice.GenerateCoolingMessage(false, false, 40));
        }

        public void SetAnimationSpeed(AnimationSpeed speedSetting, MenuItem sender) {
            if(previousSpeedBox != null) {
                previousSpeedBox.IsChecked = false;
            }

            sender.IsChecked = true;
            previousSpeedBox = sender;
            logoManager.animationSpeed = speedSetting;
            rimManager.animationSpeed = speedSetting;
            commander.AddCommand(KrakenDevice.GenerateLightMessage(logoManager.selectedSetting.mode,
                                                              new ControlBlock(false, false, logoManager.lightChannel),
                                                              new LEDConfiguration(0, 0, speedSetting), new[] {
                                                                  0xFFFFFF, 0xFFFFFF, 0xFFFFFF, 0xFFFFFF, 0xFFFFFF,
                                                                  0xFFFFFF, 0xFFFFFF, 0xFFFFFF, 0xFFFFFF, 0xFFFFFF
                                                              }));
            commander.AddCommand(KrakenDevice.GenerateLightMessage(rimManager.selectedSetting.mode,
                                                              new ControlBlock(false, false, rimManager.lightChannel),
                                                              new LEDConfiguration(0, 0, speedSetting), new[] {
                                                                  0xFFFFFF, 0xFFFFFF, 0xFFFFFF, 0xFFFFFF, 0xFFFFFF,
                                                                  0xFFFFFF, 0xFFFFFF, 0xFFFFFF, 0xFFFFFF, 0xFFFFFF
                                                              }));
        }
        
        public void CleanRegistryKeys() {
            appKey.DeleteSubKey(Constants.RegistryMaxFanRPM);
            appKey.DeleteSubKey(Constants.RegistryMaxPumpRPM);
            appKey.DeleteSubKey(Constants.RegistryMinFanRPM);
            appKey.DeleteSubKey(Constants.RegistryMinPumpRPM);
        }

        public void PerformanceBenchmark() {
            Task.Run(() => UpdateBenchmark());
        }

        public void ExitApplication() {
            Application.Current.Shutdown();
        }
    }
}