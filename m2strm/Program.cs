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
            try
            {
                //set various
                string creator = "Original by TimTester ©2020\nForked with permissions, converted to C# (Mono compatible) and developed by trix77 ©2020";
                string location = Convert.ToString(Environment.GetCommandLineArgs()[0]);
                string programFileName = Path.GetFileName(location);
                string programName = "m2strm";
                string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                string configFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
                string configFileOld = $"{configFile}.old";
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

                //set streamwriter for programLog
                StreamWriter programLog = null;

                //set webclient for source download
                WebClient webClient = new WebClient();

                //user agent for webclient
                string m2strmVer = $"{programName}/{version}";
                webClient.Headers.Add("user-agent", m2strmVer);

                //combine output dirs
                string MoviesDir = Path.Combine(OutDirectory, MoviesSubDir);
                string SeriesDir = Path.Combine(OutDirectory, SeriesSubDir);
                string TVDir = Path.Combine(OutDirectory, TVSubDir);

                //date formats
                string longDate = (DateTime.Now.ToString("yyyyMMddhhmm"));
                string shortDate = (DateTime.Now.ToString("yyyyMMdd"));

                //log files locations and names
                string logSubDir = $"log{Path.DirectorySeparatorChar}";
                string logDir = Path.Combine(BaseDirectory, logSubDir);
                string programLogFile = $"{logDir}{programName}_{shortDate}.log";
                string allGroupsFile = $"{logDir}allgroups.log"; //groups found during each parse
                string uwgLogFile = $"{logDir}uwgroups.log";     //groups that filters out through uwgCfgFile
                string newGroupsFile = $"{logDir}newgroups.log"; //groups not found at all in uwgCfgFile
                string dupeLogFile = $"{logDir}dupes.log";
                string newLogFile = $"{logDir}new_{longDate}.log";
                string newLogFileOld = $"{newLogFile}.old";
                string purgeLogFile = $"{logDir}purged_{longDate}.log";

                //init string arrays
                string[] allGroupsArray = { };      //all groups found
                string[] uwgLogArray = { };         //logs unwanted groups
                string[] newGroupArray = { };       //groups not found at all in uwgCfgFile
                string[] dupeArray = { };           //dupes found
                string[] filesOnDiskArray = { };    //files found on disk
                string[] foundArray = { };          //titles found in source
                string[] newArray = { };            //new titles this run
                string[] outArray = { };            //strm-files written to disk
                string[] purgeArray = { };          //files to purge from disk
                string[] uwgCfgArray = { };         //unwanted groups config
                string[] sourceTextLines = { };     //lines from the source file

                //init bool
                bool fileNotFound = false;         //file is not found

                //other file locations and names
                string uwgCfgFile = $"{BaseDirectory}uwgroups.cfg";
                string uwgCfgFileOld = $"{uwgCfgFile}.old";
                string userM3u8File = $"{BaseDirectory}original.m3u8";  //this will be the filename of the user downloaded m3u8-file
                string userM3uFileTemp = $"{BaseDirectory}_temp.m3u8";  //temporary m3u8 when downloading

                //misc strings
                string newLine = ("\n");
                string getNameAndGroupREGEX = @"^#EXTINF:.* \btvg-name=""([^""]+|)"".* \bgroup-title=""([^""]+|)"",.*$";
                string programLogStartLine = $"{DateTime.Now}: *** Log begin, {programFileName}, {version}";
                string UserURLCombined = $"{UserURL}:{UserPort}/get.php?username={UserName}&password={UserPass}&type=m3u_plus&output=ts";
                string strmEXT = ".strm";  //this is the file extension added to output files

                //init strings
                string GROUP = "";
                string NAME = "";
                string URL = "";                   //url of the strm
                string lowerNAME = "";             //NAME converted to lowercase
                string seriesNAME = "";            //name of the series without sxxexx
                string NameStrm = "";              //name with strmEXT added
                string contentType = "";           //tv, movie or series
                string sourceText = "";            //source text
                string sourceTextLine1 = "";
                string sourceTextLine2 = "";
                string outText = "";               //used by console and log output
                string strmContentOld = "";        //strm-content currently on a disk file
                string combinedTypeDir = "";       //combines series/movies/tv-dirs with seriesName or GROUP
                string combinedTypeNameStrm = "";  //combines combinedTypeDir, NAME, and strmEXT

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

                //check if args is given
                if (args.Length > 0)
                {
                    if (args[0] == "/?" || args[0] == "-?" || args[0].ToLower() == "/h" || args[0].ToLower() == "-h" || args[0].ToLower() == "/help" || args[0].ToLower() == "-help" || args[0].ToLower() == "--help")
                    {
                        //help section
                        Console.WriteLine("Creates STRM-files from M3U8-file.\n");
                        Console.WriteLine($"{programFileName} [OPTIONS] [drive:][path][filename]\n");
                        Console.WriteLine("  filename        The source M3U8-file to be processed.\n");
                        Console.WriteLine("  /C              Create default configuration file. Warning: resets existing settings.");
                        Console.WriteLine("  /U [filename]   Create the unwanted groups file with a list of all groups from filename.");
                        Console.WriteLine("  /D              Delete previously created directories and then quit.");
                        Console.WriteLine("  /M              Download M3U8-file and then quit. Uses config set information.");
                        Console.WriteLine("  /G              Show a guide to help you get started quickly.");
                        Console.WriteLine("  /V              Version information.");
                        Console.WriteLine("  /? or /H        This help.\n");
                        if (File.Exists(configFile))
                            Console.WriteLine($"Configuration file found: {configFile}");
                        if (!File.Exists(configFile))
                        {
                            Console.WriteLine("Configuration file not found.");
                        }
                        if (File.Exists(uwgCfgFile))
                            Console.WriteLine($"Unwanted groups file found: {uwgCfgFile}");
                        if (!File.Exists(uwgCfgFile))
                        {
                            Console.WriteLine("Unwanted groups file not found.");
                        }
                        Console.WriteLine("\nExample usage:");
                        Console.WriteLine($"{programFileName} original.m3u8");
                        return;
                    }

                    else if (args[0].ToLower() == "/m")
                    {
                        Console.WriteLine($"*** Downloading M3U8-file to: {userM3u8File}");
                        webClient.DownloadFile(new Uri(UserURLCombined), userM3uFileTemp);
                        if (File.Exists(userM3u8File))
                            File.Delete(userM3u8File);
                        File.Move(userM3uFileTemp, userM3u8File);
                        return;
                    }

                    else if (args[0].ToLower() == "/v")
                    {
                        Console.WriteLine(creator);
                        Console.WriteLine($"{programFileName} version {version}");
                        return;
                    }

                    else if (args[0].ToLower() == "/c")
                    {
                        //backup old configFile if exist
                        if (File.Exists(configFile))
                        {
                            if (File.Exists(configFileOld))
                                File.Delete(configFileOld);
                            File.Move(configFile, configFileOld);
                            Console.WriteLine($"*** WARNING: Configuration file existed and has been moved to: {configFileOld}.");
                            //starts another process in background that loads default values before writing new config
                            Process.Start(Environment.GetCommandLineArgs()[0], Environment.GetCommandLineArgs().Length > 1 ? string.Join(" ", Environment.GetCommandLineArgs().Skip(1)) : null);
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
                        Console.WriteLine($"*** Configuration file created: {configFile}");
                        return;
                    }

                    else if (args[0].ToLower() == "/g")
                    {
                        Console.WriteLine("\nQuick guide:\n");
                        Console.WriteLine($"1. Create the configuration file: {programFileName} /C");
                        Console.WriteLine("2. Edit the configuration file and set at least the following:");
                        Console.WriteLine(" - m3u8File value: /where/you/want/to/download/original.m3u8");
                        Console.WriteLine(" - DownloadM3U8Enabled value: True");
                        Console.WriteLine(" - Also fill in your UserURL, UserPort, UserName and UserPass.");
                        Console.WriteLine($"3. Download your M3U8-file using the configuration file settings: {programFileName} /M");
                        Console.WriteLine($"4. Create the unwanted groups file: {programFileName} /U");
                        Console.WriteLine("5. Edit the unwanted groups file and comment out the groups you want to keep with //.");
                        Console.WriteLine($"6. You're all done. From now on you can run {programFileName} without any arguments and the program will be using your configuration.\n");
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
                                    Console.WriteLine($"*** Using arg specified M3U8-file: {m3u8File} (overriding config).");
                                else Console.WriteLine($"*** Using arg specified M3U8-file: {m3u8File} (not set in config).");
                            }
                            else
                            {
                                fileNotFound = true;
                                Console.WriteLine($"File not found - {m3u8File}");
                                return;
                            }
                        }
                        else
                        {
                            if (ConfigurationManager.AppSettings.Get("m3u8File") != null && ConfigurationManager.AppSettings.Get("m3u8File") != "")
                                m3u8File = ConfigurationManager.AppSettings.Get("m3u8File");
                            if (File.Exists(m3u8File))
                                Console.WriteLine($"*** Using config set M3U8-file: {m3u8File}");
                            else
                            {
                                fileNotFound = true;
                                Console.WriteLine($"File not found - {m3u8File}");
                                return;
                            }
                        }

                        //used for warning message later
                        bool uwgCfgExisted = false;

                        //backup old uwgCfgFile
                        if (File.Exists(uwgCfgFile))
                        {
                            if (File.Exists(uwgCfgFileOld))
                                File.Delete(uwgCfgFileOld);
                            File.Copy(uwgCfgFile, uwgCfgFileOld, true);
                            uwgCfgExisted = true;
                        }
                                                
                        //set the content of the m3u8File to sourceText
                        sourceText = File.ReadAllText(m3u8File);

                        //normalize linefeed
                        sourceText = sourceText
                            .Replace("\r\n", "\n")
                            .Replace("\r", "\n");

                        //there is a title not in a group, used for setting the _NOGROUP
                        bool ngExist = false;

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

                                //check if noname group
                                if (GROUP == "")
                                {
                                    ngExist = true;
                                    GROUP = "_NOGROUP";
                                }

                                //less output by not letting same output twice
                                if (uwgCfgArray.Contains(GROUP) == false)
                                {
                                    Console.WriteLine($"Found group: {GROUP}");
                                    //add the group to the array
                                    Array.Resize(ref uwgCfgArray, uwgCfgArray.Length + 1);
                                    uwgCfgArray[uwgCfgArray.Length - 1] = GROUP;
                                }
                            }
                        }

                        //tidy up in the uwgCfgArray and write to uwgCfgFile
                        //remove blank lines
                        //uwgCfgArray = uwgCfgArray.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                        //sort the lines alphabetically ascending
                        Array.Sort(uwgCfgArray);
                        //remove duplicates
                        //File.WriteAllLines(uwgCfgFile, uwgCfgArray.Distinct().ToArray());
                        File.WriteAllLines(uwgCfgFile, uwgCfgArray);
                        if (uwgCfgExisted == true)
                            Console.WriteLine($"\n*** WARNING: Unwanted groups file existed and has been moved to: {uwgCfgFileOld}");
                        else
                            Console.WriteLine("");
                        Console.WriteLine($"*** Unwanted groups file created: {uwgCfgFile}");
                        if (ngExist == true)
                            Console.WriteLine("*** Note: We've found titles not in groups, they will be processed as _NOGROUP.");
                        return;
                    }

                    else if (args[0].StartsWith("/"))
                    {
                        Console.WriteLine($"Invalid switch - {args[0]}");
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
                                //Create log directory if not exist
                                if (!Directory.Exists(logDir))
                                    Directory.CreateDirectory(logDir);
                                programLog = File.AppendText(programLogFile);
                                programLog.WriteLine(programLogStartLine);
                            }
                            else
                            {
                                outText = $"{DateTime.Now}: INFO: Program logging is disabled in config.";
                                Console.WriteLine(outText);
                            }

                            if (ConfigurationManager.AppSettings.Get("m3u8File") != null && ConfigurationManager.AppSettings.Get("m3u8File") != "")
                            {
                                outText = $"{DateTime.Now}: *** Using arg specified M3U8-file: {m3u8File} (overriding config).";
                                Console.WriteLine(outText);
                                if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                            }
                            else
                            {
                                outText = $"{DateTime.Now}: Using arg specified M3U8-file: {m3u8File} (not set in config).";
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
                            //create log directory if not exist
                            if (!Directory.Exists(logDir))
                                Directory.CreateDirectory(logDir);
                            programLog = File.AppendText(programLogFile);
                            programLog.WriteLine(programLogStartLine);
                        }
                        else
                        {
                            outText = $"{DateTime.Now}: INFO: Program logging is disabled in config.";
                            Console.WriteLine(outText);
                        }
                        outText = $"{DateTime.Now}: *** Using config set M3U8-file: {m3u8File}";
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

                //output to log and console, pause
                //outText = $"{DateTime.Now}: Stuff here {stringhere}";
                //Console.WriteLine(outText);
                //if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                //Console.ReadKey();

                //check if user wants to download m3u8 before parsing
                if (DownloadM3U8Enabled == true)
                {
                    //start logging (3/3)
                    if (ProgramLogEnabled == true)
                    {
                        //create log directory if not exist
                        if (!Directory.Exists(logDir))
                            Directory.CreateDirectory(logDir);
                        programLog = File.AppendText(programLogFile);
                        programLog.WriteLine(programLogStartLine);
                    }
                    else
                    {
                        outText = $"{DateTime.Now}: INFO: Program logging is disabled in config.";
                        Console.WriteLine(outText);
                    }
                    outText = $"{DateTime.Now}: *** Downloading M3U8-file to: {userM3u8File}";
                    Console.WriteLine(outText);
                    if (ProgramLogEnabled == true) programLog.WriteLine(outText);

                    //download the m3u8-file
                    webClient.DownloadFile(new Uri(UserURLCombined), userM3uFileTemp);
                    if (File.Exists(userM3u8File))
                        File.Delete(userM3u8File);
                    File.Move(userM3uFileTemp, userM3u8File);

                    if (m3u8File != null || m3u8File != "")
                    {
                        outText = $"{DateTime.Now}: *** Using downloaded M3U8-file: {userM3u8File} (overrides config/args).";
                        Console.WriteLine(outText);
                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                    }
                    else
                    {
                        outText = $"{DateTime.Now}: *** Using downloaded M3U8-file: {userM3u8File}";
                        Console.WriteLine(outText);
                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                    }
                    //set m3u8File to userM3u8File
                    m3u8File = userM3u8File;
                }

                //start stopwatch, this late because we are not interested in how long it might take to download m3u8
                var stopWatch = Stopwatch.StartNew();

                //if m3u8File exist continue, or set fileNotFound true
                if (File.Exists(m3u8File))
                {
                    outText = $"{DateTime.Now}: *** BaseDirectory: {BaseDirectory}";
                    Console.WriteLine(outText);
                    if (ProgramLogEnabled == true) programLog.WriteLine(outText);

                    outText = $"{DateTime.Now}: *** OutDirectory: {OutDirectory}";
                    Console.WriteLine(outText);
                    if (ProgramLogEnabled == true) programLog.WriteLine(outText);

                    outText = $"{DateTime.Now}: *** Movie subfolder: {MoviesSubDir}";
                    Console.WriteLine(outText);
                    if (ProgramLogEnabled == true) programLog.WriteLine(outText);

                    outText = $"{DateTime.Now}: *** Series subfolder: {SeriesSubDir}";
                    Console.WriteLine(outText);
                    if (ProgramLogEnabled == true) programLog.WriteLine(outText);

                    outText = $"{DateTime.Now}: *** TV subfolder: {TVSubDir}";
                    Console.WriteLine(outText);
                    if (ProgramLogEnabled == true) programLog.WriteLine(outText);

                    //check if config exists
                    if (File.Exists(configFile))
                    {
                        outText = $"{DateTime.Now}: *** Configuration file in use: {configFile}";
                        Console.WriteLine(outText);
                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                    }

                    //set uwgCfgFile if wanted and exists
                    if (File.Exists(uwgCfgFile) && UnwantedCFGEnabled == true)
                    {
                        outText = $"{DateTime.Now}: *** Unwanted groups file in use: {uwgCfgFile}";
                        Console.WriteLine(outText);
                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                        uwgCfgArray = File.ReadAllLines(uwgCfgFile);
                    }
                    else if (UnwantedCFGEnabled == false)
                    {
                        outText = $"{DateTime.Now}: *** Unwanted groups file not in use.";
                        Console.WriteLine(outText);
                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                    }

                    //empty old contents of uwgLogFile
                    if (ProgramLogEnabled == true) File.WriteAllText(uwgLogFile, string.Empty);

                    //empty old contents of allGroupsFile
                    if (ProgramLogEnabled == true) File.WriteAllText(allGroupsFile, string.Empty);

                    //empty old contents of dupeLogFile
                    if (ProgramLogEnabled == true) File.WriteAllText(dupeLogFile, string.Empty);

                    //empty old contents of newGroupsFile
                    if (ProgramLogEnabled == true) File.WriteAllText(newGroupsFile, string.Empty);

                    //in the odd instance newLogFile would exist with the same name from previous run, add .old to the old one
                    if (File.Exists(newLogFile))
                    {
                        if (File.Exists(newLogFileOld))
                            File.Delete(newLogFileOld);
                        File.Move(newLogFile, newLogFileOld);
                    }

                    //delete previously created directories
                    if (DeletePreviousDirEnabled == true)
                    {
                        outText = $"{DateTime.Now}: *** Deleting previously created directories (set in config).";
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
                        outText = $"{DateTime.Now}: *** Not deleting previously created directories.";
                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                    }

                    //tell user we are using movie-group subfolders if set
                    if (MovieGroupSubdirEnabled == true)
                    {
                        outText = $"{DateTime.Now}: *** Creating movie-group subfolders (set in config).";
                        Console.WriteLine(outText);
                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                    }

                    //################################### parse start ###################################
                    //now we finally start to parse the m3u8-file
                        outText = $"{DateTime.Now}: *** Processing {m3u8File}";
                    Console.WriteLine(outText);
                    if (ProgramLogEnabled == true) programLog.WriteLine(outText);

                    //set sourceText to the content of m3u8File
                    sourceText = File.ReadAllText(m3u8File);

                    //normalize linefeed
                    sourceText = sourceText
                        .Replace("\r\n", "\n")
                        .Replace("\r", "\n");

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
                            bool isUnwanted = false;           //is unwanted

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
                                if (allGroupsArray.Contains(GROUP) == false)
                                {
                                    //add to array
                                    Array.Resize(ref allGroupsArray, allGroupsArray.Length + 1);
                                    allGroupsArray[allGroupsArray.Length - 1] = GROUP;
                                }
                            }

                            foreach (string uwgCfgLine in uwgCfgArray)
                            {
                                if (GROUP == uwgCfgLine)
                                {
                                    if (uwgLogArray.Contains(GROUP) == false && ProgramLogEnabled == true)
                                    {
                                        outText = $"{DateTime.Now}: Unwanted group: '{GROUP}' match in: '{uwgCfgLine}'";
                                        programLog.WriteLine(outText);
                                        //add to array
                                        Array.Resize(ref uwgLogArray, uwgLogArray.Length + 1);
                                        uwgLogArray[uwgLogArray.Length - 1] = GROUP;
                                    }
                                    //set isUnwanted to true
                                    isUnwanted = true;
                                }
                                continue;
                            }

                            if (isUnwanted == false)
                            {
                                //replace linefeed and carriage return with nothing
                                URL = sourceTextLine2
                                    .Replace("\r\n", "")
                                    .Replace("\n", "")
                                    .Replace("\r", "")
                                    .Replace("\n", "");

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
                                        outText = $"{DateTime.Now}: TV Show episode: {NAME} found";
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
                                        outText = $"{DateTime.Now}: Movie: {NAME} found";
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
                                    NAME = $"{cTV} {NAME}";

                                    if (VerboseConsoleOutputEnabled == true)
                                    {
                                        outText = $"{DateTime.Now}: TV Channel: {NAME} found";
                                        Console.WriteLine(outText);
                                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                                    }
                                    cTV++;
                                }

                                //create directory if not exist
                                if (!Directory.Exists(combinedTypeDir))
                                    Directory.CreateDirectory(combinedTypeDir);

                                //append strmEXT to the file name
                                NameStrm = $"{NAME}{strmEXT}";

                                //and combine the path with it
                                combinedTypeNameStrm = Path.Combine(combinedTypeDir, NameStrm);

                                //convert NAME to lowercase to get the same result on all OS
                                lowerNAME = NAME.ToLower();

                                //is a dupe
                                bool isDupe = false;

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
                                        outText = $"{DateTime.Now}: Dupe found: {NAME}";
                                        programLog.WriteLine(outText);
                                        //add to dupeArray
                                        Array.Resize(ref dupeArray, dupeArray.Length + 1);
                                        dupeArray[dupeArray.Length - 1] = NAME;
                                    }
                                }

                                //add the to outArray
                                Array.Resize(ref outArray, outArray.Length + 1);
                                outArray[outArray.Length - 1] = lowerNAME;

                                //add the title to foundArray only if not dupe
                                if (isDupe == false)
                                {
                                    Array.Resize(ref foundArray, foundArray.Length + 1);
                                    foundArray[foundArray.Length - 1] = (combinedTypeNameStrm);
                                }

                                //create output file if not exist on disk and not already in the outArray (dupe)
                                if (!File.Exists(combinedTypeNameStrm) && isDupe == false)
                                {
                                    //check if the filename plus path is too long for Windows filesystem
                                    int combinedTypeNameStrmLength = combinedTypeNameStrm.Length;
                                    if (combinedTypeNameStrmLength >= 260)
                                    {
                                        outText = $"WARNING: Destination path too long\nThe file name could be too long ({combinedTypeNameStrmLength} chars) for the destination directory.\n'{combinedTypeNameStrm}'";
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

                                    //and finally, here we create the output file
                                    File.WriteAllText(combinedTypeNameStrm, URL);

                                    //write the same path and filename to newArray if ProgramLogEnabled true
                                    if (ProgramLogEnabled == true)
                                    {
                                        Array.Resize(ref newArray, newArray.Length + 1);
                                        newArray[newArray.Length - 1] = combinedTypeNameStrm;
                                    }
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

                                        //overwrite old outdated content in output file
                                        File.WriteAllText(combinedTypeNameStrm, URL);

                                        outText = $"{DateTime.Now}: Updated content in: '{combinedTypeNameStrm}'";
                                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                                        {
                                            //add the same path and filename to newArray
                                            Array.Resize(ref newArray, newArray.Length + 1);
                                            newArray[newArray.Length - 1] = combinedTypeNameStrm;
                                        }
                                    }
                                    else
                                    {
                                        //enable/disable logFileExists
                                        bool logFileExists = false;

                                        if (ProgramLogEnabled == true && logFileExists == true)
                                        {
                                            outText = $"{DateTime.Now}: File exists: {combinedTypeNameStrm}";
                                            programLog.WriteLine(outText);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (ProgramLogEnabled == true)
                    {
                        //tidy up the newArray and write to newLogFile
                        if (newArray.Length > 0)
                        {
                            //sort the lines alphabetically ascending
                            Array.Sort(newArray);
                            //write to disk
                            File.WriteAllLines(newLogFile, newArray);
                        }

                        //tidy up the uwgLogArray and write to uwgLogFile
                        if (uwgLogArray.Length > 0)
                        {
                            //remove blank lines
                            //uwgCfgArray = uwgCfgArray.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                            //sort the lines alphabetically ascending
                            Array.Sort(uwgLogArray);
                            //write to disk
                            File.WriteAllLines(uwgLogFile, uwgLogArray);
                        }

                        //tidy up the allGroupsArray and write to allGroupsFile
                        if (allGroupsArray.Length > 0)
                        {
                            //sort the lines alphabetically ascending
                            Array.Sort(allGroupsArray);
                            //write to disk
                            File.WriteAllLines(allGroupsFile, allGroupsArray);
                        }

                        //tidy up the dupeArray and write to dupeLogFile
                        if (dupeArray.Length > 0)
                        {
                            //sort the lines alphabetically ascending
                            Array.Sort(dupeArray);
                            //remove duplicates and write to disk
                            File.WriteAllLines(dupeLogFile, dupeArray.Distinct());
                        }

                        //test newGroupArray
                        //first remove all comments // from uwgCfgArray
                        //this works but only changes the first occurance
                        //uwgCfgArray = uwgCfgArray.ToList().Select(x => Regex.Replace(x, @"^//", "")).ToArray();
                        //works -- but I would rather use regex to get the starts with ^
                        uwgCfgArray = uwgCfgArray.Select(x => x.Replace(@"//", "")).ToArray();

                        //then remove uwgCfgArray from allGroupsArray
                        newGroupArray = allGroupsArray.Except(uwgCfgArray).ToArray();
                        
                        //File.WriteAllLines("_1.log", uwgCfgArray);

                        //sort the lines alphabetically ascending
                        Array.Sort(newGroupArray);
                        //write to disk
                        File.WriteAllLines(newGroupsFile, newGroupArray);
                    }

                    //purging old strm-files and deleting empty directories
                    if (PurgeFilesEnabled == true)
                    {
                        outText = $"{DateTime.Now}: *** Purging files in: {OutDirectory}";
                        Console.WriteLine(outText);
                        if (ProgramLogEnabled == true) programLog.WriteLine(outText);

                        //get all strm-files on disk into array
                        filesOnDiskArray = Directory.GetFiles(OutDirectory, $"*{strmEXT}", SearchOption.AllDirectories);

                        //remove content of foundArray from filesOnDiskArray -- left will be titles no longer in m3u8-file
                        purgeArray = filesOnDiskArray.Except(foundArray).ToArray();

                        //set false as long as nothing was purged
                        bool hasPurged = false;

                        //tidy up the purgeArray and write to purgeLogFile
                        if (purgeArray.Length > 0)
                        {
                            Array.Sort(purgeArray);
                            File.WriteAllLines(purgeLogFile, purgeArray);

                            //purgeArray will now contain items to delete, here we split each array into string
                            foreach (string purgeItem in purgeArray)
                            {
                                //if file found
                                if (File.Exists(purgeItem))
                                {
                                    //delete file 
                                    File.Delete(purgeItem);
                                    outText = $"{DateTime.Now}: Purged file: {purgeItem}";
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

                                    //something was purged
                                    hasPurged = true;
                                }
                            }
                        }

                        //only run directory cleanup if something was purged
                        if (hasPurged == true)
                        {
                            //delete empty directories which can be a left over after a purge
                            foreach (var directory in Directory.GetDirectories(OutDirectory, "*", SearchOption.AllDirectories))
                            {
                                if (Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Length == 0)
                                {
                                    Directory.Delete(directory, true);
                                    outText = $"{DateTime.Now}: Deleted empty directory: {directory}";
                                    if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                                }
                            }
                        }
                    }       

                    //subtract actual written from found = skipped
                    cSkippedMovies = cMovies - cMoviesActual;
                    cSkippedSeries = cSeries - cSeriesActual;
                    cSkippedTV = cTV - cTVActual;

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

                    //set counters output
                    if (cMoviesActual > 0) newMovies = $"{Convert.ToString(cMoviesActual)} new";
                    if (cMoviesUpdate > 0) updateMovies = $", {Convert.ToString(cMoviesUpdate)} updated";
                    if (cSkippedMovies > 0) skipMovies = $", {Convert.ToString(cSkippedMovies)} skipped";
                    if (cMoviesDupe > 0) dupeMovies = $" (whereof {Convert.ToString(cMoviesDupe)} dupes)";
                    if (cMoviesPurge > 0) purgeMovies = $", {Convert.ToString(cMoviesPurge)} purged";
                    if (cMovies > 0) totalMovies = $", {Convert.ToString(cMovies)} in total";

                    if (cSeriesActual > 0) newSeries = $"{Convert.ToString(cSeriesActual)} new";
                    if (cSeriesUpdate > 0) updateSeries = $", {Convert.ToString(cSeriesUpdate)} updated";
                    if (cSkippedSeries > 0) skipSeries = $", {Convert.ToString(cSkippedSeries)} skipped";
                    if (cSeriesDupe > 0) dupeSeries = $" (whereof {Convert.ToString(cSeriesDupe)} dupes)";
                    if (cSeriesPurge > 0) purgeSeries = $", {Convert.ToString(cSeriesPurge)} purged";
                    if (cSeries > 0) totalSeries = $", {Convert.ToString(cSeries)} in total";

                    if (cTVActual > 0) newTV = $"{Convert.ToString(cTVActual)} new";
                    if (cTVUpdate > 0) updateTV = $", {Convert.ToString(cTVUpdate)} updated";
                    if (cSkippedTV > 0) skipTV = $", {Convert.ToString(cSkippedTV)} skipped";
                    if (cTVDupe > 0) dupeTV = $" (whereof {Convert.ToString(cTVDupe)} dupes)";
                    if (cTVPurge > 0) purgeTV = $", {Convert.ToString(cTVPurge)} purged";
                    if (cTV > 0) totalTV = $", {Convert.ToString(cTV)} in total";

                    //write summary to console (and log if enabled)
                    outText = $"{DateTime.Now}: *** Movies summary: {newMovies}{updateMovies}{skipMovies}{dupeMovies}{purgeMovies}{totalMovies}";
                    Console.WriteLine(outText);
                    if (ProgramLogEnabled == true) programLog.WriteLine(outText);

                    outText = $"{DateTime.Now}: *** Episodes summary: {newSeries}{updateSeries}{skipSeries}{dupeSeries}{purgeSeries}{totalSeries}";
                    Console.WriteLine(outText);
                    if (ProgramLogEnabled == true) programLog.WriteLine(outText);

                    outText = $"{DateTime.Now}: *** TV-channels summary: {newTV}{updateTV}{skipTV}{dupeTV}{purgeTV}{totalTV}";
                    Console.WriteLine(outText);
                    if (ProgramLogEnabled == true) programLog.WriteLine(outText);
                }
                else
                {
                    if (fileNotFound == true)
                    {
                        //we will end up here if the file given either as arg or in config was not found
                        Console.WriteLine($"File not found - {m3u8File}");
                        
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
                outText = $"{DateTime.Now}: *** Processing time: {elapsedTime}";
                Console.WriteLine(outText);
                if (ProgramLogEnabled == true) programLog.WriteLine(outText);

                if (ProgramLogEnabled == true)
                {
                    //logging ends here
                    programLog.WriteLine($"{DateTime.Now}: *** Log end, {programFileName}, {version}");
                    programLog.Flush();
                    programLog.Close();
                }
            }

            //error codes
            catch (Exception ex)
            {
                string ErrorLogFile = $"{AppDomain.CurrentDomain.BaseDirectory}error.log";
                StreamWriter ErrorLog = File.AppendText(ErrorLogFile);
                string outText = "";

                outText = $"{DateTime.Now}: ERROR: {ex.Message}";
                Console.WriteLine(outText);
                ErrorLog.WriteLine(outText);

                outText = $"{DateTime.Now}: ERROR: {ex.StackTrace}";
                ErrorLog.WriteLine(outText);

                ErrorLog.Flush();
                ErrorLog.Close();

                //outText = ("Press any key to quit...");
                //Console.WriteLine(outText);
                //Console.ReadLine();
            }
        }

        //removes illegal file name characters
        public static string NAMEFilterFileNameChars(string fileName)
        {
            //decode html-encoded chars
            fileName = WebUtility.HtmlDecode(fileName);

            //remove VOD: from beginning of names (keeping IgnoreCase because Albania uses Vod: in filenames)
            fileName = Regex.Replace(fileName, @"^VOD:\s", "", RegexOptions.IgnoreCase);
            //replace O with 0 in eg SO1E01
            fileName = Regex.Replace(fileName, @"\sSO(\d)", " S0$1", RegexOptions.IgnoreCase);
            //replace erroneous ExxExx with SxxExx
            fileName = Regex.Replace(fileName, @"E(\d+)E(\d+)", "S$1E$2", RegexOptions.IgnoreCase);
            //replace erroneous SxxSxx with SxxExx
            fileName = Regex.Replace(fileName, @"S(\d+)S(\d+)", "S$1E$2", RegexOptions.IgnoreCase);
            //replace Sxx EPxx with SxxExx
            fileName = Regex.Replace(fileName, @"\sS(\d+)\sEP(\d+)", " S$1E$2", RegexOptions.IgnoreCase);
            //add missing space after comma
            fileName = Regex.Replace(fileName, @",(\w)", ", $1");
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
                .Replace("➔", "-")
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

            //replace eg 1x01 with S01E01
            fileName = Regex.Replace(fileName, @"(\d)x(\d+)", "S0$1E$2", RegexOptions.IgnoreCase);
            fileName = Regex.Replace(fileName, @"(\d+)x(\d+)", "S$1E$2", RegexOptions.IgnoreCase);

            //remove dash before SxxExx
            fileName = Regex.Replace(fileName, @"-\s(s\d+)(e\d+)", "$1$2", RegexOptions.IgnoreCase);

            //remove everything after SxxExx
            fileName = Regex.Replace(fileName, @"(s\d+)(e\d+).*", "$1$2", RegexOptions.IgnoreCase).Trim('.', ' ');

            //remove double naming
            fileName = Regex.Replace(fileName, @".*s(\d+)\s", "", RegexOptions.IgnoreCase).Trim('.', ' ');

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

            //problematic Swedish namings; Swedish movie and serie titles should not use upper case letters on each word in a title as in the English language,
            //Windows would in some instances not be able to access duplicates through Samba on Linux if not corrected.
            //This list needs to be constantly updated:
            fileName = Regex.Replace(fileName, @"B.st i K.ket", "Bäst i köket", RegexOptions.IgnoreCase);
            fileName = Regex.Replace(fileName, @"En bondg.rd mitt i stan", "En bondgård mitt i stan", RegexOptions.IgnoreCase);
            fileName = Regex.Replace(fileName, @"Ensam i vildmarken", "Ensam i vildmarken", RegexOptions.IgnoreCase);
            fileName = Regex.Replace(fileName, @"Insats torsk - sexhandeln inifrån", "Insats Torsk - Sexhandeln inifrån", RegexOptions.IgnoreCase);
            fileName = Regex.Replace(fileName, @"Mästarnas M.stare", "Mästarnas mästare", RegexOptions.IgnoreCase);
            fileName = Regex.Replace(fileName, @"Morden I Sandhamn", "Morden i Sandhamn", RegexOptions.IgnoreCase);
            fileName = Regex.Replace(fileName, @"Sommaren med sl.kten", "Sommaren med släkten", RegexOptions.IgnoreCase);
            fileName = Regex.Replace(fileName, @"Svenska Fall", "Svenska fall", RegexOptions.IgnoreCase);
            fileName = Regex.Replace(fileName, @"Sveriges Yngsta M.sterkock", "Sveriges yngsta mästerkock", RegexOptions.IgnoreCase);
            fileName = Regex.Replace(fileName, @"Udda Veckor", "Udda veckor", RegexOptions.IgnoreCase);
            fileName = Regex.Replace(fileName, @"Wahlgrens v.rld", "Wahlgrens värld", RegexOptions.IgnoreCase);

            //correct case The Of A An To On From if not preceeded by dash
            fileName = Regex.Replace(fileName, @"(?<!-)\sThe\s", " the ");
            fileName = Regex.Replace(fileName, @"(?<!-)\sOf\s", " of ");
            fileName = Regex.Replace(fileName, @"(?<!-)\sA\s", " a ");
            fileName = Regex.Replace(fileName, @"(?<!-)\sAn\s", " an ");
            fileName = Regex.Replace(fileName, @"(?<!-)\sTo\s", " to ");
            fileName = Regex.Replace(fileName, @"(?<!-)\sOn\s", " on ");
            fileName = Regex.Replace(fileName, @"(?<!-)\sFrom\s", " from ");

            //correct case the of a an to if on from preceeded by dash
            fileName = Regex.Replace(fileName, @"-\sthe\s", "- The ");
            fileName = Regex.Replace(fileName, @"-\sof\s", "- Of ");
            fileName = Regex.Replace(fileName, @"-\sa\s", "- A ");
            fileName = Regex.Replace(fileName, @"-\san\s", "- An ");
            fileName = Regex.Replace(fileName, @"-\sto\s", "- To ");
            fileName = Regex.Replace(fileName, @"-\son\s", "- On ");
            fileName = Regex.Replace(fileName, @"-\sfrom\s", "- From ");

            //truncate if numbers has space in them, eg 10 000 to 10000
            fileName = Regex.Replace(fileName, @"(\d)\s(\d)", "$1$2");

            //replace without name with _NONAME
            fileName = Regex.Replace(fileName, @"^$", "_NONAME");

            //remove leading and trailing period and space
            fileName = fileName.Trim('.', ' ');

            //misc namefix
            fileName = Regex.Replace(fileName, @"^DAVE", "Dave");
            fileName = Regex.Replace(fileName, @"^power", "Power");
            fileName = Regex.Replace(fileName, @"^RUN", "Run");

            //add parentheses to year if missing, only if begins with 19 or 20 and is not already in parentheses
            //do not add this to start of string eg if movie name is '1917 (2019)'
            //also, if preceded with dash, remove the dash
            fileName = Regex.Replace(fileName, @"(?<!^)( -|- |-|)(?<!\()(19|20)(\d{2})(?!\))$", "($2$3)").Trim('.', ' ');

            //add missing space before year if missing, only if begins with 19 or 20, is 4 numbers and ends with number
            fileName = Regex.Replace(fileName, @"(?<!\s)\((19|20)(\d{2})\)$", " ($1$2)");

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
                .Replace("➔", "-")
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
    }
}
