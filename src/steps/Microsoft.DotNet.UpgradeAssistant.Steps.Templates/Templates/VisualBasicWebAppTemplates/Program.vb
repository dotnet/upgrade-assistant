' This application entry point is based on ASP.NET Core new project templates and is included
' as a starting point for app host configuration.
' This file may need updated according to the specific scenario of the application being upgraded.
' For more information on ASP.NET Core hosting, see https://docs.microsoft.com/aspnet/core/fundamentals/host/web-host

Imports Microsoft.AspNetCore.Hosting
Imports Microsoft.Extensions.Hosting

Module Program
    Sub Main(args As String())
        CreateHostBuilder(args).Build().Run()
    End Sub

    Public Function CreateHostBuilder(args As String()) As IHostBuilder
        Return Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(
               Sub(webBuilder)
                   webBuilder.UseStartup(Of Startup)()
               End Sub
        )
    End Function
End Module
