using System.Text.Json;
using CourierRouting.Graph;
using CourierRouting.Model;

namespace CourierRouting.Io;

/// <summary>
/// Читает постановку задачи из JSON. Формат:
/// <code>
/// {
///   "depot": "Склад",
///   "shiftMinutes": 120,
///   "roads":  [ { "from": "Склад", "to": "Рынок", "minutes": 10, "oneWay": false } ],
///   "orders": [ { "id": "A", "location": "Рынок", "value": 3, "deadline": 40, "service": 5 } ]
/// }
/// </code>
/// </summary>
public static class ScenarioLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static DeliveryProblem LoadFile(string path) => Parse(File.ReadAllText(path));

    public static DeliveryProblem Parse(string json)
    {
        ScenarioDto dto;
        try
        {
            dto = JsonSerializer.Deserialize<ScenarioDto>(json, Options)
                  ?? throw new FormatException("Пустой файл сценария.");
        }
        catch (JsonException e)
        {
            throw new FormatException($"Некорректный JSON сценария: {e.Message}", e);
        }

        if (string.IsNullOrWhiteSpace(dto.Depot))
            throw new FormatException("В сценарии не указан склад (depot).");

        var network = new RoadNetwork();
        network.AddNode(dto.Depot);

        foreach (var road in dto.Roads ?? [])
        {
            if (road.From is null || road.To is null)
                throw new FormatException("У дороги не указан from или to.");

            network.AddRoad(road.From, road.To, road.Minutes, twoWay: !road.OneWay);
        }

        var orders = (dto.Orders ?? [])
            .Select(o => new Order(
                o.Id ?? throw new FormatException("У заказа не указан id."),
                o.Location ?? throw new FormatException($"У заказа {o.Id} не указан адрес."),
                o.Value ?? 1,
                o.Deadline,
                o.Service ?? 0))
            .ToList();

        // Адрес, к которому не ведёт ни одна дорога, всё равно добавляется в сеть:
        // такой заказ корректен, просто недостижим — это решает планировщик, а не загрузчик.
        foreach (var order in orders)
            network.AddNode(order.Location);

        return new DeliveryProblem(network, dto.Depot, orders, dto.ShiftMinutes);
    }

    private sealed record ScenarioDto(string? Depot, int ShiftMinutes, List<RoadDto>? Roads, List<OrderDto>? Orders);

    private sealed record RoadDto(string? From, string? To, int Minutes, bool OneWay);

    private sealed record OrderDto(string? Id, string? Location, int? Value, int? Deadline, int? Service);
}
