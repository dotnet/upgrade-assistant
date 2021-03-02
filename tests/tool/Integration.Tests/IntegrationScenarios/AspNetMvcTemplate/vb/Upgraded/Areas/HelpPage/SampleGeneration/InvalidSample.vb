Imports System

Namespace Areas.HelpPage
    ''' <summary>
    ''' This represents an invalid sample on the help page. There's a display template named InvalidSample associated with this class.
    ''' </summary>
    Public Class InvalidSample
        Private _errorMessage As String

        Public Sub New(errorMessage As String)
            If (errorMessage Is Nothing) Then
                Throw New ArgumentNullException("errorMessage")
            End If

            Me.ErrorMessage = errorMessage
        End Sub

        Public Property ErrorMessage As String
            Get
                Return _errorMessage
            End Get
            Private Set(value As String)
                _errorMessage = value
            End Set
        End Property

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim other As InvalidSample = TryCast(obj, InvalidSample)
            Return Not other Is Nothing AndAlso ErrorMessage = other.ErrorMessage
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return ErrorMessage.GetHashCode()
        End Function

        Public Overrides Function ToString() As String
            Return ErrorMessage
        End Function
    End Class
End Namespace