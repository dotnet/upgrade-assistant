Imports System
Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Diagnostics.CodeAnalysis
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Net.Http
Imports System.Net.Http.Formatting
Imports System.Net.Http.Headers
Imports System.Runtime.InteropServices
Imports System.Web.Http.Description
Imports System.Xml.Linq
Imports Newtonsoft.Json

Namespace Areas.HelpPage
    ''' <summary>
    ''' This class will generate the samples for the help page.
    ''' </summary>
    Public Class HelpPageSampleGenerator
        Private _actualHttpMessageTypes As IDictionary(Of HelpPageSampleKey, Type)
        Private _actionSamples As IDictionary(Of HelpPageSampleKey, Object)
        Private _sampleObjects As IDictionary(Of Type, Object)
        Private _sampleObjectFactories As IList(Of Func(Of HelpPageSampleGenerator, Type, Object))

        ''' <summary>
        ''' Initializes a new instance of the <see cref="HelpPageSampleGenerator"/> class.
        ''' </summary>
        Public Sub New()
            ActualHttpMessageTypes = New Dictionary(Of HelpPageSampleKey, Type)
            ActionSamples = New Dictionary(Of HelpPageSampleKey, Object)
            SampleObjects = New Dictionary(Of Type, Object)
            SampleObjectFactories = New List(Of Func(Of HelpPageSampleGenerator, Type, Object))
            SampleObjectFactories.Add(AddressOf DefaultSampleObjectFactory)
        End Sub

        ''' <summary>
        ''' Gets CLR types that are used as the content of <see cref="HttpRequestMessage"/> or <see cref="HttpResponseMessage"/>.
        ''' </summary>
        Public Property ActualHttpMessageTypes As IDictionary(Of HelpPageSampleKey, Type)
            Get
                Return _actualHttpMessageTypes
            End Get
            Friend Set(value As IDictionary(Of HelpPageSampleKey, Type))
                _actualHttpMessageTypes = value
            End Set
        End Property

        ''' <summary>
        ''' Gets the objects that are used directly as samples for certain actions.
        ''' </summary>
        Public Property ActionSamples As IDictionary(Of HelpPageSampleKey, Object)
            Get
                Return _actionSamples
            End Get
            Friend Set(value As IDictionary(Of HelpPageSampleKey, Object))
                _actionSamples = value
            End Set
        End Property

        ''' <summary>
        ''' Gets the objects that are serialized as samples by the supported formatters.
        ''' </summary>
        Public Property SampleObjects As IDictionary(Of Type, Object)
            Get
                Return _sampleObjects
            End Get
            Friend Set(value As IDictionary(Of Type, Object))
                _sampleObjects = value
            End Set
        End Property

        ''' <summary>
        ''' Gets factories for the objects that the supported formatters will serialize as samples. Processed in order,
        ''' stopping when the factory successfully returns a non-<see langref="null"/> object.
        ''' </summary>
        ''' <remarks>
        ''' Collection includes just <see cref="ObjectGenerator.GenerateObject"/> initially. Use
        ''' <code>SampleObjectFactories.Insert(0, func)</code> to provide an override and
        ''' <code>SampleObjectFactories.Add(func)</code> to provide a fallback.</remarks>
        <SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures",
            Justification:="This is an appropriate nesting of generic types")>
        Public Property SampleObjectFactories As IList(Of Func(Of HelpPageSampleGenerator, Type, Object))
            Get
                Return _sampleObjectFactories
            End Get
            Private Set(value As IList(Of Func(Of HelpPageSampleGenerator, Type, Object)))
                _sampleObjectFactories = value
            End Set
        End Property

        ''' <summary>
        ''' Gets the request body samples for a given <see cref="ApiDescription"/>.
        ''' </summary>
        ''' <param name="api">The <see cref="ApiDescription"/>.</param>
        ''' <returns>The samples keyed by media type.</returns>
        Public Function GetSampleRequests(api As ApiDescription) As IDictionary(Of MediaTypeHeaderValue, Object)
            Return GetSample(api, SampleDirection.Request)
        End Function

        ''' <summary>
        ''' Gets the response body samples for a given <see cref="ApiDescription"/>.
        ''' </summary>
        ''' <param name="api">The <see cref="ApiDescription"/>.</param>
        ''' <returns>The samples keyed by media type.</returns>
        Public Function GetSampleResponses(api As ApiDescription) As IDictionary(Of MediaTypeHeaderValue, Object)
            Return GetSample(api, SampleDirection.Response)
        End Function

        ''' <summary>
        ''' Gets the request or response body samples.
        ''' </summary>
        ''' <param name="api">The <see cref="ApiDescription"/>.</param>
        ''' <param name="sampleDirection">The value indicating whether the sample is for a request or for a response.</param>
        ''' <returns>The samples keyed by media type.</returns>
        Public Overridable Function GetSample(api As ApiDescription, sampleDirection As SampleDirection) As IDictionary(Of MediaTypeHeaderValue, Object)
            If (api Is Nothing) Then
                Throw New ArgumentNullException("api")
            End If

            Dim controllerName As String = api.ActionDescriptor.ControllerDescriptor.ControllerName
            Dim actionName As String = api.ActionDescriptor.ActionName
            Dim parameterNames As IEnumerable(Of String) = api.ParameterDescriptions.Select(Function(p) p.Name)
            Dim formatters As New Collection(Of MediaTypeFormatter)
            Dim type As Type = ResolveType(api, controllerName, actionName, parameterNames, sampleDirection, formatters)
            Dim samples As New Dictionary(Of MediaTypeHeaderValue, Object)

            ' Use the samples provided directly for actions
            Dim actionSamples = GetAllActionSamples(controllerName, actionName, parameterNames, sampleDirection)
            For Each actionSample In actionSamples
                samples.Add(actionSample.Key.MediaType, WrapSampleIfString(actionSample.Value))
            Next

            ' Do the sample generation based on formatters only if an action doesn't return an HttpResponseMessage.
            ' Here we cannot rely on formatters because we don't know what's in the HttpResponseMessage, it might not even use formatters.
            If (Not type Is Nothing AndAlso Not GetType(HttpResponseMessage).IsAssignableFrom(type)) Then
                Dim sampleObject As Object = GetSampleObject(type)
                For Each formatter In formatters
                    For Each mediaType As MediaTypeHeaderValue In formatter.SupportedMediaTypes
                        If (Not samples.ContainsKey(mediaType)) Then
                            Dim sample As Object = GetActionSample(controllerName, actionName, parameterNames, type, formatter, mediaType, sampleDirection)
                            ' If no sample found, try generate sample using formatter and sample object
                            If (sample Is Nothing And Not sampleObject Is Nothing) Then
                                sample = WriteSampleObjectUsingFormatter(formatter, sampleObject, type, mediaType)
                            End If

                            samples.Add(mediaType, WrapSampleIfString(sample))
                        End If
                    Next
                Next
            End If
            Return samples
        End Function

        ''' <summary>
        ''' Search for samples that are provided directly through <see cref="ActionSamples"/>.
        ''' </summary>
        ''' <param name="controllerName">Name of the controller.</param>
        ''' <param name="actionName">Name of the action.</param>
        ''' <param name="parameterNames">The parameter names.</param>
        ''' <param name="type">The CLR type.</param>
        ''' <param name="formatter">The formatter.</param>
        ''' <param name="mediaType">The media type.</param>
        ''' <param name="sampleDirection">The value indicating whether the sample is for a request or for a response.</param>
        ''' <returns>The sample that matches the parameters.</returns>
        Public Overridable Function GetActionSample(controllerName As String, actionName As String, parameterNames As IEnumerable(Of String), type As Type, formatter As MediaTypeFormatter, mediaType As MediaTypeHeaderValue, sampleDirection As SampleDirection) As Object
            Dim sample As New Object

            ' First, try to get the sample provided for the specified mediaType, sampleDirection, controllerName, actionName and parameterNames.
            ' If not found, try to get the sample provided for the specified mediaType, sampleDirection, controllerName and actionName regardless of the parameterNames.
            ' If still not found, try to get the sample provided for the specified mediaType and type.
            ' Finally, try to get the sample provided for the specified mediaType.
            If (ActionSamples.TryGetValue(New HelpPageSampleKey(mediaType, sampleDirection, controllerName, actionName, parameterNames), sample) OrElse
                ActionSamples.TryGetValue(New HelpPageSampleKey(mediaType, sampleDirection, controllerName, actionName, New String() {"*"}), sample) OrElse
                ActionSamples.TryGetValue(New HelpPageSampleKey(mediaType, type), sample) OrElse
                ActionSamples.TryGetValue(New HelpPageSampleKey(mediaType), sample)) Then
                Return sample
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Gets the sample object that will be serialized by the formatters. 
        ''' First, it will look at the <see cref="SampleObjects"/>. If no sample object is found, it will try to create
        ''' one using <see cref="DefaultSampleObjectFactory"/> (which wraps an <see cref="ObjectGenerator"/>) and other
        ''' factories in <see cref="SampleObjectFactories"/>.
        ''' </summary>
        ''' <param name="type">The type.</param>
        ''' <returns>The sample object.</returns>
        <SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification:="Even if all items in SampleObjectFactories throw, problem will be visible as missing sample.")>
        Public Overridable Function GetSampleObject(type As Type) As Object
            Dim sampleObject As New Object

            If (Not SampleObjects.TryGetValue(type, sampleObject)) Then
                ' No specific object available, try our factories.
                For Each factory As Func(Of HelpPageSampleGenerator, Type, Object) In SampleObjectFactories
                    If factory Is Nothing Then
                        Continue For
                    End If

                    Try
                        sampleObject = factory(Me, type)
                        If sampleObject IsNot Nothing Then
                            Exit For
                        End If
                    Catch
                        ' Ignore any problems encountered in the factory; go on to the next one (if any).
                    End Try
                Next
            End If

            Return sampleObject
        End Function

        ''' <summary>
        ''' Resolves the actual type of <see cref="System.Net.Http.ObjectContent(Of T)"/> passed to the <see cref="System.Net.Http.HttpRequestMessage"/> in an action.
        ''' </summary>
        ''' <param name="api">The <see cref="ApiDescription"/>.</param>
        ''' <returns>The type.</returns>
        Public Overridable Function ResolveHttpRequestMessageType(api As ApiDescription) As Type
            Dim controllerName As String = api.ActionDescriptor.ControllerDescriptor.ControllerName
            Dim actionName As String = api.ActionDescriptor.ActionName
            Dim parameterNames As IEnumerable(Of String) = api.ParameterDescriptions.[Select](Function(p) p.Name)
            Dim formatters As Collection(Of MediaTypeFormatter) = Nothing
            Return ResolveType(api, controllerName, actionName, parameterNames, SampleDirection.Request, formatters)
        End Function

        ''' <summary>
        ''' Resolves the type of the action parameter or return value when <see cref="HttpRequestMessage"/> or <see cref="HttpResponseMessage"/> is used.
        ''' </summary>
        ''' <param name="api">The <see cref="ApiDescription"/>.</param>
        ''' <param name="controllerName">Name of the controller.</param>
        ''' <param name="actionName">Name of the action.</param>
        ''' <param name="parameterNames">The parameter names.</param>
        ''' <param name="sampleDirection">The value indicating whether the sample is for a request or a response.</param>
        ''' <param name="formatters">The formatters.</param>
        <SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", Justification:="This is only used in advanced scenarios.")>
        Public Overridable Function ResolveType(api As ApiDescription, controllerName As String, actionName As String, parameterNames As IEnumerable(Of String), sampleDirection As SampleDirection, <Out()> ByRef formatters As Collection(Of MediaTypeFormatter)) As Type
            If (Not [Enum].IsDefined(GetType(SampleDirection), sampleDirection)) Then
                Throw New InvalidEnumArgumentException("sampleDirection", CInt(sampleDirection), GetType(SampleDirection))
            End If
            If (api Is Nothing) Then
                Throw New ArgumentNullException("api")
            End If
            Dim type As Type = GetType(Object)

            If (ActualHttpMessageTypes.TryGetValue(New HelpPageSampleKey(sampleDirection, controllerName, actionName, parameterNames), type) OrElse
                ActualHttpMessageTypes.TryGetValue(New HelpPageSampleKey(sampleDirection, controllerName, actionName, New String() {"*"}), type)) Then
                ' Re-compute the supported formatters based on type
                Dim newFormatters As New Collection(Of MediaTypeFormatter)
                For Each formatter In api.ActionDescriptor.Configuration.Formatters
                    If (IsFormatSupported(sampleDirection, formatter, type)) Then
                        newFormatters.Add(formatter)
                    End If
                Next

                formatters = newFormatters
            Else
                Select Case sampleDirection
                    Case sampleDirection.Request
                        Dim requestBodyParameter As ApiParameterDescription = api.ParameterDescriptions.FirstOrDefault(Function(p) p.Source = ApiParameterSource.FromBody)
                        type = If(requestBodyParameter Is Nothing, Nothing, requestBodyParameter.ParameterDescriptor.ParameterType)
                        formatters = api.SupportedRequestBodyFormatters
                    Case Else
                        'Case sampleDirection.Response
                        type = If(api.ResponseDescription.ResponseType, api.ResponseDescription.DeclaredType)
                        formatters = api.SupportedResponseFormatters
                End Select
            End If
            Return type
        End Function

        ''' <summary>
        ''' Writes the sample object using formatter.
        ''' </summary>
        ''' <param name="formatter">The formatter.</param>
        ''' <param name="value">The value.</param>
        ''' <param name="type">The type.</param>
        ''' <param name="mediaType">Type of the media.</param>
        ''' <returns></returns>
        <SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification:="The exception is recorded as InvalidSample.")>
        Public Overridable Function WriteSampleObjectUsingFormatter(formatter As MediaTypeFormatter, value As Object, type As Type, mediaType As MediaTypeHeaderValue) As Object
            If (formatter Is Nothing) Then
                Throw New ArgumentNullException("formatter")
            End If
            If (mediaType Is Nothing) Then
                Throw New ArgumentNullException("mediaType")
            End If

            Dim sample As Object = String.Empty
            Dim MS As MemoryStream = Nothing
            Dim content As HttpContent = Nothing
            Try
                If (formatter.CanWriteType(type)) Then
                    MS = New MemoryStream()
                    content = New ObjectContent(type, value, formatter, mediaType)
                    formatter.WriteToStreamAsync(type, value, MS, content, Nothing).Wait()
                    MS.Position = 0
                    Dim reader As New StreamReader(MS)
                    Dim serializedSampleString As String = reader.ReadToEnd()
                    If (mediaType.MediaType.ToUpperInvariant().Contains("XML")) Then
                        serializedSampleString = TryFormatXml(serializedSampleString)
                    ElseIf (mediaType.MediaType.ToUpperInvariant().Contains("JSON")) Then
                        serializedSampleString = TryFormatJson(serializedSampleString)
                    End If

                    sample = New TextSample(serializedSampleString)
                Else
                    sample = New InvalidSample(String.Format(
                        CultureInfo.CurrentCulture,
                        "Failed to generate the sample for media type '{0}'. Cannot use formatter '{1}' to write type '{2}'.",
                        mediaType,
                        formatter.GetType().Name,
                        type.Name))
                End If
            Catch e As Exception
                sample = New InvalidSample(String.Format(
                    CultureInfo.CurrentCulture,
                    "An exception has occurred while using the formatter '{0}' to generate sample for media type '{1}'. Exception message: {2}",
                    formatter.GetType().Name,
                    mediaType.MediaType,
                    UnwrapException(e).Message))
            Finally
                If (Not MS Is Nothing) Then
                    MS.Dispose()
                End If
                If (Not content Is Nothing) Then
                    content.Dispose()
                End If
            End Try
            Return sample
        End Function

        Friend Shared Function UnwrapException(exception As Exception) As Exception
            Dim aggregateException As AggregateException = TryCast(exception, AggregateException)
            If aggregateException IsNot Nothing Then
                Return aggregateException.Flatten().InnerException
            End If
            Return exception
        End Function

        Private Shared Function DefaultSampleObjectFactory(sampleGenerator As HelpPageSampleGenerator, type As Type) As Object
            ' Try create a default sample object
            Dim objectGenerator As New ObjectGenerator()
            Return objectGenerator.GenerateObject(type)
        End Function

        <SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification:="Handling the failure by returning the original string.")>
        Private Shared Function TryFormatJson(str As String) As String
            Try
                Dim parsedJson As Object = JsonConvert.DeserializeObject(str)
                Return JsonConvert.SerializeObject(parsedJson, Formatting.Indented)
            Catch
                ' can't parse JSON, return the original string
                Return str
            End Try
        End Function

        <SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification:="Handling the failure by returning the original string.")>
        Private Shared Function TryFormatXml(str As String) As String
            Try
                Dim Xml As XDocument = XDocument.Parse(str)
                Return Xml.ToString()
            Catch
                ' can't parse XML, return the original string
                Return str
            End Try
        End Function

        Private Shared Function IsFormatSupported(sampleDirection As SampleDirection, formatter As MediaTypeFormatter, type As Type) As Boolean
            Select Case sampleDirection
                Case sampleDirection.Request
                    Return formatter.CanReadType(type)
                Case sampleDirection.Response
                    Return formatter.CanWriteType(type)
            End Select
            Return False
        End Function

        Private Iterator Function GetAllActionSamples(controllerName As String, actionName As String, parameterNames As IEnumerable(Of String), sampleDirection As SampleDirection) As IEnumerable(Of KeyValuePair(Of HelpPageSampleKey, Object))
            Dim parameterNamesSet As New HashSet(Of String)(parameterNames, StringComparer.OrdinalIgnoreCase)
            For Each sample In ActionSamples
                Dim sampleKey As HelpPageSampleKey = sample.Key
                If (String.Equals(controllerName, sampleKey.ControllerName, StringComparison.OrdinalIgnoreCase) And
                        String.Equals(actionName, sampleKey.ActionName, StringComparison.OrdinalIgnoreCase) And
                        (sampleKey.ParameterNames.SetEquals(New String() {"*"}) Or parameterNamesSet.SetEquals(sampleKey.ParameterNames)) And
                        sampleDirection = sampleKey.SampleDirection) Then
                    Yield sample
                End If
            Next
        End Function

        Private Shared Function WrapSampleIfString(sample As Object) As Object
            Dim stringSample As String = TryCast(sample, String)
            If (Not stringSample Is Nothing) Then
                Return New TextSample(stringSample)
            End If
            Return sample
        End Function
    End Class
End Namespace