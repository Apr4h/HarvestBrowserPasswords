using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HarvestBrowserPasswords
{
    public class FirefoxLoginsJSON
    {

        public class Rootobject
        {
            public int nextId { get; set; }
            public Login[] logins { get; set; }
            public int version { get; set; }
            public object[] potentiallyVulnerablePasswords { get; set; }
            public Dismissedbreachalertsbyloginguid dismissedBreachAlertsByLoginGUID { get; set; }
        }

        public class Dismissedbreachalertsbyloginguid
        {
        }

        public class Login
        {
            public int id { get; set; }
            public string hostname { get; set; }
            public string httpRealm { get; set; }
            public string formSubmitURL { get; set; }
            public string usernameField { get; set; }
            public string passwordField { get; set; }
            public string encryptedUsername { get; set; }
            public string encryptedPassword { get; set; }
            public string guid { get; set; }
            public int encType { get; set; }
            public long timeCreated { get; set; }
            public long timeLastUsed { get; set; }
            public long timePasswordChanged { get; set; }
            public int timesUsed { get; set; }
        }
    }
}
