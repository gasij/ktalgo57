using CourierRouting.Model;

namespace CourierRouting.Planning;

/// <summary>
/// Точный планировщик: динамическое программирование по подмножествам заказов
/// (идея алгоритма Хелда — Карпа для задачи коммивояжёра, дополненная дедлайнами,
/// ограничением смены и выбором подмножества).
/// </summary>
/// <remarks>
/// <para>
/// Состояние — (mask, last): mask — множество уже доставленных заказов, last — заказ,
/// доставленный последним (курьер стоит у него). Значение finish[mask, last] — самая ранняя
/// минута, в которую курьер может закончить передачу заказа last, доставив ровно заказы mask,
/// уложившись во все их дедлайны и сохранив возможность вернуться на склад до конца смены.
/// </para>
/// <para>
/// Почему достаточно хранить только самое раннее время: всё, что курьер может сделать
/// дальше, зависит лишь от того, где он стоит (last), что уже доставлено (mask) и который час.
/// Все ограничения задачи — верхние границы времени (дедлайн, конец смены), поэтому из двух
/// маршрутов в одно состояние более поздний ничем не лучше: любое его продолжение выполнимо
/// и для более раннего, и даст ту же ценность. Ценность самого состояния определяется mask
/// и от порядка объезда не зависит.
/// </para>
/// <para>
/// Сложность: O(2^k · k²) по времени и O(2^k · k) по памяти, где k — число заказов,
/// плюс построение <see cref="TravelTimes"/>. Отсюда ограничение <see cref="MaxOrders"/>.
/// </para>
/// </remarks>
public sealed class DpRoutePlanner : IRoutePlanner
{
    /// <summary>
    /// При k = 20 таблица — 2^20 · 20 ≈ 21 млн ячеек (около 100 МБ вместе с массивом
    /// предшественников). Дальше каждый новый заказ удваивает память — это граница метода.
    /// </summary>
    public const int MaxOrders = 20;

    private const int NotReached = int.MaxValue;
    private const sbyte FromDepot = -1;

    public string Name => "DP по подмножествам";

    public RoutePlan Plan(DeliveryProblem problem, TravelTimes travel)
    {
        int k = problem.Orders.Count;
        if (k > MaxOrders)
            throw new ArgumentException(
                $"Точный план строится не более чем для {MaxOrders} заказов, передано {k}: " +
                "таблица DP растёт как 2^k · k.", nameof(problem));

        if (k == 0)
            return Build(problem, travel, []);

        var deadline = new long[k];
        var service = new long[k];
        var back = new long[k];
        for (int i = 0; i < k; i++)
        {
            deadline[i] = problem.Orders[i].DeadlineMinute ?? long.MaxValue;
            service[i] = problem.Orders[i].ServiceMinutes;
            int b = travel.Minutes(i + 1, 0);
            back[i] = b == TravelTimes.Unreachable ? long.MaxValue / 4 : b;
        }

        long shift = problem.ShiftMinutes;
        int states = (1 << k) * k;
        var finish = new int[states];
        var previous = new sbyte[states];
        Array.Fill(finish, NotReached);

        // Попытка поставить заказ next следующим после состояния, закончившегося в момент time.
        void Relax(int mask, int fromPoint, sbyte fromOrder, long time, int next)
        {
            int leg = travel.Minutes(fromPoint, next + 1);
            if (leg == TravelTimes.Unreachable)
                return;

            long arrival = time + leg;
            if (arrival > deadline[next])
                return;

            long done = arrival + service[next];

            // Отсечение: если отсюда уже не успеть на склад, никакое продолжение не поможет —
            // кратчайший путь до склада через другие адреса не короче прямого кратчайшего.
            if (done + back[next] > shift)
                return;

            int index = (mask | (1 << next)) * k + next;
            if (done < finish[index])
            {
                finish[index] = (int)done;
                previous[index] = fromOrder;
            }
        }

        for (int next = 0; next < k; next++)
            Relax(0, 0, FromDepot, 0, next);

        // Переходы ведут только в бóльшие маски, поэтому обход масок по возрастанию —
        // корректный порядок: к моменту обработки состояния все пути в него уже рассмотрены.
        for (int mask = 1; mask < 1 << k; mask++)
        {
            for (int last = 0; last < k; last++)
            {
                if ((mask & (1 << last)) == 0 || finish[mask * k + last] == NotReached)
                    continue;

                long time = finish[mask * k + last];
                for (int next = 0; next < k; next++)
                {
                    if ((mask & (1 << next)) == 0)
                        Relax(mask, last + 1, (sbyte)last, time, next);
                }
            }
        }

        // Выбор ответа: максимальная ценность, при равенстве — более раннее возвращение.
        // Пустой маршрут (ценность 0, возврат в минуту 0) допустим всегда — он и есть начальный кандидат.
        var valueOfMask = new long[1 << k];
        long bestValue = 0, bestReturn = 0;
        int bestMask = 0, bestLast = -1;

        for (int mask = 1; mask < 1 << k; mask++)
        {
            int low = System.Numerics.BitOperations.TrailingZeroCount(mask);
            valueOfMask[mask] = valueOfMask[mask & (mask - 1)] + problem.Orders[low].Value;

            for (int last = 0; last < k; last++)
            {
                int t = finish[mask * k + last];
                if (t == NotReached)
                    continue;

                long returnMinute = t + back[last]; // не больше shift — гарантировано отсечением
                if (valueOfMask[mask] > bestValue || (valueOfMask[mask] == bestValue && returnMinute < bestReturn))
                {
                    (bestValue, bestReturn, bestMask, bestLast) = (valueOfMask[mask], returnMinute, mask, last);
                }
            }
        }

        // Восстановление порядка объезда по массиву предшественников — с конца к началу.
        var sequence = new List<int>();
        for (int mask = bestMask, last = bestLast; last != FromDepot;)
        {
            sequence.Add(last);
            int prev = previous[mask * k + last];
            mask &= ~(1 << last);
            last = prev;
        }

        sequence.Reverse();
        return Build(problem, travel, sequence);
    }

    private static RoutePlan Build(DeliveryProblem problem, TravelTimes travel, List<int> sequence) =>
        RouteBuilder.TryBuild(problem, travel, sequence)
        ?? throw new InvalidOperationException("Внутренняя ошибка: DP построил недопустимый маршрут.");
}
