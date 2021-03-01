Imports System
Imports System.Web.Http
Imports System.Web.Mvc
Imports SinglePageApp.Areas.HelpPage.Models
Imports SinglePageApp.Areas.HelpPage.ModelDescriptions

Namespace Areas.HelpPage.Controllers
    ''' <summary>
    ''' The controller that will handle requests for the help page.
    ''' </summary>
    Public Class HelpController
        Inherits Controller

        Private Const ErrorViewName As String = "Error"
        Private httpConfiguration As HttpConfiguration

        Public Sub New()
            Me.New(GlobalConfiguration.Configuration)
        End Sub

        Public Sub New(config As HttpConfiguration)
            Configuration = config
        End Sub

        Public Property Configuration As HttpConfiguration
            Get
                Return httpConfiguration
            End Get
            Private Set(value As HttpConfiguration)
                httpConfiguration = value
            End Set
        End Property

        Public Function Index() As ActionResult
            ViewData("DocumentationProvider") = Configuration.Services.GetDocumentationProvider()
            Return View(Configuration.Services.GetApiExplorer().ApiDescriptions)
        End Function

        Public Function Api(apiId As String) As ActionResult
            If (Not String.IsNullOrEmpty(apiId)) Then
                Dim apiModel As HelpPageApiModel = Configuration.GetHelpPageApiModel(apiId)
                If (Not apiModel Is Nothing) Then
                    Return View(apiModel)
                End If
            End If
            Return View(ErrorViewName)
        End Function

        Public Function ResourceModel(modelName As String) As ActionResult
            If Not [String].IsNullOrEmpty(modelName) Then
                Dim modelDescriptionGenerator As ModelDescriptionGenerator = Configuration.GetModelDescriptionGenerator()
                Dim modelDescription As ModelDescription = Nothing
                If modelDescriptionGenerator.GeneratedModels.TryGetValue(modelName, modelDescription) Then
                    Return View(modelDescription)
                End If
            End If

            Return View(ErrorViewName)
        End Function
    End Class
End Namespace
