Namespace Preferences

    Module EntryManager
        Private _EnumerationValues As New Dictionary(Of String, List(Of String))
        Private _EntryTypes As New Dictionary(Of String, EntryType)
        Private _EntryMins As New Dictionary(Of String, Double)
        Private _EntryMaxs As New Dictionary(Of String, Double)
        Private _EntryApps As New Dictionary(Of String, String)
        Private _EntryPrintName As New Dictionary(Of String, String)

        Public ReadOnly Property EntryMin(name As String) As Double
            Get
                If Not _EntryMins.ContainsKey(name) Then Return 0
                Return _EntryMins(name)
            End Get
        End Property

        Public ReadOnly Property EntryMax(name As String) As Double
            Get
                If Not _EntryMaxs.ContainsKey(name) Then Return 0
                Return _EntryMaxs(name)
            End Get
        End Property

        Public ReadOnly Property EnumerationSize(name As String) As Integer
            Get
                Return _EnumerationValues(name).Count
            End Get
        End Property

        Public ReadOnly Property EntryNames As String()
            Get
                Return _EntryTypes.Keys.ToArray
            End Get
        End Property

        Public ReadOnly Property EntryApp(name As String) As String
            Get
                If Not _EntryApps.ContainsKey(name) Then Return String.Empty
                Return _EntryApps(name)
            End Get
        End Property

        Public ReadOnly Property EntryPrintName(name As String) As String
            Get
                Return _EntryPrintName(name)
            End Get
        End Property

        Public Function ToEnumeration(name As String, value As Integer) As String
            Return _EnumerationValues(name)(value)
        End Function

        Public Function ToEntry(application As String, name As String, value As String) As Entry
            name = name.Trim(" ", vbTab, """").Replace("\\.", ".")
            Dim internalName As String = application & "||" & name
            _EntryApps(internalName) = application
            _EntryPrintName(internalName) = name
            Dim number As Double
            Dim entryType As EntryType = Preferences.EntryType.Undefined
            If Double.TryParse(value, number) Then
                If Not _EntryTypes.TryGetValue(internalName, entryType) Then
                    entryType = Preferences.EntryType.Number
                    _EntryTypes(internalName) = entryType
                End If
                If Not entryType = Preferences.EntryType.Number Then
                    Dim knownValues As List(Of String) = _EnumerationValues(internalName)
                    Throw New NotSupportedException("Profile entry type """ & internalName & """ is not registered as a number, but value passed was """ & number & """")
                End If
                Return New Entry(application, internalName, number)
            Else
                value = value.Trim(" ", vbTab, """").ToLower
                If Not _EntryTypes.TryGetValue(internalName, entryType) Then
                    _EntryTypes(internalName) = Preferences.EntryType.Enumeration
                    entryType = Preferences.EntryType.Enumeration
                End If
                If Not entryType = Preferences.EntryType.Enumeration Then
                    Dim knownValues As List(Of String) = _EnumerationValues(internalName)
                    Throw New NotSupportedException("Profile entry type """ & internalName & """ is not registered as a enumeration, but value passed was """ & value & """")
                End If
                Dim enumerationValues As List(Of String) = Nothing
                If Not _EnumerationValues.TryGetValue(internalName, enumerationValues) Then
                    enumerationValues = New List(Of String)
                    _EnumerationValues(internalName) = enumerationValues
                End If
                If Not enumerationValues.Contains(value) Then enumerationValues.Add(value)
                Return New Entry(application, internalName, enumerationValues.IndexOf(value))
            End If
        End Function

        Public Function IsEnumeration(entry As Entry) As Boolean
            Return IsEnumeration(entry.Name)
        End Function
        Public Function IsEnumeration(name As String) As Boolean
            Return _EntryTypes(name) = EntryType.Enumeration
        End Function

        Public Sub GetMinsAndMaxs(profiles As List(Of Profile))
            Dim value As Double
            For Each profile In profiles
                For Each pair In profile.PreferenceEntries
                    If IsEnumeration(pair.Key) Then Continue For
                    If (Not _EntryMins.TryGetValue(pair.Key, value)) OrElse value > pair.Value.Value Then
                        _EntryMins(pair.Key) = pair.Value.Value
                    End If
                    If (Not _EntryMaxs.TryGetValue(pair.Key, value)) OrElse value < pair.Value.Value Then
                        _EntryMaxs(pair.Key) = pair.Value.Value
                    End If
                Next
                For Each pair In profile.ContextEntries
                    If IsEnumeration(pair.Key) Then Continue For
                    If (Not _EntryMins.TryGetValue(pair.Key, value)) OrElse value > pair.Value.Value Then
                        _EntryMins(pair.Key) = pair.Value.Value
                    End If
                    If (Not _EntryMaxs.TryGetValue(pair.Key, value)) OrElse value < pair.Value.Value Then
                        _EntryMaxs(pair.Key) = pair.Value.Value
                    End If
                Next
            Next
        End Sub

        Public Function NormalizeNumber(entry As Entry) As Entry
            If IsEnumeration(entry) Then Return entry
            If _EntryMaxs(entry.Name) = _EntryMins(entry.Name) Then
                Return New Entry(entry.Application, entry.Name, 0)
            Else
                Return New Entry(entry.Application, entry.Name, (entry.Value - _EntryMins(entry.Name)) / (_EntryMaxs(entry.Name) - _EntryMins(entry.Name)))
            End If
        End Function

    End Module

    Public Enum EntryType
        Undefined
        Number
        Enumeration
    End Enum

End Namespace