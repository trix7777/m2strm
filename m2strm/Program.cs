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
            //check .Net Framework version
            var NetFrameworkVersion = new Version(4, 6, 57, 0);
            EnsureSupportedDotNetFrameworkVersion(NetFrameworkVersion);

            //start stopwatch to time the program run
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                //set various stuff
                string creator = "Original code by TimTester ©2020\nForked with permissions and converted to C# (Mono compatible) by trix77 ©2020";
                string location = Convert.ToString(Environment.GetCommandLineArgs()[0]);
                string programFileName = Path.GetFileName(location);
                string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                string configFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
                var userConfigFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var userSettings = userConfigFile.AppSettings.Settings;
 
                //default settings, can be overridden in config
                string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string OutDirectory = BaseDirectory;
                string m3u8File = "";  //can also be overridden in args
                string MoviesSubDir = "VOD Movies";
                string SeriesSubDir = "VOD Series";
                string TVSubDir = "TV Channels";
                string UserURL = "";
                string UserPort = "";
                string UserName = "";
                string UserPass = "";
                bool DeletePreviousDirEnabled = false;
                bool UnwantedCFGEnabled = true;
                bool VerboseConsoleOutputEnabled = false;
                bool ProgramLogEnabled = true;
                bool PurgeFilesEnabled = true;
                bool MovieGroupSubdirEnabled = false;
                bool DownloadM3U8Enabled = false;

                //get settings from external config
                if (ConfigurationManager.AppSettings.Get("BaseDirectory") != null && ConfigurationManager.AppSettings.Get("BaseDirectory") != "")
                    BaseDirectory = ConfigurationManager.AppSettings.Get("BaseDirectory");

                if (ConfigurationManager.AppSettings.Get("OutDirectory") != null && ConfigurationManager.AppSettings.Get("OutDirectory") != "")
                    OutDirectory = ConfigurationManager.AppSettings.Get("OutDirectory");

                if (ConfigurationManager.AppSettings.Get("MoviesSubDir") != null && ConfigurationManager.AppSettings.Get("MoviesSubDir") != "")
                    MoviesSubDir = ConfigurationManager.AppSettings.Get("MoviesSubDir");

                if (ConfigurationManager.AppSettings.Get("SeriesSubDir") != null && ConfigurationManager.AppSettings.Get("SeriesSubDir") != "")
                    SeriesSubDir = ConfigurationManager.AppSettings.Get("SeriesSubDir");

                if (ConfigurationManager.AppSettings.Get("TVSubDir") != null && ConfigurationManager.AppSettings.Get("TVSubDir") != "")
                    TVSubDir = ConfigurationManager.AppSettings.Get("TVSubDir");

                if (ConfigurationManager.AppSettings.Get("DeletePreviousDirEnabled") != null && ConfigurationManager.AppSettings.Get("DeletePreviousDirEnabled") != "")
                    DeletePreviousDirEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("DeletePreviousDirEnabled"));

                if (ConfigurationManager.AppSettings.Get("UnwantedCFGEnabled") != null && ConfigurationManager.AppSettings.Get("UnwantedCFGEnabled") != "")
                    UnwantedCFGEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("UnwantedCFGEnabled"));

                if (ConfigurationManager.AppSettings.Get("VerboseConsoleOutputEnabled") != null && ConfigurationManager.AppSettings.Get("VerboseConsoleOutputEnabled") != "")
                    VerboseConsoleOutputEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("VerboseConsoleOutputEnabled"));

                if (ConfigurationManager.AppSettings.Get("ProgramLogEnabled") != null && ConfigurationManager.AppSettings.Get("ProgramLogEnabled") != "")
                    ProgramLogEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("ProgramLogEnabled"));

                if (ConfigurationManager.AppSettings.Get("PurgeFilesEnabled") != null && ConfigurationManager.AppSettings.Get("PurgeFilesEnabled") != "")
                    PurgeFilesEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("PurgeFilesEnabled"));

                if (ConfigurationManager.AppSettings.Get("MovieGroupSubdirEnabled") != null && ConfigurationManager.AppSettings.Get("MovieGroupSubdirEnabled") != "")
                    MovieGroupSubdirEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("MovieGroupSubdirEnabled"));

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

                //set up the streamwriter for the programLog
                StreamWriter programLog = null;

                //set up the webclient for downloading the source file
                WebClient webClient = new WebClient();

                //user agent for webclient
                webClient.Headers.Add("user-agent", "m2strm/" + version);

                //combine output dirs
                string MoviesDir = Path.Combine(OutDirectory, MoviesSubDir);
                string SeriesDir = Path.Combine(OutDirectory, SeriesSubDir);
                string TVDir = Path.Combine(OutDirectory, TVSubDir);

                //date formats for file names
                string longDate = (DateTime.Now.ToString("yyyyMMddhhmm"));
                string shortDate = (DateTime.Now.ToString("yyyyMMdd"));

                //log files locations and names
                string logSubDir = "log" + Path.DirectorySeparatorChar;
                string logDir = Path.Combine(BaseDirectory, logSubDir);
                string allGroupsFile = (logDir + "allgroups.log"); //groups found just like /U, during each parse
                string uwgLogFile = (logDir + "uwgroups.log");     //groups NOT in or commented out in uwgCfgFile
                string newLogFile = (logDir + "new_" + longDate + ".log");
                string programLogFile = (logDir + "m2strm_" + shortDate + ".log");
                string dupeLogFile = (logDir + "dupes.log");
                string purgeFile = (logDir + "purged_" + longDate + ".log");

                //other file locations and names
                string uwgCfgFile = (BaseDirectory + "uwgroups.cfg");
                string userM3u8File = (BaseDirectory + "original.m3u8");  //this is the filename of the user downloaded m3u8-file

                //misc strings
                string newLine = ("\n");
                string getNameAndGroupREGEX = @"^#EXTINF:.* \btvg-name=""([^""]+|)"".* \bgroup-title=""([^""]+|)"",.*$";
                string programLogStartLine = ("\n" + DateTime.Now + ": " + "*** Log begin, " + programFileName + ", " + version);
                string UserURLCombined = (UserURL + ":" + UserPort + "/get.php?username=" + UserName + "&password=" + UserPass + "&type=m3u_plus&output=ts");

                //init strings
                string GROUP = "";
                string NAME = "";
                string URL = "";                   //url of the strm
                string lowerNAME = "";             //NAME converted to lowercase
                string seriesNAME = "";            //name of the series without sxxexx
                string contentType = "";           //tv/movie/series
                string sourceText = "";            //m3u8 source text
                string sourceTextLine1 = "";
                string sourceTextLine2 = "";
                string appendText = "";            //used when appending text to files or arrays
                string outText = "";               //used by console and log output
                string noRepeat = "";              //less output in some places by not letting name output twice in a row
                string strmContentOld = "";        //strm-content currently on disk
                string combinedTypeDir = "";       //combines series/movies/tv-dirs with seriesName or GROUP
                string combinedTypeNameStrm = "";  //combines combinedTypeDir + NAME + .strm

                //init string arrays
                string[] allGroupsArray = {};      //groups found
                string[] dupeArray = {};           //dupes found
                string[] filesOnDiskArray = {};    //files found on disk
                string[] foundArray = {};          //titles found -- later only purged items left in it
                string[] newArray = {};            //new titles
                string[] outArray = {};            //strm-files written to disk
                string[] purgeArray = {};          //files that will be purged from disk
                string[] uwgCfgArray = {};         //unwanted groups
                string[] sourceTextLines = {};     //lines from the source file

                //init bool
                bool fileNotFound = false;         //file is not found
                bool ngExist = false;              //there is a title not in a group
                bool isDupe = false;               //is a dupe
                bool isUnwanted = false;           //if unwanted

                //init int
                int combinedTypeNameStrmLength = 0;  //used for checking strm and path length on filesystem

                //init counters
                int cMovies = 0;
                int cMoviesUpdate = 0;
                int cSkippedMovies;
                int cMoviesDupe = 0;
                int cMoviesPurge = 0;
                int cMoviesActual = 0;
                int cSeries = 0;
                int cSeriesUpdate = 0;
                int cSkippedSeries;
                int cSeriesDupe = 0;
                int cSeriesPurge = 0;
                int cSeriesActual = 0;
                int cTV = 0;
                int cTVUpdate = 0;
                int cSkippedTV;
                int cTVDupe = 0;
                int cTVPurge = 0;
                int cTVActual = 0;

                //set counters output strings
                string newMovies = "0 new";
                string updateMovies = "";
                string skipMovies = "";
                string dupeMovies = "";
                string purgeMovies = "";
                string totalMovies = "";
                string newSeries = "0 new";
                string updateSeries = "";
                string skipSeries = "";
                string dupeSeries = "";
                string purgeSeries = "";
                string totalSeries = "";
                string newTV = "0 new";
                string updateTV = "";
                string skipTV = "";
                string dupeTV = "";
                string purgeTV = "";
                string totalTV = "";

                //check if args is given
                if (args.Length > 0)
                {
                    if (args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "/h" || args[0].ToLower() == "-h" || args[0].ToLower() == "/help" || args[0].ToLower() == "-help" || args[0].ToLower() == "--help")
                    {
                        //help section
                        Console.WriteLine("Creates STRM-files from M3U8-file.");
                        Console.WriteLine("\n" + programFileName + " [OPTIONS] [drive:][path][filename]");
                        Console.WriteLine("\n  filename        Specifies the M3U8-file to be processed. If not specified tries to get from configuration file.");
                        Console.WriteLine("\n  /C              Create default configuration file. Warning: resets existing settings.");
                        Console.WriteLine("  /U [filename]   Create 'unwanted groups' file with a list of all groups.");
                        Console.WriteLine("  /D              Delete previously created directories only (no parsing).");
                        Console.WriteLine("  /M              Download M3U8-file only (no parsing).");
                        Console.WriteLine("  /G              Show a guide to help you get started quickly.");
                        Console.WriteLine("  /V              Version information.");
                        Console.WriteLine("  /? or /H        This help.");
                        if (File.Exists(configFile))
                            Console.WriteLine("\nConfiguration file: " + configFile);
                        if (!File.Exists(configFile))
                        {
                            Console.WriteLine("\nConfiguration file not found.");
                            Console.WriteLine("Tip: Create configuration with /C and then edit it for your needs.");
                        }
                        if (File.Exists(uwgCfgFile))
                            Console.WriteLine("\nUnwanted groups file: " + uwgCfgFile);
                        if (!File.Exists(uwgCfgFile))
                        {
                            Console.WriteLine("\nUnwanted groups file not found.");
                            Console.WriteLine("Tip: Create the unwanted groups file with /U and then edit it for your needs.");
                        }
                        Console.WriteLine("\nExample usage:");
                        Console.WriteLine(programFileName + " my.m3u8");
                        return;
                    }

                    else if (args[0].ToLower() == "/m")
                    {
                        Console.WriteLine("*** Downloading M3U8-file to: " + userM3u8File);
                        webClient.DownloadFile(new Uri(UserURLCombined), userM3u8File);
                        return;
                    }

                    else if (args[0].ToLower() == "/v")
                    {
                        Console.WriteLine(creator);
                        Console.WriteLine(programFileName + " version " + version);
                        return;
                    }

                    else if (args[0].ToLower() == "/c")
                    {
                        //backup old configFile if exist and then exit returning error
                        if (File.Exists(configFile))
                        {
                            File.Move(configFile, configFile + ".old");
                            Console.WriteLine("*** WARNING: Configuration file existed and has been moved to: " + configFile + ".old" + "\nTo fully reset the configuration you need to run /C again.");
                            return;
                        }

                        //remove user set config values
                        userSettings.Add("BaseDirectory", "");
                        userSettings.Add("OutDirectory", "");
                        userSettings.Add("m3u8File", "");
                        userSettings.Add("MoviesSubDir", "");
                        userSettings.Add("SeriesSubDir", "");
                        userSettings.Add("TVSubDir", "");
                        userSettings.Add("DeletePreviousDirEnabled", "");
                        userSettings.Add("UnwantedCFGEnabled", "");
                        userSettings.Add("VerboseConsoleOutputEnabled", "");
                        userSettings.Add("ProgramLogEnabled", "");
                        userSettings.Add("PurgeFilesEnabled", "");
                        userSettings.Add("MovieGroupSubdirEnabled", "");
                        userSettings.Add("DownloadM3U8Enabled", "");
                        userSettings.Add("UserURL", "");
                        userSettings.Add("UserPort", "");
                        userSettings.Add("UserName", "");
                        userSettings.Add("UserPass", "");

                        //set default user config values
                        userSettings["BaseDirectory"].Value = BaseDirectory;
                        userSettings["OutDirectory"].Value = OutDirectory;
                        userSettings["m3u8File"].Value = m3u8File;
                        userSettings["MoviesSubDir"].Value = MoviesSubDir;
                        userSettings["SeriesSubDir"].Value = SeriesSubDir;
                        userSettings["TVSubDir"].Value = TVSubDir;
                        userSettings["DeletePreviousDirEnabled"].Value = Convert.ToString(DeletePreviousDirEnabled);
                        userSettings["UnwantedCFGEnabled"].Value = Convert.ToString(UnwantedCFGEnabled);
                        userSettings["VerboseConsoleOutputEnabled"].Value = Convert.ToString(VerboseConsoleOutputEnabled);
                        userSettings["ProgramLogEnabled"].Value = Convert.ToString(ProgramLogEnabled);
                        userSettings["PurgeFilesEnabled"].Value = Convert.ToString(PurgeFilesEnabled);
                        userSettings["MovieGroupSubdirEnabled"].Value = Convert.ToString(MovieGroupSubdirEnabled);
                        userSettings["DownloadM3U8Enabled"].Value = Convert.ToString(DownloadM3U8Enabled);
                        userSettings["UserURL"].Value = UserURL;
                        userSettings["UserPort"].Value = UserPort;
                        userSettings["UserName"].Value = UserName;
                        userSettings["UserPass"].Value = UserPass;
                        userConfigFile.Save(ConfigurationSaveMode.Modified);
                        Console.WriteLine("*** Configuration file created: " + configFile);
                        return;
                    }

                    else if (args[0].ToLower() == "/g")
                    {
                        Console.WriteLine("\nQuick guide:\n");
                        Console.WriteLine("1. Create the configuration file: " + programFileName + " /C");
                        Console.WriteLine("2. Edit the configuration file and set at least the following:");
                        Console.WriteLine("-- m3u8File value: /where/you/want/to/download/original.m3u8");
                        Console.WriteLine("-- DownloadM3U8Enabled value: True");
                        Console.WriteLine("-- Also fill in your UserURL, UserPort, UserName and UserPass.");
                        Console.WriteLine("3. Download your m3u8-file using the configuration file settings: " + programFileName + " /M");
                        Console.WriteLine("4. Create the unwanted groups file: " + programFileName + " /U");
                        Console.WriteLine("5. Edit the unwanted groups file and comment out the groups you want to keep with //.");
                        Console.WriteLine("6. You're all done. You can from now on just run " + programFileName + " without any arguments.\n");
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
                        //download m3u8 section
                        if (args.Length > 1)
                        {
                            //arg 1 converted to m8u8File
                            m3u8File = args[1];
                            if (File.Exists(m3u8File))
                            {
                                if (ConfigurationManager.AppSettings.Get("m3u8File") != null && ConfigurationManager.AppSettings.Get("m3u8File") != "")
                                    Console.WriteLine("*** Using arg specified m3u8-file: " + m3u8File + " (overriding config).");
                                else Console.WriteLine("*** Using arg specified m3u8-file: " + m3u8File + " (not set in config).");
                            }
                            else
                            {
                                fileNotFound = true;
                                Console.WriteLine("File not found - " + m3u8File);
                                return;
                            }
                        }
                        else
                        {
                            if (ConfigurationManager.AppSettings.Get("m3u8File") != null && ConfigurationManager.AppSettings.Get("m3u8File") != "")
                                m3u8File = ConfigurationManager.AppSettings.Get("m3u8File");
                            if (File.Exists(m3u8File))
                                Console.WriteLine("*** Using config set m3u8-file: " + m3u8File);
                            else
                            {
                                fileNotFound = true;
                                Console.WriteLine("File not found - " + m3u8File);
                                return;
                            }
                        }

                        //backup old uwgCfgFile
                        if (File.Exists(uwgCfgFile))
                        {
                            File.Copy(uwgCfgFile, uwgCfgFile + ".old", true);
                            Console.WriteLine("*** WARNING: Unwanted groups file already existed, it has now been moved to: " + uwgCfgFile + ".old" );
                        }

                        //set the content of the m3u8File to sourceText
                        sourceText = File.ReadAllText(m3u8File);

                        //normalize linefeed
                        sourceText = sourceText.Replace("\r\n", "\n");
                        sourceText = sourceText.Replace("\r", "\n");

                        //ngExist, used for setting the _NOGROUP
                        ngExist = false;

                        //empty old contents of uwgCfgFile
                        File.WriteAllText(uwgCfgFile, string.Empty);

                        //create a uwgCfgFile with a list of all groups
                        sourceTextLines = sourceText.Split(newLine.ToCharArray());
                        for (var index = 1; index <= sourceTextLines.Length - 1; index++)
                        {
                            sourceTextLine1 = sourceTextLines[(int)index];
                            //find a line that starts with #EXTINF:
                            if (sourceTextLine1.ToLower().StartsWith("#extinf:"))
                            {
                                //reset GROUP
                                GROUP = "";

                                //get NAME and GROUP using regex pattern and capture groups (only using GROUP here)
                                Match match = Regex.Match(sourceTextLine1, getNameAndGroupREGEX, RegexOptions.IgnoreCase);
                                GROUP = match.Groups[2].Value;

                                //set the contents of uwgCfgFile
                                uwgCfgArray = File.ReadAllLines(uwgCfgFile);

                                //reset appendText
                                appendText = "";

                                //check if noname group
                                if (GROUP == "")
                                {
                                    ngExist = true;
                                    GROUP = "_NOGROUP";
                                }

                                //less output by not letting same output twice
                                if (uwgCfgArray.Contains(GROUP) == false)
                                {
                                    Console.WriteLine("Found group: " + GROUP);
                                    //Combine GROUP + newLine
                                    appendText = GROUP + newLine;
                                    //and output them to file
                                    File.AppendAllText(uwgCfgFile, appendText);
                                }
                            }
                        }

                        //remove blanks, doubles and sorts the groups found
                        uwgCfgArray = File.ReadAllLines(uwgCfgFile);
                        //remove blank lines
                        uwgCfgArray = uwgCfgArray.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                        //sort the lines alphabetically ascending
                        Array.Sort(uwgCfgArray);
                        //remove duplicates
                        File.WriteAllLines(uwgCfgFile, uwgCfgArray.Distinct().ToArray());
                        Console.WriteLine("\n*** Unwanted groups file created: " + uwgCfgFile);
                        Console.WriteLine("INFO: Edit this file and remove/comment out groups you want to process.\n*** Everything not removed/commented out will be ignored while processing.\n*** (comment out a line by putting // before the group name.)\n*** To make use of it, UnwantedCFGEnabled must be set to True (default).");
                        if (ngExist == true)
                            Console.WriteLine("*** Note: We've found titles not in groups, they will be processed as _NOGROUP.");
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
                        //if we've come here, the user has given an arg not starting with /
                        //which we now believe is a local m3u8-file
                        m3u8File = args[0];
                        if (File.Exists(m3u8File))
                        {
                            //start logging (1/3)
                            if (ProgramLogEnabled == true)
                            {
                                //Create directory if not exist
                                if (!Directory.Exists(logDir))
                                    Directory.CreateDirectory(logDir);
                                programLog = File.AppendText(programLogFile);
                                programLog.WriteLine(programLogStartLine);
                            }
                            else
                            {
                                outText = (DateTime.Now + ": " + "INFO: Program logging is disabled in config.");
                                Console.WriteLine(outText);
                            }

                            if (ConfigurationManager.AppSettings.Get("m3u8File") != null && ConfigurationManager.AppSettings.Get("m3u8File") != "")
                            {
                                outText = (DateTime.Now + ": " + "*** Using arg specified m3u8-file: " + m3u8File + " (overriding config).");
                                Console.WriteLine(outText);
                                if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                            }
                            else
                            {
                                outText = (DateTime.Now + ": " + "Using arg specified m3u8-file: " + m3u8File + " (not set in config).");
                                Console.WriteLine(outText);
                                if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                            }
                        }
                        else
                            //we did not find the file given in arg
                            fileNotFound = true;
                    }
                }
                else
                {
                    //we end up here if arg is not given, trying to set the m3u8File with config set location
                    if (ConfigurationManager.AppSettings.Get("m3u8File") != null && ConfigurationManager.AppSettings.Get("m3u8File") != "")
                        m3u8File = ConfigurationManager.AppSettings.Get("m3u8File");

                    if (File.Exists(m3u8File) && (DownloadM3U8Enabled != true))
                    {
                        //start logging (2/3)
                        if (ProgramLogEnabled == true)
                        {
                            //create directory if not exist
                            if (!Directory.Exists(logDir))
                                Directory.CreateDirectory(logDir);
                            programLog = File.AppendText(programLogFile);
                            programLog.WriteLine(programLogStartLine);
                        }
                        else
                        {
                            outText = (DateTime.Now + ": " + "INFO: Program logging is disabled in config.");
                            Console.WriteLine(outText);
                        }
                        outText = (DateTime.Now + ": " + "*** Using config set m3u8-file: " + m3u8File);
                        Console.WriteLine(outText);
                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                    }
                    else
                    {
                        //if we did not find the config set m3u8-file
                        if (!File.Exists(m3u8File))
                            fileNotFound = true;
                    }
                }

                //output to log and console
                //Console.WriteLine(DateTime.Now + ": " + "Stuff here");
                //if (ProgramLogEnabled == true) programLog.WriteLine(DateTime.Now + ": " + "Stuff here");

                //pause
                //Console.WriteLine("I am here:");
                //Console.ReadKey();

                //check if user wants to download m3u8 before parsing
                if (DownloadM3U8Enabled == true)
                {
                    //start logging (3/3)
                    if (ProgramLogEnabled == true)
                    {
                        //create directory if not exist
                        if (!Directory.Exists(logDir))
                            Directory.CreateDirectory(logDir);
                        programLog = File.AppendText(programLogFile);
                        programLog.WriteLine(programLogStartLine);
                    }
                    else
                    {
                        outText = (DateTime.Now + ": " + "INFO: Program logging is disabled in config.");
                        Console.WriteLine(outText);
                    }
                    outText = (DateTime.Now + ": " + "*** Downloading M3U8-file to: " + userM3u8File);
                    Console.WriteLine(outText);
                    if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                    
                    //download the m3u8-file
                    webClient.DownloadFile(new Uri(UserURLCombined), userM3u8File);
                    //Console.WriteLine(UserURLCombined);

                    if (m3u8File != null || m3u8File != "")
                    {
                        outText = (DateTime.Now + ": " + "*** Using downloaded m3u8-file: " + userM3u8File + " (overriding both config and args).");
                        Console.WriteLine(outText);
                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                        outText = (DateTime.Now + ": " + "INFO: To disable this override, DownloadM3U8Enabled must be set to False (default).");
                        Console.WriteLine(outText);
                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                    }
                    else
                    {
                        outText = (DateTime.Now + ": " + "*** Using downloaded m3u8-file: " + userM3u8File);
                        Console.WriteLine(outText);
                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                    }
                    //set m3u8File to userM3u8File
                    m3u8File = userM3u8File;
                }

                //if m3u8File exist continue, or set fileNotFound true
                if (File.Exists(m3u8File))
                {
                    outText = (DateTime.Now + ": " + "*** BaseDirectory: " + BaseDirectory);
                    Console.WriteLine(outText);
                    if (ProgramLogEnabled == true) programLog.WriteLine(outText);

                    outText = (DateTime.Now + ": " + "*** OutDirectory: " + OutDirectory);
                    Console.WriteLine(outText);
                    if (ProgramLogEnabled == true) programLog.WriteLine(outText);

                    outText = (DateTime.Now + ": " + "*** Type subfolders: " + MoviesSubDir + ", " + SeriesSubDir + ", " + TVSubDir);
                    Console.WriteLine(outText);
                    if (ProgramLogEnabled == true) programLog.WriteLine(outText);

                    //check if config exists
                    if (File.Exists(configFile))
                    {
                        outText = (DateTime.Now + ": " + "*** Config file found and in use: " + configFile);
                        Console.WriteLine(outText);
                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                    }

                    //set uwgCfgFile if wanted and exists
                    if (File.Exists(uwgCfgFile) && UnwantedCFGEnabled == true)
                    {
                        outText = (DateTime.Now + ": " + "*** Unwanted groups file found and in use: " + uwgCfgFile);
                        Console.WriteLine(outText);
                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                        uwgCfgArray = File.ReadAllLines(uwgCfgFile);
                    }
                    else if (File.Exists(uwgCfgFile) && UnwantedCFGEnabled == false)
                    {
                        outText = (DateTime.Now + ": " + "*** Unwanted groups file found but not in use: " + uwgCfgFile);
                        Console.WriteLine(outText);
                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                        uwgCfgArray = File.ReadAllLines(uwgCfgFile);
                    }

                    //empty old contents of uwgLogFile
                    if (ProgramLogEnabled == true) File.WriteAllText(uwgLogFile, string.Empty);

                    //empty old contents of allGroupsFile
                    if (ProgramLogEnabled == true) File.WriteAllText(allGroupsFile, string.Empty);

                    //empty old contents of dupeLogFile
                    if (ProgramLogEnabled == true) File.WriteAllText(dupeLogFile, string.Empty);

                    //in the odd instance newLogFile would exist with the same name from previous run, add .old to the old one
                    if (File.Exists(newLogFile))
                        File.Move(newLogFile, newLogFile + ".old");
                    
                    //delete previously created directories
                    if (DeletePreviousDirEnabled == true)
                    {
                        outText = (DateTime.Now + ": " + "*** Deleting all previously created directories (set in config).");
                        Console.WriteLine(outText);
                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);

                        //Delete directories
                        if (Directory.Exists(MoviesDir))
                            Directory.Delete(MoviesDir, true);
                        if (Directory.Exists(SeriesDir))
                            Directory.Delete(SeriesDir, true);
                        if (Directory.Exists(TVDir))
                            Directory.Delete(TVDir, true);
                    }
                    else
                    {
                        //this is the default
                        outText = (DateTime.Now + ": " + "*** Not deleting all previously created directories.");
                        //Console.WriteLine(outText);
                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                    }

                    //################################### parse start ###################################
                    //now we finally start to parse the m3u8-file
                    outText = (DateTime.Now + ": " + "*** Processing " + m3u8File);
                    Console.WriteLine(outText);
                    if (ProgramLogEnabled == true) programLog.WriteLine(outText);

                    //set sourceText to the content of m3u8File
                    sourceText = File.ReadAllText(m3u8File);

                    //normalize linefeed
                    sourceText = sourceText.Replace("\r\n", "\n");
                    sourceText = sourceText.Replace("\r", "\n");

                    //split each line into array
                    sourceTextLines = sourceText.Split(newLine.ToCharArray());
                    for (var index = 1; index <= sourceTextLines.Length - 1; index++)
                    {
                        sourceTextLine1 = sourceTextLines[(int)index];
                        //find a line that starts with #EXTINF:
                        if (sourceTextLine1.ToLower().StartsWith("#extinf:"))
                        {
                            //sourceTextLine2 will be the URL
                            sourceTextLine2 = sourceTextLines[index + 1];

                            //reset
                            NAME = "";
                            GROUP = "";
                            URL = "";
                            isUnwanted = false;
                            appendText = "";

                            //get NAME and GROUP using regex pattern and capture groups
                            Match match = Regex.Match(sourceTextLine1, getNameAndGroupREGEX, RegexOptions.IgnoreCase);
                            NAME = match.Groups[1].Value;
                            GROUP = match.Groups[2].Value;

                            //all items found that are not in a group will get the new NOGROUP value
                            if (GROUP == "")
                            {
                                GROUP = "_NOGROUP";
                            }

                            if (ProgramLogEnabled == true)
                            {
                                //set the contents of allGroupsFile
                                allGroupsArray = File.ReadAllLines(allGroupsFile);

                                //less output by not letting same output twice
                                if (allGroupsArray.Contains(GROUP) == false)
                                {
                                    //append GROUP + newLine
                                    appendText = GROUP + newLine;
                                    File.AppendAllText(allGroupsFile, appendText);
                                }
                            }

                            foreach (string uwgCfgLine in uwgCfgArray)
                            {
                                if (GROUP == uwgCfgLine)
                                    {
                                    //less output lines if noRepeat is still same as GROUP
                                    if (noRepeat != GROUP && ProgramLogEnabled == true)
                                    {
                                        programLog.WriteLine(DateTime.Now + ": " + "Unwanted group: '" + GROUP + "' match in uwgCfgLine: '" + uwgCfgLine + "'");
                                        //append newLine
                                        appendText = GROUP + newLine;
                                        //add to uwgLogFile
                                        File.AppendAllText(uwgLogFile, appendText);
                                    }
                                    //set isUnwanted to true
                                    isUnwanted = true;
                                    //used to get less repeats in console/log output
                                    noRepeat = GROUP;
                                }
                                continue;
                            }

                            //will be set to isUnwanted if group was found in uwgroups
                            if (isUnwanted == false)
                            {
                                //replace linefeed and carriage return with nothing
                                URL = (sourceTextLine2.Replace("\r\n", "").Replace("\n", ""));
                                URL = (sourceTextLine2.Replace("\r", "").Replace("\n", ""));

                                //run NAME and GROUP through special char filters
                                NAME = NAMEFilterFileNameChars(NAME);
                                GROUP = GROUPFilterFileNameChars(GROUP);

                                //set contentType (movie or series) from URL
                                if (URL.ToLower().Contains("/series"))
                                    contentType = "series";
                                else if (URL.ToLower().Contains("/movie"))
                                    contentType = "movie";
                                else
                                    contentType = "tv";

                                //set stuff depending on contentType set
                                if (contentType == "series")
                                {
                                    //strip out SxxExx from NAME
                                    //needs .Trim('.', ' ') here again because we now strip SxxExx from NAME and seriesNAME might end with period or space
                                    seriesNAME = Regex.Replace(NAME, "s(\\d+)e(\\d+)", "", RegexOptions.IgnoreCase).Trim('.', ' ');

                                    //combine path for series
                                    combinedTypeDir = Path.Combine(SeriesDir, seriesNAME);

                                    if (VerboseConsoleOutputEnabled == true)
                                    {
                                        outText = (DateTime.Now + ": " + "TV Show episode: " + NAME + " found");
                                        Console.WriteLine(outText);
                                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                                    }
                                    cSeries++;
                                }
                                if (contentType == "movie")
                                {
                                    //combine path for movies
                                    if (MovieGroupSubdirEnabled == true)
                                        combinedTypeDir = Path.Combine(MoviesDir, GROUP);  //with GROUP subdir
                                    else
                                        combinedTypeDir = MoviesDir;  //without GROUP subdir

                                    if (VerboseConsoleOutputEnabled == true)
                                    {
                                        outText = (DateTime.Now + ": " + "Movie: " + NAME + " found");
                                        Console.WriteLine(outText);
                                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                                    }
                                    cMovies++;
                                }
                                if (contentType == "tv")
                                {
                                    //combine path for tv
                                    combinedTypeDir = Path.Combine(TVDir, GROUP);

                                    //prepend a tv channel number to NAME
                                    NAME = cTV + " " + NAME;

                                    if (VerboseConsoleOutputEnabled == true)
                                    {
                                        outText = (DateTime.Now + ": " + "TV Channel: " + NAME + " found");
                                        Console.WriteLine(outText);
                                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                                    }
                                    cTV++;
                                }

                                //create directory if not exist
                                if (!Directory.Exists(combinedTypeDir))
                                    Directory.CreateDirectory(combinedTypeDir);

                                //append .strm to the file name
                                combinedTypeNameStrm = (Path.Combine(combinedTypeDir, NAME) + ".strm");

                                //convert NAME to lowercase to get the same result on all OS
                                lowerNAME = NAME.ToLower();

                                //compare with outArray, if already exist in array set isDupe = true
                                if (outArray.Contains(lowerNAME))
                                {
                                    //counter dupe up
                                    if (contentType == "movie")
                                        cMoviesDupe++;
                                    if (contentType == "series")
                                        cSeriesDupe++;
                                    if (contentType == "tv")
                                        cTVDupe++;

                                    //set isDupe to true
                                    isDupe = true;
                                    if (ProgramLogEnabled == true)
                                    {
                                        appendText = NAME + newLine;
                                        outText = (DateTime.Now + ": " + "Dupe found: " + NAME);
                                        programLog.WriteLine(outText);
                                        //add to dupeLogFile
                                        File.AppendAllText(dupeLogFile, appendText);
                                    }
                                }
                                else
                                    isDupe = false;

                                //add the item to outArray
                                Array.Resize(ref outArray, outArray.Length + 1);
                                outArray[outArray.Length - 1] = lowerNAME;

                                //add the title to foundArray only if not dupe
                                if (isDupe == false)
                                {
                                    Array.Resize(ref foundArray, foundArray.Length + 1);
                                    foundArray[foundArray.Length - 1] = (combinedTypeNameStrm);
                                }

                                //create .strm file if not exist on disk and not already in the outArray (dupe)
                                if (!File.Exists(combinedTypeNameStrm) && isDupe == false)
                                {
                                    //check if the filename+path is too long for Windows filesystem
                                    combinedTypeNameStrmLength = combinedTypeNameStrm.Length;
                                    if (combinedTypeNameStrmLength >= 260)
                                    {
                                        outText = ("WARNING: Destination path too long\nThe file name could be too long (" + combinedTypeNameStrmLength + " chars) for the destination directory.\n'" + combinedTypeNameStrm + "'");
                                        Console.WriteLine(outText);
                                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                                    }

                                    //counter actual up
                                    if (contentType == "movie")
                                        cMoviesActual++;
                                    if (contentType == "series")
                                        cSeriesActual++;
                                    if (contentType == "tv")
                                        cTVActual++;

                                    //and finally, here we create the strm-file
                                    File.WriteAllText(combinedTypeNameStrm, URL);

                                    //write the same path and filename to new.log
                                    appendText = combinedTypeNameStrm + newLine;
                                    File.AppendAllText(newLogFile, appendText);
                                }

                                //if file exist and is not a dupe; check if new content, if so; update file with new content
                                else if (File.Exists(combinedTypeNameStrm) && isDupe == false)
                                {
                                    //compare old with new content
                                    strmContentOld = File.ReadAllText(combinedTypeNameStrm);

                                    //if old content not same as new then
                                    if (strmContentOld != URL)
                                    {
                                        //counter update up
                                        if (contentType == "movie")
                                            cMoviesUpdate++;
                                        if (contentType == "series")
                                            cSeriesUpdate++;
                                        if (contentType == "tv")
                                            cTVUpdate++;

                                        //overwrite old outdated content in strm-file
                                        File.WriteAllText(combinedTypeNameStrm, URL);

                                        outText = (DateTime.Now + ": " + "Updated content in: '" + combinedTypeNameStrm + "'");
                                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                                        {
                                            //write the same path and filename to newLogFile
                                            appendText = combinedTypeNameStrm + newLine;
                                            File.AppendAllText(newLogFile, appendText);
                                        }
                                    }
                                    else
                                    {
                                        if (ProgramLogEnabled == true)
                                        {
                                            outText = (DateTime.Now + ": " + "File exists: " + combinedTypeNameStrm);
                                            programLog.WriteLine(outText);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //tidy up in the newLogFile if exist
                    if (File.Exists(newLogFile))
                    {
                        newArray = File.ReadAllLines(newLogFile);
                        //sort the lines alphabetically ascending
                        Array.Sort(newArray);
                        //write to disk
                        File.WriteAllLines(newLogFile, newArray.ToArray());
                    }
                        
                    if (ProgramLogEnabled == true)
                    {
                        if (File.Exists(uwgLogFile) && uwgCfgArray.Length > 0)
                        {
                            //tidy up uwgLogFile
                            uwgCfgArray = File.ReadAllLines(uwgLogFile);
                            //remove blank lines
                            uwgCfgArray = uwgCfgArray.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                            //sort the lines alphabetically ascending
                            Array.Sort(uwgCfgArray);
                            //remove duplicates
                            File.WriteAllLines(uwgLogFile, uwgCfgArray.Distinct().ToArray());
                        }

                        if (File.Exists(allGroupsFile) && allGroupsArray.Length > 0)
                        {
                            //tidy up allGroupsFile
                            allGroupsArray = File.ReadAllLines(allGroupsFile);
                            //remove blank lines
                            allGroupsArray = allGroupsArray.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                            //sort the lines alphabetically ascending
                            Array.Sort(allGroupsArray);
                            //remove duplicates
                            File.WriteAllLines(allGroupsFile, allGroupsArray.Distinct().ToArray());
                        }

                        if (File.Exists(dupeLogFile) && dupeArray.Length > 0)
                        {
                            //tidy up dupeLogFile
                            dupeArray = File.ReadAllLines(dupeLogFile);
                            //sort the lines alphabetically ascending
                            Array.Sort(dupeArray);
                            //write to disk if not empty
                            File.WriteAllLines(dupeLogFile, dupeArray.ToArray());
                        }
                    }

                    //purging old strm-files and deleting empty directories
                    if (PurgeFilesEnabled == true)
                    {
                        outText = (DateTime.Now + ": " + "*** Purging files in: " + OutDirectory);
                        Console.WriteLine(outText);
                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);

                        //get all strm-files on disk into array
                        filesOnDiskArray = Directory.GetFiles(OutDirectory, "*.strm", SearchOption.AllDirectories);

                        //remove content of foundArray from filesOnDiskArray -- left will be titles no longer in m3u8-file
                        purgeArray = filesOnDiskArray.Except(foundArray).ToArray();

                        //write purgeArray to purgeFile if not empty
                        if (purgeArray.Length > 0) File.WriteAllLines(purgeFile, purgeArray.ToArray());

                        //purgeArray will now contain items to delete, here we split each array into string
                        foreach (string purgeItem in purgeArray)
                        {
                            //if file found
                            if (File.Exists(purgeItem))
                            {
                                //delete file 
                                File.Delete(purgeItem);
                                outText = (DateTime.Now + ": " + "Purged file: " + purgeItem);
                                if (ProgramLogEnabled == true) programLog.WriteLine(outText);

                                //set contentType
                                if (purgeItem.Contains(MoviesSubDir))
                                    contentType = "movie";
                                else if (purgeItem.Contains(SeriesSubDir))
                                    contentType = "series";
                                else
                                    contentType = "tv";

                                //counter purge up
                                if (contentType == "movie")
                                    cMoviesPurge++;
                                if (contentType == "series")
                                    cSeriesPurge++;
                                if (contentType == "tv")
                                    cTVPurge++;
                            }
                        }

                        //delete empty directories which can be a left over after purging
                        foreach (var directory in Directory.GetDirectories(OutDirectory, "*", SearchOption.AllDirectories))
                        {
                            if (Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Length == 0)
                            {
                                Directory.Delete(directory, true);
                                outText = (DateTime.Now + ": " + "Deleted empty directory: " + directory);
                                if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                            }
                        }
                    }       

                    //subtract actual written from found = skipped
                    cSkippedMovies = cMovies - cMoviesActual;
                    cSkippedSeries = cSeries - cSeriesActual;
                    cSkippedTV = cTV - cTVActual;

                    //set counters output
                    if (cMoviesActual > 0) newMovies = (Convert.ToString(cMoviesActual) + " new");
                    if (cMoviesUpdate > 0) updateMovies = ", " + (Convert.ToString(cMoviesUpdate) + " updated");
                    if (cSkippedMovies > 0) skipMovies = ", " + (Convert.ToString(cSkippedMovies) + " skipped");
                    if (cMoviesDupe > 0) dupeMovies = " (whereof " + (Convert.ToString(cMoviesDupe) + " dupes)");
                    if (cMoviesPurge > 0) purgeMovies = ", " + (Convert.ToString(cMoviesPurge) + " purged");
                    if (cMovies > 0) totalMovies = ", " + (Convert.ToString(cMovies) + " in total");

                    if (cSeriesActual > 0) newSeries = (Convert.ToString(cSeriesActual) + " new");
                    if (cSeriesUpdate > 0) updateSeries = ", " + (Convert.ToString(cSeriesUpdate) + " updated");
                    if (cSkippedSeries > 0) skipSeries = ", " + (Convert.ToString(cSkippedSeries) + " skipped");
                    if (cSeriesDupe > 0) dupeSeries = " (whereof " + (Convert.ToString(cSeriesDupe) + " dupes)");
                    if (cSeriesPurge > 0) purgeSeries = ", " + (Convert.ToString(cSeriesPurge) + " purged");
                    if (cSeries > 0) totalSeries = ", " + (Convert.ToString(cSeries) + " in total");

                    if (cTVActual > 0) newTV = (Convert.ToString(cTVActual) + " new");
                    if (cTVUpdate > 0) updateTV = ", " + (Convert.ToString(cTVUpdate) + " updated");
                    if (cSkippedTV > 0) skipTV = ", " + (Convert.ToString(cSkippedTV) + " skipped");
                    if (cTVDupe > 0) dupeTV = " (whereof " + (Convert.ToString(cTVDupe) + " dupes)");
                    if (cTVPurge > 0) purgeTV = ", " + (Convert.ToString(cTVPurge) + " purged");
                    if (cTV > 0) totalTV = ", " + (Convert.ToString(cTV) + " in total");

                    //write summary to console (and log if enabled)
                    outText = (DateTime.Now + ": *** Movies summary: " + newMovies + updateMovies + skipMovies + dupeMovies + purgeMovies + totalMovies);
                    Console.WriteLine(outText);
                    if (ProgramLogEnabled == true) programLog.WriteLine(outText);

                    outText = (DateTime.Now + ": *** Episodes summary: " + newSeries + updateSeries + skipSeries + dupeSeries + purgeSeries + totalSeries);
                    Console.WriteLine(outText);
                    if (ProgramLogEnabled == true) programLog.WriteLine(outText);

                    outText = (DateTime.Now + ": *** TV-channels summary: " + newTV + updateTV + skipTV + dupeTV + purgeTV + totalTV);
                    Console.WriteLine(outText);
                    if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                }
                else
                {
                    if (fileNotFound == true)
                    {
                        //we will end up here if the file given either as arg or in config was not found
                        Console.WriteLine("File not found - " + m3u8File);
                        
                        //also check and tell if config or uwg was not found
                        if (!File.Exists(configFile))
                            Console.WriteLine("Configuration file not found.");
                        if (!File.Exists(uwgCfgFile))
                            Console.WriteLine("Unwanted groups file not found.");
                        Console.WriteLine("Type /? for help.");
                    }
                    
                    //if no arg for m3u8-file was given or not specified in conf
                    else Console.WriteLine("No file to process.\nType /? for help.");
                    return;
                }

                //stop stopwatch, calculate and then print the result to both console and log
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
                outText = (DateTime.Now + ": " + "*** Processing time: " + elapsedTime);
                Console.WriteLine(outText);
                if (ProgramLogEnabled == true) programLog.WriteLine(outText);

                if (ProgramLogEnabled == true)
                {
                    //logging ends here
                    programLog.WriteLine(DateTime.Now + ": " + "*** Log end, " + programFileName + ", " + version);
                    programLog.Flush();
                    programLog.Close();
                }
            }

            //error codes
            catch (Exception ex)
            {
                string ErrorLogFile = (AppDomain.CurrentDomain.BaseDirectory + "error.log");
                StreamWriter ErrorLog = File.AppendText(ErrorLogFile);
                string outText = "";

                outText = (DateTime.Now + ": " + "ERROR: " + ex.Message);
                Console.WriteLine(outText);
                ErrorLog.WriteLine(outText);

                outText = (DateTime.Now + ": " + "ERROR: " + ex.StackTrace);
                ErrorLog.WriteLine(outText);

                ErrorLog.Flush();
                ErrorLog.Close();

                outText = ("Press any key to quit...");
                Console.WriteLine(outText);
                Console.ReadLine();
            }
        }

        //removes illegal file name characters
        public static string NAMEFilterFileNameChars(string fileName)
        {
            //remove VOD: from beginning of names (keeping IgnoreCase because Albania uses Vod: in filenames)
            fileName = Regex.Replace(fileName, @"^VOD:\s", "", RegexOptions.IgnoreCase);

            //remove special (really really customized)
            fileName = Regex.Replace(fileName, @"Se/dk/no", "", RegexOptions.IgnoreCase);

            //remove and replace chars -- this is done because GetInvalidFileNameChars behaves differently depending on OS
            //here we do it on all OS to make the output more alike no matter OS
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

            //normal filter, which behaves differently depending on OS
            fileName = Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), "")).Trim();

            //add spaces between ) or ] and ( or [ (this is to conform)
            fileName = Regex.Replace(fileName, @"(\)|\])(\(|\[)", "$1 $2");

            //correct year with more than 4 digits (example "Valley Girl [201920]" to "Valley Girl [2020]"
            fileName = Regex.Replace(fileName, @"\[(\d{2})\d{2}(\d{2})]", "[$1$2]");

            //add missing end bracket (for example "The Hunt [2020" to "The Hunt [2020]")
            fileName = Regex.Replace(fileName, @"\[(?!.*\])([\w]+)", "[$1]");

            //align misaligned bracket (example "The Last Scout ]2017]" to "The Last Scout [2017]")
            fileName = Regex.Replace(fileName, @"\](\w+)]", "[$1]");

            //add missing bracket (for example "Spotlight [IMDB [2015]" to "Spotlight [IMDB] [2015]")
            fileName = Regex.Replace(fileName, @"\[(\w+) (\[)", "[$1] $2");

            //remove all tags (only tags without numbers to keep years)
            fileName = Regex.Replace(fileName, @"\[\D+\]", "");

            //remove [PRE] and misspellings thereof (not needed when remove all tags used)
            //fileName = Regex.Replace(fileName, @"\[(P|R)(R|F)E\]", "", RegexOptions.IgnoreCase);

            //remove [Multi-Audio] (not needed when remove all tags used)
            //fileName = Regex.Replace(fileName, @"\[(Mu.*|Dual)(-|\s)Audio\]", "", RegexOptions.IgnoreCase);

            //remove [Multi-Subs] (not needed when remove all tags used)
            //fileName = Regex.Replace(fileName, @"\[Mu.*(-|\s)Sub(|s)\]", "", RegexOptions.IgnoreCase);

            //remove [Nordic] tag (keeping this because of some nordic without start bracket)
            fileName = Regex.Replace(fileName, @"(\s|\[)nordic\]", "", RegexOptions.IgnoreCase);

            //remove [Only On 4K Devices] tag
            fileName = Regex.Replace(fileName, @"\[Only (On|For) 4K Devices\]", "", RegexOptions.IgnoreCase);

            //remove [4K] tag
            fileName = Regex.Replace(fileName, @"\[4K\]", "", RegexOptions.IgnoreCase);

            //remove 4K tag
            fileName = Regex.Replace(fileName, @"4K", "", RegexOptions.IgnoreCase);

            //remove stuff (not needed when remove all tags used)
            //fileName = Regex.Replace(fileName, @"\[K(|I)DS\]", "", RegexOptions.IgnoreCase);
            //fileName = Regex.Replace(fileName, @"\[SE\]", "", RegexOptions.IgnoreCase);
            //fileName = Regex.Replace(fileName, @"\[IMDB\]", "", RegexOptions.IgnoreCase);
            //fileName = Regex.Replace(fileName, @"\[IMDB\]", "", RegexOptions.IgnoreCase);

            //replace space between Sxx and Exx (escape \ by double \\) ($1 and $2 capture group 1 and 2)
            fileName = Regex.Replace(fileName, @"(s\d+)\s(e\d+)", "$1$2", RegexOptions.IgnoreCase);

            //remove erroneous spaces
            fileName = fileName
                .Replace("( ", "(")
                .Replace(" )", ")")
                .Replace("[ ", "[")
                .Replace(" ]", "]");

            //replace "9 - 1 - 1", "9 - 11", "9- 11", "9 -11" with no space in between dash
            fileName = Regex.Replace(fileName, @"(\d)(\s-|-\s|\s-\s)(\d)", "$1-$3");

            //add space before dash (-) only if space already after
            fileName = Regex.Replace(fileName, @"(\w)-\s(\w)", "$1 - $2");

            //remove space before dash (-) only if no space after
            fileName = Regex.Replace(fileName, @"(\w)\s-(\w)", "$1-$2");

            //replace more than one space with one space
            fileName = Regex.Replace(fileName, @"\s{2,}", " ");

            //replace more than one dash with one dash
            fileName = Regex.Replace(fileName, @"-{2,}", "-");
            
            //correct case of 4K tag
            //fileName = Regex.Replace(fileName, @"(4k|\[4k\])", "[4K]", RegexOptions.IgnoreCase);
            fileName = Regex.Replace(fileName, @"4k", "4K", RegexOptions.IgnoreCase);

            //replace brackets with parentheses only on 4-digit year
            fileName = Regex.Replace(fileName, @"\[(\d{4})\]", "($1)");

            //replace without name with _NONAME
            fileName = Regex.Replace(fileName, @"^$", "_NONAME");

            //remove leading and trailing period and space
            fileName = fileName.Trim('.', ' ');

            return fileName;
        }

        //removes illegal file name characters from GROUP
        public static string GROUPFilterFileNameChars(string fileName)
        {
            //remove VOD: from beginning of names (keeping IgnoreCase because Albania uses Vod:)
            fileName = Regex.Replace(fileName, @"^VOD:\s", "", RegexOptions.IgnoreCase);

            //remove and replace chars
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

            //normal filter, which behaves differently depending on OS
            fileName = Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), "")).Trim();

            //remove [Multi-Sub/Audio]
            fileName = Regex.Replace(fileName, @"\[Multi.*(-|\s)(Audio|Sub(|s))\]", "", RegexOptions.IgnoreCase);

            //correct case of 4K tag
            //fileName = Regex.Replace(fileName, @"(4k|\[4k\])", "[4K]", RegexOptions.IgnoreCase);
            fileName = Regex.Replace(fileName, @"4k", "4K", RegexOptions.IgnoreCase);

            //remove [Only On 4K Devices] tag
            fileName = Regex.Replace(fileName, @"\[Only (On|For) 4K Devices\]", "", RegexOptions.IgnoreCase);

            //replace more than one space with one space
            fileName = Regex.Replace(fileName, @"\s{2,}", " ");

            //replace more than one dash with one dash
            fileName = Regex.Replace(fileName, @"-{2,}", "-");

            //remove leading and trailing period and space
            fileName = fileName.Trim('.', ' ');

            return fileName;
        }

        public static Version EnsureSupportedDotNetFrameworkVersion(Version supportedVersion)
        //check .Net version
        {
            var fileVersion = typeof(int).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
            var currentVersion = new Version(fileVersion.Version);
            if (currentVersion < supportedVersion)
                throw new NotSupportedException($"Microsoft .NET Framework {supportedVersion} or newer is required. Current version ({currentVersion}) is not supported.");
            return currentVersion;
        }
    }
}
