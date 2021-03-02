@Imports System.Collections.ObjectModel
@Imports System.Web.Http.Description
@Imports System.Threading
@Imports SinglePageApp.Areas.HelpPage.ModelDescriptions
@ModelType IList(Of ParameterDescription)

@If Model.Count > 0 Then
    @<table class="help-page-table">
        <thead>
            <tr><th>Name</th><th>Description</th><th>Type</th><th>Additional information</th></tr>
        </thead>
        <tbody>
            @For Each parameter As ParameterDescription In Model
                Dim modelDescription As ModelDescription = parameter.TypeDescription
                @<tr>
                    <td class="parameter-name">@parameter.Name</td>
                    <td class="parameter-documentation">
                        <p>@parameter.Documentation</p>
                    </td>
                    <td class="parameter-type">
                        @Html.DisplayFor(Function(m) modelDescription.ModelType, "ModelDescriptionLink", New With {.modelDescription = modelDescription})
                    </td>
                    <td class="parameter-annotations">
                        @If parameter.Annotations.Count > 0 Then
                            @For Each annotation As ParameterAnnotation In parameter.Annotations
                                @<p>@annotation.Documentation</p>
                            Next
                        else
                            @<p>None.</p>
                        End If 
                    </td>
                </tr>
            Next
        </tbody>
    </table>
Else
    @<p>None.</p>
End If

