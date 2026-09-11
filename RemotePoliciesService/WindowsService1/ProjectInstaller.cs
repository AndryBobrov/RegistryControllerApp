using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;
using System.ServiceProcess;

namespace WindowsService1
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();

            //serviceInstaller1.AfterInstall += serviceInstaller1_AfterInstall;
        }

        private void serviceProcessInstaller1_AfterInstall(object sender, InstallEventArgs e)
        {

        }


        private void serviceInstaller1_AfterInstall(object sender, InstallEventArgs e)
        {
            //using (ServiceController service = new ServiceController(serviceInstaller1.ServiceName))
            //{
            //    if (service.Status == ServiceControllerStatus.Stopped)
            //    {
            //        service.Start();
            //        service.WaitForStatus(ServiceControllerStatus.Running);
            //    }
            //}
        }


        protected override void OnCommitted(System.Collections.IDictionary savedState)
        {
            base.OnCommitted(savedState);

            try
            {
                using (var service = new System.ServiceProcess.ServiceController("RemPolService"))
                {
                    if (service.Status == ServiceControllerStatus.Stopped)
                    {
                        service.Start();
                    }
                }
            }
            catch
            {
                
            }
        }


    }
}
