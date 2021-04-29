Imports System.Web.Http
Imports System.Collections.Generic

Public Class ValuesController
    Inherits ApiController

    ' GET api/values
    Public Function GetValues() As IEnumerable(Of String)
        Return New String() {"value1", "value2"}
    End Function
End Class

Public Class MoviesController
    Inherits System.Web.Http.ApiController

    ' GET api/movies
    Public Function GetMovies() As IEnumerable(Of String)
        Return New String() {"Star Wars", "Iron Man", "Star Trek"}
    End Function
End Class

Public Class NotAWebController
    Inherits Foo.ApiController

End Class

Namespace Foo
    Public Class ApiController

    End Class
End Namespace
