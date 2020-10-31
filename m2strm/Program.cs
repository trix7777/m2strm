using System;
using Microsoft.VisualBasic;
using System.IO;
using System.Text.RegularExpressions;

namespace m2strm
{
	sealed class Program
	{

		public static void Main(string[] args)
		{
			try
			{
				string creator = "Original code by TimTester, forked and converted to c# (mono compat) by trix77";
				Console.WriteLine(creator);

				int counter_Movie = 0;
				int counter_Series = 0;

				//Get location, appName and version
				string location = System.Convert.ToString(Environment.GetCommandLineArgs()[0]);
				string appName = Path.GetFileName(location);
				string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

				//m3u8
				string m3u8 = "";
				string param2 = "";
				param2 = "";

				//Check args
				if (args.Length > 0)
				{
					m3u8 = args[0];
					if (args[0] == "/?")
					{
						Console.WriteLine("Creates STRM-files from M3U8-file.");
						Console.WriteLine("");
						Console.WriteLine(appName + " [drive:][path]filename [/D]");
						Console.WriteLine("");
						Console.WriteLine("  filename    Specifies the M3U8-file to be processed.");
						Console.WriteLine("");
						Console.WriteLine("  /D          Delete previously created directories.");
						Console.WriteLine("  /V          Version information.");
						Console.WriteLine("");
						Console.WriteLine("Examples:");
						Console.WriteLine(appName + " original.m3u8");
						Console.WriteLine(appName + " original.m3u8 /D");
						return;
					}
					else if (args[0].ToLower() == "/d")
					{
						Console.WriteLine("Invalid option: " + args[0] + " cannot be used without filename.");
						Console.WriteLine("Type /? for help");
						return;
					}
					else if (args[0].ToLower() == "/v")
					{
						Console.WriteLine("Filename: " + appName);
						Console.WriteLine("Version: " + version);
						return;
					}
					else if (args[0].StartsWith("/"))
					{
						Console.WriteLine("Invalid switch - " + args[0]);
						Console.WriteLine("Type /? for help");
						return;
					}
					else if (args[0].ToLower().Contains("m3u"))
					{
						if ((new Microsoft.VisualBasic.Devices.ServerComputer()).FileSystem.FileExists(m3u8) == false)
						{
							Console.WriteLine("File not found - " + args[0]);
							Console.WriteLine("Type /? for help");
							return;
						}
					}
					else
					{
						Console.WriteLine("File not found - " + args[0]);
						Console.WriteLine("Type /? for help");
						return;
					}
				}
				else
				{
					Console.WriteLine("No file to process");
					Console.WriteLine("Type /? for help");
					return;
				}
				if (args.Length > 1)
				{
					param2 = args[1];
					if (param2.ToLower() == "/d")
					{
					}
					else
					{
						Console.WriteLine("Invalid switch - " + param2);
						Console.WriteLine("Type /? for help");
						return;
					}
				}

				//Read file
				string Main_folder = System.AppDomain.CurrentDomain.BaseDirectory;
				string folder_Movies = Main_folder + Path.DirectorySeparatorChar + "Movies";
				string folder_Series = Main_folder + Path.DirectorySeparatorChar + "Series";

				//Delete previously created directories
				if (param2.ToLower() == "/d")
				{
					Console.WriteLine("Deleting previously created directories...");
					System.Threading.Thread.Sleep(5000);
					if ((new Microsoft.VisualBasic.Devices.ServerComputer()).FileSystem.DirectoryExists(folder_Movies))
					{
						(new Microsoft.VisualBasic.Devices.ServerComputer()).FileSystem.DeleteDirectory(folder_Movies, Microsoft.VisualBasic.FileIO.DeleteDirectoryOption.DeleteAllContents);
					}
					if ((new Microsoft.VisualBasic.Devices.ServerComputer()).FileSystem.DirectoryExists(folder_Series))
					{
						(new Microsoft.VisualBasic.Devices.ServerComputer()).FileSystem.DeleteDirectory(folder_Series, Microsoft.VisualBasic.FileIO.DeleteDirectoryOption.DeleteAllContents);
					}
				}
				else
				{
					Console.WriteLine("Info: /D switch not specified.");
					Console.WriteLine("Info: No deletion of previously created directories will occur.");
					System.Threading.Thread.Sleep(5000);
				}

				//Create directories
				(new Microsoft.VisualBasic.Devices.ServerComputer()).FileSystem.CreateDirectory(folder_Movies);
				(new Microsoft.VisualBasic.Devices.ServerComputer()).FileSystem.CreateDirectory(folder_Series);

				//M3U
				if ((new Microsoft.VisualBasic.Devices.ServerComputer()).FileSystem.FileExists(m3u8) == true)
				{
					string FileText = (new Microsoft.VisualBasic.Devices.ServerComputer()).FileSystem.ReadAllText(m3u8);

					string[] FileText_lines = FileText.Split(Constants.vbLf.ToCharArray());
					for (var index = 1; index <= FileText_lines.Length - 1; index++)
					{
						string line1 = FileText_lines[(int)index];
						if (line1.ToLower().StartsWith("#extinf:") &&
								line1.ToLower().Contains("series: barn") || //Only get these series groups
								line1.ToLower().Contains("series: english [multi-sub]") ||
								line1.ToLower().Contains("series: nordic") ||
								(line1.ToLower().Contains("vod:") && //Get all vod movie groups but not these:
								!line1.ToLower().Contains("vod: 4k movies [multi-sub] [only on 4k devices]") &&
								!line1.ToLower().Contains("vod: albania") &&
								!line1.ToLower().Contains("vod: arabic") &&
								!line1.ToLower().Contains("vod: crtani filmovi [ex-yu]") &&
								!line1.ToLower().Contains("vod: danske - norska - suomalainen film") &&
								!line1.ToLower().Contains("vod: english movies [arabic subtitle]") &&
								!line1.ToLower().Contains("vod: events") &&
								!line1.ToLower().Contains("vod: germany") &&
								!line1.ToLower().Contains("vod: india") &&
								!line1.ToLower().Contains("vod: iran") &&
								!line1.ToLower().Contains("vod: polska") &&
								!line1.ToLower().Contains("vod: turkey") &&
								!line1.ToLower().Contains("vod: vietnam") &&
								!line1.ToLower().Contains("vod: ex-yu movies")))
						{

							string line2 = FileText_lines[index + 1];

							string NAME = "";
							string URL = "";

							int Start_Name = line1.IndexOf("tvg-name=") + 10;
							int Start_Logo = line1.IndexOf("tvg-logo=") + 10;
							//Name
							NAME = System.Convert.ToString(line1.Substring(Start_Name, Start_Logo - Start_Name - 12).Replace("&", ""));
							//URL
							URL = System.Convert.ToString(line2.Replace("\r\n", "").Replace(Constants.vbCr, ""));

							//############################## FILTER START ##############################

							//Replace with dash
							NAME = NAME.Replace(":", "-");
							NAME = NAME.Replace("/", "-");
							NAME = NAME.Replace("|", "-");

							//Replace with space
							//NAME = NAME.Replace(".", " ")

							//Replace with nothing
							NAME = NAME.Replace(",", "");

							//Call the RemoveIllegalFileNameChars function
							NAME = System.Convert.ToString(RemoveIllegalFileNameChars(NAME).Trim());

							//Replace without name with NONAME
							NAME = Regex.Replace(NAME, "^$", "NONAME");

							//Remove erroneous spaces
							NAME = NAME.Replace("( ", "(");
							NAME = NAME.Replace(" )", ")");
							NAME = NAME.Replace("[ ", "[");
							NAME = NAME.Replace(" ]", "]");

							//Add spaces
							NAME = NAME.Replace(")", ") ");
							NAME = NAME.Replace("(", " (");
							NAME = NAME.Replace("]", "] ");
							NAME = NAME.Replace("[", " [");

							//Remove doubles
							NAME = Regex.Replace(NAME, "\\[{2,}", "\\[");
							NAME = Regex.Replace(NAME, "\\]{2,}", "\\]");
							NAME = Regex.Replace(NAME, "\\({2,}", "\\(");
							NAME = Regex.Replace(NAME, "\\){2,}", "\\)");

							//Remove [PRE] and misspellings thereof
							NAME = Regex.Replace(NAME, "\\[(P|R)(R|F)E\\]", "", RegexOptions.IgnoreCase);

							//Remove [Multi-Audio]
							NAME = Regex.Replace(NAME, "\\[(Mu.*|Dual)(-|\\s)Audio\\]", "", RegexOptions.IgnoreCase);

							//Remove [Multi-Subs]
							NAME = Regex.Replace(NAME, "\\[Mu.*(-|\\s)Sub(|s)\\]", "", RegexOptions.IgnoreCase);

							//Remove [Nordic]
							NAME = Regex.Replace(NAME, "( |\\[)nordic\\]", "", RegexOptions.IgnoreCase);

							//Remove [Only On 4K Devices]
							NAME = Regex.Replace(NAME, "\\[Only On 4K Devices\\]", "", RegexOptions.IgnoreCase);

							//Remove 4k
							//NAME = Regex.Replace(NAME, "4k", "", RegexOptions.IgnoreCase)

							//Remove stuff
							NAME = Regex.Replace(NAME, "\\[K(|I)DS\\]", "", RegexOptions.IgnoreCase);
							NAME = Regex.Replace(NAME, "\\[SE\\]", "", RegexOptions.IgnoreCase);
							NAME = Regex.Replace(NAME, "\\[IMDB\\]", "", RegexOptions.IgnoreCase);
							NAME = Regex.Replace(NAME, "\\[IMDB\\]", "", RegexOptions.IgnoreCase);

							//Remove empty parentheses and brackets
							NAME = NAME.Replace("()", "");
							NAME = NAME.Replace("[]", "");

							//Replace "9 - 1 - 1", "9 - 11", "9- 11", "9 -11" with no space in between dash
							NAME = Regex.Replace(NAME, "(\\d)(\\s-|-\\s|\\s-\\s)(\\d)", "$1-$3");
							//Doing it twice. Maybe do a while loop
							NAME = Regex.Replace(NAME, "(\\d)(\\s-|-\\s|\\s-\\s)(\\d)", "$1-$3");

							//Replace "Alien 5- Title", "Alien 5 -Title", "Alien 5-Title" with space before and after dash
							NAME = Regex.Replace(NAME, "(\\d)(-\\s|\\s-|-)([a-zA-Z])", "$1 - $3");
							NAME = Regex.Replace(NAME, "(\\d)(-\\s|\\s-|-)([a-zA-Z])", "$1 - $3");

							//Replace "5- Title", "5 -Title", "5-Title" with no space in between dash
							NAME = Regex.Replace(NAME, "^(\\d)(\\s-\\s)([a-zA-Z])", "$1-$3");
							NAME = Regex.Replace(NAME, "^(\\d)(\\s-\\s)([a-zA-Z])", "$1-$3");

							//Replace "Alien- 5", "Alien -5", "Alien-5" with space before and after dash
							NAME = Regex.Replace(NAME, "([a-zA-Z])(-\\s|\\s-|-)(\\d)", "$1 - $3");
							NAME = Regex.Replace(NAME, "([a-zA-Z])(-\\s|\\s-|-)(\\d)", "$1 - $3");

							//Replace "Movie- Title", "Movie -Title", "Movie-Title" with space before and after dash - but only on words that has two or more letters in them
							NAME = Regex.Replace(NAME, "([a-zA-Z]{2,})(-\\s|\\s-|-)([a-zA-Z{2,}])", "$1 - $3");
							NAME = Regex.Replace(NAME, "([a-zA-Z]{2,})(-\\s|\\s-|-)([a-zA-Z{2,}])", "$1 - $3");

							//Replace space between Sxx and Exx
							NAME = Regex.Replace(NAME, "(s\\d+)\\s(e\\d+)", "$1$2", RegexOptions.IgnoreCase);

							//Replace more than one space with one space
							NAME = Regex.Replace(NAME, "\\s{2,}", " ");

							//Replace brackets with parentheses
							NAME = NAME.Replace("[", "(");
							NAME = NAME.Replace("]", ")");

							//Remove leading and trailing period
							NAME = NAME.Trim(".".ToCharArray());

							//Remove leading and trailing space
							NAME = NAME.Trim();

							//############################## FILTER END ##############################

							//Extract group from URL
							string GROUP = "";
							if (URL.ToLower().Contains("/series"))
							{
								GROUP = "series";
							}
							if (URL.ToLower().Contains("/movie"))
							{
								GROUP = "movie";
							}

							//Check if series or movie
							string folder = "";
							if (GROUP.ToLower().Contains("series"))
							{

								//Combine series
								folder = folder_Series + Path.DirectorySeparatorChar + NAME;
								//folder = Regex.Replace(folder, "s(\d+) e(\d+)", "", RegexOptions.IgnoreCase).Trim
								//removed non-used capture groups and changed to the new sxxexx after replace above
								folder = Regex.Replace(folder, "s\\d+e\\d+", "", RegexOptions.IgnoreCase).Trim();
								Console.WriteLine("series: " + NAME + " found");
								counter_Series++;
							}
							if (GROUP.ToLower().Contains("movie"))
							{
								folder = folder_Movies + Path.DirectorySeparatorChar + NAME;
								Console.WriteLine("movie: " + NAME + " found");
								counter_Movie++;
							}

							//Create folder
							if ((new Microsoft.VisualBasic.Devices.ServerComputer()).FileSystem.DirectoryExists(folder) == false)
							{
								(new Microsoft.VisualBasic.Devices.ServerComputer()).FileSystem.CreateDirectory(folder);
							}

							//Create .strm file
							if ((new Microsoft.VisualBasic.Devices.ServerComputer()).FileSystem.FileExists(folder + Path.DirectorySeparatorChar + NAME + ".strm") == false)
							{
								(new Microsoft.VisualBasic.Devices.ServerComputer()).FileSystem.WriteAllText(folder + Path.DirectorySeparatorChar + NAME + ".strm", URL, false);
							}

						}
						else
						{
							continue;
						}
					}

					Console.WriteLine("");
					Console.WriteLine("");
					Console.WriteLine(counter_Movie + " movies found");
					Console.WriteLine(counter_Series + " series found");
					Console.WriteLine("");
					Console.WriteLine("");

					Console.WriteLine(creator);

				}
				else
				{
					Console.WriteLine("File not found - " + m3u8);
					Console.WriteLine("Type /? for help.");
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex.Message);
				Console.WriteLine("Error: " + ex.StackTrace);
				Console.ReadLine();
			}

		}

		public static string RemoveIllegalFileNameChars(string input, string replacement = "")
		{
			var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
			var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
			//Return r.Replace(input, replacement).Replace(".", "")
			return r.Replace(input, replacement); //don't want to remove period
		}

	}

}
