using CourierRouting.Model;

namespace CourierRouting.Planning;

public interface IRoutePlanner
{
    string Name { get; }

    /// <summary>
    /// Строит допустимый маршрут. Пустой маршрут (никуда не ехать) допустим всегда,
    /// поэтому метод всегда возвращает план, а невыполнимые заказы перечислены в Skipped.
    /// </summary>
    RoutePlan Plan(DeliveryProblem problem, TravelTimes travel);
}
