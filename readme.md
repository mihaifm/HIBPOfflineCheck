# haveibeenpwned.plgx

![screen](https://user-images.githubusercontent.com/981184/37559417-71ac2bc4-2a2e-11e8-8e3d-5877d9d7a999.png)

This is a [KeePass](https://keepass.info/) plugin for [Have I been pwned?](https://haveibeenpwned.com/).
It performs a secure offline check against the password file for any individual password, 
only when requested by the user by double-clicking the plugin column.

## Motivation

[Have I been pwned?](https://haveibeenpwned.com/) is an excelent tool for checking leaked passwords.
While it does provide an API for securely checking the passwords online, some bits of a hashed password still need
to be sent to the service when performing this type of check.

This plugin offers the alternative of an offline check, by using the 30GB file provided by [Have I been pwned?](https://haveibeenpwned.com/).

The plugin adds a new column to KeePass. When double-clicking the column for a specific entry, the sha1 hash is calculated for the password,
which is then searched in the file. A status will be displayed on the column for that specific password.

Features:

- binary search in the 30GB file gives an instant result
- the status (Pwned or Secure) is saved in the KeePass database and will be retrieved when reopening the app, and cleared if the password entry changes
- each password is individually checked only on user request

## Prerequisites

- Download the [pwned-passwords-ordered-2.0.txt](https://downloads.pwnedpasswords.com/passwords/pwned-passwords-2.0.txt.7z.torrent) file from 
haveibeenpwned.com [password list](https://haveibeenpwned.com/Passwords). Use the torrent if possible, as suggested by the author. It's important
that you get the __ordered by hash__ version of the file.
- Extract the file from the 7zip archive
- Place the `pwned-passwords-ordered-2.0.txt` in the same location as `KeePass.exe`

## Installation

#### Secure:

- Build the plugin from source using Visual Studio: open the .sln file and compile the Release configuration.
- Copy the .dll from bin\Release to the Plugins folder of the KeePass installation

#### Quick

- Download haveibeenpwned.plgx from Realeases
- Copy it in the Plugins folder of the KeePass instalation

## Usage

In KeePass, enable the plugin column in `View->Configure Columns`. Double clicking the "Have I been pwned?" column for any entry will display
the password status. Editing an entry will clear the status.

Enjoy!

