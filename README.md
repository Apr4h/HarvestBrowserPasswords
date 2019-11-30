# HarvestBrowserPasswords
A Windows tool for extracting credentials locally stored by Google Chrome and Mozilla Firefox

Decrypts Google Chrome passwords for the currently logged-on user by locating "Login Data" database files and using DPAPI to decrypt passwords

Decrypts Mozilla Firefox passwords for all available profiles by locating 'key4.db' databases and 'logins.json' files for 3DES decryption. Supports master password decryption and brute-forcing if enabled.

## Usage
`HarvestBrowserPasswords.exe <options>`

## Options
-h                  Display help message

-g                  Find and decrypt Google Chrome Passwords

-f                  Find and decrypt Mozilla Firefox passwords

-a                  Find and decrypt all (chrome and Firefox) passwords

-p                  Specify master password if set

-b                  Brute-force master password if set

-w                  Specify wordlist for master password brute-force

-v                  Verbose output
