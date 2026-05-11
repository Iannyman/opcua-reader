Imports System.Data.SqlClient

Public Class SqlService

    Private ReadOnly _connectionString As String

    Public Sub New(connectionString As String)
        _connectionString = connectionString
    End Sub

    Public Async Function TestConnectionAsync() As Task(Of Boolean)
        Try
            Using conn As New SqlConnection(_connectionString)
                Await conn.OpenAsync()
                Return True
            End Using
        Catch
            Return False
        End Try
    End Function

    Public Async Function InsertZsbDataAsync(payload As String) As Task(Of String)
        Using conn As New SqlConnection(_connectionString)
            Await conn.OpenAsync()
            Using cmd As New SqlCommand("dbo.DC_insert_ZSB_data", conn)
                cmd.CommandType = CommandType.StoredProcedure
                cmd.Parameters.Add("@payload", SqlDbType.NVarChar, -1).Value = payload
                Dim resultParam = cmd.Parameters.Add("@result", SqlDbType.NVarChar, -1)
                resultParam.Direction = ParameterDirection.Output

                Await cmd.ExecuteNonQueryAsync()

                Return If(resultParam.Value?.ToString(), "")
            End Using
        End Using
    End Function

End Class
