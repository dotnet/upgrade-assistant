Imports System
Imports System.Globalization
Imports System.Linq
Imports System.Reflection

Namespace Areas.HelpPage.ModelDescriptions
    Friend NotInheritable Class ModelNameHelper
        Private Sub New()
        End Sub

        ' Modify this to provide custom model name mapping.
        Public Shared Function GetModelName(type As Type) As String
            Dim modelNameAttribute As ModelNameAttribute = type.GetCustomAttribute(Of ModelNameAttribute)()
            If modelNameAttribute IsNot Nothing AndAlso Not [String].IsNullOrEmpty(modelNameAttribute.Name) Then
                Return modelNameAttribute.Name
            End If

            Dim modelName As String = type.Name
            If type.IsGenericType Then
                ' Format the generic type name to something like: GenericOfAgurment1AndArgument2
                Dim genericType As Type = type.GetGenericTypeDefinition()
                Dim genericArguments As Type() = type.GetGenericArguments()
                Dim genericTypeName As String = genericType.Name

                ' Trim the generic parameter counts from the name
                genericTypeName = genericTypeName.Substring(0, genericTypeName.IndexOf("`"c))
                Dim argumentTypeNames As String() = genericArguments.[Select](Function(t) GetModelName(t)).ToArray()
                modelName = [String].Format(CultureInfo.InvariantCulture, "{0}Of{1}", genericTypeName, [String].Join("And", argumentTypeNames))
            End If

            Return modelName
        End Function
    End Class
End Namespace