' Uncomment the following to provide samples for PageResult(Of T). Must also add the Microsoft.AspNet.WebApi.OData
' package to your project.
''#Const Handle_PageResultOfT = 1

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Diagnostics.CodeAnalysis
Imports System.Linq
Imports System.Net.Http.Headers
Imports System.Reflection
Imports System.Web
Imports System.Web.Http
#If Handle_PageResultOfT Then
Imports System.Web.Http.OData
#End If

Namespace Areas.HelpPage
    ''' <summary>
    ''' Use this class to customize the Help Page.
    ''' For example you can set a custom <see cref="System.Web.Http.Description.IDocumentationProvider"/> to supply the documentation
    ''' or you can provide the samples for the requests/responses.
    ''' </summary>
    Public Module HelpPageConfig
        <SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters",
            MessageId:="SinglePageApp.Areas.HelpPage.TextSample.#ctor(System.String)",
            Justification:="End users may choose to merge this string with existing localized resources.")>
        <SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly",
            MessageId:="bsonspec",
            Justification:="Part of a URI.")>
        Public Sub Register(config As HttpConfiguration)
            '' Uncomment the following to use the documentation from XML documentation file.
            'config.SetDocumentationProvider(New XmlDocumentationProvider(HttpContext.Current.Server.MapPath("~/App_Data/XmlDocument.xml")))

            '' Uncomment the following to use "sample string" as the sample for all actions that have string as the body parameter or return type.
            '' Also, the string arrays will be used for IEnumerable(Of String). The sample objects will be serialized into different media type 
            '' formats by the available formatters.
            'config.SetSampleObjects(New Dictionary(Of Type, Object) From
            '{
            '     {GetType(String), "sample string"},
            '     {GetType(IEnumerable(Of String)), New String() {"sample 1", "sample 2"}}
            '})

            ' Extend the following to provide factories for types not handled automatically (those lacking parameterless
            ' constructors) or for which you prefer to use non-default property values. Line below provides a fallback
            ' since automatic handling will fail and GeneratePageResult handles only a single type.
#If Handle_PageResultOfT Then
            config.GetHelpPageSampleGenerator().SampleObjectFactories.Add(AddressOf GeneratePageResult)
#End If

            ' Extend the following to use a preset object directly as the sample for all actions that support a media
            ' type, regardless of the body parameter or return type. The lines below avoid display of binary content.
            ' The BsonMediaTypeFormatter (if available) is not used to serialize the TextSample object.
            config.SetSampleForMediaType(
                New TextSample("Binary JSON content. See http://bsonspec.org for details."),
                New MediaTypeHeaderValue("application/bson"))

            '' Uncomment the following to use "[0]=foo&[1]=bar" directly as the sample for all actions that support form URL encoded format
            '' and have IEnumerable(Of String) as the body parameter or return type.
            'config.SetSampleForType("[0]=foo&[1]=bar", New MediaTypeHeaderValue("application/x-www-form-urlencoded"), GetType(IEnumerable(Of String)))

            '' Uncomment the following to use "1234" directly as the request sample for media type "text/plain" on the controller named "Values"
            '' and action named "Put".
            'config.SetSampleRequest("1234", New MediaTypeHeaderValue("text/plain"), "Values", "Put")

            '' Uncomment the following to use the image on "../images/aspNetHome.png" directly as the response sample for media type "image/png"
            '' on the controller named "Values" and action named "Get" with parameter "id".
            'config.SetSampleResponse(New ImageSample("../images/aspNetHome.png"), New MediaTypeHeaderValue("image/png"), "Values", "Get", "id")

            '' Uncomment the following to correct the sample request when the action expects an HttpRequestMessage with ObjectContent(Of string).
            '' The sample will be generated as if the controller named "Values" and action named "Get" were having String as the body parameter.
            'config.SetActualRequestType(GetType(String), "Values", "Get")

            '' Uncomment the following to correct the sample response when the action returns an HttpResponseMessage with ObjectContent(Of String).
            '' The sample will be generated as if the controller named "Values" and action named "Post" were returning a String.
            'config.SetActualResponseType(GetType(String), "Values", "Post")
        End Sub

#If Handle_PageResultOfT Then
        Private Function GeneratePageResult(sampleGenerator As HelpPageSampleGenerator, type As Type) As Object
            If type.IsGenericType Then
                Dim openGenericType As Type = type.GetGenericTypeDefinition()
                If openGenericType = GetType(PageResult(Of )) Then
                    ' Get the T in PageResult(Of T)
                    Dim typeParameters() As Type = type.GetGenericArguments()
                    Debug.Assert(typeParameters.Length = 1)

                    ' Create an enumeration to pass as the first parameter to the PageResult(Of T) constuctor
                    Dim itemsType As Type = GetType(List(Of )).MakeGenericType(typeParameters)
                    Dim items As Object = sampleGenerator.GetSampleObject(itemsType)

                    ' Fill in the other information needed to invoke the PageResult(Of T) constuctor
                    Dim parameterTypes() As Type = New Type() {itemsType, GetType(Uri), GetType(Long?)}
                    Dim parameters() As Object = New Object() {items, Nothing, CLng(ObjectGenerator.DefaultCollectionSize)}

                    ' Call PageResult(items As IEnumerable(Of T), nextPageLink As Uri, count As Long?) constructor
                    Dim constructor As ConstructorInfo = type.GetConstructor(parameterTypes)
                    Return constructor.Invoke(parameters)
                End If
            End If

            Return Nothing
        End Function
#End If
    End Module
End Namespace