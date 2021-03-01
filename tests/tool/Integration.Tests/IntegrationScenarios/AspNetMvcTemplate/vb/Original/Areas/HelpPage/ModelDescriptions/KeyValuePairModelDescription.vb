Namespace Areas.HelpPage.ModelDescriptions
    Public Class KeyValuePairModelDescription
        Inherits ModelDescription
        Private _keyModelDescription As ModelDescription
        Private _valueModelDescription As ModelDescription

        Public Property KeyModelDescription() As ModelDescription
            Get
                Return _keyModelDescription
            End Get
            Set(value As ModelDescription)
                _keyModelDescription = value
            End Set
        End Property

        Public Property ValueModelDescription() As ModelDescription
            Get
                Return _valueModelDescription
            End Get
            Set(value As ModelDescription)
                _valueModelDescription = value
            End Set
        End Property
    End Class
End Namespace