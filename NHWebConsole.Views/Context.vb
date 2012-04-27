Public Class Context
    Public Property Url As String
    Public Property Query As String
    Public Property QueryType As QueryType
    Public Property MaxResults As Integer?
    Public Property FirstResult As Integer?
    Public Property Results As IEnumerable(Of Row)
    Public Property Total As Integer
    Public Property [Error] As String
    Public Property NextPageUrl As String
    Public Property PrevPageUrl As String
    Public Property FirstPageUrl As String
    Public Property LimitLength As Boolean
    Public Property RawResult As Object
    Public Property Raw As Boolean
    Public Property ImageFields As String()
    Public Property ContentType As String
    Public Property AllEntities As IEnumerable(Of KeyValuePair(Of String, String))
    Public Property Output As String
    Public Property RssUrl As String
    Public Property ExtraRowTemplate As String
    Public Property Version As String
    Public Sub New()
        AllEntities = New List(Of KeyValuePair(Of String, String))
        Results = New List(Of Row)
    End Sub
End Class

Public Class Row
    Inherits List(Of KeyValuePair(Of String, XNode()))
End Class

Public Enum OperationType
    List
    Update
End Enum

Public Enum QueryType
    SQL
    HQL
End Enum
