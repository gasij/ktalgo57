using CourierRouting.Io;

namespace CourierRouting.Tests;

/// <summary>
/// Обоснование отказа от жадного алгоритма: конкретные данные, на которых он теряет ценность,
/// и проверка, что на случайных данных он никогда не лучше DP (DP — точный оптимум).
/// </summary>
public class GreedyVsDpTests
{
    [Theory]
    [InlineData("02-greedy-trap.json", 6, 10)]    // ближний дешёвый заказ «съедает» время дальних ценных
    [InlineData("03-deadline-trap.json", 1, 2)]   // ближний несрочный заказ — опоздание к срочному
    [InlineData("01-city.json", 16, 19)]          // реалистичный сценарий
    public void GreedyLosesValueOnCounterexamples(string file, long greedyValue, long dpValue)
    {
        var problem = ScenarioLoader.LoadFile(Path.Combine(ScenarioFiles.Directory, file));

        var greedy = TestData.PlanGreedy(problem);
        var dp = TestData.PlanDp(problem);

        Assert.Equal(greedyValue, greedy.TotalValue);
        Assert.Equal(dpValue, dp.TotalValue);
        Reference.AssertPlanIsValid(problem, greedy); // жадный план допустим, но не оптимален
    }

    [Fact]
    public void GreedyIsAlwaysValidAndNeverBetterThanDp()
    {
        var random = new Random(7);
        int greedyWorse = 0;

        for (int t = 0; t < 1500; t++)
        {
            var problem = TestData.RandomProblem(random, maxNodes: 8, maxOrders: 6);
            var greedy = TestData.PlanGreedy(problem);
            var dp = TestData.PlanDp(problem);

            Reference.AssertPlanIsValid(problem, greedy);
            Assert.True(greedy.TotalValue <= dp.TotalValue);
            if (greedy.TotalValue < dp.TotalValue)
                greedyWorse++;
        }

        // Расхождения — не редкость, а заметная доля случаев.
        Assert.True(greedyWorse > 50, $"Жадный хуже DP лишь в {greedyWorse} случаях из 1500.");
    }
}
