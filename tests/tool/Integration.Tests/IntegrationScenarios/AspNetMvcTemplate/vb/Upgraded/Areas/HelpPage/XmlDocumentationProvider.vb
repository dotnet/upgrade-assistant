Imports System
Imports System.Globalization
Imports System.Linq
Imports System.Reflection
Imports System.Web.Http.Controllers
Imports System.Web.Http.Description
Imports System.Xml.XPath
Imports SinglePageApp.Areas.HelpPage.ModelDescriptions

Namespace Areas.HelpPage
    ''' <summary>
    ''' A custom <see cref="IDocumentationProvider"/> that reads the API documentation from an XML documentation file.
    ''' </summary>
    Public Class XmlDocumentationProvider
        Implements IDocumentationProvider
        Implements IModelDocumentationProvider

        Private _documentNavigator As XPathNavigator
        Private Const TypeExpression As String = "/doc/members/member[@name='T:{0}']"
        Private Const MethodExpression As String = "/doc/members/member[@name='M:{0}']"
        Private Const PropertyExpression As String = "/doc/members/member[@name='P:{0}']"
        Private Const FieldExpression As String = "/doc/members/member[@name='F:{0}']"
        Private Const ParameterExpression As String = "param[@name='{0}']"

        ''' <summary>
        ''' Initializes a new instance of the <see cref="XmlDocumentationProvider"/> class.
        ''' </summary>
        ''' <param name="documentPath">The physical path to XML document.</param>
        Public Sub New(documentPath As String)
            If (documentPath Is Nothing) Then
                Throw New ArgumentNullException("documentPath")
            End If
            Dim xpath As New XPathDocument(documentPath)
            _documentNavigator = xpath.CreateNavigator()
        End Sub

        Public Function GetDocumentation(controllerDescriptor As HttpControllerDescriptor) As String Implements IDocumentationProvider.GetDocumentation
            Dim typeNode As XPathNavigator = GetTypeNode(controllerDescriptor.ControllerType)
            Return GetTagValue(typeNode, "summary")
        End Function

        Public Function GetDocumentation(actionDescriptor As HttpActionDescriptor) As String Implements IDocumentationProvider.GetDocumentation
            Dim methodNode As XPathNavigator = GetMethodNode(actionDescriptor)
            Return GetTagValue(methodNode, "summary")
        End Function

        Public Function GetDocumentation(parameterDescriptor As HttpParameterDescriptor) As String Implements IDocumentationProvider.GetDocumentation
            Dim reflectedParameterDescriptor As ReflectedHttpParameterDescriptor = TryCast(parameterDescriptor, ReflectedHttpParameterDescriptor)
            If (Not reflectedParameterDescriptor Is Nothing) Then
                Dim methodNode As XPathNavigator = GetMethodNode(reflectedParameterDescriptor.ActionDescriptor)
                If (Not methodNode Is Nothing) Then
                    Dim parameterName As String = reflectedParameterDescriptor.ParameterInfo.Name
                    Dim parameterNode As XPathNavigator = methodNode.SelectSingleNode(String.Format(CultureInfo.InvariantCulture, ParameterExpression, parameterName))
                    If (Not parameterNode Is Nothing) Then
                        Return parameterNode.Value.Trim()
                    End If
                End If
            End If

            Return Nothing
        End Function

        Public Function GetResponseDocumentation(actionDescriptor As HttpActionDescriptor) As String Implements IDocumentationProvider.GetResponseDocumentation
            Dim methodNode As XPathNavigator = GetMethodNode(actionDescriptor)
            Return GetTagValue(methodNode, "returns")
        End Function

        Public Function GetDocumentation(member As MemberInfo) As String Implements IModelDocumentationProvider.GetDocumentation
            Dim memberName As String = [String].Format(CultureInfo.InvariantCulture, "{0}.{1}", GetTypeName(member.DeclaringType), member.Name)
            Dim expression As String = If(member.MemberType = MemberTypes.Field, FieldExpression, PropertyExpression)
            Dim selectExpression As String = [String].Format(CultureInfo.InvariantCulture, expression, memberName)
            Dim propertyNode As XPathNavigator = _documentNavigator.SelectSingleNode(selectExpression)
            Return GetTagValue(propertyNode, "summary")
        End Function

        Public Function GetDocumentation(type As Type) As String Implements IModelDocumentationProvider.GetDocumentation
            Dim typeNode As XPathNavigator = GetTypeNode(type)
            Return GetTagValue(typeNode, "summary")
        End Function

        Private Function GetMethodNode(actionDescriptor As HttpActionDescriptor) As XPathNavigator
            Dim reflectedActionDescriptor As ReflectedHttpActionDescriptor = TryCast(actionDescriptor, ReflectedHttpActionDescriptor)
            If (Not reflectedActionDescriptor Is Nothing) Then
                Dim selectExpression As String = String.Format(CultureInfo.InvariantCulture, MethodExpression, GetMemberName(reflectedActionDescriptor.MethodInfo))
                Return _documentNavigator.SelectSingleNode(selectExpression)
            End If

            Return Nothing
        End Function

        Private Shared Function GetMemberName(method As MethodInfo) As String
            Dim name As String = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", method.DeclaringType.FullName, method.Name)
            Dim parameters() As ParameterInfo = method.GetParameters()
            If (parameters.Length <> 0) Then
                Dim parameterTypeNames() As String = parameters.Select(Function(param) GetTypeName(param.ParameterType)).ToArray()
                name += String.Format(CultureInfo.InvariantCulture, "({0})", String.Join(",", parameterTypeNames))
            End If

            Return name
        End Function

        Private Shared Function GetTagValue(parentNode As XPathNavigator, tagName As String) As String
            If (Not parentNode Is Nothing) Then
                Dim node As XPathNavigator = parentNode.SelectSingleNode(tagName)
                If (Not node Is Nothing) Then
                    Return node.Value.Trim()
                End If
            End If

            Return Nothing
        End Function

        Private Function GetTypeNode(type As Type) As XPathNavigator
            Dim controllerTypeName As String = GetTypeName(type)
            Dim selectExpression As String = [String].Format(CultureInfo.InvariantCulture, TypeExpression, controllerTypeName)
            Return _documentNavigator.SelectSingleNode(selectExpression)
        End Function

        Private Shared Function GetTypeName(type As Type) As String
            Dim name As String = type.FullName
            If type.IsGenericType Then
                ' Format the generic type name to something like: Generic{System.Int32,System.String}
                Dim genericType As Type = type.GetGenericTypeDefinition()
                Dim genericArguments As Type() = type.GetGenericArguments()
                Dim genericTypeName As String = genericType.FullName

                ' Trim the generic parameter counts from the name
                genericTypeName = genericTypeName.Substring(0, genericTypeName.IndexOf("`"c))
                Dim argumentTypeNames As String() = genericArguments.[Select](Function(t) GetTypeName(t)).ToArray()
                name = [String].Format(CultureInfo.InvariantCulture, "{0}{{{1}}}", genericTypeName, [String].Join(",", argumentTypeNames))
            End If
            If type.IsNested Then
                ' Changing the nested type name from OuterType+InnerType to OuterType.InnerType to match the XML documentation syntax.
                name = name.Replace("+", ".")
            End If

            Return name
        End Function
    End Class
End Namespace