Module Module1

    Sub Main()
#If DEBUG Then
        My.Application.Log.WriteEntry(String.Format("{0} 版本 {1} DEBUG",
                                  My.Application.Info.Title,
                                  My.Application.Info.Version), TraceEventType.Information)
#Else
        My.Application.Log.WriteEntry(String.Format("{0} 版本 {1} RELEASE",
                            My.Application.Info.Title,
                            My.Application.Info.Version), TraceEventType.Information)
#End If
        Randomize(Timer)
        Dim NewInstance As New TiebaStatService
        System.ServiceProcess.ServiceBase.Run(NewInstance)
    End Sub

End Module
