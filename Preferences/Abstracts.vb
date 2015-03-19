Public Module Abstracts

    Public Sub FindCenter(cluster As HashSet(Of Profile), ByRef resultProfile As Profile, ByRef resultDistances As SortedDictionary(Of Double, HashSet(Of Profile)), Optional ByRef resultMeanDistance As Double = Double.PositiveInfinity)
        Dim curMeanDistance As Double
        Dim curDistances As SortedDictionary(Of Double, HashSet(Of Profile))
        Dim curDistancesSub As HashSet(Of Profile) = Nothing
        Dim curDistance As Double
        For Each p In cluster
            curMeanDistance = 0
            curDistances = New SortedDictionary(Of Double, HashSet(Of Profile))
            For Each c In cluster
                curDistance = p.DistanceTo(c)
                If Not curDistances.TryGetValue(curDistance, curDistancesSub) Then
                    curDistancesSub = New HashSet(Of Profile)
                    curDistances(curDistance) = curDistancesSub
                End If
                curDistancesSub.Add(c)
                curMeanDistance += curDistance
            Next
            curMeanDistance /= cluster.Count
            If curMeanDistance < resultMeanDistance Then
                resultProfile = p
                resultMeanDistance = curMeanDistance
                resultDistances = curDistances
            End If
        Next
    End Sub

    Public Function GeneralizeProfile(center As Profile, clusterDistances As SortedDictionary(Of Double, HashSet(Of Profile)), profiles As List(Of Profile)) As Profile
        Dim result As New Profile
        Dim centerUser, centerOs As Integer
        centerUser = center.ContextEntries("context||user").Value
        centerOs = center.ContextEntries("context||os").Value
        For Each p In profiles
            If p.ContextEntries("context||user").Value = centerUser And p.ContextEntries("context||os").Value <> centerOs Then
                For Each pair In p.PreferenceEntries
                    result.PreferenceEntries(pair.Key) = pair.Value
                Next
            End If
        Next
        For Each pair In center.PreferenceEntries
            result.PreferenceEntries(pair.Key) = pair.Value
        Next
        'enter missing stuff
        Dim allDistances As New SortedDictionary(Of Double, HashSet(Of Profile))
        Dim allDistancesSub As HashSet(Of Profile) = Nothing
        Dim curDistance As Double
        For Each p In profiles
            curDistance = center.DistanceTo(p)
            If Not allDistances.TryGetValue(curDistance, allDistancesSub) Then
                allDistancesSub = New HashSet(Of Profile)
                allDistances(curDistance) = allDistancesSub
            End If
            allDistancesSub.Add(p)
        Next
        For Each k In EntryNames
            If result.PreferenceEntries.ContainsKey(k) Then Continue For
            Dim found As Boolean = False
            For Each c In clusterDistances
                For Each cp In c.Value
                    If Not cp.PreferenceEntries.ContainsKey(k) Then Continue For
                    result.PreferenceEntries(k) = cp.PreferenceEntries(k)
                    found = True
                    Exit For
                Next
                If found Then Exit For
            Next
            If found Then Continue For
            For Each c In allDistances
                For Each cp In c.Value
                    If Not cp.PreferenceEntries.ContainsKey(k) Then Continue For
                    result.PreferenceEntries(k) = cp.PreferenceEntries(k)
                    found = True
                    Exit For
                Next
                If found Then Exit For
            Next
        Next
        Return result
    End Function

End Module
