namespace CooperativeProcurement.Core.Models;

/// <summary>
/// Заказ на закупку.
/// </summary>
public class PurchaseOrder
{
    /// <summary>
    /// Gets or sets уникальный идентификатор заказа.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets наименование поставщика.
    /// </summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets дата создания заказа.
    /// </summary>
    public DateTime OrderDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets список товарных позиций.
    /// </summary>
    public List<OrderItem> Items { get; set; } = [];

    /// <summary>
    /// Gets общая сумма заказа.
    /// </summary>
    public decimal TotalAmount => Items.Sum(item => item.TotalPrice);

    /// <summary>
    /// Gets or sets статус заказа (черновик, отправлен, получен).
    /// </summary>
    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    /// <summary>
    /// Метод для проверки валидности заказа.
    /// </summary>
    /// <returns>Возвращает true, если заказ валиден; иначе — false.</returns>
    public bool IsValid() => !string.IsNullOrWhiteSpace(SupplierName) && Items.Any() && Items.All(item => item.Quantity > 0 && item.UnitPrice >= 0);
}
