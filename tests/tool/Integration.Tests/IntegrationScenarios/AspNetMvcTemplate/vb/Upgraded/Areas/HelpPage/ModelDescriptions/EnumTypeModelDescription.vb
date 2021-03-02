Imports System.Collections.Generic
Imports System.Collections.ObjectModel

Namespace Areas.HelpPage.ModelDescriptions
    Public Class EnumTypeModelDescription
        Inherits ModelDescription
        Private _values As Collection(Of EnumValueDescription)

        Public Sub New()
            Values = New Collection(Of EnumValueDescription)()
        End Sub

        Public Property Values() As Collection(Of EnumValueDescription)
            Get
                Return _values
            End Get
            Private Set(value As Collection(Of EnumValueDescription))
                _values = value
            End Set
        End Property
    End Class
End Namespace