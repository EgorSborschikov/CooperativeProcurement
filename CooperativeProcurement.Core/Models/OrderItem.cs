namespace CooperativeProcurement.Core.Models;

/// <summary>
/// Позиция заказа (товарная позиция).
/// </summary>
public class OrderItem
{
    /// <summary>
    /// Gets or sets уникальный идентификатор позиции.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets наименование товара.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets количество товара.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets цена за единицу товара.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets общая стоимость позиции (Quantity * UnitPrice).
    /// </summary>
    public decimal TotalPrice => Quantity * UnitPrice;
}
