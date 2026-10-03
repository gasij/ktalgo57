namespace CourierRouting.Graph;

/// <summary>
/// Дерево кратчайших путей от одного источника: время до каждого перекрёстка
/// и предшественник на кратчайшем пути (для восстановления маршрута по улицам).
/// </summary>
public sealed class ShortestPathTree
{
    /// <summary>Значение «недостижимо» в массиве расстояний.</summary>
    public const int Unreachable = int.MaxValue;

    private readonly int[] _distance;
    private readonly int[] _previous;

    internal ShortestPathTree(int source, int[] distance, int[] previous)
    {
        Source = source;
        _distance = distance;
        _previous = previous;
    }

    public int Source { get; }

    public int DistanceTo(int node) => _distance[node];

    public bool CanReach(int node) => _distance[node] != Unreachable;

    /// <summary>Последовательность перекрёстков от источника до node включительно; пустая, если пути нет.</summary>
    public IReadOnlyList<int> PathTo(int node)
    {
        if (!CanReach(node))
            return [];

        var path = new List<int>();
        for (int v = node; v != -1; v = _previous[v])
            path.Add(v);

        path.Reverse();
        return path;
    }
}

public static class ShortestPaths
{
    /// <summary>
    /// Алгоритм Дейкстры с двоичной кучей (<see cref="PriorityQueue{TElement,TPriority}"/>).
    /// Сложность O((V + E) log V) по времени, O(V) по памяти (без учёта самого графа).
    /// </summary>
    /// <remarks>
    /// В PriorityQueue нет операции «уменьшить ключ», поэтому вершина может лежать в куче
    /// несколько раз; устаревшие записи пропускаются при извлечении («ленивое удаление»).
    /// Всего записей не больше E + 1, поэтому оценка сохраняется: O(E log E) = O(E log V).
    /// </remarks>
    public static ShortestPathTree Dijkstra(RoadNetwork network, int source)
    {
        int n = network.NodeCount;
        var distance = new int[n];
        var previous = new int[n];
        Array.Fill(distance, ShortestPathTree.Unreachable);
        Array.Fill(previous, -1);

        var heap = new PriorityQueue<int, int>();
        distance[source] = 0;
        heap.Enqueue(source, 0);

        while (heap.TryDequeue(out int node, out int dist))
        {
            if (dist > distance[node])
                continue; // устаревшая запись: вершину уже извлекли с меньшим расстоянием

            foreach (var road in network.RoadsFrom(node))
            {
                // long — защита от переполнения на очень длинных путях.
                long candidate = (long)dist + road.Minutes;
                if (candidate < distance[road.To])
                {
                    distance[road.To] = (int)candidate;
                    previous[road.To] = node;
                    heap.Enqueue(road.To, (int)candidate);
                }
            }
        }

        return new ShortestPathTree(source, distance, previous);
    }
}
