using System;
using System.Collections.Generic;
using System.Text;

namespace Shop.OrdersWorker;

public class WorkerOptions
{
    public int PollSeconds { get; set; } = 5;
    public int ConfirmAfterSeconds { get; set; } = 15;
    public int ShipAfterSeconds { get; set; } = 40;
}
