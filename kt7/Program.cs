using Kt7;

Console.OutputEncoding = System.Text.Encoding.UTF8;

int failed = 0;

// ---------------------------------------------------------------------------
// Граф из задания: станция 2 — зарядная
// ---------------------------------------------------------------------------
Console.WriteLine("=== Граф из задания ===");
Graph sample = BuildSample(withCharger: true);

Check(DroneRouting.MinCostRoute(sample, 0, 4, 100), 25, "ёмкость 100: маршрут 0→2→1→3→4 с дозаправкой на станции 2");
Check(DroneRouting.MinCostRoute(sample, 0, 4, 35), -1, "ёмкость 35: дрон не может покинуть старт");
Check(DroneRouting.MinCostRoute(sample, 0, 0, 35), 0, "старт совпадает с назначением");
Check(DroneRouting.MinCostRoute(sample, 0, 4, 90), 30, "ёмкость 90: после 0→2→1→3 остаётся 10 < 20, допустим только более дорогой 0→2→3→4");
Check(DroneRouting.MinCostRoute(sample, 0, 4, 70), -1, "ёмкость 70: до станции 3 долететь можно, но на перелёт 3→4 заряда уже нет");

// Тот же граф без зарядки: рёбра от 0 до 4 есть, но заряда не хватает ни на одном варианте.
Console.WriteLine();
Console.WriteLine("=== Тот же граф без зарядной станции ===");
Graph noCharger = BuildSample(withCharger: false);
Report(IsReachable(noCharger, 0, 4), "обход в глубину: последовательность рёбер 0 → 4 существует");
Check(DroneRouting.MinCostRoute(noCharger, 0, 4, 100), -1, "ёмкость 100: маршрут есть в графе, но недостижим по заряду");
Check(DroneRouting.MinCostRoute(noCharger, 0, 4, 130), 25, "ёмкость 130: хватает на 0→1→3→4 без дозаправки");
Check(DroneRouting.MinCostRoute(noCharger, 0, 4, 129), -1, "ёмкость 129: любому маршруту нужно не меньше 130, не хватает единицы заряда");

// ---------------------------------------------------------------------------
// «Ловушка»: пример из письменного обоснования (пункт 2)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== Граф «Ловушка»: почему состояние — не просто станция ===");
var trap = new Graph();
trap.AddEdge(0, 1, cost: 1, batteryUsage: 80);
trap.AddEdge(0, 2, cost: 2, batteryUsage: 10);
trap.AddEdge(2, 1, cost: 2, batteryUsage: 10);
trap.AddEdge(1, 3, cost: 1, batteryUsage: 50);
trap.AddEdge(0, 3, cost: 20, batteryUsage: 100);

Check(DijkstraIgnoringBattery(trap, 0, 3), 2, "Дейкстра без учёта заряда: 0→1→3 — маршрут недопустим (остаток 20 < 50)");
Check(DijkstraByStation(trap, 0, 3, 100), 20, "Дейкстра по станциям с зарядом самого дешёвого пути: теряет прибытие на станцию 1 с остатком 80");
Check(DroneRouting.MinCostRoute(trap, 0, 3, 100), 5, "Дейкстра по состояниям: 0→2→1→3 — минимум среди допустимых");

// ---------------------------------------------------------------------------
// Дешёвый маршрут недопустим, дорогой через зарядку — допустим
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== Два маршрута разной стоимости ===");
var twoRoutes = new Graph();
twoRoutes.AddEdge(0, 1, cost: 1, batteryUsage: 60);
twoRoutes.AddEdge(1, 3, cost: 1, batteryUsage: 60);
twoRoutes.AddEdge(0, 2, cost: 10, batteryUsage: 60);
twoRoutes.AddEdge(2, 3, cost: 10, batteryUsage: 60);
twoRoutes.ChargingStations.Add(2);

Check(DijkstraIgnoringBattery(twoRoutes, 0, 3), 2, "без учёта заряда самый дешёвый маршрут 0→1→3 стоит 2");
Check(DroneRouting.MinCostRoute(twoRoutes, 0, 3, 100), 20, "ёмкость 100: 0→1→3 недопустим (120 > 100), выбран 0→2→3 через зарядку");
Check(DroneRouting.MinCostRoute(twoRoutes, 0, 3, 120), 2, "ёмкость 120: дешёвый маршрут стал допустимым и выбран он");

// ---------------------------------------------------------------------------
// «Возврат»: на станцию 1 нужно попасть дважды
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== Граф «Возврат»: повторное посещение станции ===");
var detour = new Graph();
detour.AddEdge(0, 1, cost: 1, batteryUsage: 50);
detour.AddEdge(1, 2, cost: 1, batteryUsage: 60);
detour.AddEdge(1, 3, cost: 1, batteryUsage: 40);
detour.AddEdge(3, 1, cost: 1, batteryUsage: 40);
detour.ChargingStations.Add(3);

Check(DroneRouting.MinCostRoute(detour, 0, 2, 100), 4, "0→1→3(зарядка)→1→2: станция 1 пройдена с остатком 50 и с остатком 60");
Check(DijkstraByStation(detour, 0, 2, 100), -1, "Дейкстра по станциям этот маршрут не находит");

