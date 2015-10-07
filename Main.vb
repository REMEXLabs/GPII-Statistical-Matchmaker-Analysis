Module Main
    'Arguments
    Private Const _ArgConfigFile As String = "-c"
    Private Const _ArgProfilesDirectory As String = "-p"
    Private Const _ArgOutputFile As String = "-o"
    'Profiles
    Private _ConfigFile As IO.FileInfo
    Private _OutputFile As IO.FileInfo
    Private _ProfilesDirectory As IO.DirectoryInfo
    Private _Profiles As New List(Of Preferences.Profile)

    Sub Main()
        'Parse arguments
        Dim args() As String = System.Environment.GetCommandLineArgs
        Dim i As Integer = 1
        While i < args.Count
            Select Case args(i).Trim
                Case _ArgProfilesDirectory
                    If i < args.Count - 1 Then _ProfilesDirectory = New IO.DirectoryInfo(args(i + 1).TrimEnd("\"))
                Case _ArgConfigFile
                    If i < args.Count - 1 Then _ConfigFile = New IO.FileInfo(args(i + 1).Trim)
                Case _ArgOutputFile
                    If i < args.Count - 1 Then _OutputFile = New IO.FileInfo(args(i + 1).Trim)
            End Select
            i += 2
        End While
        'Check directories and config
        If _ProfilesDirectory Is Nothing OrElse Not _ProfilesDirectory.Exists Then
            _ProfilesDirectory = New IO.DirectoryInfo(IO.Directory.GetCurrentDirectory.TrimEnd("\") & "\GPII-Statistical-Matchmaker-Data")
            Console.WriteLine("Could not find profiles directory, reverted to :'" & _ProfilesDirectory.FullName & "'")
        End If
        If _ConfigFile Is Nothing OrElse Not _ConfigFile.Exists Then
            _ConfigFile = New IO.FileInfo(IO.Directory.GetCurrentDirectory.TrimEnd("\") & "\GPII-Statistical-Matchmaker-Data\config.ini")
            Console.WriteLine("Could not find config file, reverted to :'" & _ConfigFile.FullName & "'")
        End If
        If _OutputFile Is Nothing Then
            _OutputFile = New IO.FileInfo(_ProfilesDirectory.FullName & "\StatisticalMatchMakerData.js")
            Console.WriteLine("Output file not specified, reverted to :'" & _OutputFile.FullName & "'")
        End If
        'Check if everything is there
        If Not _ProfilesDirectory.Exists Then
            Console.WriteLine("CRITICAL: Profiles directory (""" & _ProfilesDirectory.FullName & """) does not exist.")
            Exit Sub
        End If
        If Not _ConfigFile.Exists Then
            Console.WriteLine("CRITICAL: Config File (""" & _ConfigFile.FullName & """) does not exist.")
            Exit Sub
        End If
        'Read Config
        Clustering.ReadConfiguration(_ConfigFile)
        'Preprocess Profiles
        Dim generatedDirectory As New IO.DirectoryInfo(_ProfilesDirectory.FullName & "\generated")
        If Not generatedDirectory.Exists Then generatedDirectory.Create()
        PreprocessFrom(_ProfilesDirectory, generatedDirectory)
        'Read Profiles
        If _ConfigFile Is Nothing OrElse Not _ConfigFile.Exists Then Throw New NotSupportedException("Config File not set.")
        If _ProfilesDirectory IsNot Nothing AndAlso _ProfilesDirectory.Exists Then
            ReadProfilesFrom(_ProfilesDirectory)
        Else
            Throw New NotSupportedException("Preference Directory not set.")
        End If
        Console.WriteLine("--------")
        Console.WriteLine("Finished loading " & _Profiles.Count & " profiles.")
        Console.WriteLine("--------")
        Console.WriteLine("Analyzing preferences...")
        Preferences.GetMinsAndMaxs(_Profiles)
        Console.WriteLine("--------")
        Console.WriteLine("Clustering...")
        'Cluster
        Dim clusters As List(Of HashSet(Of Preferences.Profile)) = Nothing
        Dim noise As HashSet(Of Preferences.Profile) = Nothing
        For Each clusterer In Clustering.Config
            clusterer.Run(_Profiles, clusters, noise)
        Next
        'Start Processing
        Console.WriteLine("--------")
        Console.WriteLine("Reducing clusters...")
        Dim generalized As New List(Of Preferences.Profile)
        For Each c As HashSet(Of Preferences.Profile) In clusters
            Dim center As Preferences.Profile = Nothing
            Dim distances As SortedDictionary(Of Double, HashSet(Of Preferences.Profile)) = Nothing
            Preferences.FindCenter(c, center, distances)
            'generalized.Add(Preferences.GeneralizeProfile(center, distances, _Profiles))
            generalized.Add(center)
        Next
        'Write JS
        WriteJavaScript(generalized)
    End Sub

    Private Sub PreprocessFrom(directory As IO.DirectoryInfo, generatedDirectory As IO.DirectoryInfo)
        If directory Is Nothing Then Exit Sub
        If Not directory.Exists Then Exit Sub
        For Each containedFile In directory.GetFiles
            If containedFile.Extension = ".ini" And containedFile.Name.Contains(".gen") Then Parser.PreprocessProfile(containedFile, generatedDirectory)
        Next
        For Each containedDirectory In directory.GetDirectories
            PreprocessFrom(containedDirectory, generatedDirectory)
        Next
    End Sub

    Private Sub ReadProfilesFrom(directory As IO.DirectoryInfo)
        If directory Is Nothing Then Exit Sub
        If Not directory.Exists Then Exit Sub
        For Each containedFile In directory.GetFiles
            If containedFile.Extension = ".ini" And Not containedFile.Name.Contains(".gen") Then Parser.ReadProfile(containedFile, _Profiles)
        Next
        For Each containedDirectory In directory.GetDirectories
            ReadProfilesFrom(containedDirectory)
        Next
    End Sub

    Private Sub WriteJavaScript(clusters As List(Of Preferences.Profile))
        If Not _OutputFile.Directory.Exists Then
            _OutputFile.Directory.Create()
        End If
        Dim writer As New IO.StreamWriter(_OutputFile.FullName, False)
        writer.WriteLine("/*!")
        writer.WriteLine()
        writer.WriteLine("GPII/Cloud4all Statistical Matchmaker ")
        writer.WriteLine()
        writer.WriteLine("Copyright 2012-2015 Hochschule der Medien (HdM) / Stuttgart Media University")
        writer.WriteLine()
        writer.WriteLine("Licensed under the New BSD License. You may not use this file except in")
        writer.WriteLine("compliance with this licence.")
        writer.WriteLine()
        writer.WriteLine("You may obtain a copy of the licence at")
        writer.WriteLine("https://github.com/REMEXLabs/GPII-Statistical-Matchmaker/blob/master/LICENSE.txt")
        writer.WriteLine()
        writer.WriteLine("The research leading to these results has received funding from")
        writer.WriteLine("the European Union's Seventh Framework Programme (FP7/2007-2013)")
        writer.WriteLine("under grant agreement no. 289016.")
        writer.WriteLine("*/")
        writer.WriteLine()
        writer.WriteLine("//Generated " & Now.ToString("dd MMM HH:mm:ss"))
        writer.WriteLine("var fluid = fluid || require(""universal"");")
        writer.WriteLine("var stat = fluid.registerNamespace(""gpii.matchMaker.statistical"");")

        'entry count
        writer.WriteLine("stat.entryCount = " & Preferences.EntryNames.Count & ";")

        'clusters
        writer.WriteLine("stat.clusters = [")
        For c = 0 To clusters.Count - 1
            Dim cluster As Preferences.Profile = clusters(c)
            If cluster.PreferenceEntries.Count = 0 Then Continue For
            writer.WriteLine(vbTab & "{")
            'sort by application
            Dim prefsByApp As New Dictionary(Of String, HashSet(Of Preferences.Entry))
            For n = 0 To Preferences.EntryNames.Count - 1
                Dim entry As Preferences.Entry = Nothing
                If cluster.PreferenceEntries.TryGetValue(Preferences.EntryNames(n), entry) Then
                    Dim appPrefs As HashSet(Of Preferences.Entry) = Nothing
                    If Not prefsByApp.TryGetValue(entry.Application, appPrefs) Then
                        appPrefs = New HashSet(Of Preferences.Entry)
                        prefsByApp(entry.Application) = appPrefs
                    End If
                    appPrefs.Add(entry)
                End If
            Next
            'print
            For Each pair In prefsByApp
                writer.WriteLine(vbTab & vbTab & """" & pair.Key & """: {")
                For n = 0 To pair.Value.Count - 1
                    If n < pair.Value.Count - 1 Then
                        writer.WriteLine(vbTab & vbTab & vbTab & WriteJavaScriptEntry(pair.Value(n)) & ",")
                    Else
                        writer.WriteLine(vbTab & vbTab & vbTab & WriteJavaScriptEntry(pair.Value(n)))
                    End If
                Next
                If prefsByApp.Last.Key = pair.Key Then
                    writer.WriteLine(vbTab & vbTab & "}")
                Else
                    writer.WriteLine(vbTab & vbTab & "},")
                End If
            Next
            If c < clusters.Count - 1 Then writer.WriteLine(vbTab & "},") Else writer.WriteLine(vbTab & "}")
        Next
        writer.WriteLine("];")

        'properties
        writer.WriteLine("stat.preferenceTypes = {")
        Dim typesByApp As New Dictionary(Of String, List(Of PreferenceType))
        For n = 0 To Preferences.EntryNames.Count - 1
            Dim entryName As String = Preferences.EntryNames(n)
            Dim appPrefs As List(Of PreferenceType) = Nothing
            If Not typesByApp.TryGetValue(Preferences.EntryApp(entryName), appPrefs) Then
                appPrefs = New List(Of PreferenceType)
                typesByApp(Preferences.EntryApp(entryName)) = appPrefs
            End If
            Dim newPrefType As New PreferenceType
            newPrefType.name = entryName
            newPrefType.IsEnum = Preferences.IsEnumeration(entryName)
            newPrefType.Min = Preferences.EntryMin(entryName)
            newPrefType.Max = Preferences.EntryMax(entryName)
            appPrefs.Add(newPrefType)
        Next
        'print
        For Each pair In typesByApp
            writer.WriteLine(vbTab & """" & pair.Key & """: {")
            For n = 0 To pair.Value.Count - 1
                writer.WriteLine(vbTab & vbTab & """" & Preferences.EntryPrintName(pair.Value(n).name) & """: {")
                If pair.Value(n).IsEnum Then
                    writer.WriteLine(vbTab & vbTab & vbTab & """isEnum"": true,")
                    writer.WriteLine(vbTab & vbTab & vbTab & """min"": 0,")
                    writer.WriteLine(vbTab & vbTab & vbTab & """max"": " & Preferences.EnumerationSize(pair.Value(n).name))
                Else
                    writer.WriteLine(vbTab & vbTab & vbTab & """isEnum"": false,")
                    writer.WriteLine(vbTab & vbTab & vbTab & """min"": " & pair.Value(n).Min & ",")
                    writer.WriteLine(vbTab & vbTab & vbTab & """max"": " & pair.Value(n).Max)
                End If
                If n < pair.Value.Count - 1 Then
                    writer.WriteLine(vbTab & vbTab & "},")
                Else
                    writer.WriteLine(vbTab & vbTab & "}")
                End If
            Next
            If typesByApp.Last.Key = pair.Key Then
                writer.WriteLine(vbTab & "}")
            Else
                writer.WriteLine(vbTab & "},")
            End If
        Next
        writer.WriteLine("};")
        writer.Close()
    End Sub

    Private Function WriteJavaScriptEntry(entry As Preferences.Entry) As String
        If Preferences.IsEnumeration(entry) Then
            Dim enumValue As String = Preferences.ToEnumeration(entry.Name, entry.Value).Trim(",", """", " ")
            If Not (enumValue.ToLower = "true" Or enumValue.ToLower = "false" Or enumValue.First = "["c Or enumValue.First = "{"c) Then enumValue = """" & enumValue & """"
            Return """" & Preferences.EntryPrintName(entry.Name) & """: " & enumValue & ""
        Else
            Return """" & Preferences.EntryPrintName(entry.Name) & """: " & entry.Value & ""
        End If
    End Function

    Private Structure PreferenceType
        Public name As String
        Public IsEnum As Boolean
        Public Min, Max As Double
    End Structure

End Module
