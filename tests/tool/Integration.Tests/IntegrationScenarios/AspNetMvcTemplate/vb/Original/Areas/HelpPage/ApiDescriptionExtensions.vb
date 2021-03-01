Imports System
Imports System.Runtime.CompilerServices
Imports System.Text
Imports System.Web
Imports System.Web.Http.Description

Namespace Areas.HelpPage
    Public Module ApiDescriptionExtensions
        '''<summary>
        '''Generates an URI-friendly ID for the <see cref="ApiDescription"/>. E.g. "Get-Values-id_name" instead of "GetValues/{id}?name={name}"
        '''</summary>
        '''<param name="description">The <see cref="ApiDescription"/>.</param>
        '''<returns>The ID as a string.</returns>
        <Extension()>
        Public Function GetFriendlyId(ByVal description As ApiDescription) As String
            Dim path As String = description.RelativePath
            Dim urlParts() As String = path.Split("?"c)
            Dim localPath As String = urlParts(0)

            Dim queryKeyString As String = Nothing

            If (urlParts.Length > 1) Then
                Dim query As String = urlParts(1)
                Dim queryKeys() As String = HttpUtility.ParseQueryString(query).AllKeys
                queryKeyString = String.Join("_", queryKeys)
            End If

            Dim friendlyPath As New StringBuilder

            friendlyPath.AppendFormat("{0}-{1}",
                description.HttpMethod.Method,
                localPath.Replace("/", "-").Replace("{", String.Empty).Replace("}", String.Empty))

            If (Not queryKeyString Is Nothing) Then
                friendlyPath.AppendFormat("_{0}", queryKeyString.Replace(".", "-"))
            End If

            GetFriendlyId = friendlyPath.ToString()
        End Function
    End Module
End Namespace