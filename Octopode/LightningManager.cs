using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Octopode.Core;

namespace Octopode {
    public class LightningManager {
        public readonly LightChannel lightChannel;

        private readonly List<LightSetting> availableModes;
        public readonly List<MenuItem> menuItems;
        public AnimationSpeed animationSpeed;
        public LightSetting selectedSetting;

        public Action<LightningManager, LightSetting> OnNewLightSetting;

        public List<MenuItem> MenuItems {
            get { return menuItems; }
        }
        
        public LightningManager(LightChannel observedChannel) {
            lightChannel = observedChannel;
            availableModes = new List<LightSetting>();
            BuildLightOptions();
            menuItems = MenuFactory(availableModes);
            animationSpeed = AnimationSpeed.Normal;
            selectedSetting = availableModes[0];
        }

        private void BuildLightOptions() {
            var modes = LightningHelper.PossibleColorModes(lightChannel);
            foreach(var mode in modes) {
                availableModes.Add(new LightSetting(mode));
            }
        }

        private List<MenuItem> MenuFactory(List<LightSetting> settings) {
            var list = new List<MenuItem>();
            foreach(var setting in settings) {
                var menuItem = new MenuItem();
                menuItem.Header = setting.mode.ToString();
                menuItem.IsCheckable = true;
                menuItem.Checked += MenuItemOnChecked;

                var dummy = new MenuItem();
                dummy.Header = "Dummy Text";
                menuItem.Items.Add(dummy);
                list.Add(menuItem);
            }
            return list;
        }

        private void MenuItemOnChecked(object sender, RoutedEventArgs e) {
            menuItems.ForEach(x => x.IsChecked = x.Equals(sender));
            var item = sender as MenuItem;
            if(item == null) {
                return;
            }
            var setting = availableModes.First(x => x.mode.ToString() == item.Header.ToString());
            selectedSetting = setting;
            OnNewLightSetting.Invoke(this, setting);
        }
    }
}