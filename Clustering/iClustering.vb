Imports StatisticalAnalysis.Preferences

Namespace Clustering

    Public Interface iClustering
        Sub Run(profiles As List(Of Profile), ByRef clusters As List(Of HashSet(Of Profile)), ByRef noise As HashSet(Of Profile))
        Function Clone() As iClustering
    End Interface

End Namespace