Imports System.Web.Http

Public Class TasksController
    Inherits ApiController

    Public Shared UserTasks As List(Of Task)

    ' GET api/tasks
    Public Function GetTasks() As IEnumerable(Of Task)
        Return UserTasks
    End Function

    ' POST api/tasks
    Public Sub PostTask(<FromBody()> ByVal newTask As Task)
        UserTasks.Add(newTask)
    End Sub
End Class
