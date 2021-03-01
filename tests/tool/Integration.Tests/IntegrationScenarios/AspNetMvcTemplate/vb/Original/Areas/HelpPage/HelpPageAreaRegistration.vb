Imports System.Web.Http
Imports System.Web.Mvc

Namespace Areas.HelpPage
    Public Class HelpPageAreaRegistration
        Inherits AreaRegistration

        Public Overrides ReadOnly Property AreaName As String
            Get
                Return "HelpPage"
            End Get
        End Property

        Public Overrides Sub RegisterArea(context As AreaRegistrationContext)
            context.MapRoute(
                "HelpPage_Default",
                "Help/{action}/{apiId}",
                New With {.Controller = "Help", .action = "Index", .apiId = UrlParameter.Optional})
            HelpPageConfig.Register(GlobalConfiguration.Configuration)
        End Sub
    End Class
End Namespace