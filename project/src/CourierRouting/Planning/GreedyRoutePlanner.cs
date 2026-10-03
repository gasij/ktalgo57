using CourierRouting.Model;

namespace CourierRouting.Planning;

/// <summary>
/// Жадный планировщик «ближайший сосед»: из текущей точки едет к ближайшему по времени
/// заказу, который ещё успевает к своему дедлайну и после которого можно вернуться на склад.
/// Сложность O(k²) после построения <see cref="TravelTimes"/>.
/// </summary>
/// <remarks>
/// Реализован не как решение, а как отклонённая альтернатива для сравнения: маршрут всегда
/// допустим, но не обязательно оптимален. Жадный выбор не учитывает, что ближайший заказ
/// может «съесть» время, нужное для более ценных или срочных заказов (контрпримеры — в тестах
/// и в демонстрации). Для этой задачи у жадного выбора нет свойства, аналогичного
/// каноничности монетной системы, при котором его можно было бы доказать.
/// </remarks>
public sealed class GreedyRoutePlanner : IRoutePlanner
{
    public string Name => "Жадный (ближайший сосед)";

    public RoutePlan Plan(DeliveryProblem problem, TravelTimes travel)
    {
        int k = problem.Orders.Count;
        var done = new bool[k];
        var sequence = new List<int>();
        long time = 0;
        int point = 0;

        while (true)
        {
            int best = -1;
            long bestLeg = long.MaxValue;

            for (int i = 0; i < k; i++)
            {
                if (done[i])
                    continue;

                int leg = travel.Minutes(point, i + 1);
                int back = travel.Minutes(i + 1, 0);
                if (leg == TravelTimes.Unreachable || back == TravelTimes.Unreachable)
                    continue;

                var order = problem.Orders[i];
                long arrival = time + leg;
                if (arrival > (order.DeadlineMinute ?? long.MaxValue))
                    continue;
                if (arrival + order.ServiceMinutes + back > problem.ShiftMinutes)
                    continue;

                // При равном расстоянии — более ценный заказ.
                if (leg < bestLeg || (leg == bestLeg && order.Value > problem.Orders[best].Value))
                {
                    best = i;
                    bestLeg = leg;
                }
            }

            if (best == -1)
                break;

            done[best] = true;
            sequence.Add(best);
            time += bestLeg + problem.Orders[best].ServiceMinutes;
            point = best + 1;
        }

        return RouteBuilder.TryBuild(problem, travel, sequence)
            ?? throw new InvalidOperationException("Внутренняя ошибка: жадный алгоритм построил недопустимый маршрут.");
    }
}
