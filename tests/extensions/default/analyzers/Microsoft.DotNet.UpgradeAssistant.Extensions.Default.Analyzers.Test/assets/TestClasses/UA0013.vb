Imports System.Web.Http
Imports System.Collections.Generic

Namespace TestProject.TestClasses

    Public Class ValuesController2
        Inherits ApiController

        ' GET api/values
        Public Function GetValues() As IEnumerable(Of String)
            Dim fooService = New NotAWebController2()

            Return fooService
        End Function
    End Class

    Public Class MoviesController2
        Inherits System.Web.Http.ApiController

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

End Namespace

Namespace Foo
    Public Class ApiController

    End Class
End Namespace
