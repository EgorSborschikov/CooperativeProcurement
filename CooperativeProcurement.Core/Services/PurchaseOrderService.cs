using CooperativeProcurement.Core.Interfaces;
using CooperativeProcurement.Core.Models;

namespace CooperativeProcurement.Core.Services;

/// <summary>
/// Сервис для работы с заказами на закупку.
/// </summary>
public class PurchaseOrderService
{
    private readonly IPurchaseOrderRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="PurchaseOrderService"/> class.
    /// </summary>
    /// <param name="repository"></param>
    public PurchaseOrderService(IPurchaseOrderRepository repository)
    {
        this._repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Получить все заказы.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<IEnumerable<PurchaseOrder>> GetAllOrdersAsync() => await this._repository.GetAllAsync();

    /// <summary>
    /// Получить заказ по ID.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<PurchaseOrder?> GetOrderByIdAsync(Guid id)
    {
        return id == Guid.Empty
            ? throw new ArgumentException("ID не может быть пустым", nameof(id))
            : await this._repository.GetByIdAsync(id);
    }

    /// <summary>
    /// Создать новый заказ.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<PurchaseOrder> CreateOrderAsync(string supplierName, List<OrderItem> items)
    {
        if (string.IsNullOrWhiteSpace(supplierName))
        {
            throw new ArgumentException("Имя поставщика не может быть пустым", nameof(supplierName));
        }

        if (items == null || !items.Any())
        {
            throw new ArgumentException("Заказ должен содержать хотя бы одну позицию", nameof(items));
        }

        if (items.Any(item => item.Quantity <= 0))
        {
            throw new ArgumentException("Количество товара должно быть положительным", nameof(items));
        }

        if (items.Any(item => item.UnitPrice < 0))
        {
            throw new ArgumentException("Цена товара не может быть отрицательной", nameof(items));
        }

        var order = new PurchaseOrder
        {
            SupplierName = supplierName,
            Items = items,
            OrderDate = DateTime.Now,
            Status = OrderStatus.Draft,
        };

        return await this._repository.CreateAsync(order);
    }

    /// <summary>
    /// Обновить статус заказа.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<PurchaseOrder> UpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus)
    {
        PurchaseOrder? order = await this._repository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new InvalidOperationException($"Заказ с ID {orderId} не найден");
        }

        if (!order.IsValid())
        {
            throw new InvalidOperationException("Нельзя изменить статус невалидного заказа");
        }

        order.Status = newStatus;
        return await this._repository.UpdateAsync(order);
    }

    /// <summary>
    /// Удалить заказ.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<bool> DeleteOrderAsync(Guid id)
    {
        return id == Guid.Empty
            ? throw new ArgumentException("ID не может быть пустым", nameof(id))
            : await this._repository.DeleteAsync(id);
    }

    /// <summary>
    /// Получить общую сумму всех заказов.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task<decimal> GetTotalAmountAllOrdersAsync()
    {
        IEnumerable<PurchaseOrder> orders = await this._repository.GetAllAsync();
        return orders.Sum(o => o.TotalAmount);
    }
}