// ---------------------------------------------------------------------------
// Сверка с независимым решением на случайных графах
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== Случайные графы ===");
var random = new Random(7);
int mismatches = 0, unreachableByBattery = 0, differsFromNaive = 0;
const int Trials = 5000;
for (int t = 0; t < Trials; t++)
{
    int n = random.Next(2, 7);
    int capacity = random.Next(1, 13);
    var g = new Graph();
    int edgeCount = random.Next(1, n * 3);
    for (int e = 0; e < edgeCount; e++)
        g.AddEdge(random.Next(n), random.Next(n), random.Next(0, 10), random.Next(0, 11));
    for (int v = 0; v < n; v++)
        if (random.Next(4) == 0)
            g.ChargingStations.Add(v);

    int actual = DroneRouting.MinCostRoute(g, 0, n - 1, capacity);
    if (actual != RelaxationReference(g, n, 0, n - 1, capacity))
        mismatches++;
    if (actual == -1 && IsReachable(g, 0, n - 1))
        unreachableByBattery++;
    if (actual != DijkstraByStation(g, 0, n - 1, capacity))
        differsFromNaive++;
}
Report(mismatches == 0, $"{Trials} графов: результат совпадает с независимым решением (релаксация таблицы [станция, заряд] до сходимости)");
Console.WriteLine($"  из них: {unreachableByBattery} — путь в графе есть, но по заряду недостижим; {differsFromNaive} — Дейкстра по станциям ошиблась");

Console.WriteLine();
Console.WriteLine(failed == 0 ? "Все проверки пройдены." : $"Проверок не пройдено: {failed}");
return failed == 0 ? 0 : 1;

Graph BuildSample(bool withCharger)
{
    var g = new Graph();
    g.AddEdge(0, 1, cost: 10, batteryUsage: 60);
    g.AddEdge(0, 2, cost: 5, batteryUsage: 40);
    g.AddEdge(2, 1, cost: 5, batteryUsage: 30);
    g.AddEdge(1, 3, cost: 10, batteryUsage: 50);
    g.AddEdge(2, 3, cost: 20, batteryUsage: 70);
    g.AddEdge(3, 4, cost: 5, batteryUsage: 20);
    if (withCharger)
        g.ChargingStations.Add(2);

    return g;
}

// Обход в глубину без учёта заряда: есть ли вообще последовательность рёбер from → to.
bool IsReachable(Graph g, int from, int to)
{
    var visited = new HashSet<int>();
    var stack = new Stack<int>();
    stack.Push(from);

    while (stack.Count > 0)
    {
        int v = stack.Pop();
        if (v == to)
            return true;
        if (!visited.Add(v))
            continue;

        foreach (var edge in g.Adjacency.GetValueOrDefault(v, []))
            stack.Push(edge.To);
    }

    return false;
}

// НЕВЕРНОЕ решение для сравнения: обычная Дейкстра, заряд не учитывается вовсе.
int DijkstraIgnoringBattery(Graph g, int start, int destination) =>
    DijkstraByStation(g, start, destination, int.MaxValue);

// НЕВЕРНОЕ решение для сравнения: Дейкстра по станциям. Для каждой станции хранится одна
// стоимость и остаток заряда того пути, который дал эту стоимость.
int DijkstraByStation(Graph g, int start, int destination, int capacity)
{
    var best = new Dictionary<int, (int Cost, int Battery)> { [start] = (0, capacity) };
    var done = new HashSet<int>();
    var pq = new PriorityQueue<int, int>();
    pq.Enqueue(start, 0);

    while (pq.TryDequeue(out int station, out _))
    {
        if (!done.Add(station))
            continue;
        if (station == destination)
            return best[station].Cost;

        var (cost, battery) = best[station];
        foreach (var edge in g.Adjacency.GetValueOrDefault(station, []))
        {
            if (edge.BatteryUsage > battery || done.Contains(edge.To))
                continue;

            int remaining = g.ChargingStations.Contains(edge.To) ? capacity : battery - edge.BatteryUsage;
            int newCost = cost + edge.Cost;
            if (!best.TryGetValue(edge.To, out var known) || newCost < known.Cost)
            {
                best[edge.To] = (newCost, remaining);
                pq.Enqueue(edge.To, newCost);
            }
        }
    }

    return -1;
}

// Независимое эталонное решение без приоритетной очереди: таблица cost[станция, заряд]
// релаксируется по всем рёбрам, пока что-то меняется (в духе Беллмана — Форда).
int RelaxationReference(Graph g, int stationCount, int start, int destination, int capacity)
{
    const int Inf = int.MaxValue;
    var cost = new int[stationCount, capacity + 1];
    for (int v = 0; v < stationCount; v++)
        for (int b = 0; b <= capacity; b++)
            cost[v, b] = Inf;
    cost[start, capacity] = 0;

    bool changed = true;
    while (changed)
    {
        changed = false;
        foreach (var (from, edges) in g.Adjacency)
        {
            foreach (var edge in edges)
            {
                for (int b = edge.BatteryUsage; b <= capacity; b++)
                {
                    if (cost[from, b] == Inf)
                        continue;

                    int nb = g.ChargingStations.Contains(edge.To) ? capacity : b - edge.BatteryUsage;
                    if (cost[from, b] + edge.Cost < cost[edge.To, nb])
                    {
                        cost[edge.To, nb] = cost[from, b] + edge.Cost;
                        changed = true;
                    }
                }
            }
        }
    }

    int result = Inf;
    for (int b = 0; b <= capacity; b++)
        result = Math.Min(result, cost[destination, b]);

    return result == Inf ? -1 : result;
}

void Check(int actual, int expected, string note) =>
    Report(actual == expected, $"{actual} (ожидалось {expected}) — {note}");

void Report(bool ok, string message)
{
    if (!ok)
        failed++;

    Console.WriteLine($"  [{(ok ? "OK" : "FAIL")}] {message}");
}
