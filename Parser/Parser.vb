Module Parser
    Private Const _TokenContext As String = "[context]"
    Private Const _TokenPreferences As String = "[preferences]"

    Public Sub ReadProfile(file As IO.FileInfo, profiles As List(Of Preferences.Profile))
        Dim profile As New Preferences.Profile(file)
        profiles.Add(profile)
        'Read file
        Dim parseMode As ParseMode = parseMode.None
        Dim line, name, value As String
        Dim application As String = String.Empty
        Dim reader As New IO.StreamReader(file.FullName)
        While Not reader.EndOfStream
            line = reader.ReadLine.Trim
            'Remove comments
            Dim commentStart As Integer = line.IndexOf(";")
            If commentStart >= 0 Then line = line.Remove(commentStart)
            'Parse line
            Select Case line.ToLower
                Case _TokenContext
                    parseMode = Parser.ParseMode.Context
                Case _TokenPreferences
                    parseMode = Parser.ParseMode.Preferences
                    application = String.Empty
                Case Else
                    'Splitting
                    If line.Contains("=") Then
                        name = line.Split("=")(0).Trim
                        value = line.Split("=")(1).Trim
                    ElseIf line.Contains(":") Then
                        name = line.Split(":")(0).Trim
                        value = line.Split(":")(1).Trim
                    Else
                        Continue While
                    End If
                    'Parsing
                    If name = "app" Then
                        application = value.Trim("""")
                    Else
                        Select Case parseMode
                            Case Parser.ParseMode.Context
                                Dim newEntry As Preferences.Entry = Preferences.ToEntry("context", name, value)
                                profile.ContextEntries(newEntry.Name) = newEntry
                            Case Parser.ParseMode.Preferences
                                Dim newEntry As Preferences.Entry = Preferences.ToEntry(application, name, value)
                                profile.PreferenceEntries(newEntry.Name) = newEntry
                        End Select
                    End If
            End Select
        End While
        reader.Close()
        reader.Dispose()
    End Sub

    Public Sub PreprocessProfile(file As IO.FileInfo, output As IO.DirectoryInfo)
        Dim reader As New IO.StreamReader(file.FullName)
        Dim line As String
        Dim steps As Integer = 0
        Dim result As New Text.StringBuilder
        Dim vars As New Dictionary(Of String, PreprocessorVars)
        While Not reader.EndOfStream
            line = reader.ReadLine.Trim
            If String.IsNullOrEmpty(line) Then Continue While
            If line.First = "|"c And line.Last = "|"c Then
                line = line.Trim("|")
                Dim split As String() = line.Split("="c)
                If split.Count = 2 Then
                    Dim key As String = split.First.Trim
                    Dim value As String = split.Last.Trim
                    If key = "steps" Then
                        Integer.TryParse(value, steps)
                    Else
                        Dim varParams As String() = value.Split(";"c)
                        If varParams.Count = 2 Then
                            Dim start As Double = 0
                            Dim stepInc As Double = 0
                            Double.TryParse(varParams.First, start)
                            Double.TryParse(varParams.Last, stepInc)
                            vars("|" & key & "|") = New PreprocessorVars With {.start = start, .stepInc = stepInc}
                        End If
                    End If
                End If
            Else
                result.Append(line)
                result.Append(vbNewLine)
            End If
        End While
        reader.Close()
        reader.Dispose()

        Dim resultContent As String = result.ToString
        For i As Integer = 0 To steps - 1
            Dim curResult As String = resultContent
            For Each var In vars
                curResult = curResult.Replace(var.Key, var.Value.start + var.Value.stepInc * i)
            Next
            Dim writer As New IO.StreamWriter(output.FullName & "\" & file.Name.Split(".").First & "_" & i & ".ini", False)
            writer.Write(curResult)
            writer.Close()
            writer.Dispose()
        Next
    End Sub

    Private Structure PreprocessorVars
        Public start As Double
        Public stepInc As Double
    End Structure

    Private Enum ParseMode As Byte
        None
        Context
        Preferences
    End Enum
End Module
