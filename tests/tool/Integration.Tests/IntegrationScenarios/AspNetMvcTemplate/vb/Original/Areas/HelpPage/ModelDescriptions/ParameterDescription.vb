Imports System.Collections.Generic
Imports System.Collections.ObjectModel

Namespace Areas.HelpPage.ModelDescriptions
    Public Class ParameterDescription
        Private _annotations As Collection(Of ParameterAnnotation)
        Private _documentation As String
        Private _name As String
        Private _typeDescription As ModelDescription

        Public Sub New()
            Annotations = New Collection(Of ParameterAnnotation)()
        End Sub

        Public Property Annotations() As Collection(Of ParameterAnnotation)
            Get
                Return _annotations
            End Get
            Private Set(value As Collection(Of ParameterAnnotation))
                _annotations = value
            End Set
        End Property

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

        Public Property TypeDescription() As ModelDescription
            Get
                Return _typeDescription
            End Get
            Set(value As ModelDescription)
                _typeDescription = value
            End Set
        End Property
    End Class
End Namespace