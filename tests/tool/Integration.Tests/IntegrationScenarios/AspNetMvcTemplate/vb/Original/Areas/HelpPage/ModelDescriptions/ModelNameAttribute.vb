Imports System

Namespace Areas.HelpPage.ModelDescriptions
    ''' <summary>
    ''' Use this attribute to change the name of the <see cref="ModelDescription"/> generated for a type.
    ''' </summary>
    <AttributeUsage(AttributeTargets.[Class] Or AttributeTargets.Struct Or AttributeTargets.[Enum], AllowMultiple:=False, Inherited:=False)> _
    Public NotInheritable Class ModelNameAttribute
        Inherits Attribute
        Private _name As String

        Public Sub New(name As String)
            _name = name
        End Sub

        Public Property Name() As String
            Get
                Return _name
            End Get
            Private Set(value As String)
                _name = value
            End Set
        End Property
    End Class
End Namespace