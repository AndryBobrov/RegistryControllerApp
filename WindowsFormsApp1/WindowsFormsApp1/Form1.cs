using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ServiceProcess;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();


        public Form1()
        {
            InitializeComponent();

            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            this.FormBorderStyle = FormBorderStyle.None;

            CommandReader();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            Hide();
        }


        private void RunAsAdmin(string command)
        {
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = Application.ExecutablePath,
                Arguments = command,
                UseShellExecute = true,
                Verb = "runas"
            };

            try
            {
                Process.Start(psi);
            }
            catch (Exception ex)
            {

            }
        }


        private void CommandReader()
        {
            string[] command = Environment.GetCommandLineArgs();

            if (command.Length < 2)
            {
                return;
            }

            if (command[1].Equals("start", StringComparison.OrdinalIgnoreCase))
            {
                Start();
                Environment.Exit(0);
            } else if (command[1].Equals("stop", StringComparison.OrdinalIgnoreCase))
            {
                Stop();
                Environment.Exit(0);
            } else if (command[1].Equals("restart", StringComparison.OrdinalIgnoreCase))
            {
                Restart();
                Environment.Exit(0);
            }
        }


        private void Start()
        {
            try
            {
                using (var controller = new ServiceController("RemPolService"))
                {
                    controller.Refresh();

                    if (controller.Status == ServiceControllerStatus.Stopped)
                    {
                        controller.Start();
                        controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                    }

                    MessageBox.Show("Service Running");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void Stop()
        {
            try
            {
                using (var controller = new ServiceController("RemPolService"))
                {
                    controller.Refresh();

                    if (controller.Status == ServiceControllerStatus.Running)
                    {
                        controller.Stop();
                        controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }

                    MessageBox.Show("Service Stopped");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void Restart()
        {
            try
            {
                using (var controller = new ServiceController("RemPolService"))
                {
                    controller.Refresh();

                    if (controller.Status == ServiceControllerStatus.Running)
                    {
                        controller.Stop();

                        controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }

                    controller.Start();

                    controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));

                    MessageBox.Show("Restart Successful");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void button1_Click_1(object sender, EventArgs e)
        {
            MessageBox.Show("Hello my Dear");
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void остановкаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunAsAdmin("stop");
        }



        private void запуститьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunAsAdmin("start");
        }



        private void паузаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void djpjyjdbnToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            AllocConsole();
            Console.WriteLine("Hello World");
        }



        private void restartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RunAsAdmin("restart");
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using(var controller = new ServiceController("RemPolService"))
            {
                notifyIcon1.Visible = false;
                notifyIcon1.Dispose();

                Application.Exit();
            }
        }
    }
}
