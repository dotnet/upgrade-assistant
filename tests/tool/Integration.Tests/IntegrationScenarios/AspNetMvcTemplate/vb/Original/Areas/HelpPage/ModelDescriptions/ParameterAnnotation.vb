Imports System

Namespace Areas.HelpPage.ModelDescriptions
    Public Class ParameterAnnotation
        Private _annotationAttribute As Attribute
        Private _documentation As String

        Public Property AnnotationAttribute() As Attribute
            Get
                Return _annotationAttribute
            End Get
            Set(value As Attribute)
                _annotationAttribute = value
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
    End Class
End Namespace