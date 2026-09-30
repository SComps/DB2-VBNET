Imports System.IO
Imports System.Text.Json
Imports Db2Spufi.Core.Models

Namespace Config

    Public Class ProfileManager
        Private ReadOnly _configFilePath As String

        Public Sub New(Optional configFilePath As String = Nothing)
            If String.IsNullOrWhiteSpace(configFilePath) Then
                Dim appDataDir As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Db2Spufi")
                If Not Directory.Exists(appDataDir) Then
                    Directory.CreateDirectory(appDataDir)
                End If
                _configFilePath = Path.Combine(appDataDir, "profiles.json")
            Else
                _configFilePath = configFilePath
                Dim dir As String = Path.GetDirectoryName(_configFilePath)
                If Not String.IsNullOrEmpty(dir) AndAlso Not Directory.Exists(dir) Then
                    Directory.CreateDirectory(dir)
                End If
            End If
        End Sub

        Public ReadOnly Property ConfigFilePath As String
            Get
                Return _configFilePath
            End Get
        End Property

        Public Function LoadProfiles() As List(Of ConnectionProfile)
            Try
                If File.Exists(_configFilePath) Then
                    Dim json As String = File.ReadAllText(_configFilePath)
                    Dim options As New JsonSerializerOptions With {.PropertyNameCaseInsensitive = True}
                    Dim list = JsonSerializer.Deserialize(Of List(Of ConnectionProfile))(json, options)
                    If list IsNot Nothing AndAlso list.Count > 0 Then
                        Return list
                    End If
                End If
            Catch ex As Exception
                ' Fall through to create default list if corrupt
            End Try

            Dim defaultList As New List(Of ConnectionProfile) From {
                New ConnectionProfile With {
                    .Name = "Db2 z/OS (DBD1LOC)",
                    .Server = "10.10.13.2",
                    .Port = 8103,
                    .Database = "DBD1LOC",
                    .User = "SCOTT",
                    .Password = "mlkhbu",
                    .CurrentSqlId = "SCOTT",
                    .IsDefault = True
                }
            }
            SaveProfiles(defaultList)
            Return defaultList
        End Function

        Public Sub SaveProfiles(profiles As IEnumerable(Of ConnectionProfile))
            Dim options As New JsonSerializerOptions With {
                .WriteIndented = True
            }
            Dim json As String = JsonSerializer.Serialize(profiles, options)
            File.WriteAllText(_configFilePath, json)
        End Sub

        Public Function GetDefaultProfile() As ConnectionProfile
            Dim profiles = LoadProfiles()
            Dim def = profiles.FirstOrDefault(Function(p) p.IsDefault)
            If def IsNot Nothing Then
                Return def
            End If
            Return profiles.FirstOrDefault()
        End Function

    End Class

End Namespace
