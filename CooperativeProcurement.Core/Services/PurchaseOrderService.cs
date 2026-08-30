using CooperativeProcurement.Core.Interfaces;
using CooperativeProcurement.Core.Models;

namespace CooperativeProcurement.Core.Services
{
    /// <summary>
    /// Сервис для работы с заказами на закупку
    /// </summary>
    public class PurchaseOrderService
    {
        private readonly IPurchaseOrderRepository _repository;

        public PurchaseOrderService(IPurchaseOrderRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Получить все заказы
        /// </summary>
        public async Task<IEnumerable<PurchaseOrder>> GetAllOrdersAsync()
        {
            return await _repository.GetAllAsync();
        }

        /// <summary>
        /// Получить заказ по ID
        /// </summary>
        public async Task<PurchaseOrder?> GetOrderByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("ID не может быть пустым", nameof(id));

            return await _repository.GetByIdAsync(id);
        }

        /// <summary>
        /// Создать новый заказ
        /// </summary>
        public async Task<PurchaseOrder> CreateOrderAsync(string supplierName, List<OrderItem> items)
        {
            if (string.IsNullOrWhiteSpace(supplierName))
                throw new ArgumentException("Имя поставщика не может быть пустым", nameof(supplierName));

            if (items == null || !items.Any())
                throw new ArgumentException("Заказ должен содержать хотя бы одну позицию", nameof(items));

            if (items.Any(item => item.Quantity <= 0))
                throw new ArgumentException("Количество товара должно быть положительным", nameof(items));

            if (items.Any(item => item.UnitPrice < 0))
                throw new ArgumentException("Цена товара не может быть отрицательной", nameof(items));

            var order = new PurchaseOrder
            {
                SupplierName = supplierName,
                Items = items,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Draft
            };

            return await _repository.CreateAsync(order);
        }

        /// <summary>
        /// Обновить статус заказа
        /// </summary>
        public async Task<PurchaseOrder> UpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus)
        {
            var order = await _repository.GetByIdAsync(orderId);
            if (order == null)
                throw new InvalidOperationException($"Заказ с ID {orderId} не найден");

            if (!order.IsValid())
                throw new InvalidOperationException("Нельзя изменить статус невалидного заказа");

            order.Status = newStatus;
            return await _repository.UpdateAsync(order);
        }

        /// <summary>
        /// Удалить заказ
        /// </summary>
        public async Task<bool> DeleteOrderAsync(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("ID не может быть пустым", nameof(id));

            return await _repository.DeleteAsync(id);
        }

        /// <summary>
        /// Получить общую сумму всех заказов
        /// </summary>
        public async Task<decimal> GetTotalAmountAllOrdersAsync()
        {
            var orders = await _repository.GetAllAsync();
            return orders.Sum(o => o.TotalAmount);
        }
    }
}
