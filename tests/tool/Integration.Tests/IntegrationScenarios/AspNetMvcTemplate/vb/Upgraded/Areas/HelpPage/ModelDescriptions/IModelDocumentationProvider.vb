Imports System
Imports System.Reflection

Namespace Areas.HelpPage.ModelDescriptions
    Public Interface IModelDocumentationProvider
        Function GetDocumentation(member As MemberInfo) As String
        Function GetDocumentation(type As Type) As String
    End Interface
End Namespace