using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using Newtonsoft.Json;

namespace HarvestBrowserPasswords
{
    public class FirefoxDatabaseDecryptor
    {
        private string profileDir { get; set; }
        public FirefoxDatabaseDecryptor(string profile)
        {
            this.profileDir = profile;

            //Store a RootObject from FirefoxLoginsJSON (hopefully) containing multiple FirefoxLoginsJSON.Login instances
            FirefoxLoginsJSON.Rootobject JSONLogins = GetJSONLogins(profileDir);
        }

        // read logins.json file and deserialize the JSON into FirefoxLoginsJSON class
        public FirefoxLoginsJSON.Rootobject GetJSONLogins(string profileDir)
        {

            //Read logins.json from file and deserialise JSON into FirefoxLoginsJson object
            string file = File.ReadAllText(profileDir + @"\logins.json");
            FirefoxLoginsJSON.Rootobject JSONlogins = JsonConvert.DeserializeObject<FirefoxLoginsJSON.Rootobject>(file);

            //Iterate over each login node found in logins.json and decrypt usernames and passwords
            foreach (FirefoxLoginsJSON.Login login in JSONlogins.logins)
            {
                
            }

            return JSONlogins;
        }


    }
}
