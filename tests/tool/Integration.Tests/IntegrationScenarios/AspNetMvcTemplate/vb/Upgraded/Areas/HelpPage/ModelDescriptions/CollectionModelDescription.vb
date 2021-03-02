Namespace Areas.HelpPage.ModelDescriptions
    Public Class CollectionModelDescription
        Inherits ModelDescription
        Private _elementDescription As ModelDescription

        Public Property ElementDescription() As ModelDescription
            Get
                Return _elementDescription
            End Get
            Set(value As ModelDescription)
                _elementDescription = value
            End Set
        End Property
    End Class
End Namespace