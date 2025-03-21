Imports MethodTrackerTool.VisualBasic.MockProject.MethodTracker.MockProject
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Public Class MethodLoggerTests

    <TestMethod>
    Public Async Function Sample() As Task
        MethodLogger.Initialize("Sample")

        Await New OrderService().ProcessOrderAsync(New OrderRequest With {
                                                      .UserId = 13,
                                                      .ProductIds = New List(Of Integer)({1, 4, 55, 342, 33, 334, 864, 268, 1042}),
                                                      .TotalAmount = 20
                                                      })

        MethodLogger.PrintJson()
    End Function

End Class

