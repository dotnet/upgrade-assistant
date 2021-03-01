Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics.CodeAnalysis
Imports System.Globalization
Imports System.Linq
Imports System.Reflection
Imports Microsoft.VisualBasic

Namespace Areas.HelpPage
    ''' <summary>
    ''' This class will create an object of a given type and populate it with sample data.
    ''' </summary>
    Public Class ObjectGenerator
        Friend Const DefaultCollectionSize As Integer = 2
        Private ReadOnly SimpleObjectGenerator As New SimpleTypeObjectGenerator()

        ''' <summary>
        ''' Generates an object for a given type. The type needs to be public, have a public default constructor and settable public properties/fields. Currently it supports the following types:
        ''' Simple types: <see cref="int"/>, <see cref="string"/>, <see cref="[Enum]"/>, <see cref="DateTime"/>, <see cref="Uri"/>, etc.
        ''' Complex types: POCO types.
        ''' Nullables: <see cref="Nullable(Of T)"/>.
        ''' Arrays: arrays of simple types or complex types.
        ''' Key value pairs: <see cref="KeyValuePair(Of TKey,TValue)"/>
        ''' Tuples: <see cref="Tuple(Of T1)"/>, <see cref="Tuple(Of T1,T2)"/>, etc
        ''' Dictionaries: <see cref="IDictionary(Of TKey,TValue)"/> or anything deriving from <see cref="IDictionary(Of TKey,TValue)"/>.
        ''' Collections: <see cref="IList(Of T)"/>, <see cref="IEnumerable(Of T)"/>, <see cref="ICollection(Of T)"/>, <see cref="IList"/>, <see cref="IEnumerable"/>, <see cref="ICollection"/> or anything deriving from <see cref="ICollection(Of T)"/> or <see cref="IList"/>.
        ''' Queryables: <see cref="System.Linq.IQueryable"/>, <see cref="IQueryable(Of T)"/>.
        ''' </summary>
        ''' <param name="type">The type.</param>
        ''' <returns>An object of the given type.</returns>
        Public Function GenerateObject(type As Type) As Object
            GenerateObject = GenerateObject(type, New Dictionary(Of Type, Object)())
        End Function

        <SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification:="Here we just want to return null if anything goes wrong.")>
        Private Function GenerateObject(type As Type, createdObjectReferences As Dictionary(Of Type, Object)) As Object
            Try
                If (SimpleTypeObjectGenerator.CanGenerateObject(type)) Then
                    Return SimpleObjectGenerator.GenerateObject(type)
                End If

                If (type.IsArray) Then
                    Return GenerateArray(type, DefaultCollectionSize, createdObjectReferences)
                End If

                If (type.IsGenericType) Then
                    Return GenerateGenericType(type, DefaultCollectionSize, createdObjectReferences)
                End If

                If (type Is GetType(IDictionary)) Then
                    Return GenerateDictionary(GetType(Hashtable), DefaultCollectionSize, createdObjectReferences)
                End If

                If (GetType(IDictionary).IsAssignableFrom(type)) Then
                    Return GenerateDictionary(type, DefaultCollectionSize, createdObjectReferences)
                End If

                If (type Is GetType(IList) Or
                        type Is GetType(IEnumerable) Or
                        type Is GetType(ICollection)) Then
                    Return GenerateCollection(GetType(ArrayList), DefaultCollectionSize, createdObjectReferences)
                End If

                If (GetType(IList).IsAssignableFrom(type)) Then
                    Return GenerateCollection(type, DefaultCollectionSize, createdObjectReferences)
                End If

                If (type Is GetType(IQueryable)) Then
                    Return GenerateQueryable(type, DefaultCollectionSize, createdObjectReferences)
                End If

                If (type.IsEnum) Then
                    Return GenerateEnum(type)
                End If

                If (type.IsPublic Or type.IsNestedPublic) Then
                    Return GenerateComplexObject(type, createdObjectReferences)
                End If
            Catch
                ' Returns Nothing if anything fails
                Return Nothing
            End Try

            Return Nothing
        End Function

        Private Shared Function GenerateGenericType(type As Type, collectionSize As Integer, createdObjectReferences As Dictionary(Of Type, Object)) As Object
            Dim genericTypeDefinition As Type = type.GetGenericTypeDefinition()

            If (genericTypeDefinition Is GetType(Nullable()) Or genericTypeDefinition Is GetType(Nullable(Of ))) Then
                Return GenerateNullable(type, createdObjectReferences)
            End If

            If (genericTypeDefinition Is GetType(KeyValuePair(Of ,))) Then
                Return GenerateKeyValuePair(type, createdObjectReferences)
            End If

            If (IsTuple(genericTypeDefinition)) Then
                Return GenerateTuple(type, createdObjectReferences)
            End If

            Dim genericArguments() As Type = type.GetGenericArguments()

            If (genericArguments.Length = 1) Then
                If (genericTypeDefinition Is GetType(IList(Of )) Or
                    genericTypeDefinition Is GetType(IEnumerable(Of )) Or
                    genericTypeDefinition Is GetType(ICollection(Of ))) Then

                    Dim collectionType As Type = GetType(List(Of )).MakeGenericType(genericArguments)
                    Return GenerateCollection(collectionType, collectionSize, createdObjectReferences)
                End If

                If (genericTypeDefinition Is GetType(IQueryable(Of ))) Then
                    Return GenerateQueryable(type, collectionSize, createdObjectReferences)
                End If

                Dim closedCollectionType As Type = GetType(ICollection(Of )).MakeGenericType(genericArguments(0))
                If (closedCollectionType.IsAssignableFrom(type)) Then
                    Return GenerateCollection(type, collectionSize, createdObjectReferences)
                End If
            End If

            If (genericArguments.Length = 2) Then
                If (genericTypeDefinition Is GetType(IDictionary(Of ,))) Then
                    Dim dictionaryType As Type = GetType(Dictionary(Of ,)).MakeGenericType(genericArguments)
                    Return GenerateDictionary(dictionaryType, collectionSize, createdObjectReferences)
                End If

                Dim closedDictionaryType As Type = GetType(IDictionary(Of ,)).MakeGenericType(genericArguments(0), genericArguments(1))
                If (closedDictionaryType.IsAssignableFrom(type)) Then
                    Return GenerateDictionary(type, collectionSize, createdObjectReferences)
                End If
            End If

            If (type.IsPublic Or type.IsNestedPublic) Then
                Return GenerateComplexObject(type, createdObjectReferences)
            End If

            Return Nothing
        End Function

        Private Shared Function GenerateTuple(type As Type, createdObjectReferences As Dictionary(Of Type, Object)) As Object
            Dim genericArgs() As Type = type.GetGenericArguments()
            Dim parameterValues(genericArgs.Length - 1) As Object
            Dim failedToCreateTuple As Boolean = True

            Dim objectGenerator As New ObjectGenerator()
            For i As Integer = 0 To genericArgs.Length - 1
                parameterValues(i) = objectGenerator.GenerateObject(genericArgs(i), createdObjectReferences)
                failedToCreateTuple = failedToCreateTuple And (parameterValues(i) Is Nothing)
            Next

            If (failedToCreateTuple) Then
                Return Nothing
            End If

            Return Activator.CreateInstance(type, parameterValues)
        End Function

        Private Shared Function IsTuple(genericTypeDefinition As Type) As Boolean
            Return (genericTypeDefinition Is GetType(Tuple(Of )) Or
                genericTypeDefinition Is GetType(Tuple(Of ,)) Or
                genericTypeDefinition Is GetType(Tuple(Of ,,)) Or
                genericTypeDefinition Is GetType(Tuple(Of ,,,)) Or
                genericTypeDefinition Is GetType(Tuple(Of ,,,,)) Or
                genericTypeDefinition Is GetType(Tuple(Of ,,,,,)) Or
                genericTypeDefinition Is GetType(Tuple(Of ,,,,,,)) Or
                genericTypeDefinition Is GetType(Tuple(Of ,,,,,,,)))
        End Function

        Private Shared Function GenerateKeyValuePair(keyValuePairType As Type, createdObjectReferences As Dictionary(Of Type, Object)) As Object
            Dim genericArgs() As Type = keyValuePairType.GetGenericArguments()
            Dim typeK As Type = genericArgs(0)
            Dim typeV As Type = genericArgs(1)

            Dim objectGenerator As New ObjectGenerator()

            Dim keyObject As Object = objectGenerator.GenerateObject(typeK, createdObjectReferences)
            Dim valueObject As Object = objectGenerator.GenerateObject(typeV, createdObjectReferences)
            If (keyObject Is Nothing And valueObject Is Nothing) Then
                ' Failed to create key and values
                Return Nothing
            End If

            Return Activator.CreateInstance(keyValuePairType, keyObject, valueObject)
        End Function

        Private Shared Function GenerateArray(arrayType As Type, size As Integer, createdObjectReferences As Dictionary(Of Type, Object)) As Object
            Dim type As Type = arrayType.GetElementType()
            Dim result As Array = Array.CreateInstance(type, size)
            Dim areAllElementsNothing As Boolean = True

            Dim objectGenerator As New ObjectGenerator()

            For i As Integer = 0 To size - 1
                Dim element As Object = objectGenerator.GenerateObject(type, createdObjectReferences)
                result.SetValue(element, i)
                areAllElementsNothing = areAllElementsNothing And (element Is Nothing)
            Next

            If (areAllElementsNothing) Then
                Return Nothing
            End If

            Return result
        End Function

        Private Shared Function GenerateDictionary(dictionaryType As Type, size As Integer, createdObjectReferences As Dictionary(Of Type, Object)) As Object
            Dim typeK As Type = GetType(Object)
            Dim typeV As Type = GetType(Object)
            If (dictionaryType.IsGenericType) Then
                Dim genericArgs() As Type = dictionaryType.GetGenericArguments()
                typeK = genericArgs(0)
                typeV = genericArgs(1)
            End If

            Dim result As Object = Activator.CreateInstance(dictionaryType)
            Dim addMethod As MethodInfo = If(dictionaryType.GetMethod("Add"), dictionaryType.GetMethod("TryAdd"))
            Dim containsMethod As MethodInfo = If(dictionaryType.GetMethod("Contains"), dictionaryType.GetMethod("ContainsKey"))

            Dim objectGenerator As New ObjectGenerator()

            For i As Integer = 0 To size - 1
                Dim newKey As Object = objectGenerator.GenerateObject(typeK, createdObjectReferences)
                If (newKey Is Nothing) Then
                    ' Cannot generate a valid key
                    Return Nothing
                End If

                Dim containsKey As Boolean = DirectCast(containsMethod.Invoke(result, New Object() {newKey}), Boolean)

                If Not containsKey Then
                    Dim newValue As Object = objectGenerator.GenerateObject(typeV, createdObjectReferences)
                    addMethod.Invoke(result, New Object() {newKey, newValue})
                End If
            Next

            Return result
        End Function

        Private Shared Function GenerateEnum(enumType As Type) As Object
            Dim possibleValues As Array = [Enum].GetValues(enumType)
            If possibleValues.Length > 0 Then
                Return possibleValues.GetValue(0)
            End If
            Return Nothing
        End Function

        Private Shared Function GenerateQueryable(queryableType As Type, size As Integer, createdObjectReferences As Dictionary(Of Type, Object)) As Object
            Dim isGeneric As Boolean = queryableType.IsGenericType
            Dim list As Object = Nothing
            If (isGeneric) Then
                Dim listType As Type = GetType(List(Of )).MakeGenericType(queryableType.GetGenericArguments())
                list = GenerateCollection(listType, size, createdObjectReferences)
            Else
                list = GenerateArray(GetType(Object()), size, createdObjectReferences)
            End If

            If (list Is Nothing) Then
                Return Nothing
            End If

            If (isGeneric) Then
                Dim argumentType As Type = GetType(IEnumerable(Of )).MakeGenericType(queryableType.GetGenericArguments())
                Dim asQueryableMethod As MethodInfo = GetType(Queryable).GetMethod("AsQueryable", New Type() {argumentType})
                Return asQueryableMethod.Invoke(Nothing, New Object() {list})
            End If

            Return Queryable.AsQueryable(DirectCast(list, IEnumerable))
        End Function

        Private Shared Function GenerateCollection(collectionType As Type, size As Integer, createdObjectReferences As Dictionary(Of Type, Object)) As Object
            Dim type As Type = If(collectionType.IsGenericType, collectionType.GetGenericArguments()(0), GetType(Object))

            Dim result As Object = Activator.CreateInstance(collectionType)
            Dim addMethod As MethodInfo = collectionType.GetMethod("Add")
            Dim areAllElementsNothing As Boolean = True
            Dim objectGenerator As New ObjectGenerator()

            For i As Integer = 0 To size - 1
                Dim element As Object = objectGenerator.GenerateObject(type, createdObjectReferences)
                addMethod.Invoke(result, New Object() {element})
                areAllElementsNothing = areAllElementsNothing And (element Is Nothing)
            Next

            If (areAllElementsNothing) Then
                Return Nothing
            End If

            Return result
        End Function

        Private Shared Function GenerateNullable(nullableType As Type, createdObjectReferences As Dictionary(Of Type, Object)) As Object
            Dim type As Type = nullableType.GetGenericArguments()(0)
            Dim objectGenerator As New ObjectGenerator()
            Return objectGenerator.GenerateObject(type, createdObjectReferences)
        End Function

        Private Shared Function GenerateComplexObject(type As Type, createdObjectReferences As Dictionary(Of Type, Object)) As Object
            Dim result As Object = Nothing

            If (createdObjectReferences.TryGetValue(type, result)) Then
                ' The object has been created already, just return it. This will handle the circular reference case.
                Return result
            End If

            If (type.IsValueType) Then
                result = Activator.CreateInstance(type)
            Else
                Dim defaultCtor As ConstructorInfo = type.GetConstructor(type.EmptyTypes)
                If (defaultCtor Is Nothing) Then
                    ' Cannot instantiate the type because it doesn't have a default constructor
                    Return Nothing
                End If

                result = defaultCtor.Invoke(New Object() {})
            End If

            createdObjectReferences.Add(type, result)
            SetPublicProperties(type, result, createdObjectReferences)
            SetPublicFields(type, result, createdObjectReferences)
            Return result
        End Function

        Private Shared Sub SetPublicProperties(type As Type, obj As Object, createdObjectReferences As Dictionary(Of Type, Object))
            Dim properties() As PropertyInfo = type.GetProperties(BindingFlags.Public Or BindingFlags.Instance)
            Dim objectGenerator As New ObjectGenerator()
            For Each prop As PropertyInfo In properties
                If (prop.CanWrite) Then
                    Dim propertyValue As Object = objectGenerator.GenerateObject(prop.PropertyType, createdObjectReferences)
                    prop.SetValue(obj, propertyValue, Nothing)
                End If
            Next
        End Sub

        Private Shared Sub SetPublicFields(type As Type, obj As Object, createdObjectReferences As Dictionary(Of Type, Object))
            Dim fields() As FieldInfo = type.GetFields(BindingFlags.Public Or BindingFlags.Instance)
            Dim objectGenerator As New ObjectGenerator()
            For Each field As FieldInfo In fields
                Dim fieldValue As Object = objectGenerator.GenerateObject(field.FieldType, createdObjectReferences)
                field.SetValue(obj, fieldValue)
            Next
        End Sub

        Private Class SimpleTypeObjectGenerator
            Private _index As Long = 0

            Private Shared ReadOnly DefaultGenerators As Dictionary(Of Type, Func(Of Long, Object)) = InitializeGenerators()

            <SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification:="These are simple type factories and cannot be split up.")>
            Private Shared Function InitializeGenerators() As Dictionary(Of Type, Func(Of Long, Object))
                Return New Dictionary(Of Type, Func(Of Long, Object)) From
                {
                    {GetType(Boolean), Function(index As Long) True},
                    {GetType(Byte), Function(index As Long) CByte(64)},
                    {GetType(Char), Function(index As Long) ChrW(65)},
                    {GetType(DateTime), Function(index As Long) DateTime.Now},
                    {GetType(DateTimeOffset), Function(index As Long) New DateTimeOffset(DateTime.Now)},
                    {GetType(DBNull), Function(index As Long) DBNull.Value},
                    {GetType(Decimal), Function(index As Long) CDec(index)},
                    {GetType(Double), Function(index As Long) CDbl(index) + 0.1},
                    {GetType(Guid), Function(index As Long) Guid.NewGuid()},
                    {GetType(Int16), Function(index As Long) CType(index Mod Int16.MaxValue, Int16)},
                    {GetType(Int32), Function(index As Long) CType(index Mod Int32.MaxValue, Int32)},
                    {GetType(Int64), Function(index As Long) CType(index, Int64)},
                    {GetType(Object), Function(index As Long) New Object},
                    {GetType(SByte), Function(index As Long) CSByte(64)},
                    {GetType(Single), Function(index As Long) CSng(index + 0.1)},
                    {GetType(String), Function(index As Long) String.Format(CultureInfo.CurrentCulture, "sample string {0}", index)},
                    {GetType(TimeSpan), Function(index As Long) TimeSpan.FromTicks(1234567)},
                    {GetType(UInt16), Function(index As Long) CType(index Mod UInt16.MaxValue, UInt16)},
                    {GetType(UInt32), Function(index As Long) CType(index Mod UInt32.MaxValue, UInt32)},
                    {GetType(UInt64), Function(index As Long) CType(index, UInt64)},
                    {GetType(Uri), Function(index As Long) New Uri(String.Format(CultureInfo.CurrentCulture, "http://webapihelppage{0}.com", index))}
                }
            End Function

            Public Shared Function CanGenerateObject(type As Type) As Boolean
                Return DefaultGenerators.ContainsKey(type)
            End Function

            Public Function GenerateObject(type As Type) As Object
                _index += 1
                Return DefaultGenerators(type)(_index)
            End Function
        End Class
    End Class
End Namespace