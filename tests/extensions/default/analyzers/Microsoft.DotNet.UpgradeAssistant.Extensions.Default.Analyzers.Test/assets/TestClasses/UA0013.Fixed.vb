Imports System.Web.Http
Imports System.Collections.Generic
Imports Microsoft.AspNetCore.Mvc

Namespace TestProject.TestClasses

    Public Class ValuesController2
        Inherits Controller

        ' GET api/values
        Public Function GetValues() As IEnumerable(Of String)
            Return New String() {"value1", "value2"}
        End Function
    End Class

    Public Class MoviesController2
        Inherits Controller

        ' GET api/movies
        Public Function GetMovies() As IEnumerable(Of String)
            Return New String() {"Star Wars", "Iron Man", "Star Trek"}
        End Function
    End Class

    Public Class NotAWebController2
        Inherits Foo.ApiController

    End Class

End Namespace

Namespace Foo
    Public Class ApiController

    End Class
End Namespace
