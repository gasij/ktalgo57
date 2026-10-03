namespace CourierRouting.Model;

/// <summary>Заказ на доставку.</summary>
/// <param name="Id">Уникальный номер заказа.</param>
/// <param name="Location">Перекрёсток дорожной сети, куда нужно доставить заказ.</param>
/// <param name="Value">Ценность заказа (приоритет, оплата) — то, что максимизируется. Не меньше 1.</param>
/// <param name="DeadlineMinute">
/// Крайняя минута смены, к которой курьер должен прибыть по адресу; null — без дедлайна.
/// </param>
/// <param name="ServiceMinutes">Сколько минут занимает передача заказа на месте.</param>
public sealed record Order(
    string Id,
    string Location,
    int Value = 1,
    int? DeadlineMinute = null,
    int ServiceMinutes = 0);
