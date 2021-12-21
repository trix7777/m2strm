## m2strm

Creates STRM-files from M3U8-file.

Download latest release [here](https://github.com/trix7777/m2strm/releases)

#### New in version 3.0:
- *Configuration file*: You can now use a configuration file for all user settings.
- *Unwanted groups file*: this file contains groups you do not want to process.
- Program log enable/disable.
- Download M3U8-file: downloads your m3u8 directly from your IPTV-provider and then processes it in one go.
- Fully Mono compatible = runs on Linux with Mono installed - see [here](https://www.mono-project.com/download/stable/#download-lin)
- Dupe checking: only output the first of dupes found.
- Updates content: if URL for an item changed, the strm-file will be updated accordingly.
- Purging of old strm-files and empty directories.
- New groups log: a file containing new groups since configuration last changed.
- Even more...  still working on it. Please don't hesitate to come forward with ideas or feature requests, under [Discussions](https://github.com/trix7777/m2strm/discussions)
- Found a bug? Please report it under [Issues](https://github.com/trix7777/m2strm/issues) with details on how to replicate it.

#### Tested with IPTV-providers:
- N1

#### How to use
```
m2strm.exe /?

Creates STRM-files from M3U8-file.

m2strm.exe [OPTIONS] [drive:][path][filename]

  filename        The source M3U8-file to be processed.

  /C              Create default configuration file. Warning: resets existing settings.
  /U [filename]   Create the unwanted groups file with a list of all groups from filename.
  /D              Delete previously created directories and then quit.
  /M              Download M3U8-file and then quit. Uses config set information.
  /G              Show a guide to help you get started quickly.
  /V              Version information.
  /? or /H        This help.

Configuration file found: X:\m2strm\m2strm.exe.config
Unwanted groups file found: X:\m2strm\uwgroups.cfg

Example usage:
m2strm.exe original.m3u8
```

#### Configuration file
Create configuration with `m2strm.exe /C` and then edit the configuration for your needs.

`BaseDirectory`: Where the process is working and where the config files are.
- Default value is: "" (which translates to the directory where m2strm.exe is running from).
- Example values: `"X:\myapp\m2strm\"`, `"/home/trix77/m2strm/"`, `""`
- Mandatory: No (uses default value).

`OutDirectory`: Where the processed content should be output to.
- Default value is: "" (which translates to the directory where m2strm.exe is running from).
- Example values: `"X:\videos\strm\"`, `"\\server\videos\strm\"`, `"strm"`
- Mandatory: No (uses default value).

`m3u8File`: Where your M3U8-file is/will be located.
- Default value is: ""
- Example values: `"X:\myotherapp\original.m3u8"`, `"original.m3u8"`, `""`
- Mandatory: No (needs be set if not specified when arg `/U` or when `DownloadM3U8Enabled` is `False`).

`MoviesSubDir`, `SeriesSubDir`, `TVSubDir`: Sub-directory names for your movies, series and tv content.
- Default values are respectively: `"VOD Movies"`, `"VOD Series"`, `"TV Channels"`.
- Mandatory: No (uses default values).

`DeletePreviousDirEnabled`
- Default value is: `"False"` -- previously created directories will not be deleted before processing starts.
- Accepted values are: "" (same as default above), `"True"` (Enabled), `"False"` (Disabled).
- Mandatory: No (uses default value).

`UnwantedCFGEnabled`
- Default value is: `"True"` -- will use the unwanted groups file if found.
- Accepted values are: "" (same as default above), `"True"` (Enabled), `"False"` (Disabled).
- Mandatory: No (uses default value).

`VerboseConsoleOutputEnabled`
- Default value is: `"False"` -- will not output found items to console.
- Accepted values are: "" (same as default above), `"True"` (Enabled), `"False"` (Disabled).
- Mandatory: No (uses default value).

`ProgramLogEnabled`
- Default value is: `"True"` -- will create the program log (`m2strm.log`) in a subdirectory `/log` of the program file. Will also create a couple of other logs (see below).
- Accepted values are: "" (same as default above), `"True"` (Enabled), `"False"` (Disabled).  
- Mandatory: No (uses default value).  
`allgroups.log`: contains all groups found in source just like `m2strm.exe /U` does but outputs to allgroups.log instead, during each parse.  
`dupes.log`: contains duplicate items found. Read more about dupes below.  
`new.log`: contains new items found.  
`purged.log`: contains items that was purged (if `PurgeFilesEnabled` is set to `True`).  
`uwgroups.log`: contains the groups not in or commented out in `uwgroups.cfg`.  
`newgroups.log`: contains new groups found since the unwanted groups file (see below) was edited.

`PurgeFilesEnabled`
- Default value is: `"True"` -- will purge (delete) files no longer found in the source. If, after purge, found an empty directory, will also be deleted.
- Accepted values are: "" (same as default above), `"True"` (Enabled), `"False"` (Disabled).
- Mandatory: No (uses default value).

`MovieGroupSubdirEnabled`
- Default value is: `"False"` -- will not create a movie group subdir if the movie belongs to a group.
- Accepted values are: "" (same as default above), `"True"` (Enabled), `"False"` (Disabled).
- Mandatory: No (uses default value).

`DownloadM3U8Enabled`
- Default value is: `"False"` -- will not try to download a M3U8-file.
- Accepted values are: "" (same as default above), `"True"` (Enabled), `"False"` (Disabled).
- Mandatory: No (uses default value).

`UserURL`, `UserPort`, `UserName`, `UserPass`: The information you have gotten from your IPTV-provider.
- Default value for all of them are: "".
- Mandatory: Yes (if `DownloadM3U8Enabled` is `True`).
- Look at the link you've got from your IPTV-provider, something like this:  
`http://ip.tv:8080/get.php?username=ABCDEFGHIJ&password=0123456789&type=m3u_plus&output=ts`  
You can get all the information you need from it, look carefully, the above ampersands `&` for instance, are not part of either the username nor the password.  
`UserURL`: `"http://ip.tv"`  
`UserPort`: `"8080"`  
`UserName`: `"ABCDEFGHIJ"`  
`UserPass`: `"0123456789"`  

#### Unwanted groups file
Create a fully populated unwanted groups file with `m2strm.exe /U` and then edit this file and "comment out" groups you **want** to process.
- Everything **not** commented out will be ignored while processing.
- Comment out a line by putting `//` before the group name.
- To make use of the unwanted groups file, `UnwantedCFGEnabled` must be set to `True` (default).
- The _NOGROUP group is a group for titles not in a group (thanks to IPTV-provider bug).

Example content, with both wanted (`//`) and not wanted groups:
```
//_NOGROUP
Sweden
Norway
Denmark
Finland
//Series: English [Multi-Sub]
//VOD: Premiere Cinemas [Multi-Sub]
```
In this example we tell the program that we want to process the following groups: *_NOGROUP*, *Series: English [Multi-Sub]* and *VOD: Premiere Cinemas [Multi-Sub]*. The rest: *Sweden*, *Norway*, *Denmark* and *Finland* will be ignored.  
**It is important that there is no space before or after // on each line and that no groups are removed, or the "new groups" function will not work.**

#### Using this program with Mono
You need to install Mono to use this program under Linux OS. In Debian that could be:  
`$ sudo apt-get install mono-devel mono-vbnc`  
Then run the program with:  
`$ mono m2strm.exe`

#### Notes about differences running under a Windows or a Linux environment:
- UNC network paths:  
Windows: Network paths like `"\\myserver\somepath"` can be used.  
Linux: Use a local or a mounted network location path instead.
- Case sensitivity:  
Windows: NTFS does not normally differentiate between upper or lower case letters in filenames, so when this program encounters two titles, the first one named `"This Title"` and the second one named `"This title"`, it will se them both as the same title and only create output from the first one it finds in the source file, the second one will be seen as a duplicate.  
Linux: This is resolved by dupe-checking in lowercase (see below under dupes); running on Linux will most likely produce the same output as if running on Windows.
- Illegal printable file characters, names and path length:
This program will try its best to make the output more or less the same no matter which platform it is running from. This might however set unnecessary restrictions to Linux filesystems. One example is the file path + filename length which normally in Windows is no more than 260 characters.

#### Notes about dupes:
- So what is a dupe according to the programming?
All titles found in the source are "filtered" though a special process, some characters are removed, some are replaced. Also all "tags" (everything in the title name that are within brackets, but the year, are considered to be a tag) are removed from the title. This process can in it self, as a side effect, create dupes. An example would be these two titles: `"The Title [PRE] [2020]"` and `"The Title [2020]"`, which both through this filter will become the same `"The Title (2020)"`.
- Version 3.0.0.5 introduces dupe-checking: Comparing lowercase title with an array of already found lowercase titles, if match; discard. This also resolves the problem with different output depending on OS case-sensitivity.
- If there are dupes: Only the first found in the source will be considered, the rest will be discarded.