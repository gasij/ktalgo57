using CourierRouting.Io;
using CourierRouting.Model;
using CourierRouting.Planning;

namespace CourierRouting.Tests;

public class DpRoutePlannerTests
{
    // ---------------------------------------------------------------- основной сценарий

    [Fact]
    public void CityScenario_FindsOptimalRoute()
    {
        var problem = ScenarioLoader.LoadFile(Path.Combine(ScenarioFiles.Directory, "01-city.json"));

        var plan = TestData.PlanDp(problem);

        Assert.Equal(19, plan.TotalValue);
        Assert.Equal(["F", "B", "H", "C", "D", "E", "G"], plan.OrderIds);
        Assert.Equal(100, plan.ReturnMinute);
        Assert.Equal(Reference.BestByBruteForce(problem), (plan.TotalValue, plan.ReturnMinute));
        Reference.AssertPlanIsValid(problem, plan);
    }

    [Fact]
    public void AllOrdersFit_DeliversEverythingWithShortestTour()
    {
        // Склад в середине: Запад —5— Склад —5— Восток.
        // Склад→Запад 5, Запад→Восток 10 (через склад), Восток→Склад 5 — итого 20, это минимум.
        var net = TestData.Network(("Склад", "Запад", 5), ("Склад", "Восток", 5));
        var problem = new DeliveryProblem(net, "Склад",
            [new Order("W", "Запад"), new Order("E", "Восток")], shiftMinutes: 100);

        var plan = TestData.PlanDp(problem);

        Assert.Equal(2, plan.Stops.Count);
        Assert.Equal(20, plan.ReturnMinute);
        Assert.Empty(plan.Skipped);
    }

    [Fact]
    public void PrefersOneValuableOrderOverSeveralCheapOnes()
    {
        // Время на одно направление: либо три дешёвых (по 1), либо один ценный (5).
        var net = TestData.Network(
            ("Склад", "Д1", 5), ("Д1", "Д2", 5), ("Д2", "Д3", 5),
            ("Склад", "Особняк", 15));
        var problem = new DeliveryProblem(net, "Склад",
        [
            new Order("1", "Д1"), new Order("2", "Д2"), new Order("3", "Д3"),
            new Order("VIP", "Особняк", Value: 5),
        ], shiftMinutes: 30);

        var plan = TestData.PlanDp(problem);

        Assert.Equal(["VIP"], plan.OrderIds);
        Assert.Equal(5, plan.TotalValue);
    }

    [Fact]
    public void EqualValue_ChoosesEarlierReturn()
    {
        var net = TestData.Network(("Склад", "Близко", 2), ("Склад", "Далеко", 9));
        var problem = new DeliveryProblem(net, "Склад",
            [new Order("far", "Далеко", Value: 2), new Order("near", "Близко", Value: 2)], shiftMinutes: 20);

        var plan = TestData.PlanDp(problem);

        Assert.Equal(["near"], plan.OrderIds); // оба вместе не помещаются (2·2 + 2·9 = 22 > 20)
        Assert.Equal(4, plan.ReturnMinute);
    }

    [Fact]
    public void DeadlineForcesOrderOfVisits()
    {
        // Ближний заказ не срочный, дальний — срочный: правильный порядок «сначала дальний».
        var net = TestData.Network(("Склад", "Дом", 5), ("Склад", "Аптека", 10));
        var problem = new DeliveryProblem(net, "Склад",
        [
            new Order("Посылка", "Дом"),
            new Order("Лекарство", "Аптека", DeadlineMinute: 10),
        ], shiftMinutes: 60);

        var plan = TestData.PlanDp(problem);

        Assert.Equal(["Лекарство", "Посылка"], plan.OrderIds);
        Assert.Equal(10, plan.Stops[0].ArrivalMinute);
    }

    [Fact]
    public void ServiceTimeCountsTowardsLaterDeadlines()
    {
        // Без учёта времени передачи успели бы оба (5 и 10), с ним — второй прибывает в 15 > 10.
        var net = TestData.Network(("Склад", "A", 5), ("A", "B", 5));
        var problem = new DeliveryProblem(net, "Склад",
        [
            new Order("a", "A", ServiceMinutes: 5, DeadlineMinute: 5),
            new Order("b", "B", DeadlineMinute: 10),
        ], shiftMinutes: 100);

        var plan = TestData.PlanDp(problem);

        Assert.Equal(1, plan.TotalValue);
        Reference.AssertPlanIsValid(problem, plan);
    }

    [Fact]
    public void RevisitsIntersectionsWhenShortestPathRequiresIt()
    {
        // Звезда: всё сообщение — через центр, курьер проезжает его несколько раз.
        var net = TestData.Network(("Склад", "Центр", 1), ("Центр", "A", 1), ("Центр", "B", 1), ("Центр", "C", 1));
        var problem = new DeliveryProblem(net, "Склад",
            [new Order("a", "A"), new Order("b", "B"), new Order("c", "C")], shiftMinutes: 100);

        var plan = TestData.PlanDp(problem);

        Assert.Equal(3, plan.Stops.Count);
        Assert.Equal(8, plan.ReturnMinute); // 1 + (1+1)·2 + 1 + 1 + 1
        Assert.Equal(4, plan.StreetPath.Count(n => n == "Центр"));
    }

    // ---------------------------------------------------------------- пограничные случаи

    [Fact]
    public void NoOrders_EmptyRouteStaysAtDepot()
    {
        var problem = new DeliveryProblem(TestData.Network(), "Склад", [], shiftMinutes: 60);

        var plan = TestData.PlanDp(problem);

        Assert.Empty(plan.Stops);
        Assert.Empty(plan.Skipped);
        Assert.Equal(0, plan.ReturnMinute);
        Assert.Equal(["Склад"], plan.StreetPath);
    }

