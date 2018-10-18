# HIBP Offline Check

![screen](https://user-images.githubusercontent.com/981184/37559417-71ac2bc4-2a2e-11e8-8e3d-5877d9d7a999.png)

This is a __[KeePass](https://keepass.info/)__ plugin for __[Have I been pwned?](https://haveibeenpwned.com/)__.    
It performs a secure __offline__ check against the password file for any selected password entry.    
Double click the plugin column to get an instant status check, or use the right click menu to perform the same check 
for all selected passwords.

## Motivation

[Have I been pwned?](https://haveibeenpwned.com/) is an excellent tool for checking leaked passwords.
While it does provide an API for securely checking the passwords online, some bits of a hashed password still need
to be sent to the service when performing this type of check.

This plugin offers the alternative of an offline check, by using the 30GB file provided by [Have I been pwned?](https://haveibeenpwned.com/).

The plugin adds a new column to KeePass. When double-clicking the column for a specific entry, the sha1 hash is calculated for the password,
which is then searched in the file. A status will be displayed on the column for that specific password.

## Features

- binary search in the 30GB file gives an instant result
- the status (Pwned or Secure) is saved in the KeePass database and will be retrieved when reopening the app, and updated if the password entry changes
- each password is individually checked only on user request
- multiple passwords can be checked in bulk by using the right click menu (under "Selected Entries")

## Prerequisites

- Download the [pwned-passwords-ordered-by-hash.txt](https://downloads.pwnedpasswords.com/passwords/pwned-passwords-ordered-by-hash.7z.torrent) file from 
haveibeenpwned.com [password list](https://haveibeenpwned.com/Passwords). Use the torrent if possible, as suggested by the author.

    __It's important that you get the -ordered-by-hash- version of the file, the plugin uses it for fast searching__.
- Extract the file from the 7zip archive
- Place the `pwned-passwords-ordered-by-hash.txt` in the same location as `KeePass.exe`

## Installation

#### Secure:

- Build the plugin from source using Visual Studio: open the .sln file and compile the Release configuration.
- Copy the .dll from `bin\Release` to the Plugins folder of the KeePass installation

#### Quick

- Download [HIBPOfflineCheck.plgx](https://github.com/mihaifm/HIBPOfflineCheck/releases/latest) from Releases
- Copy it in the Plugins folder of the KeePass installation

## Usage

* __Enable__

In KeePass, enable the plugin column in `View->Configure Columns->Provided by Plugins`. Double clicking the "Have I been pwned?" column for any entry will display
the password status. Editing an entry will update the status.

--------------------------------------------------------------------------------------------

* __Double click__ a password entry under the `Have I been pwned?` column to get the status

![image](https://user-images.githubusercontent.com/981184/46235975-6ce7d700-c385-11e8-9a1e-2d473d825ba1.png)    
    
--------------------------------------------------------------------------------------------

* __Select multiple entries__, right click -> Selected Entries -> Have I been pwned?
    
![image](https://user-images.githubusercontent.com/981184/47184669-f1e66080-d333-11e8-8b14-01808a36706a.png)
        
    
--------------------------------------------------------------------------------------------

* __Entries are checked automattically after being updated__

![image](https://user-images.githubusercontent.com/981184/46236364-11b6e400-c387-11e8-8034-416c7c3ee492.png)


## Configuration

To configure the plugin, open `Tools -> HIBP Offline Check...`

![image](https://user-images.githubusercontent.com/981184/47183839-bba7e180-d331-11e8-8b4a-3afe75fd8dbe.png)

Note that after changing the `Column name`, a new column will be create with the new name, that needs to be enabled under `View->Configure Columns->Provided by Plugins`    
Before changing the column name, it is recommended that you clear the status of all entries (using `right click->Selected Entries->Clear pwned status`)    
To make sure that you no longer have entries under the old column name, see if it still appears under `View->Configure Columns->Custom Fields`.    

Enjoy!

