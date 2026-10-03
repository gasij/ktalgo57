using CourierRouting.Io;

namespace CourierRouting.Tests;

/// <summary>Демонстрационные сценарии из папки scenarios — те же, что показывает консольное приложение.</summary>
public class ScenarioFiles
{
    public static string Directory { get; } = FindScenarios();

    private static string FindScenarios()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            string candidate = Path.Combine(dir.FullName, "scenarios");
            if (System.IO.Directory.Exists(candidate) && File.Exists(Path.Combine(dir.FullName, "README.md")))
                return candidate;
        }

        throw new DirectoryNotFoundException("Не найдена папка scenarios рядом с README.md.");
    }

    [Fact]
    public void AllValidScenariosLoadAndProduceValidPlans()
    {
        var files = System.IO.Directory.GetFiles(Directory, "*.json").Where(f => !f.Contains("invalid")).ToList();
        Assert.NotEmpty(files);

        foreach (var file in files)
        {
            var problem = ScenarioLoader.LoadFile(file);
            var plan = TestData.PlanDp(problem);

            Reference.AssertPlanIsValid(problem, plan);
            Assert.Equal(Reference.BestByBruteForce(problem), (plan.TotalValue, (long)plan.ReturnMinute));
        }
    }
}
