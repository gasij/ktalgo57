using CourierRouting.Graph;
using CourierRouting.Model;

namespace CourierRouting.Tests;

/// <summary>
/// Независимое эталонное решение для сверки: не использует ни Дейкстру, ни TravelTimes,
/// ни RouteBuilder. Расстояния — Флойд — Уоршелл по всему графу, маршрут — полный перебор
/// всех упорядоченных подмножеств заказов. Работает за O(V³ + k!·k), пригодно только для
/// маленьких входов, зато его корректность очевидна.
/// </summary>
internal static class Reference
{
    public const long Inf = long.MaxValue / 4;

    public static long[,] FloydWarshall(RoadNetwork network)
    {
        int n = network.NodeCount;
        var d = new long[n, n];
        for (int i = 0; i < n; i++)
        for (int j = 0; j < n; j++)
            d[i, j] = i == j ? 0 : Inf;

        for (int u = 0; u < n; u++)
            foreach (var road in network.RoadsFrom(u))
                d[u, road.To] = Math.Min(d[u, road.To], road.Minutes);

        for (int m = 0; m < n; m++)
        for (int i = 0; i < n; i++)
        for (int j = 0; j < n; j++)
            if (d[i, m] + d[m, j] < d[i, j])
                d[i, j] = d[i, m] + d[m, j];

        return d;
    }

    /// <summary>Лучшая (ценность, минута возврата): максимум ценности, при равенстве — минимум времени.</summary>
    public static (long Value, long ReturnMinute) BestByBruteForce(DeliveryProblem problem)
    {
        var d = FloydWarshall(problem.Network);
        int depot = problem.Network.IndexOf(problem.Depot);
        var nodes = problem.Orders.Select(o => problem.Network.IndexOf(o.Location)).ToArray();
        var used = new bool[problem.Orders.Count];

        (long Value, long Return) best = (0, 0); // пустой маршрут

        void Search(int at, long time, long value)
        {
            long ret = time + d[at, depot];
            if (ret <= problem.ShiftMinutes && (value > best.Value || (value == best.Value && ret < best.Return)))
                best = (value, ret);

            for (int i = 0; i < used.Length; i++)
            {
                if (used[i])
                    continue;

                var order = problem.Orders[i];
                long arrival = time + d[at, nodes[i]];
                if (arrival >= Inf || arrival > (order.DeadlineMinute ?? long.MaxValue))
                    continue;

                used[i] = true;
                Search(nodes[i], arrival + order.ServiceMinutes, value + order.Value);
                used[i] = false;
            }
        }

        Search(depot, 0, 0);
        return best;
    }

    /// <summary>
    /// Проверяет план «с нуля» по расстояниям Флойда — Уоршелла: путь по улицам состоит из
    /// реальных дорог, времена прибытия и дедлайны соблюдены, возврат не позже конца смены.
    /// </summary>
    public static void AssertPlanIsValid(DeliveryProblem problem, RoutePlan plan)
    {
        var d = FloydWarshall(problem.Network);
        var net = problem.Network;
        int at = net.IndexOf(problem.Depot);
        long time = 0;

        foreach (var stop in plan.Stops)
        {
            int to = net.IndexOf(stop.Order.Location);
            time += d[at, to];
            Assert.Equal(time, stop.ArrivalMinute);
            Assert.True(stop.ArrivalMinute <= (stop.Order.DeadlineMinute ?? int.MaxValue),
                $"Заказ {stop.Order.Id} доставлен после дедлайна.");
            time += stop.Order.ServiceMinutes;
            at = to;
        }

        time += d[at, net.IndexOf(problem.Depot)];
        Assert.Equal(time, plan.ReturnMinute);
        Assert.True(plan.ReturnMinute <= problem.ShiftMinutes, "Возврат после конца смены.");

        Assert.Equal(plan.Stops.Count, plan.Stops.Select(s => s.Order.Id).Distinct().Count());
        Assert.Equal(problem.Orders.Count, plan.Stops.Count + plan.Skipped.Count);

        // Путь по улицам: начинается и заканчивается на складе, каждый шаг — существующая дорога.
        Assert.Equal(problem.Depot, plan.StreetPath[0]);
        Assert.Equal(problem.Depot, plan.StreetPath[^1]);
        long pathMinutes = 0;
        for (int i = 1; i < plan.StreetPath.Count; i++)
        {
            int from = net.IndexOf(plan.StreetPath[i - 1]), to = net.IndexOf(plan.StreetPath[i]);
            var roads = net.RoadsFrom(from).Where(r => r.To == to).ToList();
            Assert.NotEmpty(roads);
            pathMinutes += roads.Min(r => r.Minutes);
        }

        Assert.Equal(plan.ReturnMinute - plan.Stops.Sum(s => (long)s.Order.ServiceMinutes), pathMinutes);
    }
}
