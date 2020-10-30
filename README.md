# m2strm  
Creates STRM-files from M3U8-file.  
  
Original developer: [TimTester.in](https://timtester.in/)  
  
This is a customized version of TimTesters original.  
Specialized for N1 SE users.  
This version will only make STRM-files of movies and series and will only get the following groups:  
* Series: Barn  
* Series: English [Multi-Sub]  
* Series: NORDIC  
* VOD: (all but not the following):  
- 4k Movies [Multi-Sub] [Only On 4K Devices]  
- Albania  
- Arabic  
- Crtani Filmovi [Ex-yu]  
- Danske - Norska - Suomalainen Film  
- English Movies [Arabic Subtitle]  
- Events  
- Germany  
- India  
- Iran  
- Polska  
- Turkey  
- Vietnam  
- ex-Yu Movies  
  	
### How to use  
  
X:\source\repos\m2strm\m2strm\bin\Debug>m2strm.exe /?  
Original code by TimTester, forked by trix77 for N1/SB/SE  
Creates STRM-files from M3U8-file.  
  
m2strm.exe [drive:][path]filename [/D]  
  
  filename    Specifies the M3U8-file to be processed.  
  
  /D          Delete previously created directories.  
  /V          Version information.  
  
Examples:  
m2strm.exe original.m3u8  
m2strm.exe original.m3u8 /D  
  
- The STRM files are located in the application's startup directory  
  
- Add the folders to your Emby / Jellyfin / Kodi library  