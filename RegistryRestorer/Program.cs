using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;
using Microsoft.Win32;


namespace RegistryRestorer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var fn = new Program();

                try
                {
                    fn.disableService();
                }
                catch (Exception ex)
                {
                    
                    return;
                }


                string backuppath = @"SOFTWARE\ServiceSettings\Backup";

                string[] backupnames;

                try
                {
                    using (RegistryKey checking = Registry.LocalMachine.OpenSubKey(backuppath, true))
                    {
                        if (checking == null)
                        {
                            backupnames = Array.Empty<string>();
                        }
                        else
                        {
                            backupnames = checking.GetValueNames();
                        }

                        fn.BackupSequence(backupnames, backuppath, checking);
                    }
                }
                catch (Exception ex)
                {
                    
                    return;
                }


                
                
            }
            catch (Exception ex) 
            {
                return;
            }
        }


        public void BackupSequence(string[] backupnames, string backuppath, RegistryKey checking)
        {
            
            foreach (string backupname in backupnames)
            {

                string[] check = checking.GetValue(backupname) as string[];

                if (check[0] == "[TERMINATE]")
                {
                    if (check[2] == "HKEY_LOCAL_MACHINE")
                    {
                        Registry.LocalMachine.OpenSubKey(check[1], true).DeleteValue(backupname);
                        checking.DeleteValue(backupname);
                    }
                    else if (check[2] == "HKEY_CURRENT_USER")
                    {
                        Registry.Users.OpenSubKey(check[1], true).DeleteValue(backupname);
                        checking.DeleteValue(backupname);
                    }
                }
                else
                {
                    string[] backupdata = checking.GetValue(backupname) as string[];

                    RegistryValueKind datatype = (RegistryValueKind)Enum.Parse(typeof(RegistryValueKind), backupdata[2]);

                    string path = backupdata[1];
                    object value = backupdata[0];

                    Registry.SetValue(path, backupname, value, datatype);

                    checking.DeleteValue(backupname);
                }

            }
        }


        public void disableService()
        {
            bool serviceExists = ServiceController.GetServices().Any(s => s.ServiceName.Equals("RemPolService", StringComparison.OrdinalIgnoreCase));

            if (serviceExists)
            {
                using (var controller = new ServiceController("RemPolService"))
                {
                    controller.Refresh();

                    if (controller.Status == ServiceControllerStatus.Running)
                    {
                        controller.Stop();
                        controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }
                }
            }
        }
    }
}
