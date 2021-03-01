Imports System

Namespace Areas.HelpPage.ModelDescriptions
    ''' <summary>
    ''' Describes a type model.
    ''' </summary>
    Public MustInherit Class ModelDescription
        Private _name As String
        Private _documentation As String
        Private _modelType As Type

        Public Property Documentation() As String
            Get
                Return _documentation
            End Get
            Set(value As String)
                _documentation = value
            End Set
        End Property

        Public Property ModelType() As Type
            Get
                Return _modelType
            End Get
            Set(value As Type)
                _modelType = value
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
    End Class
End Namespace