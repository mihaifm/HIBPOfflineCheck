# HIBP Offline Check

![screen](https://user-images.githubusercontent.com/981184/37559417-71ac2bc4-2a2e-11e8-8e3d-5877d9d7a999.png)

This is a __[KeePass](https://keepass.info/)__ plugin for __[Have I been pwned](https://haveibeenpwned.com/)__.    
It can perform both __offline__ and __online__ checks against the password breach list for any selected password entry.    
Double click the plugin column to get an instant status check, or use the right click menu to perform the same check for all selected passwords.

## Motivation

[Have I been pwned?](https://haveibeenpwned.com/) is an excellent tool for checking leaked passwords.
While it does provide an API for securely checking the passwords online, some bits of a hashed password still need to be sent to the service when performing this type of check.

This plugin offers the alternative of an offline check, by using the downloadable file provided by [Have I been pwned](https://haveibeenpwned.com/).    

Online check mode is also provided as an option, being implemented using the [k-anonimity](https://haveibeenpwned.com/API/v2#SearchingPwnedPasswordsByRange) model required by the HIBP public API.

The plugin adds a new column to KeePass. When double-clicking the column for a specific entry, the SHA1 hash is calculated for the password, which is then searched in the file. A status will be displayed on the column for that specific password.

## Features

- passwords can be checked in offline or online mode
- binary search in the large password file gives an instant result for the offline mode
- [bloom filter](https://en.wikipedia.org/wiki/Bloom_filter) support
- [k-anonimity](https://haveibeenpwned.com/API/v2#SearchingPwnedPasswordsByRange) method implemented for the online mode
- the status (Pwned or Secure) is saved in the KeePass database and will be retrieved when reopening the app, and updated if the password entry changes
- each password is individually checked only on user request
- multiple passwords can be checked in bulk by using the right click menu
- option to check all passwords in the database

## Prerequisites

Download the latest version of the [password list](https://haveibeenpwned.com/Passwords) using the [haveibeenpwned-downloader](https://github.com/HaveIBeenPwned/PwnedPasswordsDownloader):

    haveibeenpwned-downloader.exe pwnedpasswords

Downloading the file is not required if Online mode is selected in the options.

If you are using KeePass on Windows, this plugin requires **Microsoft .NET Framework 4.5** to run.

When running KeePass under Mono (on Ubuntu/Debian), if the plugin does not compile or load, verify that you have installed the `mono-mcs` package.

## Installation

- Download [HIBPOfflineCheck.plgx](https://github.com/mihaifm/HIBPOfflineCheck/releases/latest) from Releases.
- Copy it in the Plugins folder of the KeePass installation.

## Configuration

To configure the plugin, open `Tools` -> `HIBP Offline Check...`

![image](https://github.com/mihaifm/HIBPOfflineCheck/assets/981184/b134904f-5a8a-4cff-86cb-89fcd6abdf43)

If the Offline mode is selected then `Pwned passwords file` must be set to the password list file. If `Pwned passwords file` is not set then the plugin will try to find a password list file in the same location as `KeePass.exe`.

If you want to change `Column name`, a new column will be created with the new name and needs to be enabled under `View` -> `Configure Columns` -> `Provided by Plugins`. Before changing the column name, it is recommended that you clear the status of all entries (`Tools` -> `HIBP Offline Check` -> `Clear Status`).

## Usage

### Enable

In KeePass, enable the plugin column in `View` -> `Configure Columns...` -> `Provided by Plugins`.     
Double clicking the `Have I been pwned?` column for any entry will display the password status. The status is also automatically checked when creating or updating an entry.

### Single password check

__Double click__ a password entry under the `Have I been pwned?` column to get the status.

![image](https://user-images.githubusercontent.com/981184/46235975-6ce7d700-c385-11e8-9a1e-2d473d825ba1.png)    
    
### Multiple passwords check

__Select multiple entries__, then right click on the selection -> `Have I been pwned?` -> `Check`
    
![image](https://user-images.githubusercontent.com/981184/64819685-86465b00-d5b7-11e9-8e81-e95b31acbfd7.png)
        
### Check all passwords 

To check all the passwords in the database:    

`Tools` -> `HIBP Offline Check...` -> `Check All Passwords`

### Automatic checks

Newly created and updated entries are automatically checked. There is also an option to display a warning after creating an insecure password. 

### Find all pwned passwords

To view all your insecure passwords, use the Find menu (it will only display passwords which have been checked, so make sure to check all first):

`Find` -> `Pwned Passwords`

### Bloom filter

A [Bloom filter](https://en.wikipedia.org/wiki/Bloom_filter) allows you to save disk space by not having to store the HIBP passwords file on your drive. Instead, a generated file (currently under 1GB in size) would be loaded, providing an accuracy of 99.9% for password checking. Only about 1/1000 Secure passwords would be false positives, showing up as Pwned. Pwned passwords will *never* show up as Secure.

You can generate the Bloom filter by selecting `Tools` -> `HIBP Offline Check` -> `Bloom filter` and then `Generate Bloom Filter...`.
It may take anywhere between 15-45 minutes to generate the filter, depending on your hardware. For convenience the filter has also been uploaded to this separate [HIBPBloomFilter](https://github.com/mihaifm/HIBPBloomFilter) repository, so you can download it instead of generating it.

## Building the plugin

You can build the plugin from source using Visual Studio: open the .sln file and compile the Release configuration.
Copy the .dll from `bin\Release` to the Plugins folder of the KeePass installation.

**Enjoy!**

