using System.ComponentModel;
using System.Configuration.Install;

namespace IMMULIS
{
     [RunInstaller(true)]
     public partial class ProjectInstaller : System.Configuration.Install.Installer
     {
          public ProjectInstaller()
          {
               InitializeComponent();
          }

          private void ServiceProcessInstaller1_AfterInstall(object sender, InstallEventArgs e)
          {

          }
     }
}
