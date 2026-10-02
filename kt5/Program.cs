using Kt5;

Console.OutputEncoding = System.Text.Encoding.UTF8;

int failed = 0;

// ---------------------------------------------------------------------------
// Часть 1. Максимальная сумма непрерывного подмассива
// ---------------------------------------------------------------------------
Console.WriteLine("=== Часть 1. Максимальная сумма непрерывного подмассива ===");

CheckMaxSubarray([-2, 1, -3, 4, -1, 2, 1, -5, 4], 6, "пример из задания: [4, -1, 2, 1]");
CheckMaxSubarray([1, 2, 3, 4], 10, "все положительные — весь массив целиком");
CheckMaxSubarray([-1, -2, -3], -1, "все отрицательные — наименьший по модулю элемент");
CheckMaxSubarray([5], 5, "один элемент");
CheckMaxSubarray([-4, -1, 7, -3, -8], 7, "единственное положительное среди отрицательных");
CheckMaxSubarray([3, -1, -1, 4], 5, "выгодно пройти через отрицательные");
CheckMaxSubarray([2, -5, 3], 3, "выгодно начать заново");
CheckMaxSubarray([0, 0, 0], 0, "нули");

// Сверка с перебором всех подмассивов O(n^2) на случайных данных.
var random = new Random(5);
bool bruteOk = true;
for (int t = 0; t < 2000 && bruteOk; t++)
{
    int[] arr = new int[random.Next(1, 13)];
    for (int i = 0; i < arr.Length; i++)
        arr[i] = random.Next(-20, 21);

    bruteOk = Algorithms.MaxSubarraySum(arr) == BruteMaxSubarray(arr);
}
Report(bruteOk, "2000 случайных массивов: результат совпадает с полным перебором");

// ---------------------------------------------------------------------------
// Часть 2. Набор суммы: жадный алгоритм и DP
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== Часть 2. Набор суммы номиналами: жадный алгоритм и DP ===");

// Обоснования написаны после запуска по фактическим результатам; строки «Сравнение»
// и «Проверка каноничности» программа вычисляет сама, поэтому текст не может разойтись с ними.

CompareOnSet([1, 5, 10], 18, expectedGreedy: 5, expectedDp: 5,
    "Жадный выбор корректен всегда, и это доказывается: каждый номинал делится на " +
    "предыдущий (5 = 5*1, 10 = 2*5). В оптимальном наборе не может быть пяти монет по 1 " +
    "(их заменит одна 5) и двух монет по 5 (их заменит одна 10), значит мелкими номиналами " +
    "набирается меньше ближайшего крупного, и крупный брать обязательно. Система каноническая.");

CompareOnSet([1, 2, 5, 10, 50, 100], 388, expectedGreedy: 10, expectedDp: 10,
    "Привычная денежная система. Номинал 5 не делится на 2, поэтому простого довода про " +
    "делимость нет, но перебор всех сумм ниже подтверждает: расхождений нет, система " +
    "каноническая, жадный алгоритм можно использовать вместо DP.");

CompareOnSet([1, 3, 4], 6, expectedGreedy: 3, expectedDp: 2,
    "Жадный алгоритм берёт 4 и вынужден добирать остаток 2 единицами (4+1+1), хотя 3+3 " +
    "короче. Причина: 4 < 2*3, то есть две «средние» монеты перекрывают крупную и дают " +
    "сумму, которую крупная с мелочью набирает хуже. Система неканоническая, нужен DP.");

CompareOnSet([1, 5, 8], 10, expectedGreedy: 3, expectedDp: 2,
    "Набор подобран самостоятельно по тому же принципу: 8 < 2*5 и 8 != 5+1. Жадный берёт 8 " +
    "и добирает 1+1, оптимум — 5+5. Система неканоническая, жадный выбор некорректен.");

CompareOnSet([1, 7, 10], 14, expectedGreedy: 5, expectedDp: 2,
    "Ещё один самостоятельно подобранный набор: 10 < 2*7. Разрыв больше: жадный даёт " +
    "10+1+1+1+1 (5 номиналов) против 7+7 (2 номинала). Система неканоническая.");

CompareOnSet([3, 5], 9, expectedGreedy: null, expectedDp: 3,
    "Набор без единицы. Жадный берёт 5, затем 3, остаётся 1 — тупик, возвращает null, хотя " +
    "сумма набирается как 3+3+3. Здесь жадный алгоритм не просто неоптимален, а не находит " +
    "решение вовсе. Корректен только DP.");

