Imports System.ServiceProcess
Imports System.Threading
Imports System.ComponentModel
Imports System.Configuration.Install

Public Class TiebaStatService
    Inherits ServiceBase

    Public Const ThisServiceName As String = "TiebaStatService"

    Private Timer1 As New Threading.Timer(AddressOf Timer1_Callback)
    Private LastNetworkIsAvailable As Boolean?
    Private ConfigurationPath As String = "F:\Dump\Tieba_Stat\config.xml"
    Private OutputPath As String = "F:\Dump\Tieba_Stat\stat.txt"
    Private TotalIntervalCount As Integer, TotalStatCount As Integer
    Private Config As StatConfig

    Private Sub StartTimer()
        Dim delay = Config.TimerModule - (Now.TimeOfDay.TotalSeconds Mod Config.TimerModule)
        Timer1.Change(CInt(1000 * delay), CInt(1000 * Config.Interval))
        EventLog.WriteEntry(String.Format("下一次统计将开始于{0}秒后，即{1}。", delay, Now + TimeSpan.FromSeconds(delay)), EventLogEntryType.Information)
    End Sub

    Private Sub StopTimer()
        Timer1.Change(Threading.Timeout.Infinite, Threading.Timeout.Infinite)
    End Sub

    Protected Overrides Sub OnStart(ByVal args() As String)
        ' 请在此处添加代码以启动您的服务。此方法应完成设置工作，
        ' 以使您的服务开始工作。
        My.Application.Log.DefaultFileLogWriter.TraceOutputOptions = TraceOptions.DateTime
        For Each EachArg In args
            Dim Cmd As String, Arg As String, Locator As Integer
            Locator = EachArg.IndexOf(":"c)
            If Locator > 0 Then
                Cmd = EachArg.Substring(0, Locator)
                Arg = EachArg.Substring(Locator + 1).Trim(" "c, ControlChars.Tab, """"c)
            Else
                Cmd = EachArg
                Arg = ""
            End If
            Select Case Cmd
                Case "-C"
                    ConfigurationPath = Arg
                Case "-O"
                    OutputPath = Arg
                    'Case "-V"
                    '    My.Application.Log.TraceSource.Switch = New SourceSwitch("VerboseSwitch", "Verbose")
            End Select
        Next
        '输出文件
        EnsureOutputFile
        '载入配置
        Try
            Config = New StatConfig(XDocument.Load(ConfigurationPath))
        Catch ex As Exception
            EventLog.WriteEntry(String.Format("无法载入配置文档：{0}，异常：{1}",
                                              ConfigurationPath, ex), EventLogEntryType.Error)
            Me.Stop()
        End Try
        TotalIntervalCount = 0
        TotalStatCount = 0
        LastNetworkIsAvailable = Nothing
        StartTimer()
        My.Application.Log.WriteEntry("OnStart()", TraceEventType.Information)
        My.Application.Log.DefaultFileLogWriter.Flush()
        EventLog.WriteEntry(String.Format("{0} 已运行。版本 {1}，日志文件：{2}。扫描间隔 {3} sec，配置文档 {4}，输出文档 {5}。目标：" & vbCrLf & "{6}",
                                          ThisServiceName, My.Application.Info.Version, My.Application.Log.DefaultFileLogWriter.FullLogFileName,
                                          Config.Interval, ConfigurationPath, OutputPath,
                                          String.Join(vbCrLf, Config.StatEntries)),
                                      EventLogEntryType.Information)
        MyBase.OnStart(args)
    End Sub

    Private Sub EnsureOutputFile()
        Try
            If Not My.Computer.FileSystem.FileExists(OutputPath) Then
                My.Computer.FileSystem.WriteAllText(OutputPath,
                            String.Join(vbTab, "Time", "Key", "Name", "SignsIn", "Topics", "Posts", "Members") & vbCrLf, True, Text.Encoding.UTF8)
            End If
        Catch ex As Exception
            EventLog.WriteEntry(String.Format("无法建立输出文档：{0}，异常：{1}",
                                              OutputPath, ex), EventLogEntryType.Error)
            Me.Stop()
        End Try
    End Sub

    Protected Overrides Sub OnStop()
        ' 在此处添加代码以执行任何必要的拆解操作，从而停止您的服务。
        StopTimer()
        My.Application.Log.WriteEntry("OnStop()", TraceEventType.Information)
        My.Application.Log.DefaultFileLogWriter.Flush()
        EventLog.WriteEntry(String.Format("{0} 已停止。TotalIntervalEntries : {1}, TotalStatistics : {2}.",
                                          ThisServiceName, TotalIntervalCount, TotalStatCount))
        MyBase.OnStop()
    End Sub

    Protected Overrides Sub OnPause()
        StopTimer()
        My.Application.Log.WriteEntry("OnPause()", TraceEventType.Information)
        My.Application.Log.DefaultFileLogWriter.Flush()
        EventLog.WriteEntry(String.Format("{0} 已暂停。TotalIntervalEntries : {1}, TotalStatistics : {2}.",
                                          ThisServiceName, TotalIntervalCount, TotalStatCount))
        MyBase.OnPause()
    End Sub

    Protected Overrides Sub OnContinue()
        StartTimer()
        My.Application.Log.WriteEntry("OnContinue()", TraceEventType.Information)
        My.Application.Log.DefaultFileLogWriter.Flush()
        EventLog.WriteEntry(String.Format("{0} 已恢复。", ThisServiceName))
        MyBase.OnContinue()
    End Sub

    Public Sub New()
        With Me
            .CanPauseAndContinue = True
            .ServiceName = ThisServiceName
            .AutoLog = False
        End With
    End Sub

    Private Sub Timer1_Callback(state As Object)
        If Now.TimeOfDay.TotalSeconds Mod Config.TimerModule > 2 Then
            StartTimer()    '重新对时
        End If
        TotalIntervalCount += 1
        If LastNetworkIsAvailable Is Nothing OrElse My.Computer.Network.IsAvailable <> LastNetworkIsAvailable Then
            If My.Computer.Network.IsAvailable Then
                My.Application.Log.WriteEntry("网络已连接。", TraceEventType.Information)
                LastNetworkIsAvailable = True
            Else
                My.Application.Log.WriteEntry("网络连接已断开。", TraceEventType.Information)
                LastNetworkIsAvailable = False
            End If
        End If
        Try
            DoStatistics()
            TotalStatCount += 1
        Catch ex As AggregateException
            My.Application.Log.WriteException(ex.InnerException)
        Catch ex As Exception
            My.Application.Log.WriteException(ex)
        End Try
        My.Application.Log.DefaultFileLogWriter.Flush()
    End Sub

    Private Sub TiebaStatService_Disposed(sender As Object, e As EventArgs) Handles Me.Disposed
        Timer1.Dispose()
    End Sub

    Sub DoStatistics()
        Dim SuccessCounter As Integer, ValidCounter As Integer
        Dim Writer As IO.StreamWriter = Nothing
        Try
            EnsureOutputFile()
            My.Application.Log.WriteEntry("开始统计。", TraceEventType.Information)
            Writer = My.Computer.FileSystem.OpenTextFileWriter(OutputPath, True, Text.Encoding.UTF8)
            For Each Item In Config.StatEntries
                Try
                    Dim Result = Item.DoStatistics
                    If Result IsNot Nothing Then
                        With Result.Value
                            Writer.WriteLine(String.Join(vbTab, .Time, .Key, .Name, .SignsIn, .Topics, .Posts, .Members))
                            SuccessCounter += 1
                            If .IsValid Then ValidCounter += 1
                        End With
#If DEBUG Then
                        My.Application.Log.WriteEntry(String.Format("成功写入统计记录：{0}。", Item.Key), TraceEventType.Verbose)
#End If
                    End If
                Catch ex As AggregateException
                    If LastNetworkIsAvailable OrElse Not TypeOf ex.InnerException Is Net.WebException Then
                        My.Application.Log.WriteException(ex.InnerException)
                    End If
                Catch ex As Exception
                    If LastNetworkIsAvailable OrElse Not TypeOf ex Is Net.WebException Then
                        My.Application.Log.WriteException(ex)
                    End If
                End Try
            Next
            My.Application.Log.WriteEntry(String.Format("统计完毕。{0}项成功，{1}项有效。", SuccessCounter, ValidCounter), TraceEventType.Information)
        Catch ex As Exception
            My.Application.Log.WriteException(ex)
        Finally
            If Writer IsNot Nothing Then Writer.Close()
        End Try
    End Sub
End Class

<RunInstaller(True)>
Public Class TiebaStatInstaller
    Inherits Installer

    Public Sub New()
        Me.Installers.Add(New ServiceProcessInstaller() With {.Account = ServiceAccount.LocalSystem})
        Me.Installers.Add(New ServiceInstaller() With {.ServiceName = TiebaStatService.ThisServiceName,
                                                       .Description = "百度帖吧宏观数据统计实用工具。by Chen X.y., 2013",
                                                       .StartType = ServiceStartMode.Manual})
    End Sub
End Class

Public Class StatConfig
    Private m_UrlTemplate As String = "http://tieba.baidu.com/f?kw={0}"
    Private m_Interval As Single = 600
    Private m_TimerModule As Single = 600
    Private m_StatEntries As New List(Of MonitorEntry)
    Private m_s_StatEntries As IList(Of MonitorEntry) = m_StatEntries.AsReadOnly

    Public ReadOnly Property UrlTemplate As String
        Get
            Return m_UrlTemplate
        End Get
    End Property

    Public ReadOnly Property Interval As Single
        Get
            Return m_Interval
        End Get
    End Property

    Public ReadOnly Property TimerModule As Single
        Get
            Return m_TimerModule
        End Get
    End Property

    Public ReadOnly Property StatEntries As IList(Of MonitorEntry)
        Get
            Return m_s_StatEntries
        End Get
    End Property

    Public Sub New(doc As XDocument)
        m_UrlTemplate = If(doc.Root.<urlTemplate>.Value, m_UrlTemplate)
        m_Interval = If(CType(doc.Root.<interval>.FirstOrDefault, Single?), m_Interval)
        m_TimerModule = If(CType(doc.Root.<timerModule>.FirstOrDefault, Single?), m_TimerModule)
        For Each EachEntry In doc.Root.<stat>.<entry>
            m_StatEntries.Add(New MonitorEntry(EachEntry.@key,
                          If(EachEntry.@url,
                             String.Format(m_UrlTemplate, EachEntry.Value))))
        Next
    End Sub
End Class
