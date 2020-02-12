# owdl
CS:GO Overwatch Downloader

This tool sniffs the http packages to get the link for the original overwatch replay.

It will automatically download and extract the file.

## Screenshot
![](https://github.com/labo89/owdl/blob/master/screenshots/screenshot1.png?raw=true "")

## Prerequisites
[.NET Framework 4.6.1](https://www.microsoft.com/de-de/download/details.aspx?id=49982)

[WinPcap](https://www.winpcap.org/install/default.htm)

## Usage
Download and start the file. Then go to csgo and download the evidence. 

The file will be downloaded to temp directory and extracted to desktop.

## Download
Download here: https://github.com/labo89/owdl/releases

## Dependencies
The following dependencies are included in the executable.

[System.Runtime.CompilerServices.Unsafe](https://github.com/dotnet/runtime/tree/master/src/libraries/System.Runtime.CompilerServices.Unsafe)

[Packet.Net](https://github.com/chmorgan/packetnet)

[sharppcap](https://github.com/chmorgan/sharppcap)

[SharpZipLib](https://github.com/icsharpcode/SharpZipLib)

## Acknowledgments
[aequabit for OwSniff, which I used as a base](https://github.com/aequabit/OwSniff)

## License
This project is licensed under the MIT License - see the LICENSE file for details