CompareOnSet([5, 10], 12, expectedGreedy: null, expectedDp: -1,
    "Сумма 12 не кратна 5, набрать её нельзя: оба метода сообщают об этом (null и -1). " +
    "На достижимых суммах (кратных 5) жадный выбор корректен — это система {1, 2}, " +
    "умноженная на 5, а в ней каждый номинал делится на предыдущий.");

Console.WriteLine();
Console.WriteLine("=== Общий вывод ===");
Console.WriteLine(
    "Совпадение жадного алгоритма и DP на отдельных суммах ничего не доказывает: для {1, 3, 4}\n" +
    "они совпадают на сумме 5 (4+1) и расходятся на 6. Жадный алгоритм допустим только для\n" +
    "канонических систем, где его корректность доказана или проверена на всех суммах до\n" +
    "суммы двух старших номиналов. Для произвольного набора номиналов надёжен только DP:\n" +
    "он перебирает все варианты последнего номинала и гарантирует минимум за O(target * k).");

Console.WriteLine();
Console.WriteLine(failed == 0 ? "Все проверки пройдены." : $"Проверок не пройдено: {failed}");
return failed == 0 ? 0 : 1;

void CheckMaxSubarray(int[] arr, int expected, string note)
{
    int actual = Algorithms.MaxSubarraySum(arr);
    Report(actual == expected, $"[{string.Join(", ", arr)}] -> {actual} (ожидалось {expected}) — {note}");
}

void CompareOnSet(int[] denominations, int target, int? expectedGreedy, int expectedDp, string reasoning)
{
    List<int>? greedy = Algorithms.GreedyMakeSum(target, denominations);
    int dp = Algorithms.MinCountDP(target, denominations);
    int? greedyCount = greedy?.Count;

    Console.WriteLine();
    Console.WriteLine($"Номиналы {{{string.Join(", ", denominations)}}}, сумма {target}");
    Console.WriteLine(greedy is null
        ? "  Жадный: сумму набрать не удалось (null)"
        : $"  Жадный: количество номиналов — {greedy.Count} ({string.Join(" + ", greedy)})");
    Console.WriteLine(dp == -1
        ? "  DP:     сумму набрать невозможно (-1)"
        : $"  DP:     количество номиналов — {dp}");

    bool same = (greedyCount ?? -1) == dp;
    Console.WriteLine($"  Сравнение: результаты {(same ? "СОВПАДАЮТ" : "РАЗЛИЧАЮТСЯ")}");
    Console.WriteLine($"  Проверка каноничности: {DescribeCanonicity(denominations)}");
    Console.WriteLine($"  Обоснование: {reasoning}");

    Report(greedyCount == expectedGreedy && dp == expectedDp, "результаты соответствуют ожидаемым");
}

// Для системы с номиналом 1 наименьший контрпример к жадному алгоритму, если он есть,
// меньше суммы двух старших номиналов (теорема Козена — Закса). Поэтому перебор всех сумм
// до этой границы — доказательство каноничности, а не выборочная проверка.
// Без номинала 1 теорема неприменима, там перебор лишь ищет контрпример.
string DescribeCanonicity(int[] denominations)
{
    int[] sorted = denominations.OrderBy(d => d).ToArray();
    bool hasOne = sorted[0] == 1;
    int limit = sorted.Length >= 2 ? sorted[^1] + sorted[^2] : sorted[^1];

    for (int s = 1; s <= limit; s++)
    {
        int dp = Algorithms.MinCountDP(s, denominations);
        int greedy = Algorithms.GreedyMakeSum(s, denominations)?.Count ?? -1;
        if (greedy != dp)
        {
            string greedyText = greedy == -1 ? "не набирает" : greedy.ToString();
            return $"НЕ каноническая — наименьший контрпример: сумма {s} (жадный: {greedyText}, DP: {dp})";
        }
    }

    return hasOne
        ? $"каноническая — жадный и DP совпали на всех суммах 1..{limit}, по теореме Козена — Закса этого достаточно"
        : $"расхождений на суммах 1..{limit} нет (номинала 1 нет, перебор не является доказательством)";
}

int BruteMaxSubarray(int[] arr)
{
    int best = int.MinValue;
    for (int i = 0; i < arr.Length; i++)
    {
        int sum = 0;
        for (int j = i; j < arr.Length; j++)
        {
            sum += arr[j];
            best = Math.Max(best, sum);
        }
    }

    return best;
}

void Report(bool ok, string message)
{
    if (!ok)
        failed++;

    Console.WriteLine($"  [{(ok ? "OK" : "FAIL")}] {message}");
}
