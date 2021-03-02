@Imports System.Net.Http.Headers
@ModelType Dictionary(Of MediaTypeHeaderValue, Object)

@Code
    'Group the samples into a single tab if they are the same.
    Dim samples As Dictionary(Of String, Object) = Model.GroupBy(Function(pair) pair.Value).ToDictionary(
        Function(pair) String.Join(", ", pair.Select(Function(m) m.Key.ToString()).ToArray()),
        Function(pair) pair.Key)
    Dim mediaTypes As Dictionary(Of String, Object).KeyCollection = samples.Keys
End Code
<div>
    @For Each mediaType As String In mediaTypes
        @<h4 class="sample-header">@mediaType</h4>
        @<div class="sample-content">
            <span><b>Sample:</b></span>
            @Code
            Dim sample As Object = samples(mediaType)
            If sample Is Nothing Then
                @<p>Sample not available.</p>
            Else
                @Html.DisplayFor(Function(s) sample)
            End If
            End code
        </div>
    Next
</div>