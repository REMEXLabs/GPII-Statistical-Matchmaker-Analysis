Public Class Profile
    Private _IsAbstract As Boolean = True
    Private _File As IO.FileInfo
    Public ContextEntries As New Dictionary(Of String, Entry)
    Public PreferenceEntries As New Dictionary(Of String, Entry)

    Public ReadOnly Property IsAbstract As Boolean
        Get
            Return _IsAbstract
        End Get
    End Property

    Public ReadOnly Property File As IO.FileInfo
        Get
            Return _File
        End Get
    End Property

    Public Sub New()
        _IsAbstract = True
    End Sub

    Public Sub New(file As IO.FileInfo)
        _IsAbstract = False
        _File = file
    End Sub

    'Distance Functions

    Public Function DistanceTo(other As Profile) As Double
        Return EuclideanDistanceTo(other)
    End Function

    Private Const NoiseReduction As Double = 0.2
    Private Function EuclideanDistanceTo(other As Profile) As Double
        Dim result As Double
        Dim difference As Double
        For Each pair In PreferenceEntries
            difference = 0
            If other.PreferenceEntries.ContainsKey(pair.Key) Then
                If IsEnumeration(pair.Key) Then
                    If PreferenceEntries(pair.Key).Value <> other.PreferenceEntries(pair.Key).Value Then difference = 1 / EnumerationSize(pair.Key)
                Else
                    difference = Math.Abs(other.PreferenceEntries(pair.Key).NormalizedValue - PreferenceEntries(pair.Key).NormalizedValue)
                End If
            Else
                If IsEnumeration(pair.Key) Then
                    difference = 1 / EnumerationSize(pair.Key)
                Else
                    difference = Math.Abs(0.5 - PreferenceEntries(pair.Key).NormalizedValue)
                End If
                difference *= NoiseReduction
            End If
            result += difference
        Next
        For Each pair In other.PreferenceEntries
            difference = 0
            If Not PreferenceEntries.ContainsKey(pair.Key) Then
                If IsEnumeration(pair.Key) Then
                    difference = 1 / EnumerationSize(pair.Key)
                Else
                    difference = Math.Abs(other.PreferenceEntries(pair.Key).NormalizedValue - 0.5)
                End If
                difference *= NoiseReduction
            End If
            result += difference
        Next
        Return result
    End Function

    'Debug

    Public Overrides Function ToString() As String
        Return _File.FullName
    End Function

End Class
