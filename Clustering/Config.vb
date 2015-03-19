Public Module Configuration
    Public Config As New List(Of iClustering)
    Private _Clusterers As New List(Of iClustering) From {New DBScan}
    Private _CurrentClusterer As iClustering

    Public Sub ReadConfiguration(file As IO.FileInfo)
        'Read file
        Dim parseMode As ParseMode = parseMode.None
        Dim line, name, value As String
        Dim reader As New IO.StreamReader(file.FullName)
        While Not reader.EndOfStream
            line = reader.ReadLine.Trim
            If line.First = "["c Then
                parseMode = parseMode.None
                For Each clusterer In _Clusterers
                    If line.Trim("[", "]").ToLower = clusterer.GetType.Name.ToLower Then
                        parseMode = parseMode.Clusterer
                        Config.Add(clusterer.Clone)
                        _CurrentClusterer = Config.Last
                    End If
                Next
            Else
                Select Case parseMode
                    Case Configuration.ParseMode.Clusterer
                        name = line.Split("=")(0).Trim
                        value = line.Split("=")(1).Trim
                        Dim field As Reflection.FieldInfo = _CurrentClusterer.GetType.GetField(name)
                        field.SetValue(_CurrentClusterer, CDbl(value))
                End Select
            End If
        End While
        reader.Close()
        reader.Dispose()
    End Sub

    Private Enum ParseMode As Byte
        None
        Clusterer
    End Enum
End Module
