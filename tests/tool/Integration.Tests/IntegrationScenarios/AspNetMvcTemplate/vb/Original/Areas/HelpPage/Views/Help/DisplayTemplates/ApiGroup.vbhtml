@Imports System.Web.Http
@Imports System.Web.Http.Controllers
@Imports System.Web.Http.Description
@Imports SinglePageApp.Areas.HelpPage
@ModelType IGrouping(Of HttpControllerDescriptor, ApiDescription)

@Code
    Dim controllerDocumentation As String = If(Not ViewData("DocumentationProvider") Is Nothing,
        ViewData("DocumentationProvider").GetDocumentation(Model.Key),
        Nothing)
End Code

<h2 id="@Model.Key.ControllerName">@Model.Key.ControllerName</h2>
@If Not controllerDocumentation Is Nothing Then
    @<p>@controllerDocumentation</p>
End If
<table class="help-page-table">
    <thead>
        <tr><th>API</th><th>Description</th></tr>
    </thead>
    <tbody>
    @For Each api As ApiDescription In Model
        @<tr>
            <td class="api-name"><a href="@Url.Action("Api", "Help", routeValues:=New With {.apiId = api.GetFriendlyId()})">@api.HttpMethod.Method @api.RelativePath</a></td>
            <td class="api-documentation">
            @If Not api.Documentation Is Nothing Then
                @<p>@api.Documentation</p>
            Else
                @<p>No documentation available.</p>
            End If
            </td>
        </tr>
    Next
    </tbody>
</table>