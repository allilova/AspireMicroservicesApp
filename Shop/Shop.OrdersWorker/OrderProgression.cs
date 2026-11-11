using System;
using System.Collections.Generic;
using System.Text;

namespace Shop.OrdersWorker;

public static class OrderProgression
{
    // връща следващия статус или null, ако поръчката още не е готова за промяна
    public static string? NextStatus(string status, TimeSpan age, WorkerOptions options) => status switch
    {
        "New" when age >= TimeSpan.FromSeconds(options.ConfirmAfterSeconds) => "Confirmed",
        "Confirmed" when age >= TimeSpan.FromSeconds(options.ShipAfterSeconds) => "Shipped",
        _ => null
    };
}
