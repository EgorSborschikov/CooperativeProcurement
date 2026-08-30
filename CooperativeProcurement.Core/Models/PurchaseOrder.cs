namespace CooperativeProcurement.Core.Models
{
    /// <summary>
    /// Заказ на закупку
    /// </summary>
    public class PurchaseOrder
    {
        /// <summary>
        /// Уникальный идентификатор заказа
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Наименование поставщика
        /// </summary>
        public string SupplierName { get; set; } = string.Empty;

        /// <summary>
        /// Дата создания заказа
        /// </summary>
        public DateTime OrderDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Список товарных позиций
        /// </summary>
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();

        /// <summary>
        /// Общая сумма заказа
        /// </summary>
        public decimal TotalAmount => Items.Sum(item => item.TotalPrice);

        /// <summary>
        /// Статус заказа (черновик, отправлен, получен)
        /// </summary>
        public OrderStatus Status { get; set; } = OrderStatus.Draft;

        /// <summary>
        /// Метод для проверки валидности заказа
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(SupplierName) && Items.Any() && Items.All(item => item.Quantity > 0 && item.UnitPrice >= 0);
        }
    }
}
