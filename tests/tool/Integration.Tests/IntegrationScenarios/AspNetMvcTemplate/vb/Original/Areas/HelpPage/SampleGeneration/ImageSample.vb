Imports System

Namespace Areas.HelpPage
    ''' <summary>
    ''' This represents an image sample on the help page. There's a display template named ImageSample associated with this class.
    ''' </summary>
    Public Class ImageSample
        Private _src As String

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ImageSample"/> class.
        ''' </summary>
        ''' <param name="src">The URL of an image.</param>
        Public Sub New(src As String)
            If (src Is Nothing) Then
                Throw New ArgumentNullException("src")
            End If
            Me.Src = src
        End Sub

        Public Property Src As String
            Get
                Return _src
            End Get
            Private Set(value As String)
                _src = value
            End Set
        End Property

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim other As ImageSample = TryCast(obj, ImageSample)

            Return Not other Is Nothing AndAlso Src = other.Src
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Src.GetHashCode()
        End Function

        Public Overrides Function ToString() As String
            Return Src
        End Function
    End Class
End Namespace