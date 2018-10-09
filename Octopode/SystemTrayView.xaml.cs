using System.Windows;

namespace Octopode {
    /// <summary>
    ///     Interaction logic for SystemTrayView.xaml
    /// </summary>
    public partial class SystemTrayView : Window {


        public SystemTrayView() {
            InitializeComponent();
            return;
            // this.DataContext = new SystemTrayViewModel(this);
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            ((SystemTrayViewModel) DataContext).AddContextMenu(this);
        }
    }
}