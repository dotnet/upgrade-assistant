Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Net.Http.Headers

Namespace Areas.HelpPage
    ''' <summary>
    ''' This is used to identify the place where the sample should be applied.
    ''' </summary>
    Public Class HelpPageSampleKey
        Private _actionName As String
        Private _controllerName As String
        Private _mediaType As MediaTypeHeaderValue
        Private _parameterNames As HashSet(Of String)
        Private _parameterType As Type
        Private _sampleDirection As Nullable(Of SampleDirection)

        ''' <summary>
        ''' Creates a new <see cref="HelpPageSampleKey"/> based on media type.
        ''' </summary>
        ''' <param name="mediaType">The media type.</param>
        Public Sub New(mediaType As MediaTypeHeaderValue)
            If (mediaType Is Nothing) Then
                Throw New ArgumentNullException("mediaType")
            End If

            _actionName = String.Empty
            _controllerName = String.Empty
            _parameterNames = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            _mediaType = mediaType
        End Sub

        ''' <summary>
        ''' Creates a new <see cref="HelpPageSampleKey"/> based on media type and CLR type.
        ''' </summary>
        ''' <param name="mediaType">The media type.</param>
        ''' <param name="type">The CLR type.</param>
        Public Sub New(mediaType As MediaTypeHeaderValue, type As Type)
            MyClass.New(mediaType)

            If (type Is Nothing) Then
                Throw New ArgumentNullException("type")
            End If

            _parameterType = type
        End Sub

        ''' <summary>
        ''' Creates a new <see cref="HelpPageSampleKey"/> based on <see cref="SampleDirection"/>, controller name, action name and parameter names.
        ''' </summary>
        ''' <param name="sampleDirection">The <see cref="SampleDirection"/>.</param>
        ''' <param name="controllerName">Name of the controller.</param>
        ''' <param name="actionName">Name of the action.</param>
        ''' <param name="parameterNames">The parameter names.</param>
        Public Sub New(sampleDirection As SampleDirection, controllerName As String, actionName As String, parameterNames As IEnumerable(Of String))
            If (Not [Enum].IsDefined(GetType(SampleDirection), sampleDirection)) Then
                Throw New InvalidEnumArgumentException("sampleDirection", CInt(sampleDirection), GetType(SampleDirection))
            End If
            If (controllerName Is Nothing) Then
                Throw New ArgumentNullException("controllerName")
            End If
            If (actionName Is Nothing) Then
                Throw New ArgumentNullException("actionName")
            End If
            If (parameterNames Is Nothing) Then
                Throw New ArgumentNullException("parameterNames")
            End If

            _controllerName = controllerName
            _actionName = actionName
            _parameterNames = New HashSet(Of String)(parameterNames, StringComparer.OrdinalIgnoreCase)
            _sampleDirection = sampleDirection
        End Sub

        ''' <summary>
        ''' Creates a new <see cref="HelpPageSampleKey"/> based on media type, <see cref="SampleDirection"/>, controller name, action name and parameter names.
        ''' </summary>
        ''' <param name="mediaType">The media type.</param>
        ''' <param name="sampleDirection">The <see cref="SampleDirection"/>.</param>
        ''' <param name="controllerName">Name of the controller.</param>
        ''' <param name="actionName">Name of the action.</param>
        ''' <param name="parameterNames">The parameter names.</param>
        Public Sub New(mediaType As MediaTypeHeaderValue, sampleDirection As SampleDirection, controllerName As String, actionName As String, parameterNames As IEnumerable(Of String))
            MyClass.New(sampleDirection, controllerName, actionName, parameterNames)

            If (mediaType Is Nothing) Then
                Throw New ArgumentNullException("mediaType")
            End If

            _mediaType = mediaType
        End Sub

        ''' <summary>
        ''' Gets the name of the controller.
        ''' </summary>
        ''' <value>
        ''' The name of the controller.
        ''' </value>
        Public ReadOnly Property ControllerName As String
            Get
                Return _controllerName
            End Get
        End Property

        ''' <summary>
        ''' Gets the name of the action.
        ''' </summary>
        ''' <value>
        ''' The name of the action.
        ''' </value>
        Public ReadOnly Property ActionName As String
            Get
                Return _actionName
            End Get
        End Property

        ''' <summary>
        ''' Gets the media type.
        ''' </summary>
        ''' <value>
        ''' The media type.
        ''' </value>
        Public ReadOnly Property MediaType As MediaTypeHeaderValue
            Get
                Return _mediaType
            End Get
        End Property

        ''' <summary>
        ''' Gets the parameter names.
        ''' </summary>
        Public ReadOnly Property ParameterNames As HashSet(Of String)
            Get
                Return _parameterNames
            End Get
        End Property

        Public ReadOnly Property ParameterType As Type
            Get
                Return _parameterType
            End Get
        End Property

        ''' <summary>
        ''' Gets the <see cref="SampleDirection"/>.
        ''' </summary>
        Public ReadOnly Property SampleDirection As Nullable(Of SampleDirection)
            Get
                Return _sampleDirection
            End Get
        End Property

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim otherKey As HelpPageSampleKey = TryCast(obj, HelpPageSampleKey)
            If (otherKey Is Nothing) Then
                Return False
            End If

            Return String.Equals(ControllerName, otherKey.ControllerName, StringComparison.OrdinalIgnoreCase) And
                String.Equals(ActionName, otherKey.ActionName, StringComparison.OrdinalIgnoreCase) And
                (MediaType Is otherKey.MediaType Or (Not MediaType Is Nothing AndAlso MediaType.Equals(otherKey.MediaType))) And
                ParameterType = otherKey.ParameterType And
                SampleDirection.Equals(otherKey.SampleDirection) And
                ParameterNames.SetEquals(otherKey.ParameterNames)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Dim hashCode As Integer = ControllerName.ToUpperInvariant().GetHashCode() Xor ActionName.ToUpperInvariant().GetHashCode()
            If (Not MediaType Is Nothing) Then
                hashCode = hashCode Xor MediaType.GetHashCode()
            End If
            If (SampleDirection.HasValue) Then
                hashCode = hashCode Xor SampleDirection.GetHashCode()
            End If
            If (Not ParameterType Is Nothing) Then
                hashCode = hashCode Xor ParameterType.GetHashCode()
            End If
            For Each parameterName As String In ParameterNames
                hashCode = hashCode Xor parameterName.ToUpperInvariant().GetHashCode()
            Next
            Return hashCode
        End Function
    End Class
End Namespace