﻿Module Main
    'Arguments
    Private Const _ArgConfigFile As String = "-c"
    Private Const _ArgProfilesDirectory As String = "-p"
    'Profiles
    Private _ConfigFile As IO.FileInfo
    Private _ProfilesDirectory As IO.DirectoryInfo
    Private _Profiles As New List(Of Profile)

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
        'Read Config
        ReadConfiguration(_ConfigFile)
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
        GetMinsAndMaxs(_Profiles)
        'Cluster
        Dim clusters As List(Of HashSet(Of Profile)) = Nothing
        Dim noise As HashSet(Of Profile) = Nothing
        For Each clusterer In Config
            clusterer.Run(_Profiles, clusters, noise)
        Next
        'Start Processing
        Dim generalized As New List(Of Profile)
        For Each c As HashSet(Of Profile) In clusters
            Dim center As Profile = Nothing
            Dim distances As SortedDictionary(Of Double, HashSet(Of Profile)) = Nothing
            FindCenter(c, center, distances)
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

    Private Sub WriteJavaScript(clusters As List(Of Profile))
        Dim writer As New IO.StreamWriter(_ProfilesDirectory.FullName & "\StatisticalMatchMakerData.js", False)
        writer.WriteLine("/*!")
        writer.WriteLine()
        writer.WriteLine("GPII/Cloud4all Statistical Matchmaker ")
        writer.WriteLine()
        writer.WriteLine("Copyright 2014 Hochschule der Medien (HdM) / Stuttgart Media University")
        writer.WriteLine()
        writer.WriteLine("Licensed under the New BSD License. You may not use this file except in")
        writer.WriteLine("compliance with this licence.")
        writer.WriteLine()
        writer.WriteLine("You may obtain a copy of the licence at")
        writer.WriteLine("https://github.com/REMEXLabs/GPII-Statistical-Matchmaker-Analysis/blob/master/LICENSE.txt")
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
        writer.WriteLine("stat.entryCount = " & EntryNames.Count & ";")

        'clusters
        writer.WriteLine("stat.clusters = [")
        For c = 0 To clusters.Count - 1
            Dim cluster As Profile = clusters(c)
            writer.WriteLine(vbTab & "{")
            'sort by application
            Dim prefsByApp As New Dictionary(Of String, HashSet(Of Entry))
            For n = 0 To EntryNames.Count - 1
                Dim entry As Entry = Nothing
                If cluster.PreferenceEntries.TryGetValue(EntryNames(n), entry) Then
                    Dim appPrefs As HashSet(Of Entry) = Nothing
                    If Not prefsByApp.TryGetValue(entry.Application, appPrefs) Then
                        appPrefs = New HashSet(Of Entry)
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
        For n = 0 To EntryNames.Count - 1
            Dim entryName As String = EntryNames(n)
            Dim appPrefs As List(Of PreferenceType) = Nothing
            If Not typesByApp.TryGetValue(EntryApp(entryName), appPrefs) Then
                appPrefs = New List(Of PreferenceType)
                typesByApp(EntryApp(entryName)) = appPrefs
            End If
            Dim newPrefType As New PreferenceType
            newPrefType.name = entryName
            newPrefType.IsEnum = IsEnumeration(entryName)
            newPrefType.Min = EntryMin(entryName)
            newPrefType.Max = EntryMax(entryName)
            appPrefs.Add(newPrefType)
        Next
        'print
        For Each pair In typesByApp
            writer.WriteLine(vbTab & """" & pair.Key & """: {")
            For n = 0 To pair.Value.Count - 1
                writer.WriteLine(vbTab & vbTab & """" & EntryPrintName(pair.Value(n).name) & """: {")
                If pair.Value(n).IsEnum Then
                    writer.WriteLine(vbTab & vbTab & vbTab & """isEnum"": true,")
                    writer.WriteLine(vbTab & vbTab & vbTab & """min"": 0,")
                    writer.WriteLine(vbTab & vbTab & vbTab & """max"": " & EnumerationSize(pair.Value(n).name))
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

    Private Function WriteJavaScriptEntry(entry As Entry) As String
        If IsEnumeration(entry) Then
            Dim enumValue As String = ToEnumeration(entry.Name, entry.Value)
            If Not (enumValue.ToLower = "true" Or enumValue.ToLower = "false") Then enumValue = """" & enumValue & """"
            Return """" & EntryPrintName(entry.Name) & """: " & enumValue & ""
        Else
            Return """" & EntryPrintName(entry.Name) & """: " & entry.Value & ""
        End If
    End Function

    Private Structure PreferenceType
        Public name As String
        Public IsEnum As Boolean
        Public Min, Max As Double
    End Structure

End Module
