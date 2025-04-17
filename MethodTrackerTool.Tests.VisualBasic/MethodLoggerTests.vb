
Imports MethodTrackerTool.VisualBasic.MockProject
Imports Xunit

Public Class MethodLoggerTests

    <Fact>
    Public Async Function Sample() As Task
        MethodLogger.Initialize()

        Await New OrderService().ProcessOrderAsync(New OrderRequest With {
                                                      .UserId = 13,
                                                      .ProductIds = New List(Of Integer)({1, 4, 55, 342, 33, 334, 864, 268, 1042}),
                                                      .TotalAmount = 20
                                                      })

        MethodLogger.PrintJson()
    End Function

End Class

