using CourierRouting.Model;

namespace CourierRouting.Planning;

/// <summary>
/// Превращает выбранную последовательность заказов в готовый план: считает время прибытия
/// на каждый адрес, проверяет дедлайны и возврат на склад, восстанавливает путь по улицам.
/// Общая для всех планировщиков часть — так их результаты проверяются одними и теми же правилами.
/// </summary>
public static class RouteBuilder
{
    /// <summary>
    /// Строит план для заказов в порядке sequence (индексы в problem.Orders).
    /// Возвращает null, если последовательность нарушает дедлайн, смену или недостижима.
    /// </summary>
    public static RoutePlan? TryBuild(DeliveryProblem problem, TravelTimes travel, IReadOnlyList<int> sequence)
    {
        var stops = new List<Stop>(sequence.Count);
        long time = 0;
        int point = 0; // склад

        foreach (int orderIndex in sequence)
        {
            int next = orderIndex + 1;
            int leg = travel.Minutes(point, next);
            if (leg == TravelTimes.Unreachable)
                return null;

            long arrival = time + leg;
            var order = problem.Orders[orderIndex];
            if (arrival > (order.DeadlineMinute ?? int.MaxValue))
                return null;

            time = arrival + order.ServiceMinutes;
            stops.Add(new Stop(order, (int)arrival, (int)time));
            point = next;
        }

        int back = travel.Minutes(point, 0);
        if (back == TravelTimes.Unreachable || time + back > problem.ShiftMinutes)
            return null;

        return new RoutePlan
        {
            Stops = stops,
            Skipped = ClassifySkipped(problem, travel, sequence),
            StreetPath = BuildStreetPath(problem, travel, sequence),
            ReturnMinute = (int)(time + back),
        };
    }

    private static List<string> BuildStreetPath(DeliveryProblem problem, TravelTimes travel, IReadOnlyList<int> sequence)
    {
        var points = new List<int> { 0 };
        points.AddRange(sequence.Select(i => i + 1));
        points.Add(0);

        var nodes = new List<int> { travel.Path(0, 0)[0] };
        for (int i = 1; i < points.Count; i++)
            nodes.AddRange(travel.Path(points[i - 1], points[i]).Skip(1)); // первый узел уже добавлен

        return nodes.Select(problem.Network.Name).ToList();
    }

    private static List<SkippedOrder> ClassifySkipped(DeliveryProblem problem, TravelTimes travel, IReadOnlyList<int> sequence)
    {
        var selected = sequence.ToHashSet();
        var skipped = new List<SkippedOrder>();

        for (int i = 0; i < problem.Orders.Count; i++)
        {
            if (selected.Contains(i))
                continue;

            var order = problem.Orders[i];
            int there = travel.Minutes(0, i + 1);
            int back = travel.Minutes(i + 1, 0);

            SkipReason reason;
            if (there == TravelTimes.Unreachable || back == TravelTimes.Unreachable)
                reason = SkipReason.Unreachable;
            else if (there > (order.DeadlineMinute ?? int.MaxValue))
                reason = SkipReason.DeadlineUnreachable;
            else if ((long)there + order.ServiceMinutes + back > problem.ShiftMinutes)
                reason = SkipReason.ShiftTooShort;
            else
                reason = SkipReason.NotSelected;

            skipped.Add(new SkippedOrder(order, reason));
        }

        return skipped;
    }
}
