using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Win32;
using MySqlConnector;

using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using Microsoft.SqlServer.Server;



namespace WindowsService1
{
    public partial class Service1 : ServiceBase
    {

        private CancellationTokenSource cancellationTokenSource;


        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern bool WTSQueryUserToken(int sessionId, out IntPtr phToken);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int WTSGetActiveConsoleSessionId();



        public Service1()
        {
            InitializeComponent();
        }
        


        protected override void OnStart(string[] args)
        {
            cancellationTokenSource = new CancellationTokenSource();

            Task.Run(() => Loop(cancellationTokenSource.Token));
        }
        


        protected override void OnStop()
        {
            cancellationTokenSource?.Cancel();
        }



        private async Task Loop(CancellationToken token)
        {
            try
            {

                while (!token.IsCancellationRequested)
                {

                    HttpClient myclient = new HttpClient();

                    RegistryKey rootkey = null;

                    object CompID = "";
                    object UserID = "";


                    while (true)
                    {

                        using (RegistryKey Skey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\ServiceSettings"))
                        {
                            CompID = Skey.GetValue("CompID");
                            UserID = Skey.GetValue("UserID");
                        }




                        string url = $"https://remotepoliciesmanagement.de/getpolicies.php?compid={CompID}&userid={UserID}";

                        Policy[] policies = null;


                        HttpResponseMessage response;




                        string json = myclient.GetStringAsync(url).Result;

                        policies = JsonSerializer.Deserialize<Policy[]>(json);


                        string backuppath = @"SOFTWARE\ServiceSettings\Backup";


                        string[] backupnames;


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


                            foreach (string backupname in backupnames)
                            {
                                bool detector = false;

                                foreach (Policy policy in policies)
                                {
                                    if (backupname == policy.PolicyName)
                                    {
                                        detector = true;
                                    }
                                }

                                if (detector == false)
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
                        }



                        foreach (Policy policy in policies)
                        {
                            string type = policy.DataType;

                            RegistryValueKind datatype = (RegistryValueKind)Enum.Parse(typeof(RegistryValueKind), type);

                            bool detector = false;


                            if (policy.Directory == "HKEY_LOCAL_MACHINE")
                            {

                                foreach (string backupname in backupnames)
                                {
                                    if (policy.PolicyName == backupname)
                                    {
                                        detector = true;
                                    }
                                }

                                if (detector == false)
                                {
                                    string basicvalue = "";

                                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(policy.PolicyPath))
                                    {

                                        basicvalue = key?.GetValue(policy.PolicyName)?.ToString();

                                        if (basicvalue == null)
                                        {
                                            basicvalue = "[TERMINATE]";
                                        }

                                        if (basicvalue == "[TERMINATE]")
                                        {
                                            string[] data = { basicvalue, policy.PolicyPath, policy.Directory };

                                            Registry.LocalMachine.OpenSubKey(backuppath, true).SetValue(policy.PolicyName, data, RegistryValueKind.MultiString);

                                        }
                                        else
                                        {
                                            using (RegistryKey bkey = Registry.LocalMachine.OpenSubKey(backuppath, true))
                                            {
                                                string[] data = { basicvalue, policy.FullPath, policy.DataType };

                                                bkey.SetValue(policy.PolicyName, data, RegistryValueKind.MultiString);
                                            }
                                        }

                                    }
                                }




                                rootkey = Registry.LocalMachine;

                                try
                                {
                                    using (RegistryKey key = rootkey.CreateSubKey(policy.PolicyPath))
                                    {
                                        key.SetValue(policy.PolicyName, policy.PolicyValue, datatype);
                                    }
                                }
                                catch
                                {
                                    using (RegistryKey DelBackup = Registry.LocalMachine.OpenSubKey(backuppath, true)) 
                                    {
                                        string[] values = DelBackup?.GetValue(policy.PolicyName) as string[];

                                        if (values[0] == "[TERMINATE]")
                                        {
                                            try
                                            {
                                                Registry.LocalMachine.OpenSubKey(policy.PolicyPath, true).DeleteValue(policy.PolicyName);
                                            } catch { }
                                        }
                                        else if (values[0] != "[TERMINATE]")
                                        {
                                            Registry.LocalMachine.OpenSubKey(policy.PolicyPath, true).SetValue(policy.PolicyName, values[0], datatype);
                                        }

                                        DelBackup.DeleteValue(policy.PolicyName);
                                    }
                                }
                                
                            }
                            else if (policy.Directory == "HKEY_CURRENT_USER")
                            {

                                int sessionID = WTSGetActiveConsoleSessionId();

                                IntPtr userToken = IntPtr.Zero;

                                if (WTSQueryUserToken(sessionID, out userToken))
                                {
                                    using (WindowsIdentity windowsidentity = new WindowsIdentity(userToken))
                                    {
                                        SecurityIdentifier sid = windowsidentity.User;
                                        string sidString = sid.Value;

                                        string registryPath = $@"{sidString}\{policy.PolicyPath}";


                                        foreach (string backupname in backupnames)
                                        {
                                            if (policy.PolicyName == backupname)
                                            {
                                                detector = true;
                                            }
                                        }

                                        if (detector == false)
                                        {
                                            string basicvalue = "";

                                            using (RegistryKey key = Registry.Users.OpenSubKey(registryPath))
                                            {

                                                basicvalue = key?.GetValue(policy.PolicyName)?.ToString();

                                                if (basicvalue == null)
                                                {
                                                    basicvalue = "[TERMINATE]";
                                                }

                                                if (basicvalue == "[TERMINATE]")
                                                {
                                                    string[] data = { basicvalue, registryPath, policy.Directory };

                                                    Registry.LocalMachine.OpenSubKey(backuppath, true).SetValue(policy.PolicyName, data, RegistryValueKind.MultiString);

                                                }
                                                else
                                                {
                                                    using (RegistryKey bkey = Registry.LocalMachine.OpenSubKey(backuppath, true))
                                                    {
                                                        string fullregistryPath = $@"HKEY_USERS\{registryPath}";

                                                        string[] data = { basicvalue, fullregistryPath, policy.DataType };

                                                        bkey.SetValue(policy.PolicyName, data, RegistryValueKind.MultiString);
                                                    }
                                                }

                                            }
                                        }

                                        try
                                        {
                                            Registry.Users.CreateSubKey(registryPath).SetValue(policy.PolicyName, policy.PolicyValue, datatype);
                                        } catch
                                        {
                                            using (RegistryKey DelBackup = Registry.LocalMachine.OpenSubKey(backuppath, true))
                                            {
                                                string[] values = DelBackup?.GetValue(policy.PolicyName) as string[] ?? new string[0];

                                                if (values[0] == "[TERMINATE]")
                                                {
                                                    try
                                                    {
                                                        Registry.Users.OpenSubKey(registryPath, true).DeleteValue(policy.PolicyName);
                                                    }
                                                    catch { }
                                                }
                                                else if (values[0] != "[TERMINATE]")
                                                {
                                                    Registry.Users.OpenSubKey(registryPath, true).SetValue(policy.PolicyName, values[0], datatype);
                                                }

                                                DelBackup.DeleteValue(policy.PolicyName);
                                            }
                                        }
                                        
                                    }

                                }




                                //rootkey = Registry.CurrentUser;


                                //using (RegistryKey key = rootkey.OpenSubKey(policy.PolicyPath, true))
                                //{
                                //    key.SetValue(policy.PolicyName, policy.PolicyValue);

                                //    Registry.CurrentUser.OpenSubKey(showchange, true).SetValue("LastKey", policy.FullPath);
                                //}

                            }


                        }




                        //string time = DateTime.Now.ToString("HH:mm:ss.fff");
                        //string fulltext = $" Служба Работает!!!! {time}{Environment.NewLine}";

                        //File.AppendAllText(@"C:\Users\AndrushaPC\Desktop\1.txt", fulltext);

                        ////Process.Start(@"C:\Users\AndrushaPC\Desktop\Icon\WindowsFormsApp1.exe");
                        await Task.Delay(1000);
                    }

                }
            }
            catch (Exception ex)
            {

            }

        }



        public class Policy
        {
            public string UserID { get; set; }
            public string CompID { get; set; }
            public string Directory { get; set; }
            public string PolicyValue { get; set; }
            public string PolicyPath { get; set; }
            public string PolicyName { get; set; }
            public string DataType { get; set; }


            public override string ToString()
            {
                return $"{Environment.NewLine}" +
                    $"UserID: {UserID} {Environment.NewLine}" +
                    $"CompID: {CompID} {Environment.NewLine}" +
                    $"Directory: {Directory} {Environment.NewLine}" +
                    $"PolicyPath: {PolicyPath} {Environment.NewLine}" +
                    $"PolicyName: {PolicyName} {Environment.NewLine}" +
                    $"PolicyValue: {PolicyValue} {Environment.NewLine}";
            }


            public string FullPath
            {
                get { return $"{Directory}\\{PolicyPath}"; }
            }
        }
    }
}
