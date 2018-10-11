using System.Windows;
using Autofac;
using Caliburn.Micro;
using Caliburn.Micro.Autofac;

namespace Octopode {
    internal class Bootstrapper : AutofacBootstrapper<SystemTrayViewModel> {

        public Bootstrapper() {
            Initialize();
            // ConsoleManager.Show();
        }

        protected override void ConfigureContainer(ContainerBuilder builder) {
            base.ConfigureContainer(builder);
            builder.RegisterInstance<IWindowManager>(new WindowManager());
            builder.RegisterType<SystemTrayViewModel>();
        }

        protected override void OnStartup(object sender, StartupEventArgs e) {
            base.OnStartup(sender, e);
            DisplayRootViewFor<SystemTrayViewModel>();
        }
    }
}