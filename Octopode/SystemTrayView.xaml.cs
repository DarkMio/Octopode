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
            if(rimManager != null) {
                foreach(var rimLightItem in rimManager.menuItems) {
                    RimLightMenu.Items.Add(rimLightItem);
                }
            }
            if(logoManager != null) {
                foreach(var logoLightItem in logoManager.menuItems) {
                    LogoLightMenu.Items.Add(logoLightItem);
                }
            }
        }
    }
}