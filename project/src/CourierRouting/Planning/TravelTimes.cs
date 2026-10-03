using CourierRouting.Graph;
using CourierRouting.Model;

namespace CourierRouting.Planning;

/// <summary>
/// Кратчайшие времена проезда между ключевыми точками: склад (индекс 0) и адреса заказов
/// (заказ i — индекс i + 1). Это «сжатие» дорожной сети из V перекрёстков до k + 1 точек,
/// после которого планировщику уже не нужен сам граф.
/// </summary>
/// <remarks>
/// Строится запуском Дейкстры из каждой различной ключевой точки: O(k · (V + E) log V).
/// Альтернатива — Флойд — Уоршелл за O(V³) — отклонена: он считает расстояния между ВСЕМИ
/// перекрёстками города, хотя нужны только k + 1 из них, и при V ≫ k это на порядки дороже.
/// </remarks>
public sealed class TravelTimes
{
    public const int Unreachable = ShortestPathTree.Unreachable;

    private readonly int[] _nodeOfPoint;
    private readonly ShortestPathTree[] _treeOfPoint;

    public TravelTimes(DeliveryProblem problem)
    {
        int k = problem.Orders.Count;
        _nodeOfPoint = new int[k + 1];
        _treeOfPoint = new ShortestPathTree[k + 1];

        _nodeOfPoint[0] = problem.Network.IndexOf(problem.Depot);
        for (int i = 0; i < k; i++)
            _nodeOfPoint[i + 1] = problem.Network.IndexOf(problem.Orders[i].Location);

        // Несколько заказов по одному адресу (или заказ на складе) не требуют повторной Дейкстры.
        var treeByNode = new Dictionary<int, ShortestPathTree>();
        for (int p = 0; p <= k; p++)
        {
            int node = _nodeOfPoint[p];
            if (!treeByNode.TryGetValue(node, out var tree))
                treeByNode[node] = tree = ShortestPaths.Dijkstra(problem.Network, node);

            _treeOfPoint[p] = tree;
        }

        DijkstraRuns = treeByNode.Count;
    }

    /// <summary>Число точек: склад + заказы.</summary>
    public int PointCount => _nodeOfPoint.Length;

    /// <summary>Сколько раз пришлось запустить Дейкстру (различных ключевых перекрёстков).</summary>
    public int DijkstraRuns { get; }

    /// <summary>Кратчайшее время из точки from в точку to или <see cref="Unreachable"/>.</summary>
    public int Minutes(int from, int to) => _treeOfPoint[from].DistanceTo(_nodeOfPoint[to]);

    /// <summary>Перекрёстки кратчайшего пути из точки from в точку to (включая оба конца).</summary>
    public IReadOnlyList<int> Path(int from, int to) => _treeOfPoint[from].PathTo(_nodeOfPoint[to]);
}
