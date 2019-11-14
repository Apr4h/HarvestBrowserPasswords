# HarvestBrowserPasswords
A Windows tool for extracting credentials locally stored by web browsers

Decrypts Google Chrome passwords for the currently logged-on user by locating "Login Data" database files and using DPAPI to decrypt passwords

Firefox password decryption functionality coming soon.

## Usage
`HarvestBrowserPasswords.exe <options>`

## Options
-h                  Display help message

-g                  Find and decrypt Google Chrome Passwords

-f                  Find and decrypt Mozilla Firefox passwords

-a                  Find and decrypt all (chrome and Firefox) passwords

-v                  Verbose output
