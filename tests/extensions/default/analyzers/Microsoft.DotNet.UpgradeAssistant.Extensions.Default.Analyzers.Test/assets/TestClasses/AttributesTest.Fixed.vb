' Licensed to the .NET Foundation under one Or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks
Imports Microsoft.AspNetCore.Authorization
Imports Microsoft.AspNetCore.Mvc

Namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Analyzers.Test.assets.TestClasses
    Public Class Class1
    End Class

    <System.AllowHtml>
    <ResponseCache>
    Public Class Class2
    End Class

    <Authorize>
    Public Class Class3
    End Class

    <Microsoft.AspNetCore.Mvc.Filters.ActionFilter>
    <Authorize>
    Public Class Class4
        <Bind>
        <Microsoft.AspNetCore.Authorization.Authorize>
        Public Sub Method1(
        <BindAttribute> ByVal s As String)
        End Sub
    End Class
End Namespace
