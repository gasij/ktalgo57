namespace CourierRouting.Graph;

/// <summary>Дорога между перекрёстками: куда ведёт и сколько минут занимает проезд.</summary>
public readonly record struct Road(int To, int Minutes);

/// <summary>
/// Дорожная сеть города: ориентированный взвешенный граф, хранящийся списком смежности.
/// Перекрёсткам (вершинам) присвоены имена, внутри алгоритмов используются только индексы.
/// </summary>
/// <remarks>
/// Список смежности, а не матрица: городская сеть разреженная (у перекрёстка 2–4 соседа),
/// поэтому память O(V + E) вместо O(V²), а Дейкстра перебирает только реальные дороги.
/// </remarks>
public sealed class RoadNetwork
{
    private readonly List<List<Road>> _adjacency = [];
    private readonly List<string> _names = [];
    private readonly Dictionary<string, int> _indexByName = new(StringComparer.Ordinal);

    public int NodeCount => _names.Count;

    public int RoadCount { get; private set; }

    /// <summary>Возвращает индекс перекрёстка, создавая его при первом упоминании.</summary>
    public int AddNode(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя перекрёстка не может быть пустым.", nameof(name));

        if (_indexByName.TryGetValue(name, out int existing))
            return existing;

        int index = _names.Count;
        _names.Add(name);
        _adjacency.Add([]);
        _indexByName[name] = index;
        return index;
    }

    /// <summary>Добавляет дорогу; по умолчанию двустороннюю (две дуги графа).</summary>
    public void AddRoad(string from, string to, int minutes, bool twoWay = true) =>
        AddRoad(AddNode(from), AddNode(to), minutes, twoWay);

    public void AddRoad(int from, int to, int minutes, bool twoWay = true)
    {
        RequireNode(from);
        RequireNode(to);

        // Отрицательное время проезда физически бессмысленно и ломает инвариант Дейкстры
        // (извлечённая из очереди вершина больше не улучшается), поэтому отвергается сразу.
        if (minutes < 0)
            throw new ArgumentOutOfRangeException(nameof(minutes), minutes,
                $"Время проезда {Name(from)} → {Name(to)} отрицательно.");

        _adjacency[from].Add(new Road(to, minutes));
        RoadCount++;

        if (twoWay && from != to)
        {
            _adjacency[to].Add(new Road(from, minutes));
            RoadCount++;
        }
    }

    public IReadOnlyList<Road> RoadsFrom(int node)
    {
        RequireNode(node);
        return _adjacency[node];
    }

    public string Name(int node)
    {
        RequireNode(node);
        return _names[node];
    }

    public int IndexOf(string name) =>
        _indexByName.TryGetValue(name, out int index)
            ? index
            : throw new KeyNotFoundException($"Перекрёсток «{name}» отсутствует в дорожной сети.");

    public bool Contains(string name) => _indexByName.ContainsKey(name);

    private void RequireNode(int node)
    {
        if ((uint)node >= (uint)_names.Count)
            throw new ArgumentOutOfRangeException(nameof(node), node, "Такого перекрёстка нет в сети.");
    }
}
