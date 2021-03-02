Imports System.Collections.ObjectModel

Namespace Areas.HelpPage.ModelDescriptions
    Public Class ComplexTypeModelDescription
        Inherits ModelDescription
        Private _properties As Collection(Of ParameterDescription)

        Public Sub New()
            Properties = New Collection(Of ParameterDescription)()
        End Sub

        Public Property Properties() As Collection(Of ParameterDescription)
            Get
                Return _properties
            End Get
            Private Set(value As Collection(Of ParameterDescription))
                _properties = value
            End Set
        End Property
    End Class
End Namespace