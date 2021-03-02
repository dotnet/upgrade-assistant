Imports System
Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Diagnostics.CodeAnalysis
Imports System.Globalization
Imports System.Linq
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Runtime.CompilerServices
Imports System.Web.Http
Imports System.Web.Http.Controllers
Imports System.Web.Http.Description
Imports SinglePageApp.Areas.HelpPage.Models
Imports SinglePageApp.Areas.HelpPage.ModelDescriptions

Namespace Areas.HelpPage
    Public Module HelpPageConfigurationExtensions
        Private Const ApiModelPrefix As String = "MS_HelpPageApiModel_"

        ''' <summary>
        ''' Sets the documentation provider for help page.
        ''' </summary>
        ''' <param name="config">The <see cref="HttpConfiguration"/>.</param>
        ''' <param name="documentationProvider">The documentation provider.</param>
        <Extension()>
        Public Sub SetDocumentationProvider(ByVal config As HttpConfiguration, documentationProvider As IDocumentationProvider)
            config.Services.Replace(GetType(IDocumentationProvider), documentationProvider)
        End Sub

        ''' <summary>
        ''' Sets the objects that will be used by the formatters to produce sample requests/responses.
        ''' </summary>
        ''' <param name="config">The <see cref="HttpConfiguration"/>.</param>
        ''' <param name="sampleObjects">The sample objects.</param>
        <Extension()>
        Public Sub SetSampleObjects(ByVal config As HttpConfiguration, sampleObjects As IDictionary(Of Type, Object))
            config.GetHelpPageSampleGenerator().SampleObjects = sampleObjects
        End Sub

        ''' <summary>
        ''' Sets the sample request directly for the specified media type and action.
        ''' </summary>
        ''' <param name="config">The <see cref="HttpConfiguration"/>.</param>
        ''' <param name="sample">The sample request.</param>
        ''' <param name="mediaType">The media type.</param>
        ''' <param name="controllerName">Name of the controller.</param>
        ''' <param name="actionName">Name of the action.</param>
        <Extension()>
        Public Sub SetSampleRequest(ByVal config As HttpConfiguration, sample As Object, mediaType As MediaTypeHeaderValue, controllerName As String, actionName As String)
            config.GetHelpPageSampleGenerator().ActionSamples.Add(New HelpPageSampleKey(mediaType, SampleDirection.Request, controllerName, actionName, New String() {"*"}), sample)
        End Sub

        ''' <summary>
        ''' Sets the sample request directly for the specified media type and action with parameters.
        ''' </summary>
        ''' <param name="config">The <see cref="HttpConfiguration"/>.</param>
        ''' <param name="sample">The sample request.</param>
        ''' <param name="mediaType">The media type.</param>
        ''' <param name="controllerName">Name of the controller.</param>
        ''' <param name="actionName">Name of the action.</param>
        ''' <param name="parameterNames">The parameter names.</param>
        <Extension()>
        Public Sub SetSampleRequest(ByVal config As HttpConfiguration, sample As Object, mediaType As MediaTypeHeaderValue, controllerName As String, actionName As String, ByVal ParamArray parameterNames() As String)
            config.GetHelpPageSampleGenerator().ActionSamples.Add(New HelpPageSampleKey(mediaType, SampleDirection.Request, controllerName, actionName, parameterNames), sample)
        End Sub

        ''' <summary>
        ''' Sets the sample request directly for the specified media type of the action.
        ''' </summary>
        ''' <param name="config">The <see cref="HttpConfiguration"/>.</param>
        ''' <param name="sample">The sample response.</param>
        ''' <param name="mediaType">The media type.</param>
        ''' <param name="controllerName">Name of the controller.</param>
        ''' <param name="actionName">Name of the action.</param>
        <Extension()>
        Public Sub SetSampleResponse(ByVal config As HttpConfiguration, sample As Object, mediaType As MediaTypeHeaderValue, controllerName As String, actionName As String)
            config.GetHelpPageSampleGenerator().ActionSamples.Add(New HelpPageSampleKey(mediaType, SampleDirection.Response, controllerName, actionName, New String() {"*"}), sample)
        End Sub

        ''' <summary>
        ''' Sets the sample response directly for the specified media type of the action with specific parameters.
        ''' </summary>
        ''' <param name="config">The <see cref="HttpConfiguration"/>.</param>
        ''' <param name="sample">The sample response.</param>
        ''' <param name="mediaType">The media type.</param>
        ''' <param name="controllerName">Name of the controller.</param>
        ''' <param name="actionName">Name of the action.</param>
        ''' <param name="parameterNames">The parameter names.</param>
        <Extension()>
        Public Sub SetSampleResponse(ByVal config As HttpConfiguration, sample As Object, mediaType As MediaTypeHeaderValue, controllerName As String, actionName As String, ByVal ParamArray parameterNames() As String)
            config.GetHelpPageSampleGenerator().ActionSamples.Add(New HelpPageSampleKey(mediaType, SampleDirection.Response, controllerName, actionName, parameterNames), sample)
        End Sub

        ''' <summary>
        ''' Sets the sample directly for all actions with the specified type.
        ''' </summary>
        ''' <param name="config">The <see cref="HttpConfiguration"/>.</param>
        ''' <param name="sample">The sample.</param>
        ''' <param name="mediaType">The media type.</param>
        <Extension()>
        Public Sub SetSampleForMediaType(ByVal config As HttpConfiguration, sample As Object, mediaType As MediaTypeHeaderValue)
            config.GetHelpPageSampleGenerator().ActionSamples.Add(New HelpPageSampleKey(mediaType), sample)
        End Sub

        ''' <summary>
        ''' Sets the sample directly for all actions with the specified type and media type.
        ''' </summary>
        ''' <param name="config">The <see cref="HttpConfiguration"/>.</param>
        ''' <param name="sample">The sample.</param>
        ''' <param name="mediaType">The media type.</param>
        ''' <param name="type">The parameter type or return type of an action.</param>
        <Extension()>
        Public Sub SetSampleForType(ByVal config As HttpConfiguration, sample As Object, mediaType As MediaTypeHeaderValue, type As Type)
            config.GetHelpPageSampleGenerator().ActionSamples.Add(New HelpPageSampleKey(mediaType, type), sample)
        End Sub

        ''' <summary>
        ''' Specifies the actual type of <see cref="System.Net.Http.ObjectContent(Of T)"/> passed to the <see cref="System.Net.Http.HttpRequestMessage"/> in an action. 
        ''' The help page will use this information to produce more accurate request samples.
        ''' </summary>
        ''' <param name="config">The <see cref="HttpConfiguration"/>.</param>
        ''' <param name="type">The type.</param>
        ''' <param name="controllerName">Name of the controller.</param>
        ''' <param name="actionName">Name of the action.</param>
        <Extension()>
        Public Sub SetActualRequestType(ByVal config As HttpConfiguration, type As Type, controllerName As String, actionName As String)
            config.GetHelpPageSampleGenerator().ActualHttpMessageTypes.Add(New HelpPageSampleKey(SampleDirection.Request, controllerName, actionName, New String() {"*"}), type)
        End Sub

        ''' <summary>
        ''' Specifies the actual type of <see cref="System.Net.Http.ObjectContent(Of T)"/> passed to the <see cref="System.Net.Http.HttpRequestMessage"/> in an action. 
        ''' The help page will use this information to produce more accurate request samples.
        ''' </summary>
        ''' <param name="config">The <see cref="HttpConfiguration"/>.</param>
        ''' <param name="type">The type.</param>
        ''' <param name="controllerName">Name of the controller.</param>
        ''' <param name="actionName">Name of the action.</param>
        ''' <param name="parameterNames">The parameter names.</param>
        <Extension()>
        Public Sub SetActualRequestType(ByVal config As HttpConfiguration, type As Type, controllerName As String, actionName As String, ByVal ParamArray parameterNames() As String)
            config.GetHelpPageSampleGenerator().ActualHttpMessageTypes.Add(New HelpPageSampleKey(SampleDirection.Request, controllerName, actionName, parameterNames), type)
        End Sub

        ''' <summary>
        ''' Specifies the actual type of <see cref="System.Net.Http.ObjectContent(Of T)"/> returned as part of the <see cref="System.Net.Http.HttpRequestMessage"/> in an action. 
        ''' The help page will use this information to produce more accurate response samples.
        ''' </summary>
        ''' <param name="config">The <see cref="HttpConfiguration"/>.</param>
        ''' <param name="type">The type.</param>
        ''' <param name="controllerName">Name of the controller.</param>
        ''' <param name="actionName">Name of the action.</param>
        <Extension()>
        Public Sub SetActualResponseType(ByVal config As HttpConfiguration, type As Type, controllerName As String, actionName As String)
            config.GetHelpPageSampleGenerator().ActualHttpMessageTypes.Add(New HelpPageSampleKey(SampleDirection.Response, controllerName, actionName, New String() {"*"}), type)
        End Sub

        ''' <summary>
        ''' Specifies the actual type of <see cref="System.Net.Http.ObjectContent(Of T)"/> returned as part of the <see cref="System.Net.Http.HttpRequestMessage"/> in an action. 
        ''' The help page will use this information to produce more accurate response samples.
        ''' </summary>
        ''' <param name="config">The <see cref="HttpConfiguration"/>.</param>
        ''' <param name="type">The type.</param>
        ''' <param name="controllerName">Name of the controller.</param>
        ''' <param name="actionName">Name of the action.</param>
        ''' <param name="parameterNames">The parameter names.</param>
        <Extension()>
        Public Sub SetActualResponseType(ByVal config As HttpConfiguration, type As Type, controllerName As String, actionName As String, ByVal ParamArray parameterNames() As String)
            config.GetHelpPageSampleGenerator().ActualHttpMessageTypes.Add(New HelpPageSampleKey(SampleDirection.Response, controllerName, actionName, parameterNames), type)
        End Sub

        ''' <summary>
        ''' Gets the help page sample generator.
        ''' </summary>
        ''' <param name="config">The <see cref="HttpConfiguration"/>.</param>
        ''' <returns>The help page sample generator.</returns>
        <Extension()>
        Public Function GetHelpPageSampleGenerator(ByVal config As HttpConfiguration) As HelpPageSampleGenerator
            Return DirectCast(config.Properties.GetOrAdd(
                GetType(HelpPageSampleGenerator),
                Function(k) New HelpPageSampleGenerator()), HelpPageSampleGenerator)
        End Function

        ''' <summary>
        ''' Gets the model description generator.
        ''' </summary>
        ''' <param name="config">The configuration.</param>
        ''' <returns>The <see cref="ModelDescriptionGenerator"/></returns>
        <Extension()>
        Public Function GetModelDescriptionGenerator(config As HttpConfiguration) As ModelDescriptionGenerator
            Return DirectCast(config.Properties.GetOrAdd(GetType(ModelDescriptionGenerator), Function(k) InitializeModelDescriptionGenerator(config)), ModelDescriptionGenerator)
        End Function

        ''' <summary>
        ''' Sets the help page sample generator.
        ''' </summary>
        ''' <param name="config">The <see cref="HttpConfiguration"/>.</param>
        ''' <param name="sampleGenerator">The help page sample generator.</param>
        <Extension()>
        Public Sub SetHelpPageSampleGenerator(ByVal config As HttpConfiguration, sampleGenerator As HelpPageSampleGenerator)
            config.Properties.AddOrUpdate(
                GetType(HelpPageSampleGenerator),
                Function(k) sampleGenerator,
                Function(k, o) sampleGenerator)
        End Sub

        ''' <summary>
        ''' Gets the model that represents an API displayed on the help page. The model is initialized on the first call and cached for subsequent calls.
        ''' </summary>
        ''' <param name="config">The <see cref="HttpConfiguration"/>.</param>
        ''' <param name="apiDescriptionId">The <see cref="ApiDescription"/> ID.</param>
        ''' <returns>
        ''' An <see cref="HelpPageApiModel"/>
        ''' </returns>

        <Extension()>
        Public Function GetHelpPageApiModel(ByVal config As HttpConfiguration, apiDescriptionId As String) As HelpPageApiModel
            Dim model As New Object

            Dim modelId As String = ApiModelPrefix + apiDescriptionId
            If (Not config.Properties.TryGetValue(modelId, model)) Then
                Dim apiDescriptions As Collection(Of ApiDescription) = config.Services.GetApiExplorer().ApiDescriptions
                Dim ApiDescription As ApiDescription = apiDescriptions.FirstOrDefault(Function(api) String.Equals(api.GetFriendlyId(), apiDescriptionId, StringComparison.OrdinalIgnoreCase))
                If (Not ApiDescription Is Nothing) Then
                    model = GenerateApiModel(ApiDescription, config)
                    config.Properties.TryAdd(modelId, model)
                End If
            End If
            Return DirectCast(model, HelpPageApiModel)
        End Function

        Public Function GenerateApiModel(apiDescription As ApiDescription, config As HttpConfiguration) As HelpPageApiModel
            Dim apiModel As New HelpPageApiModel() With {
                .ApiDescription = apiDescription
            }

            Dim sampleGenerator As HelpPageSampleGenerator = config.GetHelpPageSampleGenerator()
            Dim modelGenerator As ModelDescriptionGenerator = config.GetModelDescriptionGenerator()
            GenerateUriParameters(apiModel, modelGenerator)
            GenerateRequestModelDescription(apiModel, modelGenerator, sampleGenerator)
            GenerateResourceDescription(apiModel, modelGenerator)
            GenerateSamples(apiModel, sampleGenerator)

            Return apiModel
        End Function

        Private Sub GenerateUriParameters(apiModel As HelpPageApiModel, modelGenerator As ModelDescriptionGenerator)
            Dim apiDescription As ApiDescription = apiModel.ApiDescription
            For Each apiParameter As ApiParameterDescription In apiDescription.ParameterDescriptions
                If apiParameter.Source = ApiParameterSource.FromUri Then
                    Dim parameterDescriptor As HttpParameterDescriptor = apiParameter.ParameterDescriptor
                    Dim parameterType As Type = Nothing
                    Dim typeDescription As ModelDescription = Nothing
                    Dim complexTypeDescription As ComplexTypeModelDescription = Nothing
                    If parameterDescriptor IsNot Nothing Then
                        parameterType = parameterDescriptor.ParameterType
                        typeDescription = modelGenerator.GetOrCreateModelDescription(parameterType)
                        complexTypeDescription = TryCast(typeDescription, ComplexTypeModelDescription)
                    End If

                    '' Example:
                    '' <TypeConverter(GetType(PointConverter))>
                    '' Public Class Point
                    ''     Public Sub New(x As Integer, y As Integer)
                    ''         x = x
                    ''         y = y
                    ''     End Sub
                    ''     Public X As Integer
                    ''     Public Y As Integer
                    '' End Class
                    '' Class Point is bindable with a TypeConverter, so Point will be added to UriParameters collection.
                    ''
                    '' Public Class Point
                    ''     Public X As Integer
                    ''     Public Y As Integer
                    '' End Class
                    '' Regular complex class Point will have properties X and Y added to UriParameters collection.
                    If complexTypeDescription IsNot Nothing AndAlso Not IsBindableWithTypeConverter(parameterType) Then
                        For Each uriParameter As ParameterDescription In complexTypeDescription.Properties
                            apiModel.UriParameters.Add(uriParameter)
                        Next
                    ElseIf parameterDescriptor IsNot Nothing Then
                        Dim uriParameter As ParameterDescription =
                            AddParameterDescription(apiModel, apiParameter, typeDescription)

                        If Not parameterDescriptor.IsOptional Then
                            uriParameter.Annotations.Add(New ParameterAnnotation() With {
                                .Documentation = "Required"
                            })
                        End If

                        Dim defaultValue As Object = parameterDescriptor.DefaultValue
                        If defaultValue IsNot Nothing Then
                            uriParameter.Annotations.Add(New ParameterAnnotation() With {
                                .Documentation = "Default value is " & Convert.ToString(defaultValue, CultureInfo.InvariantCulture)
                            })
                        End If
                    Else
                        Debug.Assert(parameterDescriptor Is Nothing)

                        '' If parameterDescriptor is Nothing, this is an undeclared route parameter which only occurs
                        '' when source is FromUri. Ignored in request model and among resource parameters but listed
                        '' as a simple string here.
                        Dim modelDescription As ModelDescription =
                            modelGenerator.GetOrCreateModelDescription(GetType(String))
                        AddParameterDescription(apiModel, apiParameter, modelDescription)
                    End If
                End If
            Next
        End Sub

        Private Function IsBindableWithTypeConverter(type As Type) As Boolean
            If type Is Nothing Then
                Return False
            End If

            Return TypeDescriptor.GetConverter(type).CanConvertFrom(GetType(String))
        End Function

        Private Function AddParameterDescription(apiModel As HelpPageApiModel, apiParameter As ApiParameterDescription,
                                                 typeDescription As ModelDescription) As ParameterDescription
            Dim parameterDescription As New ParameterDescription() With
            {
                .Name = apiParameter.Name,
                .Documentation = apiParameter.Documentation,
                .TypeDescription = typeDescription
            }

            apiModel.UriParameters.Add(parameterDescription)
            Return parameterDescription
        End Function

        Private Sub GenerateRequestModelDescription(apiModel As HelpPageApiModel, modelGenerator As ModelDescriptionGenerator, sampleGenerator As HelpPageSampleGenerator)
            Dim apiDescription As ApiDescription = apiModel.ApiDescription
            For Each apiParameter As ApiParameterDescription In apiDescription.ParameterDescriptions
                If apiParameter.Source = ApiParameterSource.FromBody Then
                    Dim parameterType As Type = apiParameter.ParameterDescriptor.ParameterType
                    apiModel.RequestModelDescription = modelGenerator.GetOrCreateModelDescription(parameterType)
                    apiModel.RequestDocumentation = apiParameter.Documentation
                ElseIf apiParameter.ParameterDescriptor IsNot Nothing AndAlso
                    apiParameter.ParameterDescriptor.ParameterType = GetType(HttpRequestMessage) Then
                    Dim parameterType As Type = sampleGenerator.ResolveHttpRequestMessageType(apiDescription)

                    If parameterType IsNot Nothing Then
                        apiModel.RequestModelDescription = modelGenerator.GetOrCreateModelDescription(parameterType)
                    End If
                End If
            Next
        End Sub

        Private Sub GenerateResourceDescription(apiModel As HelpPageApiModel, modelGenerator As ModelDescriptionGenerator)
            Dim response As ResponseDescription = apiModel.ApiDescription.ResponseDescription
            Dim responseType As Type = If(response.ResponseType, response.DeclaredType)
            If responseType IsNot Nothing AndAlso responseType <> GetType(System.Void) Then
                apiModel.ResourceDescription = modelGenerator.GetOrCreateModelDescription(responseType)
            End If
        End Sub

        <SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification:="The exception is recorded as ErrorMessages.")>
        Private Sub GenerateSamples(apiModel As HelpPageApiModel, sampleGenerator As HelpPageSampleGenerator)
            Try
                For Each item In sampleGenerator.GetSampleRequests(apiModel.ApiDescription)
                    apiModel.SampleRequests.Add(item.Key, item.Value)
                    LogInvalidSampleAsError(apiModel, item.Value)
                Next

                For Each item In sampleGenerator.GetSampleResponses(apiModel.ApiDescription)
                    apiModel.SampleResponses.Add(item.Key, item.Value)
                    LogInvalidSampleAsError(apiModel, item.Value)
                Next
            Catch e As Exception
                apiModel.ErrorMessages.Add(String.Format(CultureInfo.CurrentCulture,
                                                         "An exception has occurred while generating the sample. Exception message: {0}",
                                                         HelpPageSampleGenerator.UnwrapException(e).Message))
            End Try
        End Sub

        Private Function TryGetResourceParameter(apiDescription As ApiDescription, config As HttpConfiguration, ByRef parameterDescription As ApiParameterDescription, ByRef resourceType As Type) As Boolean
            parameterDescription = apiDescription.ParameterDescriptions.FirstOrDefault(
                Function(p) p.Source = ApiParameterSource.FromBody OrElse
                    (p.ParameterDescriptor IsNot Nothing AndAlso p.ParameterDescriptor.ParameterType = GetType(HttpRequestMessage)))

            If parameterDescription Is Nothing Then
                resourceType = Nothing
                Return False
            End If

            resourceType = parameterDescription.ParameterDescriptor.ParameterType

            If resourceType = GetType(HttpRequestMessage) Then
                Dim sampleGenerator As HelpPageSampleGenerator = config.GetHelpPageSampleGenerator()
                resourceType = sampleGenerator.ResolveHttpRequestMessageType(apiDescription)
            End If

            If resourceType Is Nothing Then
                parameterDescription = Nothing
                Return False
            End If

            Return True
        End Function

        Private Function InitializeModelDescriptionGenerator(config As HttpConfiguration) As ModelDescriptionGenerator
            Dim modelGenerator As New ModelDescriptionGenerator(config)
            Dim apis As Collection(Of ApiDescription) = config.Services.GetApiExplorer().ApiDescriptions
            For Each api As ApiDescription In apis
                Dim parameterDescription As ApiParameterDescription = Nothing
                Dim parameterType As Type = Nothing
                If TryGetResourceParameter(api, config, parameterDescription, parameterType) Then
                    modelGenerator.GetOrCreateModelDescription(parameterType)
                End If
            Next
            Return modelGenerator
        End Function

        Private Sub LogInvalidSampleAsError(apiModel As HelpPageApiModel, sample As Object)
            Dim invalidSample As InvalidSample = TryCast(sample, InvalidSample)
            If (Not invalidSample Is Nothing) Then
                apiModel.ErrorMessages.Add(invalidSample.ErrorMessage)
            End If
        End Sub
    End Module
End Namespace