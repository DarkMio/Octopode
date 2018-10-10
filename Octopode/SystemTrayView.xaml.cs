using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;

namespace Octopode {
    /// <summary>
    ///     Interaction logic for SystemTrayView.xaml
    /// </summary>
    public partial class SystemTrayView : Window {


        public SystemTrayView() {
            InitializeComponent();
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            AttachLightEntries();
        }

        private void AttachLightEntries() {
            var viewModel = (SystemTrayViewModel) DataContext;
            var rimManager = viewModel?.rimManager;
            var logoManager = viewModel?.logoManager;
            var contentControl = (ContentControl) FindName("TrayElement");
            var taskbarIcon = (TaskbarIcon) contentControl?.Content;
            var items = taskbarIcon?.ContextMenu?.Items;
            if(items == null || rimManager == null || logoManager == null) {
                return;
            }
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
    }
}