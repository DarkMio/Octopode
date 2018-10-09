using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Controls;
using Octopode.Core;

namespace Octopode {
    public class LightningManager {
        private KrakenDevice context;
        private LightChannel lightChannel;

        private readonly List<LightSetting> availableModes;
        public readonly List<MenuItem> menuItems;

        public List<MenuItem> MenuItems {
            get { return menuItems; }
        }
        
        public LightningManager(KrakenDevice contextDevice, LightChannel observedChannel) {
            context = contextDevice;
            lightChannel = observedChannel;
            availableModes = new List<LightSetting>();
            BuildLightOptions();
            menuItems = MenuFactory(availableModes);
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
            context.SetColor(setting.mode, new ControlBlock(false, false, lightChannel),
                             new LEDConfiguration(0, 0, AnimationSpeed.Normal), 0xFFFFFF, 0xFFFFFF, 0xFFFFFF, 0xFFFFFF,
                             0xFFFFFF, 0xFFFFFF, 0xFFFFFF, 0xFFFFFF, 0xFFFFFF, 0xFFFFFF, 0xFFFFFF);
        }
    }
}