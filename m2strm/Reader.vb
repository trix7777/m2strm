Imports System.Threading

Class Reader
    Private Shared inputThread As Thread
    Private Shared getInput, gotInput As AutoResetEvent
    Private Shared input As String

    Shared Sub New()
        getInput = New AutoResetEvent(False)
        gotInput = New AutoResetEvent(False)
        inputThread = New Thread(AddressOf reader)
        inputThread.IsBackground = True
        inputThread.Start()
    End Sub

    Private Shared Sub reader()
        While True
            getInput.WaitOne()
            input = Console.ReadLine()
            gotInput.[Set]()
        End While
    End Sub

    Public Shared Function ReadLine(ByVal Optional timeOutMillisecs As Integer = Timeout.Infinite) As String
        getInput.[Set]()
        Dim success As Boolean = gotInput.WaitOne(timeOutMillisecs)

        If success Then
            Return input
        Else
            Return ""
        End If
    End Function
End Class