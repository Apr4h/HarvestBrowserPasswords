using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;

namespace HarvestBrowserPasswords
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

        [Option("help", HelpText = "Display help message")]
        public bool Help { get; set; }

        public Options()
        {
            Chrome = false;
            Firefox = false;
            All = false;
            Password = "";
            Outfile = "";
            Help = false;
        }
    }
}
