Imports System
Imports System.Web

Namespace TestProject.TestClasses

    Public Class UA0005

        Public Function GetContext() As HttpContext
            Dim x = Foo.HttpContext.Current
            Return If(x, HttpContext.Current)
        End Function

        Public Sub TestMethod()
            Dim w = HttpContext.Current()
            Dim u = Bar.HttpContext.Current
            Dim y = System.Web.HttpContext.Current
            Console.WriteLine(HttpContext.Current)
        End Sub

    End Class
End Namespace

Namespace Foo

    Public Class HttpContext
        Public Shared Property Current As HttpContext
    End Class
End Namespace
