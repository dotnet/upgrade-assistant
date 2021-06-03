' This Startup file Is based on ASP.NET Core New project templates And Is included
' as a starting point for DI registration And HTTP request processing pipeline configuration.
' This file will need updated according to the specific scenario of the application being upgraded.
' For more information on ASP.NET Core startup files, see https://docs.microsoft.com/aspnet/core/fundamentals/startup

Imports Microsoft.AspNetCore.Builder
Imports Microsoft.AspNetCore.Hosting
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting

Public Class Startup
    Public Sub New(configuration As IConfiguration)
        Me.Configuration = configuration
    End Sub

    Public ReadOnly Property Configuration As IConfiguration

    ' This method gets called by the runtime. Use this method to add services to the container.
    Public Sub ConfigureServices(services As IServiceCollection)
        services.AddControllers()
    End Sub

    ' This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    Public Sub Configure(app As IApplicationBuilder, env As IWebHostEnvironment)

        If (env.IsDevelopment()) Then
            app.UseDeveloperExceptionPage()
        End If

        app.UseFileServer()
        app.UseRouting()
        app.UseAuthorization()

        app.UseEndpoints(
             Sub(routes)
                 routes.MapControllers()
             End Sub)

    End Sub
End Class
