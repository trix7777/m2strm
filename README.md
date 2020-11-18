# m2strm  
Creates STRM-files from M3U8-file.  
  
Original developer: [TimTester.in](https://timtester.in/)  
  
New in version 3.0 alpha 1:  
- Configuration file: You can now use a configuration file for all user settings. Read more about the options for this file later on this page.  
- Unwanted groups file: this file contains groups you do not want to process. Read more about the options for this file later on this page.  
- Error log enable/disable.  
- Download M3U8-file: download your m3u8 directly from your IPTV-provider and then process it in one go.  
- Fully Mono compatible = runs on Linux with Mono installed - see [here](https://www.mono-project.com/docs/getting-started/install/linux/)  
- Even more...  still working on it.  
  
  
Tested with IPTV-providers:  
- N1  
  
  
### How to use  
X:\build\m2strm\bin\Debug>m2strm.exe /?  
Creates STRM-files from M3U8-file.  
  
m2strm.exe [OPTIONS] [drive:][path][filename]  
  
  filename        Specifies the M3U8-file to be processed. If not specified tries to get from configuration file.  
  
  /C              Create default configuration file. Warning: resets existing settings.  
  /U [filename]   Create 'unwanted groups' file with a list of all groups.  
  /D              Delete previously created directories only (no parsing).  
  /M              Download M3U8-file only (no parsing).  
  /G              Show a guide to help you get started quickly.  
  /V              Version information.  
  /? or /H        This help.  
  
Configuration file: X:\build\m2strm\bin\Debug\m2strm.exe.config  
Unwanted groups file: X:\build\m2strm\bin\Debug\uwgroups.cfg  
  
Example usage:  
m2strm.exe my.m3u8  
  
  
### Configuration file  
Create configuration with /C and then edit the configuration for your needs.  
  
BaseDirectory: Where the processed content should be output to.  
- Default value is "" (which translates to the directory where m2strm.exe is running from).  
- Example value: "X:\videos\strm\" or "\home\trix77\strm\"  

m3u8File: Where your M3U8-file is located.  
- Default value is ""  
- Example value: "X:\my.m3u8"  
  
Movies, Series, TV: Sub-directory names for your movies, series and tv content.  
- Default values are respectively: "VOD Movies", "VOD Series", "TV Channels"  
  
DeletePreviousDirEnabled  
- Default value is "True" -- previously created directories will be deleted before processing starts.  
Accepted values are: "" (same as default above), "True" (Enabled), "False" (Disabled).  
  
UnwantedCFGEnabled  
- Default value is "True" -- will use the unwanted groups file if found.  
Accepted values are: "" (same as default above), "True" (Enabled), "False" (Disabled).  
  
VerboseConsoleOutputEnabled  
- Default value is "False" -- will not output found items to console.  
Accepted values are: "" (same as default above), "True" (Enabled), "False" (Disabled).  
  
ErrorLogEnabled  
- Default value is "False" -- will not create the error log.  
Accepted values are: "" (same as default above), "True" (Enabled), "False" (Disabled).  
  
DownloadM3U8Enabled  
- Default value is "False" -- will not try to download a M3U8-file.  
Accepted values are: "" (same as default above), "True" (Enabled), "False" (Disabled).  
  
UserURL, UserPort, UserName, UserPass: The information you have gotten from your IPTV-provider.  
- Default value for all of them are "". Needs to be filled in if DownloadM3U8Enabled is to be enabled.  
Look at the link you've got from your IPTV-provider, something like this:  
http://ip.tv:8080/get.php?username=ABCDEFGHIJ&password=0123456789&type=m3u_plus&output=ts  
You can get all the information you need from it, look carefully, the above ampersands (&) for instance, are not part of either the username or the password.  
UserURL is "http://ip.tv"  
UserPort is "8080"  
UserName is "ABCDEFGHIJ"  
UserPass is "0123456789"  
  
### Unwanted groups file  
Create a fully populated unwanted groups file with /U and then edit this file and remove/comment out groups you **want** to process.  
- Everything **not** removed or commented out will be ignored while processing.  
- Comment out a line by putting // before the group name, eg:  
//Sweden  
//Norway  
//etc  
- To make use of the unwanted groups file, UnwantedCFGEnabled must be set to True (default).  
  
  