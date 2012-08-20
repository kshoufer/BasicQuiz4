using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.IO;

namespace BasicQuiz_v4_
{
    public partial class SplashPage : Form
    {
        bool bolMatch = false;
        int intUserID;
        string strUserName;
        string strUserPassword;
        string strUserPasswordReceived;
        byte[] bytUserPasswordReceived;
        byte[] bytUserKey;
        byte[] bytUserIV;
        System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection();
        
        public SplashPage()
        {
            InitializeComponent();
        }

        private void SplashPage_Load(object sender, EventArgs e)
        {
            
            if (!ReadUserSecurityTable() && CheckForEmptyUserProfileTable())
            {
                try
                {
                    // Create a new instance of the RijndaelManaged 
                    // class.  This generates a new key and initialization  
                    // vector (IV). 
                    using (RijndaelManaged myRijndael = new RijndaelManaged())
                    {
                    bytUserKey = myRijndael.Key;
                    bytUserIV = myRijndael.IV;
                    InsertKeyandIV();
                    }

                }// end try
                catch (Exception ex)
                {
                    Console.WriteLine("Error: {0}", ex.Message);
                }// end catch

                
            }// end if (ReadUserProfileTable)

            if (!ReadUserSecurityTable() && !CheckForEmptyUserProfileTable())
            {
                MessageBox.Show("Error!! - User Keys Detected Without Users in Database - Please Exit");
                txtPassword.Visible = false;
                lblPassword.Visible = false;
                txtUserName.Visible = false;
                lblUserName.Visible = false;
                lblHeading.Visible = false;
                btnEnter.Visible = false;
                btnExit.Visible = true;
            }

            ReadUserSecurityTable();

        }// end private void SplashPage_Load

        private void btnEnter_Click(object sender, EventArgs e)
        {
            strUserName = txtUserName.Text;
            strUserPasswordReceived = txtPassword.Text;
            
            ReadUserProfileTable();
            
            if (!bolMatch)
            {
                MessageBox.Show("The User Name and/or Passord Does Not Match" + "\n" + "Try Again, Add User or Exit");
                txtPassword.Text = "";
                txtUserName.Text = "";
                txtUserName.Focus();
                btnAddUser.Visible = true;
                btnExit.Visible = true;
                btnTryAgain.Visible = true;
                btnEnter.Visible = false;
            }
        }// end btnEnter_Click


        static byte[] EncryptStringToBytes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments. 
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;
            // Create an RijndaelManaged object 
            // with the specified key and IV. 
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption. 
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            // Return the encrypted bytes from the memory stream. 
            return encrypted;

        }

        static string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments. 
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");

            // Declare the string used to hold 
            // the decrypted text.
            string plaintext = null;

