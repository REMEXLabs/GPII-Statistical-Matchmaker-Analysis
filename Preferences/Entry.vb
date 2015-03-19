Public Structure Entry
    Private _Name As String
    Private _Application As String
    Private _Value As Double

    Public ReadOnly Property Name As String
        Get
            Return _Name
        End Get
    End Property

    Public ReadOnly Property Application As String
        Get
            Return _Application
        End Get
    End Property

    Public ReadOnly Property Value As Double
        Get
            Return _Value
        End Get
    End Property

    Public ReadOnly Property NormalizedValue() As Double
        Get
            Return NormalizeNumber(Me).Value
        End Get
    End Property

    Public Sub New(application As String, name As String, value As Double)
        _Application = application
        _Name = name
        _Value = value
    End Sub

    Public Overrides Function ToString() As String
        Return String.Format("{0}: {1}", _Name, _Value)
    End Function

End Structure
