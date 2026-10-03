namespace CourierRouting.Model;

/// <summary>Остановка на маршруте: какой заказ, во сколько курьер прибыл и во сколько уехал.</summary>
public sealed record Stop(Order Order, int ArrivalMinute, int DepartureMinute);

/// <summary>Почему заказ не попал в маршрут.</summary>
public enum SkipReason
{
    /// <summary>До адреса нет пути от склада или от адреса нет пути обратно.</summary>
    Unreachable,

    /// <summary>Даже если ехать к нему первым, курьер не успевает к дедлайну.</summary>
    DeadlineUnreachable,

    /// <summary>Даже единственный этот заказ не укладывается в смену с возвратом на склад.</summary>
    ShiftTooShort,

    /// <summary>По отдельности выполним, но вместе с выбранными заказами не помещается.</summary>
    NotSelected,
}

public sealed record SkippedOrder(Order Order, SkipReason Reason);

/// <summary>Результат планирования.</summary>
public sealed class RoutePlan
{
    public required IReadOnlyList<Stop> Stops { get; init; }

    public required IReadOnlyList<SkippedOrder> Skipped { get; init; }

    /// <summary>Полный путь по перекрёсткам: склад → … → склад.</summary>
    public required IReadOnlyList<string> StreetPath { get; init; }

    /// <summary>Минута возвращения на склад.</summary>
    public required int ReturnMinute { get; init; }

    public long TotalValue => Stops.Sum(s => (long)s.Order.Value);

    public IEnumerable<string> OrderIds => Stops.Select(s => s.Order.Id);
}
