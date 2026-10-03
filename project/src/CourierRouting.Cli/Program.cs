using CourierRouting.Cli;
using CourierRouting.Io;
using CourierRouting.Planning;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// Использование:
//   dotnet run --project src/CourierRouting.Cli                      — все демонстрационные сценарии
//   dotnet run --project src/CourierRouting.Cli -- scenarios/x.json  — один сценарий из файла
//   dotnet run --project src/CourierRouting.Cli -- --bench [k]       — замер роста времени до k заказов
try
{
    if (args.Length > 0 && args[0] == "--bench")
    {
        int max = args.Length > 1 ? int.Parse(args[1]) : DpRoutePlanner.MaxOrders;
        Benchmark.Run(Math.Min(max, DpRoutePlanner.MaxOrders));
        return 0;
    }

    var files = args.Length > 0
        ? args
        : Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "scenarios"), "*.json").Order().ToArray();

    foreach (var file in files)
    {
        Console.WriteLine();
        Console.WriteLine($"=============== {Path.GetFileName(file)} ===============");
        try
        {
            ConsoleReport.Solve(ScenarioLoader.LoadFile(file));
        }
        catch (Exception e) when (e is FormatException or ArgumentException or KeyNotFoundException)
        {
            // Некорректный сценарий — ожидаемая ситуация, а не сбой программы: сообщаем и идём дальше.
            Console.WriteLine($"Сценарий отклонён: {e.Message}");
        }
    }

    return 0;
}
catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException)
{
    Console.Error.WriteLine($"Ошибка: {e.Message}");
    return 1;
}
