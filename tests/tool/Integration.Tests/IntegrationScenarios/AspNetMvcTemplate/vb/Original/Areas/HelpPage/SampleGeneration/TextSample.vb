Imports System

Namespace Areas.HelpPage
    ''' <summary>
    ''' This represents a preformatted text sample on the help page. There's a display template named TextSample associated with this class.
    ''' </summary>
    Public Class TextSample
        Private _text As String

        Public Sub New(text As String)
            If (text Is Nothing) Then
                Throw New ArgumentNullException("text")
            End If
            Me.Text = text
        End Sub

        Public Property Text As String
            Get
                Return _text
            End Get
            Private Set(value As String)
                _text = value
            End Set
        End Property

        Public Overrides Function Equals(obj As Object) As Boolean
            Equals = False
            Dim other As TextSample = TryCast(obj, TextSample)
            If Not (other Is Nothing) Then
                Equals = (Text = other.Text)
            End If
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Text.GetHashCode()
        End Function

        Public Overrides Function ToString() As String
            Return Text
        End Function
    End Class
End Namespace