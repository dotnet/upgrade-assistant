
Public Class ValuesController
    Inherits Microsoft.AspNetCore.Mvc.Controller

    ' GET api/values
    Public Function GetValues() As IEnumerable(Of String)
        Return New String() {"value1", "value2"}
    End Function
End Class


Public Class NotAWebController
    Inherits Foo.ApiController

End Class

Namespace Foo
    Public Class ApiController

    End Class
End Namespace
