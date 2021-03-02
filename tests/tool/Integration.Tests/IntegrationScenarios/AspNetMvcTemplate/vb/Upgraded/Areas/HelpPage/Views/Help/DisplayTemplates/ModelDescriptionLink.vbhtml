@Imports SinglePageApp.Areas.HelpPage.ModelDescriptions
@ModelType Type
@Code
    Dim modelDescription As ModelDescription = ViewBag.modelDescription
    If TypeOf modelDescription Is ComplexTypeModelDescription Or TypeOf modelDescription Is EnumTypeModelDescription Then
        If Model Is GetType(Object) Then
            @:Object
        Else
            @Html.ActionLink(modelDescription.Name, "ResourceModel", "Help", New With {.modelName = modelDescription.Name}, Nothing)
        End If
    ElseIf TypeOf modelDescription Is CollectionModelDescription Then
        Dim collectionDescription As CollectionModelDescription = DirectCast(modelDescription, CollectionModelDescription)
        Dim elementDescription As ModelDescription = collectionDescription.ElementDescription
        @:Collection of @Html.DisplayFor(Function(m) elementDescription.ModelType, "ModelDescriptionLink", New With {.modelDescription = elementDescription})
    Else
        @Html.DisplayFor(Function(m) modelDescription)
    End If
End Code
