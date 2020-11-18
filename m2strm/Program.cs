using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Configuration;
using System.Reflection;
using System.Net;

namespace m2strm
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //Check .Net Framework version
            var NetFrameworkVersion = new Version(4, 6, 57, 0);
            EnsureSupportedDotNetFrameworkVersion(NetFrameworkVersion);

            //Set and start stopwatch to time the program run
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                //Set the counters to zero
                int counter_Movies = 0;
                int counter_MoviesActual = 0;
                int counter_Series = 0;
                int counter_SeriesActual = 0;
                int counter_TV = 0;
                int counter_TVActual = 0;

                //Define newline
                string NewLine = ("\n");

                //Error codes
                bool filenotfound = false;

                //Set various stuff
                string Creator = "Original code by TimTester ©2020\nForked with persmissions and converted to C# (Mono compatible) by trix77 ©2020";
                string Location = Convert.ToString(Environment.GetCommandLineArgs()[0]);
                string FileName = Path.GetFileName(Location);
                string Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                string ConfigFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
                var UserConfigFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var UserSettings = UserConfigFile.AppSettings.Settings;

                //Default settings, can be overridden in config or in args
                string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string m3u8File = "";
                string Movies = "VOD Movies";
                string Series = "VOD Series";
                string TV = "TV Channels";
                bool DeletePreviousDirEnabled = true;
                bool UnwantedCFGEnabled = true;
                bool VerboseConsoleOutputEnabled = false;
                bool ErrorLogEnabled = false;
                bool DownloadM3U8Enabled = false;
                string UserURL = "";
                string UserPort = "";
                string UserName = "";
                string UserPass = "";
 
                //Get settings from external config
                if (ConfigurationManager.AppSettings.Get("BaseDirectory") != null && ConfigurationManager.AppSettings.Get("BaseDirectory") != "")
                    BaseDirectory = ConfigurationManager.AppSettings.Get("BaseDirectory");

                if (ConfigurationManager.AppSettings.Get("Movies") != null && ConfigurationManager.AppSettings.Get("Movies") != "")
                    Movies = ConfigurationManager.AppSettings.Get("Movies");

                if (ConfigurationManager.AppSettings.Get("Series") != null && ConfigurationManager.AppSettings.Get("Series") != "")
                    Series = ConfigurationManager.AppSettings.Get("Series");

                if (ConfigurationManager.AppSettings.Get("TV") != null && ConfigurationManager.AppSettings.Get("TV") != "")
                    TV = ConfigurationManager.AppSettings.Get("TV");

                if (ConfigurationManager.AppSettings.Get("DeletePreviousDirEnabled") != null && ConfigurationManager.AppSettings.Get("DeletePreviousDirEnabled") != "")
                    DeletePreviousDirEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("DeletePreviousDirEnabled"));

                if (ConfigurationManager.AppSettings.Get("UnwantedCFGEnabled") != null && ConfigurationManager.AppSettings.Get("UnwantedCFGEnabled") != "")
                    UnwantedCFGEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("UnwantedCFGEnabled"));

                if (ConfigurationManager.AppSettings.Get("VerboseConsoleOutputEnabled") != null && ConfigurationManager.AppSettings.Get("VerboseConsoleOutputEnabled") != "")
                    VerboseConsoleOutputEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("VerboseConsoleOutputEnabled"));

                if (ConfigurationManager.AppSettings.Get("ErrorLogEnabled") != null && ConfigurationManager.AppSettings.Get("ErrorLogEnabled") != "")
                    ErrorLogEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("ErrorLogEnabled"));

                if (ConfigurationManager.AppSettings.Get("DownloadM3U8Enabled") != null && ConfigurationManager.AppSettings.Get("DownloadM3U8Enabled") != "")
                    DownloadM3U8Enabled = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("DownloadM3U8Enabled"));

                if (ConfigurationManager.AppSettings.Get("UserURL") != null && ConfigurationManager.AppSettings.Get("UserURL") != "")
                    UserURL = ConfigurationManager.AppSettings.Get("UserURL");

                if (ConfigurationManager.AppSettings.Get("UserPort") != null && ConfigurationManager.AppSettings.Get("UserPort") != "")
                    UserPort = ConfigurationManager.AppSettings.Get("UserPort");

                if (ConfigurationManager.AppSettings.Get("UserName") != null && ConfigurationManager.AppSettings.Get("UserName") != "")
                    UserName = ConfigurationManager.AppSettings.Get("UserName");

                if (ConfigurationManager.AppSettings.Get("UserPass") != null && ConfigurationManager.AppSettings.Get("UserPass") != "")
                    UserPass = ConfigurationManager.AppSettings.Get("UserPass");

                //Other settings
                string uwgFile = (BaseDirectory + "uwgroups.cfg");
                string uwgFile_old = (BaseDirectory + "uwgroups.cfg.old");
                string[] uwgArray = { };
                string ErrorLogFile = (BaseDirectory + "error.log");
                string Userm3u8File = (BaseDirectory + "original.m3u8");
                StreamWriter ErrorLog = null;
                WebClient webClient = new WebClient();
                webClient.Headers.Add("user-agent", "Wget/1.20.1 (linux-gnu)");
                string UserURLFull = (UserURL + ":" + UserPort + "/get.php?username=" + UserName + "&password=" + UserPass + "&type=m3u_plus&output=ts");

                //Combine to output dirs
                string MoviesDir = Path.Combine(BaseDirectory, Movies);
                string SeriesDir = Path.Combine(BaseDirectory, Series);
                string TVDir = Path.Combine(BaseDirectory, TV);

                //Check if args is given
                if (args.Length > 0)
                {
                    if (args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "/h" || args[0].ToLower() == "-h" || args[0].ToLower() == "/help" || args[0].ToLower() == "-help" || args[0].ToLower() == "--help")
                    {
                        Console.WriteLine("Creates STRM-files from M3U8-file.");
                        Console.WriteLine("\n" + FileName + " [OPTIONS] [drive:][path][filename]");
                        Console.WriteLine("\n  filename        Specifies the M3U8-file to be processed. If not specified tries to get from configuration file.");
                        Console.WriteLine("\n  /C              Create default configuration file. Warning: resets existing settings.");
                        Console.WriteLine("  /U [filename]   Create 'unwanted groups' file with a list of all groups.");
                        Console.WriteLine("  /D              Delete previously created directories only (no parsing).");
                        Console.WriteLine("  /M              Download M3U8-file only (no parsing).");
                        Console.WriteLine("  /G              Show a guide to help you get started quickly.");
                        Console.WriteLine("  /V              Version information.");
                        Console.WriteLine("  /? or /H        This help.");
                        if (File.Exists(ConfigFile))
                        {
                            Console.WriteLine("\nConfiguration file: " + ConfigFile);
                        }
                        if (!File.Exists(ConfigFile))
                        {
                            Console.WriteLine("\nConfiguration file not found.");
                            Console.WriteLine("Tip: Create configuration with /C and then edit it for your needs.");
                        }
                        if (File.Exists(uwgFile))
                        {
                            Console.WriteLine("\nUnwanted groups file: " + uwgFile);
                        }
                        if (!File.Exists(uwgFile))
                        {
                            Console.WriteLine("\nUnwanted groups file not found.");
                            Console.WriteLine("Tip: Create the unwanted groups file with /U and then edit it for your needs.");
                        }
                        Console.WriteLine("\nExample usage:");
                        Console.WriteLine(FileName + " my.m3u8");
                        return;
                    }
                    else if (args[0].ToLower() == "/m")
                    {
                        Console.WriteLine("*** Downloading M3U8-file to: " + Userm3u8File);
                        //webClient.DownloadFile(new Uri(UserURLFull), Userm3u8File);
                        Console.WriteLine(UserURLFull);
                        return;
                    }
                    else if (args[0].ToLower() == "/v")
                    {
                        Console.WriteLine(Creator);
                        Console.WriteLine(FileName + " version " + Version);
                        return;
                    }
                    else if (args[0].ToLower() == "/c")
                    {
                        //Remove user config values
                        UserSettings.Add("BaseDirectory", "");
                        UserSettings.Add("m3u8File", "");
                        UserSettings.Add("Movies", "");
                        UserSettings.Add("Series", "");
                        UserSettings.Add("TV", "");
                        UserSettings.Add("DeletePreviousDirEnabled", "");
                        UserSettings.Add("UnwantedCFGEnabled", "");
                        UserSettings.Add("VerboseConsoleOutputEnabled", "");
                        UserSettings.Add("ErrorLogEnabled", "");
                        UserSettings.Add("DownloadM3U8Enabled", "");
                        UserSettings.Add("UserURL", "");
                        UserSettings.Add("UserPort", "");
                        UserSettings.Add("UserName", "");
                        UserSettings.Add("UserPass", "");
                        //Set default user config values
                        UserSettings["BaseDirectory"].Value = BaseDirectory;
                        UserSettings["m3u8File"].Value = m3u8File;
                        UserSettings["Movies"].Value = Movies;
                        UserSettings["Series"].Value = Series;
                        UserSettings["TV"].Value = TV;
                        UserSettings["DeletePreviousDirEnabled"].Value = Convert.ToString(DeletePreviousDirEnabled);
                        UserSettings["UnwantedCFGEnabled"].Value = Convert.ToString(UnwantedCFGEnabled);
                        UserSettings["VerboseConsoleOutputEnabled"].Value = Convert.ToString(VerboseConsoleOutputEnabled);
                        UserSettings["ErrorLogEnabled"].Value = Convert.ToString(ErrorLogEnabled);
                        UserSettings["DownloadM3U8Enabled"].Value = Convert.ToString(DownloadM3U8Enabled);
                        UserSettings["UserURL"].Value = UserURL;
                        UserSettings["UserPort"].Value = UserPort;
                        UserSettings["UserName"].Value = UserName;
                        UserSettings["UserPass"].Value = UserPass;
                        UserConfigFile.Save(ConfigurationSaveMode.Modified);
                        Console.WriteLine("*** Configuration file created: " + ConfigFile);
                        return;
                    }

                    else if (args[0].ToLower() == "/g")
                    {
                        Console.WriteLine("Guide will come later.");
                        return;
                    }

                    else if (args[0].ToLower() == "/d")
                    {
                        Console.WriteLine("*** Deleting previously created directories.");
                        if (Directory.Exists(MoviesDir))
                            Directory.Delete(MoviesDir, true);
                        if (Directory.Exists(SeriesDir))
                            Directory.Delete(SeriesDir, true);
                        if (Directory.Exists(TVDir))
                            Directory.Delete(TVDir, true);
                        return;
                    }

                    else if (args[0].ToLower() == "/u")
                    {
                        if (args.Length > 1)
                        {
                            //arg converted to m8u8File
                            m3u8File = args[1];
                            if (File.Exists(m3u8File))
                            {
                                if (ConfigurationManager.AppSettings.Get("m3u8File") != null && ConfigurationManager.AppSettings.Get("m3u8File") != "")
                                    Console.WriteLine("*** Using arg specified m3u8-file: " + m3u8File + " (overriding config).");
                                else Console.WriteLine("*** Using arg specified m3u8-file: " + m3u8File + " (not set in config).");
                            }
                            else
                            {
                                filenotfound = true;
                                Console.WriteLine("File not found - " + m3u8File);
                                return;
                            }
                        }
                        else
                        {
                            if (ConfigurationManager.AppSettings.Get("m3u8File") != null && ConfigurationManager.AppSettings.Get("m3u8File") != "")
                                m3u8File = ConfigurationManager.AppSettings.Get("m3u8File");
                            if (File.Exists(m3u8File))
                            {
                                Console.WriteLine("*** Using config set m3u8-file: " + m3u8File);
                            }
                            else
                            {
                                filenotfound = true;
                                Console.WriteLine("File not found - " + m3u8File);
                                return;
                            }
                        }

                        //Backup old uwgFile
                        if (File.Exists(uwgFile))
                        {
                            File.Copy(uwgFile, uwgFile_old, true);
                        }

                        //Set the content of the m3u8File to FileText
                        string FileText = File.ReadAllText(m3u8File);

                        //Normalize linefeed
                        FileText = FileText.Replace("\r\n", "\n");

                        //ngexist
                        bool ngexist = false;

                        //Empty old contents of uwgFile
                        File.WriteAllText(@uwgFile, string.Empty);

                        //Create a uwgFile with a list of all groups
                        string[] FileText_lines = FileText.Split(NewLine.ToCharArray());
                        for (var index = 1; index <= FileText_lines.Length - 1; index++)
                        {
                            string line1 = FileText_lines[(int)index];
                            //Find a line that starts with #EXTINF:
                            if (line1.ToLower().StartsWith("#extinf:"))
                            {
                                //Set strings
                                string GROUP = "";

                                //Get NAME and GROUP using regex pattern and capture groups (only using GROUP here)
                                string EXTINFRegexLinePat = @"^#EXTINF:.* \btvg-name=""([^""]+|)"".* \bgroup-title=""([^""]+|)"",.*$";
                                Match match = Regex.Match(line1, EXTINFRegexLinePat, RegexOptions.IgnoreCase);
                                GROUP = match.Groups[2].Value;

                                //Set the contents of uwgFile
                                string[] contents = File.ReadAllLines(uwgFile);

                                //appendText
                                string appendText = "";

                                //Check if noname group
                                if (GROUP == "")
                                {
                                    //Console.WriteLine("WARNING: There are titles not in groups.");
                                    ngexist = true;
                                    //Append _NOGROUP + NewLine
                                    GROUP = "_NOGROUP";
                                    //appendText = "_NOGROUP" + NewLine;
                                }

                                //Less output by not letting same output twice
                                if (contents.Contains(GROUP) == false)
                                {
                                    Console.WriteLine("Found group: " + GROUP);
                                    //Append GROUP + NewLine
                                    appendText = GROUP + NewLine;
                                    File.AppendAllText(uwgFile, appendText);
                                }
                            }
                        }

                        //Remove blanks, doubles and sorts the grops found
                        string[] contents2 = File.ReadAllLines(uwgFile);
                        contents2 = contents2.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                        Array.Sort(contents2);
                        File.WriteAllLines(uwgFile, contents2.Distinct().ToArray());
                        Console.WriteLine("\n*** Unwanted groups file created: " + uwgFile);
                        Console.WriteLine("*** Info: Edit this file and remove/comment out groups you want to process.\n*** Everything not removed/commented out will be ignored while processing.\n*** (comment out a line by putting // before the group name.)\n*** To make use of it, UnwantedCFGEnabled must be set to True (default).");
                        if (ngexist == true)
                        {
                            Console.WriteLine("*** Note: We've found titles not in groups, they will be processed in _NOGROUP.");
                        }
                        return;
                    }

                    else if (args[0].StartsWith("/"))
                    {
                        Console.WriteLine("Invalid switch - " + args[0]);
                        Console.WriteLine("Type /? for help");
                        return;
                    }
                    else
                    {
                        m3u8File = args[0];
                        if (File.Exists(m3u8File))
                        {
                            if (ConfigurationManager.AppSettings.Get("m3u8File") != null && ConfigurationManager.AppSettings.Get("m3u8File") != "")
                                Console.WriteLine("*** Using arg specified m3u8-file: " + m3u8File + " (overriding config).");
                            else Console.WriteLine("*** Using arg specified m3u8-file: " + m3u8File + " (not set in config).");
                        }
                        else
                        {
                            filenotfound = true;
                        }
                    }
                }
                else
                {
                    if (ConfigurationManager.AppSettings.Get("m3u8File") != null && ConfigurationManager.AppSettings.Get("m3u8File") != "")
                        m3u8File = ConfigurationManager.AppSettings.Get("m3u8File");
                    if (File.Exists(m3u8File) && (DownloadM3U8Enabled != true))
                    {
                        Console.WriteLine("*** Using config set m3u8-file: " + m3u8File);
                    }
                    else
                    {
                        if (!File.Exists(m3u8File))
                        {
                            filenotfound = true;
                        }
                    }
                }

                //Log
                //ErrorLog.WriteLine("\n" + DateTime.Now + ": " + "Stuff here");

                //Pause
                //Console.WriteLine("I am here:");
                //Console.ReadKey();

                //Check if user wants to download m3u8 before parsing
                if (DownloadM3U8Enabled == true)
                {
                    Console.WriteLine("*** Downloading M3U8-file to: " + Userm3u8File);

                    //webClient.DownloadFile(new Uri(UserURLFull), Userm3u8File);
                    //Console.WriteLine(UserURLFull);
                    
                    if (m3u8File != null || m3u8File != "")
                    {
                        Console.WriteLine("*** Using downloaded m3u8-file: " + Userm3u8File + " (overriding both config and args).");
                        Console.WriteLine("*** Info: To disable this override, DownloadM3U8Enabled must be set to False (default).");
                    }
                    else
                    {
                        Console.WriteLine("*** Using downloaded m3u8-file: " + Userm3u8File);
                    }
                    //Setting m3u8File to Userm3u8File
                    m3u8File = Userm3u8File;
                }

                //M3U
                if (File.Exists(m3u8File))
                {
                    //Start log
                    if (ErrorLogEnabled == true)
                    {
                        ErrorLog = File.AppendText(ErrorLogFile);
                        ErrorLog.WriteLine("\n" + DateTime.Now + ": " + "*** Log begin, " + FileName + ", " + Version);
                    }

                    //Check if config exists
                    if (File.Exists(ConfigFile))
                    {
                        Console.WriteLine("*** Config file found and in use: " + ConfigFile);
                    }

                    //Set uwgFile if wanted and exists
                    if (File.Exists(uwgFile) && UnwantedCFGEnabled == true)
                    {
                        Console.WriteLine("*** Unwanted groups file found and in use: " + uwgFile);
                        uwgArray = File.ReadAllLines(uwgFile);
                    }
                    else if (File.Exists(uwgFile) && UnwantedCFGEnabled == false)
                    {
                        Console.WriteLine("*** Unwanted groups file found but not in use: " + uwgFile);
                        uwgArray = File.ReadAllLines(uwgFile);
                    }

                    //Delete previously created directories
                    if (DeletePreviousDirEnabled == true)
                    {

                        if (ConfigurationManager.AppSettings.Get("DeletePreviousDirEnabled") == null || ConfigurationManager.AppSettings.Get("DeletePreviousDirEnabled") == "")
                            Console.WriteLine("*** Deleting previously created directories (not set in config).");
                        else Console.WriteLine("*** Deleting previously created directories.");
                        if (Directory.Exists(MoviesDir))
                            Directory.Delete(MoviesDir, true);
                        if (Directory.Exists(SeriesDir))
                            Directory.Delete(SeriesDir, true);
                        if (Directory.Exists(TVDir))
                            Directory.Delete(TVDir, true);
                    }
                    else
                    {
                        Console.WriteLine("*** Not deleting previously created directories.");
                    }

                    Console.WriteLine("*** Console output is: " + VerboseConsoleOutputEnabled);

                    Console.WriteLine("*** Processing " + m3u8File);
                    //Console.ReadKey();
                    {
                        string FileText = File.ReadAllText(m3u8File);

                        //Normalize linefeed
                        FileText = FileText.Replace("\r\n", "\n");

                        string GROUPrepeat = "";

                        //Split each line into array
                        string[] FileText_lines = FileText.Split(NewLine.ToCharArray());
                        for (var index = 1; index <= FileText_lines.Length - 1; index++)
                        {
                            string line1 = FileText_lines[(int)index];
                            //Find a line that starts with #EXTINF:
                            if (line1.ToLower().StartsWith("#extinf:"))
                            {
                                string line2 = FileText_lines[index + 1];

                                //Reset strings
                                string NAME = "";
                                string GROUP = "";
                                string URL = "";
                                bool SKIP = false;

                                //Get NAME and GROUP using regex pattern and capture groups
                                string EXTINFRegexLinePat = @"^#EXTINF:.* \btvg-name=""([^""]+|)"".* \bgroup-title=""([^""]+|)"",.*$";
                                Match match = Regex.Match(line1, EXTINFRegexLinePat, RegexOptions.IgnoreCase);
                                NAME = match.Groups[1].Value;
                                GROUP = match.Groups[2].Value;

                                //What happens if GROUP is null? This will be the case if iptv-provider messed up and did not put a title in a group.
                                //As we do not put series in groups and only use NAME, series not in a group will be processed the same way as thouse in a group.
                                //Still need to check what happens if a movie-title is not in a group. In such a case we need to put it in a NOGROUP.
                                //All items that is not in a group will get the new NOGROUP value, only movies and TV will be affected as series is not put in groups.
                                if (GROUP == "")
                                {
                                    GROUP = "_NOGROUP";
                                }

                                foreach (string uwgLine in uwgArray)
                                {
                                    if (GROUP == uwgLine)
                                        {
                                        //Less output lines if GROUPcontains is still same as GROUP
                                        if (GROUPrepeat != GROUP)
                                        {
                                            if (ErrorLogEnabled == true) ErrorLog.WriteLine(DateTime.Now + ": " + "Unwanted group: '" + GROUP + "' match in uwgLine: '" + uwgLine + "'");
                                        }
                                        SKIP = true;
                                        GROUPrepeat = GROUP;
                                    }
                                    continue;
                                }

                                if (SKIP == false)
                                {
                                    //Old code for getting NAME using positions
                                    //int Start_Name = line1.IndexOf("tvg-name=") + 10;
                                    //int Start_Logo = line1.IndexOf("tvg-logo=") + 10;
                                    //Name (don't know why & was removed in NAME)
                                    //NAME = (line1.Substring(Start_Name, Start_Logo - Start_Name - 12).Replace("&", ""));

                                    //Get the URL
                                    URL = (line2.Replace("\r\n", "").Replace("\n", ""));

                                    //Run NAME and GROUP through special char filters
                                    NAME = (NAMEFilterFileNameChars(NAME));
                                    GROUP = (GROUPFilterFileNameChars(GROUP));

                                    //Set TYPE (movie or series) from URL -- this works on N1 but might not work on others. Ideas for making it work with others:
                                    //with bash I would do it like this:
                                    //SERIES=$(echo "$LINE" | sed -E 's/.*tvg-name="([^"]+)[| ][Ss][0-9].*/\1/')
                                    //that would set SERIES to the name of the series using capture group. If there were no Sxx in the line, the capture would remain empty = it's a movie
                                    string TYPE = "";
                                    if (URL.ToLower().Contains("/series"))
                                    {
                                        TYPE = "series";
                                    }
                                    if (URL.ToLower().Contains("/movie"))
                                    {
                                        TYPE = "movie";
                                    }

                                    //Check if series or movie
                                    string CombinedDir = "";
                                    if (TYPE == "series")
                                    {
                                        string SeriesNAME = "";
                                        //Strip out SxxExx from NAME
                                        //Needs .Trim('.', ' ') here again because we now strip SxxExx from NAME and SeriesNAME might end with period or space
                                        SeriesNAME = Regex.Replace(NAME, "s(\\d+)e(\\d+)", "", RegexOptions.IgnoreCase).Trim('.', ' ');

                                        //Combine path for series
                                        CombinedDir = Path.Combine(SeriesDir, SeriesNAME);

                                        if (VerboseConsoleOutputEnabled == true)
                                        {
                                            Console.WriteLine("TV Show episode: " + NAME + " found");
                                        }
                                        counter_Series++;
                                    }
                                    if (TYPE == "movie")
                                    {
                                        //Combine path for movies
                                        CombinedDir = Path.Combine(MoviesDir, GROUP);
                                        //CombinedDir = Path.Combine(MoviesDir);
                                        if (VerboseConsoleOutputEnabled == true)
                                        {
                                            Console.WriteLine("Movie: " + NAME + " found");
                                        }
                                        counter_Movies++;
                                    }
                                    if (string.IsNullOrEmpty(CombinedDir))
                                    {
                                        //Combine path for tv
                                        CombinedDir = Path.Combine(TVDir, GROUP);
                                        NAME = counter_TV + " " + NAME;
                                        if (VerboseConsoleOutputEnabled == true)
                                        {
                                            Console.WriteLine("TV Channel: " + NAME + " found");
                                        }
                                        counter_TV++;
                                    }

                                    //Create dir if not exist
                                    if (!Directory.Exists(CombinedDir))
                                    {
                                        Directory.CreateDirectory(CombinedDir);
                                    }

                                    string strmAndPath = (Path.Combine(CombinedDir, NAME) + ".strm");

                                    //Create .strm file if not exist
                                    if (!File.Exists(strmAndPath))
                                    {
                                        //Check if the filename+path is too long for Windows filesystem
                                        int strmAndPathLength = strmAndPath.Length;
                                        if (strmAndPathLength >= 260)
                                        {
                                            Console.WriteLine("*** WARNING: Destination path too long\nThe file name could be too long (" + strmAndPathLength + " chars) for the destination directory.\n'" + strmAndPath + "'");
                                            Console.WriteLine("Press CTRL+C or ESC to abort. Any other key to continue...");
                                            ConsoleKeyInfo press = Console.ReadKey();
                                            if (press.Key == ConsoleKey.Escape)
                                            {
                                                return;
                                            }
                                        }
                                        else
                                            //Create .strm
                                            if (TYPE == "movie")
                                        {
                                            counter_MoviesActual++;
                                        }
                                        else if (TYPE == "series")
                                        {
                                            counter_SeriesActual++;
                                        }
                                        else
                                        {
                                            counter_TVActual++;
                                        }
                                        File.WriteAllText(strmAndPath, URL);
                                    }
                                    else
                                    {
                                        if (ErrorLogEnabled == true) ErrorLog.WriteLine(DateTime.Now + ": " + "File exists: " + NAME + ".strm");
                                    }
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                        //Write to console the findings
                        int summovies = counter_Movies - counter_MoviesActual;
                        int sumseries = counter_Series - counter_SeriesActual;
                        int sumtv = counter_TV - counter_TVActual;

                        Console.WriteLine(counter_Movies + "/" + counter_MoviesActual + " movies found/written (" + summovies + " skipped)");
                        Console.WriteLine(counter_Series + "/" + counter_SeriesActual + " tv-show episodes found/written (" + sumseries + " skipped)");
                        Console.WriteLine(counter_TV + "/" + counter_TVActual + " tv-channels found/written (" + sumtv + " skipped)");

                        if ((counter_Movies > counter_MoviesActual) || (counter_Series > counter_SeriesActual) || (counter_TV > counter_TVActual))
                            {
                            Console.WriteLine("*** NOTE: There are more items found than written. Not deleting previously created directories and or due to duplicate items in the M3U8-file can cause this. Check log for more information.");
                        }
                    }
                    if (ErrorLogEnabled == true)
                    {
                        ErrorLog.WriteLine(DateTime.Now + ": " + "*** Log end, " + FileName + ", " + Version);
                        ErrorLog.Flush();
                        ErrorLog.Close();
                    }
                }
                else
                {
                    if (filenotfound == true)
                    {
                        Console.WriteLine("File not found - " + m3u8File);
                        if (!File.Exists(ConfigFile))
                        {
                            Console.WriteLine("Configuration file not found.");
                        }
                        if (!File.Exists(uwgFile))
                        {
                            Console.WriteLine("Unwanted groups file not found.");
                        }
                        Console.WriteLine("Type /? for help.");
                    }
                    else Console.WriteLine("No file to process.\nType /? for help.");
                    return;
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Console.WriteLine("Error: " + ex.StackTrace);
                Console.ReadLine();
            }

            //Stop the stopwatch and print the result
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
            Console.WriteLine("*** Processing time: " + elapsedTime);
        }

        public static string NAMEFilterFileNameChars(string fileName)
            //Removes illegal file name characters
        {
            //Console.WriteLine("Running RemoveIllegalFileNameChars");
            //string fileNameOriginal = "";
            //string fileName1 = fileName;

            //Remove VOD: from beginning of names (keeping IgnoreCase because Albania uses Vod: in filenames)
            fileName = Regex.Replace(fileName, @"^VOD:\s", "", RegexOptions.IgnoreCase);

            //Remove special (really really cusomized)
            fileName = Regex.Replace(fileName, @"Se/dk/no", "", RegexOptions.IgnoreCase);

            //Remove and replace chars -- this is done because GetInvalidFileNameChars behaves differently depending on OS
            //Here we do it on all OS to make the output more alike no matter OS
            fileName = fileName
                .Replace("/", "-")
                .Replace("\\", "-")
                .Replace("*", "-")
                .Replace(":", "-")
                .Replace("|", "-")
                .Replace("\"", "-")
                .Replace(";", "-")
                .Replace("=", "-")
                .Replace("–", "-")
                .Replace("·", "-")
                .Replace("{", "[")
                .Replace("}", "]")
                .Replace("’", "'")
                .Replace("‘", "'")
                .Replace("´", "'")
                .Replace("`", "'")
                .Replace("…", "...")
                .Replace("“", "'")
                .Replace("?", "").Trim()
                .Replace("<", "").Trim()
                .Replace(">", "").Trim()
                .Replace(",", "").Trim()
                .Replace("°", "").Trim();

            //Normal filter, which behaves differently depending on OS
            fileName = Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), "")).Trim();

            //Add spaces between ) or ] and ( or [ (this is to conform) - regex version
            fileName = Regex.Replace(fileName, @"(\)|\])(\(|\[)", "$1 $2");

            //Test correct year with more than 4 digits (example "Valley Girl [201920]" to "Valley Girl [2020]"
            fileName = Regex.Replace(fileName, @"\[(\d{2})\d{2}(\d{2})]", "[$1$2]");

            //Test add missing end bracket (for example "The Hunt [2020" to "The Hunt [2020]")
            fileName = Regex.Replace(fileName, @"\[(?!.*\])([\w]+)", "[$1]");

            //Test align missaligned bracket (example "The Last Scout ]2017]" to "The Last Scout [2017]")
            fileName = Regex.Replace(fileName, @"\](\w+)]", "[$1]");

            //Test add missing bracket (for example "Spotlight [IMDB [2015]" to "Spotlight [IMDB] [2015]")
            fileName = Regex.Replace(fileName, @"\[(\w+) (\[)", "[$1] $2");

            //Remove all tags (only tags without numbers to keep years)
            fileName = Regex.Replace(fileName, @"\[\D+\]", "");

            //Remove [PRE] and misspellings thereof (not needed when remove all tags used)
            //fileName = Regex.Replace(fileName, @"\[(P|R)(R|F)E\]", "", RegexOptions.IgnoreCase);

            //Remove [Multi-Audio] (not needed when remove all tags used)
            //fileName = Regex.Replace(fileName, @"\[(Mu.*|Dual)(-|\s)Audio\]", "", RegexOptions.IgnoreCase);

            //Remove [Multi-Subs] (not needed when remove all tags used)
            //fileName = Regex.Replace(fileName, @"\[Mu.*(-|\s)Sub(|s)\]", "", RegexOptions.IgnoreCase);

            //Remove [Nordic] tag (keeping this because of some nordic without start bracket)
            fileName = Regex.Replace(fileName, @"(\s|\[)nordic\]", "", RegexOptions.IgnoreCase);

            //Remove [Only On 4K Devices] tag
            fileName = Regex.Replace(fileName, @"\[Only (On|For) 4K Devices\]", "", RegexOptions.IgnoreCase);

            //Remove [4K] tag
            fileName = Regex.Replace(fileName, @"\[4K\]", "", RegexOptions.IgnoreCase);

            //Remove 4K tag
            fileName = Regex.Replace(fileName, @"4K", "", RegexOptions.IgnoreCase);

            //Remove stuff (not needed when remove all tags used)
            //fileName = Regex.Replace(fileName, @"\[K(|I)DS\]", "", RegexOptions.IgnoreCase);
            //fileName = Regex.Replace(fileName, @"\[SE\]", "", RegexOptions.IgnoreCase);
            //fileName = Regex.Replace(fileName, @"\[IMDB\]", "", RegexOptions.IgnoreCase);
            //fileName = Regex.Replace(fileName, @"\[IMDB\]", "", RegexOptions.IgnoreCase);

            //Replace space between Sxx and Exx (escape \ by double \\) ($1 and $2 capture group 1 and 2)
            fileName = Regex.Replace(fileName, @"(s\d+)\s(e\d+)", "$1$2", RegexOptions.IgnoreCase);

            //Remove erroneous spaces
            fileName = fileName.Replace("( ", "(");
            fileName = fileName.Replace(" )", ")");
            fileName = fileName.Replace("[ ", "[");
            fileName = fileName.Replace(" ]", "]");

            //Replace "9 - 1 - 1", "9 - 11", "9- 11", "9 -11" with no space in between dash
            fileName = Regex.Replace(fileName, @"(\d)(\s-|-\s|\s-\s)(\d)", "$1-$3");

            //Add space before dash (-) only if space already after
            fileName = Regex.Replace(fileName, @"(\w)-\s(\w)", "$1 - $2");

            //Remove space before dash (-) only if no space after
            fileName = Regex.Replace(fileName, @"(\w)\s-(\w)", "$1-$2");

            //Replace more than one space with one space
            fileName = Regex.Replace(fileName, @"\s{2,}", " ");

            //Replace more than one dash with one dash
            fileName = Regex.Replace(fileName, @"-{2,}", "-");
            
            //Correct case of 4K tag
            //fileName = Regex.Replace(fileName, @"(4k|\[4k\])", "[4K]", RegexOptions.IgnoreCase);
            fileName = Regex.Replace(fileName, @"4k", "4K", RegexOptions.IgnoreCase);

            //Replace brackets with parentheses only on 4-digit year
            fileName = Regex.Replace(fileName, @"\[(\d{4})\]", "($1)");

            //Replace without name with _NONAME
            fileName = Regex.Replace(fileName, @"^$", "_NONAME");

            //Remove leading and trailing period and space
            fileName = fileName.Trim('.', ' ');

            return fileName;
        }

        public static string GROUPFilterFileNameChars(string fileName)
        //Removes illegal file name characters from GROUP
        {
            //Remove VOD: from beginning of names (keeping IgnoreCase because Albania uses Vod:)
            fileName = Regex.Replace(fileName, @"^VOD:\s", "", RegexOptions.IgnoreCase);

            fileName = fileName
                .Replace("/", "-")
                .Replace("\\", "-")
                .Replace("*", "-")
                .Replace(":", "-")
                .Replace("|", "-")
                .Replace("\"", "-")
                .Replace(";", "-")
                .Replace("=", "-")
                .Replace("–", "-")
                .Replace("·", "-")
                .Replace("{", "[")
                .Replace("}", "]")
                .Replace("’", "'")
                .Replace("‘", "'")
                .Replace("´", "'")
                .Replace("`", "'")
                .Replace("…", "...")
                .Replace("“", "'")
                .Replace("?", "").Trim()
                .Replace("<", "").Trim()
                .Replace(">", "").Trim()
                .Replace(",", "").Trim()
                .Replace("°", "").Trim();

            //Normal filter, which behaves differently depending on OS
            fileName = Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), "")).Trim();

            //Remove [Multi-Sub/Audio]
            fileName = Regex.Replace(fileName, @"\[Multi.*(-|\s)(Audio|Sub(|s))\]", "", RegexOptions.IgnoreCase);

            //Correct case of 4K tag
            //fileName = Regex.Replace(fileName, @"(4k|\[4k\])", "[4K]", RegexOptions.IgnoreCase);
            fileName = Regex.Replace(fileName, @"4k", "4K", RegexOptions.IgnoreCase);

            //Remove [Only On 4K Devices] tag
            fileName = Regex.Replace(fileName, @"\[Only (On|For) 4K Devices\]", "", RegexOptions.IgnoreCase);

            //Replace more than one space with one space
            fileName = Regex.Replace(fileName, @"\s{2,}", " ");

            //Replace more than one dash with one dash
            fileName = Regex.Replace(fileName, @"-{2,}", "-");

            //Remove leading and trailing period and space
            fileName = fileName.Trim('.', ' ');

            return fileName;
        }

        public static Version EnsureSupportedDotNetFrameworkVersion(Version supportedVersion)
        {
            var fileVersion = typeof(int).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
            var currentVersion = new Version(fileVersion.Version);
            if (currentVersion < supportedVersion)
                throw new NotSupportedException($"Microsoft .NET Framework {supportedVersion} or newer is required. Current version ({currentVersion}) is not supported.");
            return currentVersion;
        }

        //public static string RemoveIllegalFileNameChars(string input, string replacement = "")
        //    //Old code, not used anymore
        //{
        //    var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        //    var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
        //    return r.Replace(input, replacement);
        //}
    }
}