            // Create an RijndaelManaged object 
            // with the specified key and IV. 
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption. 
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream 
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;

        }

        private void btnAddUser_Click(object sender, EventArgs e)
        {

            conn.ConnectionString = @"Data Source=KEN-HP\SQLSERVER2008R2;Initial Catalog=quiz1;Integrated Security=True";
            strUserName = txtUserName.Text;
            strUserPassword = txtPassword.Text;
            ReadUserSecurityTable();
            bytUserPasswordReceived = EncryptStringToBytes(strUserPassword, bytUserKey, bytUserIV);

            try
            {
                conn.Open();

                string queryStmt = "INSERT INTO UserProfile(UserName, UserPassword) VALUES(@UserName, @UserPassword)";

                using (SqlCommand _cmd = new SqlCommand(queryStmt, conn))
                {
                    _cmd.Parameters.Add("@UserName", SqlDbType.NVarChar, 100);
                    _cmd.Parameters.Add("@UserPassword", SqlDbType.VarBinary, 100);
                    _cmd.Parameters["@UserName"].Value = strUserName;
                    _cmd.Parameters["@UserPassword"].Value = bytUserPasswordReceived;
                    _cmd.ExecuteNonQuery();
                }

            }// end try

            catch (Exception ex)
            {
                MessageBox.Show("Failed to insert to UserProfile");
            }
            finally
            {
                conn.Close();
            }

            // read the Profile Table and go to Question Page
            ReadUserProfileTable();

        }// end btnAddUser_Click

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void InsertKeyandIV()
        {
            // This block is executed only once before the UserProfile table is populated.
            conn.ConnectionString = @"Data Source=KEN-HP\SQLSERVER2008R2;Initial Catalog=quiz1;Integrated Security=True";

            try
            {
                conn.Open();

                string queryStmt = "INSERT INTO UserSecurity(UserKey, UserIV) VALUES(@UserKey, @UserIV)";

                using (SqlCommand _cmd = new SqlCommand(queryStmt, conn))
                {
                    _cmd.Parameters.Add("@UserKey", SqlDbType.VarBinary, 100);
                    _cmd.Parameters.Add("@UserIV", SqlDbType.VarBinary, 100);
                    _cmd.Parameters["@UserKey"].Value = bytUserKey;
                    _cmd.Parameters["@UserIV"].Value = bytUserIV;
                    _cmd.ExecuteNonQuery();
                }

            }// end try

            catch (Exception ex)
            {
                MessageBox.Show("Failed to insert to UserSecurity");
            }
            finally
            {
                conn.Close();
            }

        }// end InsertKeyandIV


        private bool ReadUserProfileTable()
        {
            bool bolHasRows = false;
            
            conn.ConnectionString = @"Data Source=KEN-HP\SQLSERVER2008R2;Initial Catalog=quiz1;Integrated Security=True";
            
            try
            {
                conn.Open();

                SqlCommand command = new SqlCommand(
                    "SELECT * FROM UserProfile;",
                    conn);

                SqlDataReader reader = command.ExecuteReader();


                if (reader.HasRows)
                {
                    bolHasRows = true;
                    
                    while (reader.Read())
                    {
                        
                        strUserName = (string)reader[1];
                        bytUserPasswordReceived = (byte[])reader[2];
                        strUserPasswordReceived = DecryptStringFromBytes(bytUserPasswordReceived, bytUserKey, bytUserIV);
                        if (strUserName == txtUserName.Text && strUserPasswordReceived == txtPassword.Text)
                        {
                            bolMatch = true;
                            intUserID = (int)reader[0];
                        }
                    }
                }// end if (reader.HasRows)
                else
                {
                    Console.WriteLine("No rows found in UserProfile.");
                    bolHasRows = false;
                }

                if (bolMatch)
                {
                    QuestionPage qp = new QuestionPage(intUserID);
                    qp.Visible = true;
                    this.Hide();
                }

                reader.Close();

            }// end try

            catch (Exception ex)
            {
                MessageBox.Show("Failed to connect to UserProfile Table");
            }
            finally
            {
                conn.Close();
            }

            return bolHasRows;

        }// ReadUserProfileTable


        private bool ReadUserSecurityTable()
        {
            bool bolHasRows = false;
            
            conn.ConnectionString = @"Data Source=KEN-HP\SQLSERVER2008R2;Initial Catalog=quiz1;Integrated Security=True";
            
            try
            {
                conn.Open();

                SqlCommand command = new SqlCommand(
                    "SELECT * FROM UserSecurity;",
                    conn);

                SqlDataReader reader = command.ExecuteReader();


                if (reader.HasRows)
                {
                    bolHasRows = true;

                    while (reader.Read())
                    {
                        bytUserKey = (byte[])reader[0];
                        bytUserIV = (byte[])reader[1];
                    }
                }// end if (reader.HasRows)
                else
                {
                    Console.WriteLine("No rows found in UserSecurity.");
                    bolHasRows = false;
                }

                reader.Close();

            }// end try

            catch (Exception ex)
            {
                MessageBox.Show("Failed to connect to UserSecurity Table");
            }
            finally
            {
                conn.Close();
            }

            return bolHasRows;

        }

        private bool CheckForEmptyUserProfileTable()
        {
            bool bolIsEmpty = true;

            conn.ConnectionString = @"Data Source=KEN-HP\SQLSERVER2008R2;Initial Catalog=quiz1;Integrated Security=True";

            try
            {
                conn.Open();

                SqlCommand command = new SqlCommand(
                    "SELECT * FROM UserProfile;",
                    conn);

                SqlDataReader reader = command.ExecuteReader();


                if (reader.HasRows)
                {
                    bolIsEmpty = false;

                }// end if (reader.HasRows)
                else
                {
                    Console.WriteLine("No rows found in UserProfile.");
                    bolIsEmpty = true;
                }

                reader.Close();

            }// end try

            catch (Exception ex)
            {
                MessageBox.Show("Failed to connect to UserProfile Table");
            }
            finally
            {
                conn.Close();
            }

            return bolIsEmpty;

        }// CheckForEmptyUserProfileTable

    
    }// end public partial class SplashPage : Form
}// end namespace BasicQuiz_v4_

