Imports System.IO
Imports System.Text.RegularExpressions

Module Module1

    Sub Main(args As String())
        Try
            Dim creator As String = "Original code by TimTester, forked by trix77 for N1/SB/SE"
            Console.WriteLine(creator)

            Dim counter_Movie As Integer = 0
            Dim counter_Series As Integer = 0

            'Get location, appName and version
            Dim location As String = Environment.GetCommandLineArgs()(0)
            Dim appName As String = Path.GetFileName(location)
            Dim version As String = System.Reflection.Assembly.GetExecutingAssembly.GetName.Version.ToString()

            'm3u8
            Dim m3u8 As String
            Dim param2 As String
            param2 = ""

            'Check args
            If args.Length > 0 Then
                m3u8 = args(0)
                If args(0) = "/?" Then
                    Console.WriteLine("Creates STRM-files from M3U8-file.")
                    Console.WriteLine("")
                    Console.WriteLine(appName & " [drive:][path]filename [/D]")
                    Console.WriteLine("")
                    Console.WriteLine("  filename    Specifies the M3U8-file to be processed.")
                    Console.WriteLine("")
                    Console.WriteLine("  /D          Delete previously created directories.")
                    Console.WriteLine("  /V          Version information.")
                    Console.WriteLine("")
                    Console.WriteLine("Examples:")
                    Console.WriteLine(appName & " original.m3u8")
                    Console.WriteLine(appName & " original.m3u8 /D")
                    Exit Sub
                ElseIf args(0).ToLower = "/d" Then
                    Console.WriteLine("Invalid option: " & args(0) & " cannot be used without filename.")
                    Console.WriteLine("Type /? for help")
                    Exit Sub
                ElseIf args(0).ToLower = "/v" Then
                    Console.WriteLine("Filename: " & appName)
                    Console.WriteLine("Version: " & version)
                    Exit Sub
                ElseIf args(0).StartsWith("/") Then
                    Console.WriteLine("Invalid switch - " & args(0))
                    Console.WriteLine("Type /? for help")
                    Exit Sub
                ElseIf args(0).ToLower.Contains("m3u") Then
                    If My.Computer.FileSystem.FileExists(m3u8) = False Then
                        Console.WriteLine("File not found - " & args(0))
                        Console.WriteLine("Type /? for help")
                        Exit Sub
                    End If
                Else
                    Console.WriteLine("File not found - " & args(0))
                    Console.WriteLine("Type /? for help")
                    Exit Sub
                End If
            Else
                Console.WriteLine("No file to process")
                Console.WriteLine("Type /? for help")
                Exit Sub
            End If
            If args.Length > 1 Then
                param2 = args(1)
                If param2.ToLower = "/d" Then
                Else
                    Console.WriteLine("Invalid switch - " & param2)
                    Console.WriteLine("Type /? for help")
                    Exit Sub
                End If
            End If

            'Read file
            Dim Main_folder As String = System.AppDomain.CurrentDomain.BaseDirectory
            Dim folder_Movies As String = Main_folder + "\Movies"
            Dim folder_Series As String = Main_folder + "\Series"

            'Delete previously created directories
            If param2.ToLower = "/d" Then
                Console.WriteLine("Deleting previously created directories...")
                Reader.ReadLine(5000)
                If My.Computer.FileSystem.DirectoryExists(folder_Movies) Then
                    My.Computer.FileSystem.DeleteDirectory(folder_Movies, FileIO.DeleteDirectoryOption.DeleteAllContents)
                End If
                If My.Computer.FileSystem.DirectoryExists(folder_Series) Then
                    My.Computer.FileSystem.DeleteDirectory(folder_Series, FileIO.DeleteDirectoryOption.DeleteAllContents)
                End If
            Else
                Console.WriteLine("Info: /D switch not specified.")
                Console.WriteLine("Info: No deletion of previously created directories will occur.")
                Reader.ReadLine(5000)
            End If

            'Create directories
            My.Computer.FileSystem.CreateDirectory(folder_Movies)
            My.Computer.FileSystem.CreateDirectory(folder_Series)

            'M3U
            If My.Computer.FileSystem.FileExists(m3u8) = True Then
                Dim FileText As String = My.Computer.FileSystem.ReadAllText(m3u8)

                Dim FileText_lines As String() = FileText.Split(vbLf)
                For index = 1 To FileText_lines.Length - 1
                    Dim line1 As String = FileText_lines(index)
                    If (line1.ToLower.StartsWith("#extinf:") AndAlso
                        line1.ToLower.Contains("series: barn") Or 'Only get these series groups
                        line1.ToLower.Contains("series: english [multi-sub]") Or
                        line1.ToLower.Contains("series: nordic") Or
                        (line1.ToLower.Contains("vod:") AndAlso 'Get all vod movie groups but not these:
                        Not line1.ToLower.Contains("vod: 4k movies [multi-sub] [only on 4k devices]") And
                        Not line1.ToLower.Contains("vod: albania") And
                        Not line1.ToLower.Contains("vod: arabic") And
                        Not line1.ToLower.Contains("vod: crtani filmovi [ex-yu]") And
                        Not line1.ToLower.Contains("vod: danske - norska - suomalainen film") And
                        Not line1.ToLower.Contains("vod: english movies [arabic subtitle]") And
                        Not line1.ToLower.Contains("vod: events") And
                        Not line1.ToLower.Contains("vod: germany") And
                        Not line1.ToLower.Contains("vod: india") And
                        Not line1.ToLower.Contains("vod: iran") And
                        Not line1.ToLower.Contains("vod: polska") And
                        Not line1.ToLower.Contains("vod: turkey") And
                        Not line1.ToLower.Contains("vod: vietnam") And
                        Not line1.ToLower.Contains("vod: ex-yu movies"))) Then

                        Dim line2 As String = FileText_lines(index + 1)

                        Dim NAME As String = ""
                        Dim URL As String = ""

                        Dim Start_Name As Integer = line1.IndexOf("tvg-name=") + 10
                        Dim Start_Logo As Integer = line1.IndexOf("tvg-logo=") + 10
                        'Name
                        NAME = line1.Substring(Start_Name, Start_Logo - Start_Name - 12).Replace("&", "")
                        'URL
                        URL = line2.Replace(vbCrLf, "").Replace(vbCr, "")
                        URL = URL

                        '############################## FILTER START ##############################

                        'Replace with dash
                        NAME = NAME.Replace(":", "-")
                        NAME = NAME.Replace("/", "-")
                        NAME = NAME.Replace("|", "-")

                        'Replace with space
                        'NAME = NAME.Replace(".", " ")

                        'Replace with nothing
                        NAME = NAME.Replace(",", "")

                        'Call the RemoveIllegalFileNameChars function
                        NAME = RemoveIllegalFileNameChars(NAME).Trim()

                        'Replace without name with NONAME
                        NAME = Regex.Replace(NAME, "^$", "NONAME")

                        'Remove erroneous spaces
                        NAME = NAME.Replace("( ", "(")
                        NAME = NAME.Replace(" )", ")")
                        NAME = NAME.Replace("[ ", "[")
                        NAME = NAME.Replace(" ]", "]")

                        'Add spaces
                        NAME = NAME.Replace(")", ") ")
                        NAME = NAME.Replace("(", " (")
                        NAME = NAME.Replace("]", "] ")
                        NAME = NAME.Replace("[", " [")

                        'Remove doubles
                        NAME = Regex.Replace(NAME, "\[{2,}", "\[")
                        NAME = Regex.Replace(NAME, "\]{2,}", "\]")
                        NAME = Regex.Replace(NAME, "\({2,}", "\(")
                        NAME = Regex.Replace(NAME, "\){2,}", "\)")

                        'Remove [PRE] and misspellings thereof
                        NAME = Regex.Replace(NAME, "\[(P|R)(R|F)E\]", "", RegexOptions.IgnoreCase)

                        'Remove [Multi-Audio]
                        NAME = Regex.Replace(NAME, "\[(Mu.*|Dual)(-|\s)Audio\]", "", RegexOptions.IgnoreCase)

                        'Remove [Multi-Subs]
                        NAME = Regex.Replace(NAME, "\[Mu.*(-|\s)Sub(|s)\]", "", RegexOptions.IgnoreCase)

                        'Remove [Nordic]
                        NAME = Regex.Replace(NAME, "( |\[)nordic\]", "", RegexOptions.IgnoreCase)

                        'Remove [Only On 4K Devices]
                        NAME = Regex.Replace(NAME, "\[Only On 4K Devices\]", "", RegexOptions.IgnoreCase)

                        'Remove 4k
                        'NAME = Regex.Replace(NAME, "4k", "", RegexOptions.IgnoreCase)

                        'Remove stuff
                        NAME = Regex.Replace(NAME, "\[K(|I)DS\]", "", RegexOptions.IgnoreCase)
                        NAME = Regex.Replace(NAME, "\[SE\]", "", RegexOptions.IgnoreCase)
                        NAME = Regex.Replace(NAME, "\[IMDB\]", "", RegexOptions.IgnoreCase)
                        NAME = Regex.Replace(NAME, "\[IMDB\]", "", RegexOptions.IgnoreCase)

                        'Remove empty parentheses and brackets
                        NAME = NAME.Replace("()", "")
                        NAME = NAME.Replace("[]", "")

                        'Replace "9 - 1 - 1", "9 - 11", "9- 11", "9 -11" with no space in between dash
                        NAME = Regex.Replace(NAME, "(\d)(\s-|-\s|\s-\s)(\d)", "$1-$3")
                        'Doing it twice. Maybe do a while loop?
                        NAME = Regex.Replace(NAME, "(\d)(\s-|-\s|\s-\s)(\d)", "$1-$3")

                        'Replace "Alien 5- Title", "Alien 5 -Title", "Alien 5-Title" with space before and after dash
                        NAME = Regex.Replace(NAME, "(\d)(-\s|\s-|-)([a-zA-Z])", "$1 - $3")
                        NAME = Regex.Replace(NAME, "(\d)(-\s|\s-|-)([a-zA-Z])", "$1 - $3")

                        'Replace "5- Title", "5 -Title", "5-Title" with no space in between dash
                        NAME = Regex.Replace(NAME, "^(\d)(\s-\s)([a-zA-Z])", "$1-$3")
                        NAME = Regex.Replace(NAME, "^(\d)(\s-\s)([a-zA-Z])", "$1-$3")

                        'Replace "Alien- 5", "Alien -5", "Alien-5" with space before and after dash
                        NAME = Regex.Replace(NAME, "([a-zA-Z])(-\s|\s-|-)(\d)", "$1 - $3")
                        NAME = Regex.Replace(NAME, "([a-zA-Z])(-\s|\s-|-)(\d)", "$1 - $3")

                        'Replace "Movie- Title", "Movie -Title", "Movie-Title" with space before and after dash - but only on words that has two or more letters in them
                        NAME = Regex.Replace(NAME, "([a-zA-Z]{2,})(-\s|\s-|-)([a-zA-Z{2,}])", "$1 - $3")
                        NAME = Regex.Replace(NAME, "([a-zA-Z]{2,})(-\s|\s-|-)([a-zA-Z{2,}])", "$1 - $3")

                        'Replace space between Sxx and Exx
                        NAME = Regex.Replace(NAME, "(s\d+)\s(e\d+)", "$1$2", RegexOptions.IgnoreCase)

                        'Add space before dash only if space already after
                        'NAME = NAME.Replace("- ", " - ")

                        'Replace more than one space with one space
                        NAME = Regex.Replace(NAME, "\s{2,}", " ")

                        'Replace brackets with parentheses
                        NAME = NAME.Replace("[", "(")
                        NAME = NAME.Replace("]", ")")

                        'Remove leading and trailing period
                        NAME = NAME.Trim(".")

                        'Remove leading and trailing space
                        NAME = NAME.Trim()

                        '############################## FILTER END ##############################

                        'Extract group from URL
                        Dim GROUP As String = ""
                        If URL.ToLower.Contains("/series") Then
                            GROUP = "series"
                        End If
                        If URL.ToLower.Contains("/movie") Then
                            GROUP = "movie"
                        End If

                        'Check if series or movie
                        Dim folder As String = ""
                        If GROUP.ToLower.Contains("series") Then

                            'Combine series
                            folder = folder_Series & "\" & NAME
                            'folder = Regex.Replace(folder, "s(\d+) e(\d+)", "", RegexOptions.IgnoreCase).Trim
                            'removed non-used capture groups and changed to the new sxxexx after replace above
                            folder = Regex.Replace(folder, "s\d+e\d+", "", RegexOptions.IgnoreCase).Trim
                            Console.WriteLine("series: " + NAME + " found")
                            counter_Series = counter_Series + 1
                        End If
                        If GROUP.ToLower.Contains("movie") Then
                            folder = folder_Movies & "\" & NAME
                            Console.WriteLine("movie: " + NAME + " found")
                            counter_Movie = counter_Movie + 1
                        End If

                        'Create folder
                        If My.Computer.FileSystem.DirectoryExists(folder) = False Then
                            My.Computer.FileSystem.CreateDirectory(folder)
                        End If

                        'Create .strm file
                        If My.Computer.FileSystem.FileExists(folder & "\" & NAME & ".strm") = False Then
                            My.Computer.FileSystem.WriteAllText(folder & "\" & NAME & ".strm", URL, False)
                        End If

                    Else
                        Continue For
                    End If
                Next

                Console.WriteLine("")
                Console.WriteLine("")
                Console.WriteLine(counter_Movie & " movies found")
                Console.WriteLine(counter_Series & " series found")
                Console.WriteLine("")
                Console.WriteLine("")

                Console.WriteLine(creator)

            Else
                Console.WriteLine("File not found - " & m3u8)
                Console.WriteLine("Type /? for help.")
            End If

        Catch ex As Exception
            Console.WriteLine("Error: " & ex.Message)
            Console.WriteLine("Error: " & ex.StackTrace)
            Console.ReadLine()
        End Try

    End Sub

    Public Function RemoveIllegalFileNameChars(input As String, Optional replacement As String = "") As String
        Dim regexSearch = New String(Path.GetInvalidFileNameChars()) & New String(Path.GetInvalidPathChars())
        Dim r = New Regex(String.Format("[{0}]", Regex.Escape(regexSearch)))
        'Return r.Replace(input, replacement).Replace(".", "")
        Return r.Replace(input, replacement) 'don't want to remove period
    End Function

End Module
