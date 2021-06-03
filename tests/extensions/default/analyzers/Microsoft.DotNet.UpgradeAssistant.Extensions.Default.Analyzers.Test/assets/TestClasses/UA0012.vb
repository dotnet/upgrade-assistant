' Licensed to the .NET Foundation under one Or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.

Imports System
Imports System.IO
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks
Imports System.Runtime.Serialization
Imports System.Runtime.Serialization.Formatters.Binary

Namespace Global
    Namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers.Test.assets.TestClasses
        Public Class UA0012
            Public Const ADDRESS_FILE As String = "DataFile1.dat"
            Public Const CONTACTS_FILE As String = "DataFile2.dat"
            Public Const PROFILES_FILE As String = "DataFile3.dat"
            Public Const PHONE_NUMBERS_FILE As String = "DataFile4.dat"
            Public Const SUPER_FILE As String = "DataFile5.dat"

            Public Function GetSuperPowers() As Dictionary(Of String, String)
                ' Open the file containing the data that you want to deserialize.
                Dim fs = New FileStream(SUPER_FILE, FileMode.Open)
                Try
                    Dim formatter1 = New SuperHeroicSerializer.BinaryFormatter()

                    ' Deserialize the hashtable from the file And
                    ' assign the reference to the local variable.
                    Dim superPowers As Dictionary(Of String, String) = formatter1.UnsafeDeserialize(fs, Nothing)
                    Return superPowers
                Catch e As SerializationException
                    Console.WriteLine("Failed to deserialize. Reason: " + e.Message)
                    Throw
                Finally
                    fs.Close()
                End Try
            End Function

            Public Function GetAddresses() As Dictionary(Of String, String)
                ' Open the file containing the data that you want to deserialize.
                Dim fs = New FileStream(ADDRESSES_FILE, FileMode.Open)
                Try
                    Dim formatter2 = New BinaryFormatter()

                    ' Deserialize the hashtable from the file And
                    ' assign the reference to the local variable.
                    Dim addresses As Dictionary(Of String, String) = formatter2.UnsafeDeserialize(fs, Nothing)
                    Return addresses
                Catch e As SerializationException
                    Console.WriteLine("Failed to deserialize. Reason: " + e.Message)
                    Throw
                Finally
                    fs.Close()
                End Try
            End Function

            Public Function GetAlternateIdentities() As Dictionary(Of String, String)
                ' Open the file containing the data that you want to deserialize.
                Dim fs = New FileStream(SUPER_FILE, FileMode.Open)
                Try
                    Dim formatter3 = New BinaryFormatter()

                    ' Deserialize the hashtable from the file And
                    ' assign the reference to the local variable.
                    Dim someValue As String
                    Dim identities As Dictionary(Of String, String) = formatter3.UnsafeDeserialize(fs, Function(x)
                                                                                                           someValue = "something from headers"
                                                                                                       End Function)
                    Return identities
                Catch e As SerializationException
                    Console.WriteLine("Failed to deserialize. Reason: " + e.Message)
                    Throw
                Finally
                    fs.Close()
                End Try
            End Function

            Public Function GetContacts() As Dictionary(Of String, String)
                ' Open the file containing the data that you want to deserialize.
                Dim fs = New FileStream(CONTACTS_FILE, FileMode.Open)
                Try
                    ' Deserialize the hashtable from the file And
                    ' assign the reference to the local variable.
                    Dim contacts As Dictionary(Of String, String) = New BinaryFormatter().UnsafeDeserialize(fs, Nothing)
                    Return contacts
                Catch e As SerializationException
                    Console.WriteLine("Failed to deserialize. Reason: " + e.Message)
                    Throw
                Finally
                    fs.Close()
                End Try
            End Function

            Public Function GetProfiles() As Dictionary(Of String, String)
                ' Open the file containing the data that you want to deserialize.
                Dim fs = New FileStream(PROFILES_FILE, FileMode.Open)
                Try
                    ' Deserialize the hashtable from the file And
                    ' assign the reference to the local variable.
                    Dim someValue As String
                    Dim profiles As Dictionary(Of String, String) = New BinaryFormatter().UnsafeDeserialize(fs, Function(x)
                                                                                                                    someValue = "something from headers"
                                                                                                                End Function)
                    Return profiles
                Catch e As SerializationException
                    Console.WriteLine("Failed to deserialize. Reason: " + e.Message)
                    Throw
                Finally
                    fs.Close()
                End Try
            End Function

        End Class

    End Namespace

    Namespace SuperHeroicSerializer

        Public Class BinaryFormatter
            Public Function UnsafeDeserialize(serializationStream As Stream, handler As System.Runtime.Remoting.Messaging.HeaderHandler) As Object
                Throw New NotImplementedException("under construction")
            End Function
        End Class

    End Namespace
End Namespace
