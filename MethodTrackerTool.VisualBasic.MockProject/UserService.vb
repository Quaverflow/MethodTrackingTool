Imports System.Globalization
Imports System.Threading.Tasks


Public Class User
    Public Property Id As Integer
    Public Property Name As String = ""
    Public Property Age As Integer
End Class

Public Class OrderRequest
    Public Property UserId As Integer
    Public Property ProductIds As List(Of Integer) = New List(Of Integer)()
    Public Property TotalAmount As Decimal
    Public Property X As Func(Of String) = Function() ""
End Class

Public Class Order
    Public Property OrderId As Integer
    Public Property Customer As User = New User()
    Public Property ProductIds As List(Of Integer) = New List(Of Integer)()
    Public Property TotalAmount As Decimal
    Public Property OrderDate As DateTime
    Public Property Type As Type
    Public Property CultureInfo As CultureInfo
End Class

Public Class PaymentResult
    Public Property Success As Boolean
    Public Property TransactionId As String = ""
End Class

Public Class OrderService

    Public Async Function ProcessOrderAsync(request As OrderRequest) As Task(Of Order)
        Console.WriteLine("OrderService: ProcessOrderAsync started.")
        ValidateOrder(request)

        Dim userService = New UserService()
        Dim customer = userService.GetUser(request.UserId)
        Dim order = BuildOrder(request, customer)

        Dim inventoryService = New InventoryService()
        Await inventoryService.ReserveInventoryAsync(order)

        Dim paymentService = New PaymentService()
        Await paymentService.ChargeUserAsync(customer, order)

        SaveOrder(order)

        Dim notificationService = New NotificationService()
        notificationService.SendOrderConfirmation(order)

        Console.WriteLine("OrderService: ProcessOrderAsync completed.")
        Return New Order()
        'Throw New Exception("Test exception")
    End Function

    Private Shared Sub ValidateOrder(request As OrderRequest)
        Console.WriteLine($"OrderService: Validating order request. {request}")
    End Sub

    Private Function BuildOrder(request As OrderRequest, customer As User) As Order
        Console.WriteLine("OrderService: Building order.")
        Return New Order With {
            .OrderId = New Random().Next(1000, 9999),
            .Customer = customer,
            .ProductIds = request.ProductIds,
            .TotalAmount = request.TotalAmount,
            .OrderDate = DateTime.Now,
            .Type = Me.GetType(),
            .CultureInfo = CultureInfo.CurrentCulture
            }
    End Function

    Private Sub SaveOrder(order As Order)
        Console.WriteLine("OrderService: Saving order.")
        LogOrder(order)
    End Sub

    Private Sub LogOrder(order As Order)
        Console.WriteLine($"OrderService: Order {order.OrderId} for {order.Customer.Name} logged.")
    End Sub

End Class

Public Class PaymentService

    Public Async Function ChargeUserAsync(user As User, order As Order) As Task(Of PaymentResult)
        Console.WriteLine("PaymentService: Charging user.")
        Await Task.Delay(300)

        Dim success = order.TotalAmount < 1000
        Dim transactionId = If(success, Guid.NewGuid().ToString(), If(user.ToString(), ""))

        Console.WriteLine("PaymentService: Charge completed.")

        Return New PaymentResult With {
            .Success = success,
            .TransactionId = transactionId
            }
    End Function

End Class

Public Class InventoryService

    Public Async Function ReserveInventoryAsync(order As Order) As Task(Of Boolean)
        Console.WriteLine("InventoryService: Reserving inventory.")
        Await Task.Delay(200)

        Dim success = order.ProductIds.Count <= 3
        Console.WriteLine("InventoryService: Reservation " & If(success, "successful.", "failed."))

        Return success
    End Function

End Class

Public Class NotificationService

    Public Sub SendOrderConfirmation(order As Order)
        Console.WriteLine($"NotificationService: Sending confirmation for Order {order.OrderId} to {order.Customer.Name}.")
        DoNotificationWork()
    End Sub

    Private Sub DoNotificationWork()
        Console.WriteLine("NotificationService: Doing additional notification work.")
        Capture()
    End Sub

    Public Sub Capture()
        Try
            Throw1()
        Catch ex As Exception
            Console.WriteLine(ex.ToString())
        End Try
    End Sub

    Public Sub Throw1()
        Throw2()
    End Sub

    Public Sub Throw2()
        Throw3()
    End Sub

    Public Sub Throw3()
        Throw4()
    End Sub

    Public Sub Throw4()
        Throw5()
    End Sub

    Public Sub Throw5()
        Throw New Exception("test exc")
    End Sub

End Class

Public Class UserService

    Public Function GetUser(id As Integer) As User
        Console.WriteLine("UserService: Getting user.")

        Dim name = GenerateUserName(id)
        Dim age = CalculateUserAge(id)

        Return New User With {
            .Id = id,
            .Name = name,
            .Age = age
            }
    End Function

    Private Function GenerateUserName(id As Integer) As String
        Console.WriteLine("UserService: Generating user name.")
        Return $"User-{id}"
    End Function

    Private Function CalculateUserAge(id As Integer) As Integer
        Console.WriteLine("UserService: Calculating user age.")
        Return 20 + id Mod 10
    End Function

End Class