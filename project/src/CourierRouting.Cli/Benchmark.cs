using System.Diagnostics;
using CourierRouting.Graph;
using CourierRouting.Model;
using CourierRouting.Planning;

namespace CourierRouting.Cli;

/// <summary>
/// Замер роста времени работы при увеличении числа заказов на городе-решётке.
/// Показывает на практике, где проходит граница применимости DP по подмножествам.
/// </summary>
internal static class Benchmark
{
    public static void Run(int maxOrders)
    {
        const int Side = 40; // решётка 40×40 = 1600 перекрёстков
        var random = new Random(2026);
        var network = BuildGrid(Side, random);

        Console.WriteLine($"Город-решётка {Side}×{Side}: {network.NodeCount} перекрёстков, {network.RoadCount} дуг.");
        Console.WriteLine();
        Console.WriteLine(" k | Дейкстра, мс | DP, мс | состояний DP | ценность DP | жадный");
        Console.WriteLine("---+--------------+--------+--------------+-------------+-------");

        for (int k = 4; k <= maxOrders; k += 2)
        {
            var orders = Enumerable.Range(0, k)
                .Select(i => new Order(
                    $"#{i}",
                    $"{random.Next(Side)},{random.Next(Side)}",
                    Value: random.Next(1, 10),
                    DeadlineMinute: random.Next(3) == 0 ? random.Next(60, 300) : null,
                    ServiceMinutes: 3))
                .ToList();
            var problem = new DeliveryProblem(network, "20,20", orders, shiftMinutes: 480);

            var sw = Stopwatch.StartNew();
            var travel = new TravelTimes(problem);
            double dijkstraMs = sw.Elapsed.TotalMilliseconds;

            sw.Restart();
            var dp = new DpRoutePlanner().Plan(problem, travel);
            double dpMs = sw.Elapsed.TotalMilliseconds;

            var greedy = new GreedyRoutePlanner().Plan(problem, travel);
            long states = (1L << k) * k;

            Console.WriteLine($"{k,2} | {dijkstraMs,12:F1} | {dpMs,6:F0} | {states,12:N0} | {dp.TotalValue,11} | {greedy.TotalValue,5}");
        }
    }

    private static RoadNetwork BuildGrid(int side, Random random)
    {
        var network = new RoadNetwork();
        for (int x = 0; x < side; x++)
        for (int y = 0; y < side; y++)
        {
            if (x + 1 < side)
                network.AddRoad($"{x},{y}", $"{x + 1},{y}", random.Next(2, 9));
            if (y + 1 < side)
                network.AddRoad($"{x},{y}", $"{x},{y + 1}", random.Next(2, 9));
        }

        return network;
    }
}
