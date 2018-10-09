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

        public LightningManager LogoManager => logoManager;

        private object contextProp;
        public object ContextProp => contextProp;

        
        
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
                    float percentile = ((value * 100f) / (maxPumpRPM)) ;
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
                    var percentile = ((value * 100f) / (maxFanRPM)) ;
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
            
            logoManager = new LightningManager(device, LightChannel.Logo);
            rimManager = new LightningManager(device, LightChannel.Rim);



        }

        protected override void OnViewAttached(object view, object context) {
            base.OnViewAttached(view, context);
            
            var systemTrayView = (SystemTrayView) view;
            var contentControl = (ContentControl) systemTrayView.FindName("TrayElement");
            var taskbarIcon = (TaskbarIcon) contentControl.Content;
            var items = taskbarIcon.ContextMenu.Items;
            foreach(var item in items) {
                if(!(item is MenuItem menuItem)) {
                    continue;
                }

                if(menuItem.Name != "LightningSubMenu") {
                    continue;
                }

                foreach(var lightItem in menuItem.Items) {
                    if(!(lightItem is MenuItem lightMenuItem)) {
                        continue;
                    }

                    if(lightMenuItem.Name == "RimLightMenu") {
                        foreach(var rimLightItem in rimManager.menuItems) {
                            lightMenuItem.Items.Add(rimLightItem);
                        }
                    } else if(lightMenuItem.Name == "LogoLightMenu") {
                        foreach(var logoLightItem in logoManager.menuItems) {
                            lightMenuItem.Items.Add(logoLightItem);
                        }
                    }
                        
                }
            }
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
                device.SetFanSpeed(0x1E);
                device.SetPumpSpeed(0x1E);
                Thread.Sleep(100);
            }

            Console.WriteLine("- Stage 2: Full");
            for(var i = 0; i < 10; i++) {
                device.SetFanSpeed(0x64);
                device.SetPumpSpeed(0x64);
                Thread.Sleep(100);
            }

            Thread.Sleep(5000);
            Console.WriteLine("- Stage 3: Reset");
            device.SetPumpSpeed(25);
            device.SetFanSpeed(35);
        }

        public void AddContextMenu(SystemTrayView context) {
            var somethingsomething = context.window.Resources.FindName("LogoLightMenu");
            var logoMenuItem = (MenuItem) FindChild<MenuItem>(Application.Current.MainWindow, "LogoLightMenu");
            var rimMenuItem = (MenuItem) context.FindName("RimLightMenu");
            var something = context.FindName("LogoLightMenu");
            if(logoMenuItem != null) {
                foreach(var item in logoManager.menuItems) {
                    logoMenuItem.Items.Add(item);
                }
            }

            if(rimMenuItem != null) {
                foreach(var item in rimManager.menuItems) {
                    rimMenuItem.Items.Add(item);
                }
            }
        }

        public static T FindChild<T>(DependencyObject parent, string childName)
            where T : DependencyObject
        {    
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
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
            var logoMenuItem = (MenuItem) FindChild<MenuItem>(Application.Current.MainWindow, "LogoLightMenu");
            object res = Application.Current.FindResource("MainSysTrayMenu");

            Application.Current.Shutdown();
        }
    }
}