    [Fact]
    public void SingleOrder_ExactlyFillsShift()
    {
        var net = TestData.Network(("Склад", "Дом", 10));
        var problem = new DeliveryProblem(net, "Склад", [new Order("1", "Дом", ServiceMinutes: 2)], shiftMinutes: 22);

        var plan = TestData.PlanDp(problem);

        Assert.Equal(["1"], plan.OrderIds);
        Assert.Equal(22, plan.ReturnMinute);
    }

    [Fact]
    public void SingleOrder_OneMinuteShort()
    {
        var net = TestData.Network(("Склад", "Дом", 10));
        var problem = new DeliveryProblem(net, "Склад", [new Order("1", "Дом", ServiceMinutes: 2)], shiftMinutes: 21);

        var plan = TestData.PlanDp(problem);

        Assert.Empty(plan.Stops);
        Assert.Equal(SkipReason.ShiftTooShort, Assert.Single(plan.Skipped).Reason);
    }

    [Fact]
    public void DeadlineMetExactlyAtTheLastMinute()
    {
        var net = TestData.Network(("Склад", "Дом", 10));
        var problem = new DeliveryProblem(net, "Склад", [new Order("1", "Дом", DeadlineMinute: 10)], shiftMinutes: 60);

        Assert.Single(TestData.PlanDp(problem).Stops);
    }

    [Fact]
    public void ZeroShift_OnlyOrdersAtDepotWithoutServiceTimeFit()
    {
        var net = TestData.Network(("Склад", "Дом", 1));
        var problem = new DeliveryProblem(net, "Склад",
            [new Order("here", "Склад"), new Order("away", "Дом")], shiftMinutes: 0);

        var plan = TestData.PlanDp(problem);

        Assert.Equal(["here"], plan.OrderIds);
        Assert.Equal(SkipReason.ShiftTooShort, Assert.Single(plan.Skipped).Reason);
    }

    [Fact]
    public void ImpossibleOrders_ReportedWithReasons()
    {
        var problem = ScenarioLoader.LoadFile(Path.Combine(ScenarioFiles.Directory, "04-edge-unreachable.json"));

        var plan = TestData.PlanDp(problem);

        Assert.Empty(plan.Stops);
        var reasons = plan.Skipped.ToDictionary(s => s.Order.Id, s => s.Reason);
        Assert.Equal(SkipReason.Unreachable, reasons["X"]);         // изолированный адрес
        Assert.Equal(SkipReason.Unreachable, reasons["Y"]);         // туда можно, обратно нельзя
        Assert.Equal(SkipReason.DeadlineUnreachable, reasons["Z"]);
        Assert.Equal(SkipReason.ShiftTooShort, reasons["W"]);
    }

    [Fact]
    public void SeveralOrdersAtSameAddress_DeliveredInOneVisit()
    {
        var net = TestData.Network(("Склад", "Дом", 10));
        var problem = new DeliveryProblem(net, "Склад",
            [new Order("1", "Дом"), new Order("2", "Дом"), new Order("3", "Дом")], shiftMinutes: 20);

        var plan = TestData.PlanDp(problem);

        Assert.Equal(3, plan.Stops.Count);
        Assert.Equal(20, plan.ReturnMinute);
        Assert.Equal(2, new TravelTimes(problem).DijkstraRuns); // склад + один общий адрес
    }

    [Fact]
    public void MaximumSupportedOrderCount_Works()
    {
        // Все заказы на одной улице-луче: оптимум очевиден — доставить всё по пути туда.
        var net = TestData.Network();
        var orders = new List<Order>();
        for (int i = 1; i <= DpRoutePlanner.MaxOrders; i++)
        {
            net.AddRoad(i == 1 ? "Склад" : $"Дом{i - 1}", $"Дом{i}", 1);
            orders.Add(new Order($"#{i}", $"Дом{i}"));
        }

        var problem = new DeliveryProblem(net, "Склад", orders, shiftMinutes: 2 * DpRoutePlanner.MaxOrders);
        var plan = TestData.PlanDp(problem);

        Assert.Equal(DpRoutePlanner.MaxOrders, plan.Stops.Count);
        Assert.Equal(2 * DpRoutePlanner.MaxOrders, plan.ReturnMinute);
    }

    [Fact]
    public void TooManyOrders_RejectedExplicitly()
    {
        var net = TestData.Network(("Склад", "Дом", 1));
        var orders = Enumerable.Range(0, DpRoutePlanner.MaxOrders + 1).Select(i => new Order($"#{i}", "Дом")).ToList();
        var problem = new DeliveryProblem(net, "Склад", orders, shiftMinutes: 10);

        var e = Assert.Throws<ArgumentException>(() => TestData.PlanDp(problem));
        Assert.Contains(DpRoutePlanner.MaxOrders.ToString(), e.Message);
    }

    // ---------------------------------------------------------------- сверка с эталоном

    [Fact]
    public void MatchesBruteForceOnRandomProblems()
    {
        var random = new Random(42);
        for (int t = 0; t < 1500; t++)
        {
            var problem = TestData.RandomProblem(random, maxNodes: 8, maxOrders: 6);

            var plan = TestData.PlanDp(problem);

            Assert.Equal(Reference.BestByBruteForce(problem), (plan.TotalValue, (long)plan.ReturnMinute));
            Reference.AssertPlanIsValid(problem, plan);
        }
    }
}
