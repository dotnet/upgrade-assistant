Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Collections.Specialized
Imports System.ComponentModel.DataAnnotations
Imports System.Globalization
Imports System.Reflection
Imports System.Runtime.Serialization
Imports System.Web.Http
Imports System.Web.Http.Description
Imports System.Xml.Serialization
Imports Newtonsoft.Json

Namespace Areas.HelpPage.ModelDescriptions
    ''' <summary>
    ''' Generates model descriptions for given types.
    ''' </summary>
    Public Class ModelDescriptionGenerator
        ' Modify this to support more data annotation attributes.
        Private ReadOnly AnnotationTextGenerator As IDictionary(Of Type, Func(Of Object, String)) = New Dictionary(Of Type, Func(Of Object, String))() From { _
            {GetType(RequiredAttribute),
             Function(a) "Required"},
            {GetType(RangeAttribute),
             Function(a)
                 Dim range As RangeAttribute = DirectCast(a, RangeAttribute)
                 Return [String].Format(CultureInfo.CurrentCulture, "Range: inclusive between {0} and {1}", range.Minimum, range.Maximum)
             End Function},
            {GetType(MaxLengthAttribute),
             Function(a)
                 Dim maxLength As MaxLengthAttribute = DirectCast(a, MaxLengthAttribute)
                 Return [String].Format(CultureInfo.CurrentCulture, "Max length: {0}", maxLength.Length)
             End Function},
            {GetType(MinLengthAttribute),
             Function(a)
                 Dim minLength As MinLengthAttribute = DirectCast(a, MinLengthAttribute)
                 Return [String].Format(CultureInfo.CurrentCulture, "Min length: {0}", minLength.Length)
             End Function},
            {GetType(StringLengthAttribute),
             Function(a)
                 Dim strLength As StringLengthAttribute = DirectCast(a, StringLengthAttribute)
                 Return [String].Format(CultureInfo.CurrentCulture, "String length: inclusive between {0} and {1}", strLength.MinimumLength, strLength.MaximumLength)
             End Function},
            {GetType(DataTypeAttribute),
             Function(a)
                 Dim dataType As DataTypeAttribute = DirectCast(a, DataTypeAttribute)
                 Return [String].Format(CultureInfo.CurrentCulture, "Data type: {0}", If(dataType.CustomDataType, dataType.DataType.ToString()))
             End Function},
            {GetType(RegularExpressionAttribute),
             Function(a)
                 Dim regularExpression As RegularExpressionAttribute = DirectCast(a, RegularExpressionAttribute)
                 Return [String].Format(CultureInfo.CurrentCulture, "Matching regular expression pattern: {0}", regularExpression.Pattern)
             End Function}
        }

        ' Modify this to add more default documentations.
        Private ReadOnly DefaultTypeDocumentation As IDictionary(Of Type, String) = New Dictionary(Of Type, String)() From { _
            {GetType(Int16), "integer"},
            {GetType(Int32), "integer"},
            {GetType(Int64), "integer"},
            {GetType(UInt16), "unsigned integer"},
            {GetType(UInt32), "unsigned integer"},
            {GetType(UInt64), "unsigned integer"},
            {GetType([Byte]), "byte"},
            {GetType([Char]), "character"},
            {GetType([SByte]), "signed byte"},
            {GetType(Uri), "URI"},
            {GetType([Single]), "decimal number"},
            {GetType([Double]), "decimal number"},
            {GetType([Decimal]), "decimal number"},
            {GetType([String]), "string"},
            {GetType(Guid), "globally unique identifier"},
            {GetType(TimeSpan), "time interval"},
            {GetType(DateTime), "date"},
            {GetType(DateTimeOffset), "date"},
            {GetType([Boolean]), "boolean"}
        }

        Private _documentationProvider As Lazy(Of IModelDocumentationProvider)

        Public Sub New(config As HttpConfiguration)
            If config Is Nothing Then
                Throw New ArgumentNullException("config")
            End If

            _documentationProvider = New Lazy(Of IModelDocumentationProvider)(Function() TryCast(config.Services.GetDocumentationProvider(), IModelDocumentationProvider))
            GeneratedModels = New Dictionary(Of String, ModelDescription)(StringComparer.OrdinalIgnoreCase)
        End Sub

        Public Property GeneratedModels() As Dictionary(Of String, ModelDescription)
            Get
                Return m_GeneratedModels
            End Get
            Private Set(value As Dictionary(Of String, ModelDescription))
                m_GeneratedModels = value
            End Set
        End Property
        Private m_GeneratedModels As Dictionary(Of String, ModelDescription)

        Private ReadOnly Property DocumentationProvider() As IModelDocumentationProvider
            Get
                Return _documentationProvider.Value
            End Get
        End Property

        Public Function GetOrCreateModelDescription(modelType As Type) As ModelDescription
            If modelType Is Nothing Then
                Throw New ArgumentNullException("modelType")
            End If

            Dim underlyingType As Type = Nullable.GetUnderlyingType(modelType)
            If underlyingType IsNot Nothing Then
                modelType = underlyingType
            End If

            Dim modelDescription As ModelDescription = Nothing
            Dim modelName As String = ModelNameHelper.GetModelName(modelType)
            If GeneratedModels.TryGetValue(modelName, modelDescription) Then
                If modelType <> modelDescription.ModelType Then
                    Throw New InvalidOperationException([String].Format(CultureInfo.CurrentCulture, "A model description could not be created. Duplicate model name '{0}' was found for types '{1}' and '{2}'. " & "Use the [ModelName] attribute to change the model name for at least one of the types so that it has a unique name.", modelName, modelDescription.ModelType.FullName, modelType.FullName))
                End If

                Return modelDescription
            End If

            If DefaultTypeDocumentation.ContainsKey(modelType) Then
                Return GenerateSimpleTypeModelDescription(modelType)
            End If

            If modelType.IsEnum Then
                Return GenerateEnumTypeModelDescription(modelType)
            End If

            If modelType.IsGenericType Then
                Dim genericArguments As Type() = modelType.GetGenericArguments()

                If genericArguments.Length = 1 Then
                    Dim enumerableType As Type = GetType(IEnumerable(Of )).MakeGenericType(genericArguments)
                    If enumerableType.IsAssignableFrom(modelType) Then
                        Return GenerateCollectionModelDescription(modelType, genericArguments(0))
                    End If
                End If
                If genericArguments.Length = 2 Then
                    Dim dictionaryType As Type = GetType(IDictionary(Of ,)).MakeGenericType(genericArguments)
                    If dictionaryType.IsAssignableFrom(modelType) Then
                        Return GenerateDictionaryModelDescription(modelType, genericArguments(0), genericArguments(1))
                    End If

                    Dim keyValuePairType As Type = GetType(KeyValuePair(Of ,)).MakeGenericType(genericArguments)
                    If keyValuePairType.IsAssignableFrom(modelType) Then
                        Return GenerateKeyValuePairModelDescription(modelType, genericArguments(0), genericArguments(1))
                    End If
                End If
            End If

            If modelType.IsArray Then
                Dim elementType As Type = modelType.GetElementType()
                Return GenerateCollectionModelDescription(modelType, elementType)
            End If

            If modelType Is GetType(NameValueCollection) Then
                Return GenerateDictionaryModelDescription(modelType, GetType(String), GetType(String))
            End If

            If GetType(IDictionary).IsAssignableFrom(modelType) Then
                Return GenerateDictionaryModelDescription(modelType, GetType(Object), GetType(Object))
            End If

            If GetType(IEnumerable).IsAssignableFrom(modelType) Then
                Return GenerateCollectionModelDescription(modelType, GetType(Object))
            End If

            Return GenerateComplexTypeModelDescription(modelType)
        End Function

        ' Change this to provide different name for the member.
        Private Shared Function GetMemberName(member As MemberInfo, hasDataContractAttribute As Boolean) As String
            Dim jsonProperty As JsonPropertyAttribute = member.GetCustomAttribute(Of JsonPropertyAttribute)()
            If jsonProperty IsNot Nothing AndAlso Not [String].IsNullOrEmpty(jsonProperty.PropertyName) Then
                Return jsonProperty.PropertyName
            End If

            If hasDataContractAttribute Then
                Dim dataMember As DataMemberAttribute = member.GetCustomAttribute(Of DataMemberAttribute)()
                If dataMember IsNot Nothing AndAlso Not [String].IsNullOrEmpty(dataMember.Name) Then
                    Return dataMember.Name
                End If
            End If

            Return member.Name
        End Function

        Private Shared Function ShouldDisplayMember(member As MemberInfo, hasDataContractAttribute As Boolean) As Boolean
            Dim jsonIgnore As JsonIgnoreAttribute = member.GetCustomAttribute(Of JsonIgnoreAttribute)()
            Dim xmlIgnore As XmlIgnoreAttribute = member.GetCustomAttribute(Of XmlIgnoreAttribute)()
            Dim ignoreDataMember As IgnoreDataMemberAttribute = member.GetCustomAttribute(Of IgnoreDataMemberAttribute)()
            Dim nonSerialized As NonSerializedAttribute = member.GetCustomAttribute(Of NonSerializedAttribute)()
            Dim apiExplorerSetting As ApiExplorerSettingsAttribute = member.GetCustomAttribute(Of ApiExplorerSettingsAttribute)()

            Dim hasMemberAttribute As Boolean = If(member.DeclaringType.IsEnum, member.GetCustomAttribute(Of EnumMemberAttribute)() IsNot Nothing, member.GetCustomAttribute(Of DataMemberAttribute)() IsNot Nothing)

            ' Display member only if all the followings are true:
            ' no JsonIgnoreAttribute
            ' no XmlIgnoreAttribute
            ' no IgnoreDataMemberAttribute
            ' no NonSerializedAttribute
            ' no ApiExplorerSettingsAttribute with IgnoreApi set to true
            ' no DataContractAttribute without DataMemberAttribute or EnumMemberAttribute
            Return jsonIgnore Is Nothing AndAlso xmlIgnore Is Nothing AndAlso ignoreDataMember Is Nothing AndAlso nonSerialized Is Nothing AndAlso (apiExplorerSetting Is Nothing OrElse Not apiExplorerSetting.IgnoreApi) AndAlso (Not hasDataContractAttribute OrElse hasMemberAttribute)
        End Function

        Private Function CreateDefaultDocumentation(type As Type) As String
            Dim documentation As String = Nothing
            If DefaultTypeDocumentation.TryGetValue(type, documentation) Then
                Return documentation
            End If
            If DocumentationProvider IsNot Nothing Then
                documentation = DocumentationProvider.GetDocumentation(type)
            End If

            Return documentation
        End Function

        Private Sub GenerateAnnotations([property] As MemberInfo, propertyModel As ParameterDescription)
            Dim annotations As New List(Of ParameterAnnotation)()

            Dim attributes As IEnumerable(Of Attribute) = [property].GetCustomAttributes()
            For Each attribute As Attribute In attributes
                Dim textGenerator As Func(Of Object, String) = Nothing
                If AnnotationTextGenerator.TryGetValue(attribute.[GetType](), textGenerator) Then
                    annotations.Add(
                        New ParameterAnnotation() With {
                            .AnnotationAttribute = attribute,
                            .Documentation = textGenerator(attribute)
                        })
                End If
            Next

            ' Rearrange the annotations
            annotations.Sort(
                Function(x, y)
                    ' Special-case RequiredAttribute so that it shows up on top
                    If TypeOf x.AnnotationAttribute Is RequiredAttribute Then
                        Return -1
                    End If
                    If TypeOf y.AnnotationAttribute Is RequiredAttribute Then
                        Return 1
                    End If

                    ' Sort the rest based on alphabetic order of the documentation
                    Return [String].Compare(x.Documentation, y.Documentation, StringComparison.OrdinalIgnoreCase)

                End Function)

            For Each annotation As ParameterAnnotation In annotations
                propertyModel.Annotations.Add(annotation)
            Next
        End Sub

        Private Function GenerateCollectionModelDescription(modelType As Type, elementType As Type) As CollectionModelDescription
            Dim collectionModelDescription As ModelDescription = GetOrCreateModelDescription(elementType)
            If collectionModelDescription IsNot Nothing Then
                Return New CollectionModelDescription() With {
                    .Name = ModelNameHelper.GetModelName(modelType),
                    .ModelType = modelType,
                    .ElementDescription = collectionModelDescription
                }
            End If

            Return Nothing
        End Function

        Private Function GenerateComplexTypeModelDescription(modelType As Type) As ModelDescription
            Dim complexModelDescription As New ComplexTypeModelDescription() With {
                .Name = ModelNameHelper.GetModelName(modelType),
                .ModelType = modelType,
                .Documentation = CreateDefaultDocumentation(modelType)
            }

            GeneratedModels.Add(complexModelDescription.Name, complexModelDescription)
            Dim hasDataContractAttribute As Boolean = modelType.GetCustomAttribute(Of DataContractAttribute)() IsNot Nothing
            Dim properties As PropertyInfo() = modelType.GetProperties(BindingFlags.[Public] Or BindingFlags.Instance)
            For Each [property] As PropertyInfo In properties
                If ShouldDisplayMember([property], hasDataContractAttribute) Then
                    Dim propertyModel As New ParameterDescription() With {
                        .Name = GetMemberName([property], hasDataContractAttribute)
                    }

                    If DocumentationProvider IsNot Nothing Then
                        propertyModel.Documentation = DocumentationProvider.GetDocumentation([property])
                    End If

                    GenerateAnnotations([property], propertyModel)
                    complexModelDescription.Properties.Add(propertyModel)
                    propertyModel.TypeDescription = GetOrCreateModelDescription([property].PropertyType)
                End If
            Next

            Dim fields As FieldInfo() = modelType.GetFields(BindingFlags.[Public] Or BindingFlags.Instance)
            For Each field As FieldInfo In fields
                If ShouldDisplayMember(field, hasDataContractAttribute) Then
                    Dim propertyModel As New ParameterDescription() With {
                        .Name = GetMemberName(field, hasDataContractAttribute)
                    }

                    If DocumentationProvider IsNot Nothing Then
                        propertyModel.Documentation = DocumentationProvider.GetDocumentation(field)
                    End If

                    complexModelDescription.Properties.Add(propertyModel)
                    propertyModel.TypeDescription = GetOrCreateModelDescription(field.FieldType)
                End If
            Next

            Return complexModelDescription
        End Function

        Private Function GenerateDictionaryModelDescription(modelType As Type, keyType As Type, valueType As Type) As DictionaryModelDescription
            Dim keyModelDescription As ModelDescription = GetOrCreateModelDescription(keyType)
            Dim valueModelDescription As ModelDescription = GetOrCreateModelDescription(valueType)

            Return New DictionaryModelDescription() With {
                .Name = ModelNameHelper.GetModelName(modelType),
                .ModelType = modelType,
                .KeyModelDescription = keyModelDescription,
                .ValueModelDescription = valueModelDescription
            }
        End Function

        Private Function GenerateEnumTypeModelDescription(modelType As Type) As EnumTypeModelDescription
            Dim enumDescription As New EnumTypeModelDescription() With {
                .Name = ModelNameHelper.GetModelName(modelType),
                .ModelType = modelType,
                .Documentation = CreateDefaultDocumentation(modelType)
            }
            Dim hasDataContractAttribute As Boolean = modelType.GetCustomAttribute(Of DataContractAttribute)() IsNot Nothing
            For Each field As FieldInfo In modelType.GetFields(BindingFlags.[Public] Or BindingFlags.[Static])
                If ShouldDisplayMember(field, hasDataContractAttribute) Then
                    Dim enumValue As New EnumValueDescription() With {
                        .Name = field.Name,
                        .Value = field.GetRawConstantValue().ToString()
                    }
                    If DocumentationProvider IsNot Nothing Then
                        enumValue.Documentation = DocumentationProvider.GetDocumentation(field)
                    End If
                    enumDescription.Values.Add(enumValue)
                End If
            Next
            GeneratedModels.Add(enumDescription.Name, enumDescription)

            Return enumDescription
        End Function

        Private Function GenerateKeyValuePairModelDescription(modelType As Type, keyType As Type, valueType As Type) As KeyValuePairModelDescription
            Dim keyModelDescription As ModelDescription = GetOrCreateModelDescription(keyType)
            Dim valueModelDescription As ModelDescription = GetOrCreateModelDescription(valueType)

            Return New KeyValuePairModelDescription() With {
                .Name = ModelNameHelper.GetModelName(modelType),
                .ModelType = modelType,
                .KeyModelDescription = keyModelDescription,
                .ValueModelDescription = valueModelDescription
            }
        End Function

        Private Function GenerateSimpleTypeModelDescription(modelType As Type) As ModelDescription
            Dim simpleModelDescription As New SimpleTypeModelDescription() With {
                .Name = ModelNameHelper.GetModelName(modelType),
                .ModelType = modelType,
                .Documentation = CreateDefaultDocumentation(modelType)
            }
            GeneratedModels.Add(simpleModelDescription.Name, simpleModelDescription)

            Return simpleModelDescription
        End Function
    End Class
End Namespace