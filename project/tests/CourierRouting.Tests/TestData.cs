using CourierRouting.Graph;
using CourierRouting.Model;
using CourierRouting.Planning;

namespace CourierRouting.Tests;

internal static class TestData
{
    public static RoutePlan PlanDp(DeliveryProblem problem) =>
        new DpRoutePlanner().Plan(problem, new TravelTimes(problem));

    public static RoutePlan PlanGreedy(DeliveryProblem problem) =>
        new GreedyRoutePlanner().Plan(problem, new TravelTimes(problem));

    /// <summary>Сеть из списка двусторонних дорог вида ("A", "B", минуты).</summary>
    public static RoadNetwork Network(params (string From, string To, int Minutes)[] roads)
    {
        var network = new RoadNetwork();
        network.AddNode("Склад");
        foreach (var (from, to, minutes) in roads)
            network.AddRoad(from, to, minutes);

        return network;
    }

    /// <summary>Случайная задача: сеть из n перекрёстков, k заказов. Для сверки с перебором.</summary>
    public static DeliveryProblem RandomProblem(Random random, int maxNodes, int maxOrders)
    {
        int n = random.Next(1, maxNodes + 1);
        var network = new RoadNetwork();
        for (int v = 0; v < n; v++)
            network.AddNode($"v{v}");

        int roads = random.Next(0, n * 3);
        for (int e = 0; e < roads; e++)
            network.AddRoad(random.Next(n), random.Next(n), random.Next(0, 20), twoWay: random.Next(3) != 0);

        int k = random.Next(0, maxOrders + 1);
        var orders = Enumerable.Range(0, k)
            .Select(i => new Order(
                $"o{i}",
                $"v{random.Next(n)}",
                Value: random.Next(1, 6),
                DeadlineMinute: random.Next(3) == 0 ? random.Next(0, 60) : null,
                ServiceMinutes: random.Next(0, 4)))
            .ToList();

        return new DeliveryProblem(network, "v0", orders, shiftMinutes: random.Next(0, 100));
    }
}
