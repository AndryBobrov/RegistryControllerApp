using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BCrypt.Net;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Win32;
using MySqlConnector;


namespace ServiceController
{
    public partial class Authorization : Form
    {
        private readonly PasswordHasher<string> _CheckPass = new PasswordHasher<string>();


        private const int SaltSize = 16;
        private const int HashSize = 20;
        private const int Iterations = 100000;

        
        public Authorization()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private async void button1_Click(object sender, EventArgs e)
        {

            string connectdata = "server=45.84.196.164;user=Andrusha;database=lena9947_test;password=Cw9&y635g";
            


            string usersearch = "SELECT Passwort, Email, id FROM Clients WHERE Email = @email";



            using (MySqlConnection conn = new MySqlConnection(connectdata))
            {


                using (MySqlCommand command = new MySqlCommand(usersearch, conn))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@email", textBox1.Text);

                        string emailfield = textBox1.Text;
                        string passfield = textBox2.Text;

                        if(emailfield == "" && passfield == "")
                        {
                            MessageBox.Show("Please enter your Login Data", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        } else if (emailfield == "")
                        {
                            MessageBox.Show("Email Address required", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        } else if (passfield == "")
                        {
                            MessageBox.Show("Password required", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                            await conn.OpenAsync();

                        
                        MySqlDataReader reader = await command.ExecuteReaderAsync();

                        DataTable table = new DataTable();
                        table.Load(reader);


                        //dataGridView1.DataSource = table;

                        String BankPass = "";
                        String InputPass = "";
                        String userid = "";

                        try
                        {
                            BankPass = table.Rows[0][0].ToString();
                            InputPass = textBox2.Text;
                            userid = table.Rows[0][2].ToString();
                        }catch (Exception ex)
                        {
                            MessageBox.Show("Incorrect Email or Password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        

                        string currentaccname = Environment.UserName;
                        
                        reader.Close();

                        try
                        {
                            bool verified = BCrypt.Net.BCrypt.Verify(InputPass, BankPass);

                            if (verified)
                            {
                                string computers = "SELECT compid FROM userscomps WHERE userid = @userid";

                                using (MySqlCommand getcompid = new MySqlCommand(computers, conn))
                                {
                                    getcompid.Parameters.AddWithValue("@userid", userid);

                                    MySqlDataReader send = await getcompid.ExecuteReaderAsync();

                                    DataTable comptable = new DataTable();
                                    comptable.Load(send);

                                    send.Close();

                                    bool templatefound = false;

                                    foreach (DataRow row in comptable.Rows)
                                    {
                                        string compsearch = "SELECT compname FROM comps WHERE id = @compid";

                                        using (MySqlCommand searching = new MySqlCommand(compsearch, conn))
                                        {
                                            searching.Parameters.AddWithValue("@compid", row["compid"].ToString());

                                            MySqlDataReader ask = await searching.ExecuteReaderAsync();

                                            DataTable compnames = new DataTable();
                                            compnames.Load(ask);

                                            string compname = compnames.Rows[0][0].ToString();

                                            if (string.Equals(compname, currentaccname, StringComparison.OrdinalIgnoreCase) == true)
                                            {
                                                string CompID = row["compid"].ToString();


                                                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\ServiceSettings", true))
                                                {
                                                    key.SetValue("CompID", CompID, RegistryValueKind.DWord);
                                                    key.SetValue("UserID", userid, RegistryValueKind.DWord);
                                                }

                                                templatefound = true;
                                            }

                                        }
                                    }

                                    if(templatefound == false)
                                    {
                                        MessageBox.Show($"No template found. Please create a new template with the name: {currentaccname}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        return;
                                    } else
                                    {
                                        MessageBox.Show("Connection Successful", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        return;
                                    }
                                }


                                //MessageBox.Show("Successful");
                            }
                            else
                            {
                                MessageBox.Show("Incorrect Email or Password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }

                        }
                        catch (BCrypt.Net.SaltParseException ex)
                        {
                            MessageBox.Show(ex.Message);
                        }



                        //MessageBox.Show(HashPassword(InputPass));
                        
                        conn.Close();

                    }
                    catch (Exception ex)
                    {

                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    }
                }
            }


            //string CompID = textBox1.Text;
            //string UserID = textBox2.Text;

            //HttpClient client = new HttpClient();


            //using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\ServiceSettings"))
            //{
            //    key.SetValue("CompID", CompID, RegistryValueKind.DWord);
            //    Registry.LocalMachine.OpenSubKey(@"SOFTWARE\ServiceSettings", true).SetValue("UserID", UserID, RegistryValueKind.DWord);
            //}

        }


        private string ComputeHash(string Input)
        {
            using (SHA256 hash = SHA256.Create())
            {
                byte[] bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(Input));

                StringBuilder builder = new StringBuilder();
                
                for(int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }


        private static bool VerifyPassword(string enteredPass, string DbPass)
        {
            try
            {
                byte[] hashBytes = Convert.FromBase64String(DbPass);


                byte[] salt = new byte[SaltSize];
                Array.Copy(hashBytes, 0, salt, 0, SaltSize);

                byte[] storedHash = new byte[HashSize];
                Array.Copy(hashBytes, SaltSize, storedHash, 0, HashSize);


                using (var algorithm = new Rfc2898DeriveBytes(enteredPass, salt, Iterations, HashAlgorithmName.SHA256))
                {
                    byte[] generatedHash = algorithm.GetBytes(HashSize);

                    for (int i = 0; i < HashSize; i++)
                    {
                        if (storedHash[i] != generatedHash[i])
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }


        public static string HashPassword(string password)
        {
            using (var algorithm = new Rfc2898DeriveBytes(password, SaltSize, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] salt = algorithm.Salt;
                byte[] hash = algorithm.GetBytes(HashSize);

                
                byte[] hashBytes = new byte[SaltSize + HashSize];
                Array.Copy(salt, 0, hashBytes, 0, SaltSize);
                Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

                return Convert.ToBase64String(hashBytes);
            }
        }



        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }

}
