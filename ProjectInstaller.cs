using System.ComponentModel;
using System.Configuration.Install;

namespace UniversaLIS
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
