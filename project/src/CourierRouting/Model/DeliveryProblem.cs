using CourierRouting.Graph;

namespace CourierRouting.Model;

/// <summary>
/// Постановка задачи: дорожная сеть, склад, заказы и длительность смены.
/// Курьер выезжает со склада в минуту 0 и обязан вернуться на склад не позже ShiftMinutes.
/// Все входные данные проверяются в конструкторе: планировщики могут считать их корректными.
/// </summary>
public sealed class DeliveryProblem
{
    public DeliveryProblem(RoadNetwork network, string depot, IReadOnlyList<Order> orders, int shiftMinutes)
    {
        ArgumentNullException.ThrowIfNull(network);
        ArgumentNullException.ThrowIfNull(orders);

        if (!network.Contains(depot))
            throw new ArgumentException($"Склад «{depot}» отсутствует в дорожной сети.", nameof(depot));

        if (shiftMinutes < 0)
            throw new ArgumentOutOfRangeException(nameof(shiftMinutes), shiftMinutes, "Длительность смены отрицательна.");

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var order in orders)
        {
            if (!ids.Add(order.Id))
                throw new ArgumentException($"Номер заказа «{order.Id}» повторяется.", nameof(orders));

            if (!network.Contains(order.Location))
                throw new ArgumentException($"Адрес заказа {order.Id} «{order.Location}» отсутствует в дорожной сети.", nameof(orders));

            // Ценность 0 или меньше означает, что заказ выгоднее не везти — это противоречит
            // смыслу входных данных, и такой заказ скорее ошибка ввода, чем осознанный выбор.
            if (order.Value <= 0)
                throw new ArgumentException($"Ценность заказа {order.Id} должна быть положительной.", nameof(orders));

            if (order.ServiceMinutes < 0)
                throw new ArgumentException($"Время передачи заказа {order.Id} отрицательно.", nameof(orders));

            if (order.DeadlineMinute < 0)
                throw new ArgumentException($"Дедлайн заказа {order.Id} отрицателен.", nameof(orders));
        }

        Network = network;
        Depot = depot;
        Orders = orders;
        ShiftMinutes = shiftMinutes;
    }

    public RoadNetwork Network { get; }

    public string Depot { get; }

    public IReadOnlyList<Order> Orders { get; }

    public int ShiftMinutes { get; }
}
