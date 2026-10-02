namespace Kt5;

/// <summary>
/// Алгоритмы контрольной точки 5: динамическое программирование и жадный подход.
/// </summary>
public static class Algorithms
{
    /// <summary>
    /// Часть 1. Максимальная сумма непрерывного подмассива (алгоритм Кадане).
    /// Сложность: O(n) по времени, O(1) по памяти.
    /// </summary>
    /// <param name="arr">Массив, содержащий хотя бы один элемент.</param>
    public static int MaxSubarraySum(int[] arr)
    {
        if (arr is null || arr.Length == 0)
            throw new ArgumentException("Массив должен содержать хотя бы один элемент.", nameof(arr));

        // maxEndingHere — лучшая сумма подмассива, который заканчивается ровно в позиции i.
        // maxSoFar — лучшая сумма среди всех уже просмотренных позиций.
        int maxEndingHere = arr[0];
        int maxSoFar = arr[0];

        for (int i = 1; i < arr.Length; i++)
        {
            // Два варианта для подмассива, заканчивающегося в i:
            //   1) продолжить предыдущий: maxEndingHere + arr[i];
            //   2) начать новый с arr[i].
            // Продолжать выгодно только пока накопленная сумма положительна:
            // отрицательный «хвост» лишь уменьшит arr[i].
            maxEndingHere = Math.Max(arr[i], maxEndingHere + arr[i]);
            maxSoFar = Math.Max(maxSoFar, maxEndingHere);
        }

        return maxSoFar;
    }

    /// <summary>
    /// Часть 2. Жадный набор суммы: на каждом шаге берётся наибольший номинал,
    /// не превышающий остаток.
    /// </summary>
    /// <returns>Список использованных номиналов или <c>null</c>, если точно набрать сумму не удалось.</returns>
    public static List<int>? GreedyMakeSum(int target, int[] denominations)
    {
        if (target < 0)
            return null;

        // Неположительные номиналы отбрасываем: с ними цикл while не завершился бы.
        var sorted = denominations.Where(d => d > 0).Distinct().OrderByDescending(d => d).ToArray();
        var result = new List<int>();

        foreach (int value in sorted)
        {
            while (target >= value)
            {
                result.Add(value);
                target -= value;
            }
        }

        // Остаток не равен нулю — жадная стратегия зашла в тупик.
        // Это не значит, что сумму набрать нельзя: жадный алгоритм не возвращается назад.
        return target == 0 ? result : null;
    }

    /// <summary>
    /// Часть 2. Минимальное количество номиналов для набора суммы (DP, размен монет).
    /// dp[s] — минимальное количество номиналов, дающих в сумме ровно s.
    /// Сложность: O(target * k) по времени, O(target) по памяти, k — число номиналов.
    /// </summary>
    /// <returns>Минимальное количество номиналов или -1, если сумму набрать невозможно.</returns>
    public static int MinCountDP(int target, int[] denominations)
    {
        if (target < 0)
            return -1;

        int[] dp = new int[target + 1];
        Array.Fill(dp, int.MaxValue);
        dp[0] = 0;

        for (int s = 1; s <= target; s++)
        {
            foreach (int value in denominations)
            {
                if (value > 0 && value <= s && dp[s - value] != int.MaxValue)
                {
                    // Последним взят номинал value, остальное — оптимальный набор суммы s - value.
                    dp[s] = Math.Min(dp[s], dp[s - value] + 1);
                }
            }
        }

        return dp[target] == int.MaxValue ? -1 : dp[target];
    }
}
