Imports System.Net

Public Structure StatisticEntry
    Private m_Time As Date
    Private m_Key As String
    Private m_Name As String
    Private m_SignsIn As Integer?
    Private m_Topics As Integer?
    Private m_Posts As Integer?
    Private m_Members As Integer?

    Public ReadOnly Property Time As Date
        Get
            Return m_Time
        End Get
    End Property

    Public ReadOnly Property Key As String
        Get
            Return m_Key
        End Get
    End Property

    Public ReadOnly Property Name As String
        Get
            Return m_Name
        End Get
    End Property

    Public ReadOnly Property SignsIn As Integer?
        Get
            Return m_SignsIn
        End Get
    End Property

    Public ReadOnly Property Topics As Integer?
        Get
            Return m_Topics
        End Get
    End Property

    Public ReadOnly Property Posts As Integer?
        Get
            Return m_Posts
        End Get
    End Property

    Public ReadOnly Property Members As Integer?
        Get
            Return m_Members
        End Get
    End Property

    Public ReadOnly Property IsValid As Boolean
        Get
            Return m_Topics IsNot Nothing AndAlso m_Posts IsNot Nothing AndAlso m_Members IsNot Nothing
        End Get
    End Property

    Public Sub New(time As Date, key As String, name As String,
                   signsIn As Integer?, topics As Integer?, posts As Integer?, members As Integer?)
        m_Time = time
        m_Key = key
        m_Name = name
        m_SignsIn = signsIn
        m_Topics = topics
        m_Posts = posts
        m_Members = members
    End Sub
End Structure

Public Structure MonitorEntry
    Private m_Key As String
    Private m_Url As String

    Public Property Key As String
        Get
            Return m_Key
        End Get
        Set(value As String)
            m_Key = value
        End Set
    End Property

    Public Property Url As String
        Get
            Return m_Url
        End Get
        Set(value As String)
            m_Url = value
        End Set
    End Property

    Public Function DoStatistics() As StatisticEntry?
        Dim Name As String = Nothing
        Dim SignsIn As Integer?, Topics As Integer?, Posts As Integer?, Members As Integer?
        Dim Client As New WebClient
        Client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)")
        Dim Task = Client.DownloadStringTaskAsync(Me.Url)
        If Task.Wait(10 * 1000) Then
            With New FieldLocator(Task.Result)
                If .Seek("<title>") Then Name = .PeekString("吧_百度贴吧")
                If .Seek("PageData.forum") Then
                    If .Seek("sign_count") Then SignsIn = .PeekInt32 : .PopLocation()
                    If .Seek("thread_num") Then Topics = .PeekInt32 : .PopLocation()
                    If .Seek("post_num") Then Posts = .PeekInt32 : .PopLocation()
                    If .Seek("member_num") Then Members = .PeekInt32 : .PopLocation()
                Else
                    If .
                        Seek("今日已签到") OrElse .Seek("今日签到") Then SignsIn = .PeekInt32
                    If .Seek("共有主题数") Then Topics = .PeekInt32
                    If .Seek("贴子数") Then Posts = .PeekInt32
                    If .Seek("数") Then Members = .PeekInt32
                End If
                Return New StatisticEntry(Now, Key, Name, SignsIn, Topics, Posts, Members)
            End With
        Else
            Return Nothing
        End If
    End Function

    Public Overrides Function ToString() As String
        Return String.Format("{0}，{1}", m_Key, m_Url)
    End Function

    Public Sub New(key As String, url As String)
        m_Key = key
        m_Url = url
    End Sub
End Structure

Public Class FieldLocator
    Private LocStack As New Stack(Of Integer)
    Private m_Text As String

    Public ReadOnly Property Location As Integer
        Get
            Return If(LocStack.Count > 0, LocStack.Peek, 0)
        End Get
    End Property

    Public Sub ResetLocation()
        LocStack.Clear()
    End Sub

    Public Function PopLocation() As Integer
        Return LocStack.Pop()
    End Function

    Private Sub OffsetLocation(offset As Integer)
        If LocStack.Count > 0 Then
            LocStack.Push(LocStack.Pop + offset)
        Else
            Debug.Assert(offset > 0)
            LocStack.Push(offset)
        End If
    End Sub

    Public ReadOnly Property Text As String
        Get
            Return m_Text
        End Get
    End Property

    Public Function SeekBefore(locator As String) As Boolean
        Dim newIndex = m_Text.IndexOf(locator, Location, StringComparison.OrdinalIgnoreCase)
        If newIndex >= 0 Then
            LocStack.Push(newIndex)
            Return True
        Else
            Return False
        End If
    End Function

    Public Function Seek(locator As String) As Boolean
        If SeekBefore(locator) Then
            OffsetLocation(Len(locator))
            Return True
        Else
            Return False
        End If
    End Function

    Public Function PeekDouble() As Double?
        Const MaxLength = 20
        Dim ValueIndex = m_Text.IndexOfAny("0123456789".ToCharArray, Location, MaxLength)
        If ValueIndex = -1 Then Return Nothing
        Return Val(m_Text.Substring(ValueIndex, MaxLength))
    End Function

    Public Function PeekInt32() As Int32?
        Return CType(PeekDouble(), Int32?)
    End Function

    Public Function PeekString(locator As String) As String
        Dim newIndex = m_Text.IndexOf(locator, Location, StringComparison.OrdinalIgnoreCase)
        If newIndex > 0 Then
            Return m_Text.Substring(Location, newIndex - Location)
        Else
            Return Nothing
        End If
    End Function

    Public Sub New(text As String)
        m_Text = text
        ResetLocation()
    End Sub
End Class