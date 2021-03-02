Imports System.Web.Http
Imports System.Web.Optimization

Public Class WebApiApplication
    Inherits System.Web.HttpApplication

    Sub Application_Start()
        TasksController.UserTasks = New List(Of Task)({
                                                      New Task() With {
                                                          .Text = "learn AngularJS",
                                                          .Done = True
                                                      },
                                                      New Task() With {
                                                          .Text = "build an AngularJS app"
                                                      }})

        AreaRegistration.RegisterAllAreas()
        GlobalConfiguration.Configure(AddressOf WebApiConfig.Register)
        FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters)
        RouteConfig.RegisterRoutes(RouteTable.Routes)
        BundleConfig.RegisterBundles(BundleTable.Bundles)
    End Sub
End Class
