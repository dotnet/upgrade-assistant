Imports System.Web.Http
Imports System.Web.Mvc
Imports System.Collections.Generic

Namespace TestProject.TestClasses

    Public Class ValuesController2
        Inherits Microsoft.AspNetCore.Mvc.ControllerBase

        ' GET api/values
        Public Function GetValues() As IEnumerable(Of String)
            Dim fooService = New NotAWebController2()

            Return fooService
        End Function
    End Class

    Public Class MoviesController2
        Inherits Microsoft.AspNetCore.Mvc.ControllerBase

        ' GET api/movies
        Public Function GetMovies() As IEnumerable(Of String)
            Return New String() {"Star Wars", "Iron Man", "Star Trek"}
        End Function
    End Class

    Public Class NotAWebController2
        Inherits Foo.ApiController

        Public Function GetValues() As IEnumerable(Of String)
            Return New String() {"value1", "value2"}
        End Function

    End Class

    Public Class ValuesController3
        Inherits Microsoft.AspNetCore.Mvc.Controller

        ' GET api/values
        Public Function GetValues() As IEnumerable(Of String)
            Dim fooService = New NotAWebController2()

            Return fooService
        End Function
    End Class

    Public Class MoviesController3
        Inherits Microsoft.AspNetCore.Mvc.Controller

        ' GET api/movies
        Public Function GetMovies() As IEnumerable(Of String)
            Return New String() {"Star Wars", "Iron Man", "Star Trek"}
        End Function
    End Class

    Public Class NotAWebController3
        Inherits Foo.Controller

        Public Function GetValues() As IEnumerable(Of String)
            Return New String() {"value1", "value2"}
        End Function

    End Class

End Namespace

Namespace Foo
    Public Class ApiController

    End Class

    Public Class Controller

    End Class
End Namespace
