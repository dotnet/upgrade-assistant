Namespace Areas.HelpPage.ModelDescriptions
    Public Class EnumValueDescription
        Private _documentation As String
        Private _value As String
        Private _name As String

        Public Property Documentation() As String
            Get
                Return _documentation
            End Get
            Set(value As String)
                _documentation = value
            End Set
        End Property

        Public Property Name() As String
            Get
                Return _name
            End Get
            Set(value As String)
                _name = value
            End Set
        End Property

        Public Property Value() As String
            Get
                Return _value
            End Get
            Set(value As String)
                _value = value
            End Set
        End Property
    End Class
End Namespace