using System;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SetupLauncher
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string baseDirectory = AppContext.BaseDirectory;

            string msiPath = Path.Combine(baseDirectory, "RemotePoliciesSetup.msi");

            
            if (!File.Exists(msiPath))
            {   
                return;
            }

            
            Process installer = Process.Start(new ProcessStartInfo
            {
                FileName = msiPath,
                UseShellExecute = true
            });

            if (installer == null)
            {
                return;
            }



            installer.WaitForExit();



            if (installer.ExitCode != 0)
            {
                return;
            }

            string serviceControllerPath = null;

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                serviceControllerPath = key?.GetValue("ServiceControlPanel")?.ToString();
            }

            if (string.IsNullOrWhiteSpace(serviceControllerPath))
            {
                return;
            }

            
            serviceControllerPath = serviceControllerPath.Trim('"');

            if (!File.Exists(serviceControllerPath))
            {
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = serviceControllerPath,
                UseShellExecute = true
            });

        }
    }
}
