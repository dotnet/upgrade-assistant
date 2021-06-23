' Licensed to the .NET Foundation under one Or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks

Namespace TestClasses
    <AllowHtml()> ' Comment
    Public Class Class1
    End Class

    <System.AllowHtml()>
    <OutputCacheAttribute()> ' Comment2
    Public Class Class2
    End Class

    <Web.Mvc.ValidateInput(), System.Web.Mvc.AuthorizeAttribute>
    Public Class Class3
    End Class

    <Microsoft.AspNetCore.Mvc.Filters.ActionFilter()>
    <System.Web.Mvc.Authorize()>
    <ValidateInputAttribute()>
    Public Class Class4
        <Bind()>
        <Microsoft.AspNetCore.Authorization.AuthorizeAttribute>
        Public Sub Method1(
        <BindAttribute> ByVal s As String)
        End Sub
    End Class
End Namespace
