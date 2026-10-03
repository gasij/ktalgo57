using CourierRouting.Model;
using CourierRouting.Planning;

namespace CourierRouting.Cli;

/// <summary>Печать постановки и плана в консоль.</summary>
internal static class ConsoleReport
{
    public static void PrintProblem(DeliveryProblem problem)
    {
        Console.WriteLine($"Склад: {problem.Depot}; смена: {problem.ShiftMinutes} мин; " +
                          $"перекрёстков: {problem.Network.NodeCount}, дуг: {problem.Network.RoadCount}");

        if (problem.Orders.Count == 0)
        {
            Console.WriteLine("Заказов нет.");
            return;
        }

        Console.WriteLine("Заказы:");
        foreach (var o in problem.Orders)
        {
            string deadline = o.DeadlineMinute is { } d ? $"дедлайн {d} мин" : "без дедлайна";
            Console.WriteLine($"  {o.Id,-4} {o.Location,-14} ценность {o.Value,-3} {deadline,-16} передача {o.ServiceMinutes} мин");
        }
    }

    public static void PrintPlan(string title, RoutePlan plan)
    {
        Console.WriteLine();
        Console.WriteLine($"--- {title} ---");

        if (plan.Stops.Count == 0)
        {
            Console.WriteLine("Маршрут пуст: ни один заказ нельзя доставить с соблюдением ограничений.");
        }
        else
        {
            foreach (var stop in plan.Stops)
            {
                string deadline = stop.Order.DeadlineMinute is { } d ? $" (дедлайн {d})" : "";
                Console.WriteLine($"  {stop.ArrivalMinute,4} мин  заказ {stop.Order.Id,-4} {stop.Order.Location}{deadline}");
            }

            Console.WriteLine($"  {plan.ReturnMinute,4} мин  возврат на склад");
            Console.WriteLine($"Путь: {string.Join(" → ", plan.StreetPath)}");
        }

        Console.WriteLine($"Итого: доставлено {plan.Stops.Count}, ценность {plan.TotalValue}, возврат в {plan.ReturnMinute} мин");

        foreach (var s in plan.Skipped)
            Console.WriteLine($"  не доставлен {s.Order.Id,-4} — {Describe(s.Reason)}");
    }

    public static void Compare(RoutePlan dp, RoutePlan greedy)
    {
        Console.WriteLine();
        if (greedy.TotalValue == dp.TotalValue)
            Console.WriteLine($"Сравнение: результаты совпали (ценность {dp.TotalValue}). " +
                              "Совпадение на одних данных не доказывает корректность жадного подхода.");
        else
            Console.WriteLine($"Сравнение: жадный {greedy.TotalValue} против DP {dp.TotalValue} — " +
                              $"жадный потерял {dp.TotalValue - greedy.TotalValue} ед. ценности.");
    }

    public static void Solve(DeliveryProblem problem)
    {
        PrintProblem(problem);
        var travel = new TravelTimes(problem);
        IRoutePlanner dpPlanner = new DpRoutePlanner(), greedyPlanner = new GreedyRoutePlanner();

        var dp = dpPlanner.Plan(problem, travel);
        var greedy = greedyPlanner.Plan(problem, travel);
        PrintPlan($"{dpPlanner.Name} (оптимум)", dp);
        PrintPlan(greedyPlanner.Name, greedy);
        Compare(dp, greedy);
    }

    private static string Describe(SkipReason reason) => reason switch
    {
        SkipReason.Unreachable => "адрес недостижим (нет пути туда или обратно)",
        SkipReason.DeadlineUnreachable => "к дедлайну не успеть даже при поездке напрямую",
        SkipReason.ShiftTooShort => "даже один этот заказ не укладывается в смену",
        SkipReason.NotSelected => "выполним отдельно, но не поместился в этот маршрут",
        _ => reason.ToString(),
    };
}
