Imports StatisticalAnalysis.Preferences

Namespace Clustering

    Class DBScan
        Implements iClustering
        Public minRegion As Double = 3
        Public maxDistance As Double = 3

        Private _Profiles As List(Of Profile)
        Private _Clusters As List(Of HashSet(Of Profile))
        Private _Noise As HashSet(Of Profile)

        Public Sub Run(profiles As List(Of Profile), ByRef clusters As List(Of HashSet(Of Profile)), ByRef noise As HashSet(Of Profile)) Implements iClustering.Run
            _Profiles = profiles
            _Clusters = New List(Of HashSet(Of Profile))
            _Noise = New HashSet(Of Profile)
            For Each profile In _Profiles
                Dim cluster As New HashSet(Of Profile)
                ExpandCluster(profile, cluster)
                If cluster.Count > 0 Then _Clusters.Add(cluster)
            Next
            For Each profile In _Profiles
                If Not IsPartOfCluster(profile) Then _Noise.Add(profile)
            Next
            clusters = _Clusters
            noise = _Noise
        End Sub

        Private Function Region(cur As Profile) As HashSet(Of Profile)
            Dim result As New HashSet(Of Profile)
            For Each profile In _Profiles
                If cur.DistanceTo(profile) <= maxDistance Then result.Add(profile)
            Next
            Return result
        End Function

        Private Sub ExpandCluster(cur As Profile, cluster As HashSet(Of Profile))
            If IsPartOfCluster(cur) Then Exit Sub
            Dim regionPoints As HashSet(Of Profile) = Region(cur)
            If regionPoints.Count >= minRegion Then
                For Each r In regionPoints
                    If Not cluster.Contains(r) Then
                        cluster.Add(r)
                        ExpandCluster(r, cluster)
                    End If
                Next
            End If
        End Sub

        Private Function IsPartOfCluster(cur As Profile) As Boolean
            For Each cluster In _Clusters
                If cluster.Contains(cur) Then Return True
            Next
            Return False
        End Function

        Public Function Clone() As iClustering Implements iClustering.Clone
            Return New DBScan With {.minRegion = minRegion,
                                    .maxDistance = maxDistance}
        End Function
    End Class

End Namespace
