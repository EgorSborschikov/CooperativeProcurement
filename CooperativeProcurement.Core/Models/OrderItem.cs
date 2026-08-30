namespace CooperativeProcurement.Core.Models
{
    /// <summary>
    /// Позиция заказа (товарная позиция)
    /// </summary>
    public class OrderItem
    {
        /// <summary>
        /// Уникальный идентификатор позиции
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Наименование товара
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Количество товара
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Цена за единицу товара
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Общая стоимость позиции (Quantity * UnitPrice)
        /// </summary>
        public decimal TotalPrice => Quantity * UnitPrice;
    }
}
