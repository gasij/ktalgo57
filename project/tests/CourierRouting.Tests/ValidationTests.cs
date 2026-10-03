using CourierRouting.Io;
using CourierRouting.Model;

namespace CourierRouting.Tests;

/// <summary>Недопустимые и противоречивые входные данные отвергаются явно, а не дают неверный ответ.</summary>
public class ValidationTests
{
    private static readonly Graph.RoadNetwork Net = TestData.Network(("Склад", "Дом", 5));

    [Fact]
    public void UnknownDepot() =>
        Assert.Throws<ArgumentException>(() => new DeliveryProblem(Net, "Нет такого", [], 10));

    [Fact]
    public void UnknownOrderAddress() =>
        Assert.Throws<ArgumentException>(() => new DeliveryProblem(Net, "Склад", [new Order("1", "Нет такого")], 10));

    [Fact]
    public void DuplicateOrderId() =>
        Assert.Throws<ArgumentException>(() =>
            new DeliveryProblem(Net, "Склад", [new Order("1", "Дом"), new Order("1", "Склад")], 10));

    [Theory]
    [InlineData(0, null, 0)]   // ценность 0
    [InlineData(-3, null, 0)]  // отрицательная ценность
    [InlineData(1, -1, 0)]     // отрицательный дедлайн
    [InlineData(1, null, -2)]  // отрицательное время передачи
    public void InvalidOrderFields(int value, int? deadline, int service) =>
        Assert.Throws<ArgumentException>(() =>
            new DeliveryProblem(Net, "Склад", [new Order("1", "Дом", value, deadline, service)], 10));

    [Fact]
    public void NegativeShift() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new DeliveryProblem(Net, "Склад", [], -1));

    [Fact]
    public void NegativeRoadInScenarioFile()
    {
        var e = Assert.ThrowsAny<ArgumentException>(() =>
            ScenarioLoader.LoadFile(Path.Combine(ScenarioFiles.Directory, "07-invalid-negative.json")));
        Assert.Contains("отрицательно", e.Message);
    }

    [Theory]
    [InlineData("не json")]
    [InlineData("""{ "shiftMinutes": 10 }""")]                                       // нет склада
    [InlineData("""{ "depot": "С", "roads": [ { "from": "С", "minutes": 1 } ] }""")] // дорога без конца
    [InlineData("""{ "depot": "С", "orders": [ { "location": "С" } ] }""")]          // заказ без id
    public void MalformedScenario(string json) =>
        Assert.Throws<FormatException>(() => ScenarioLoader.Parse(json));
}
