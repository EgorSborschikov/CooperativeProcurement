using CooperativeProcurement.Core.Models;

namespace CooperativeProcurement.Core.Interfaces;

/// <summary>
/// Интерфейс репозитория для работы с заказами.
/// </summary>
public interface IPurchaseOrderRepository
{
    /// <summary>
    /// Получить все заказы.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<IEnumerable<PurchaseOrder>> GetAllAsync();

    /// <summary>
    /// Получить заказ по идентификатору.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<PurchaseOrder?> GetByIdAsync(Guid id);

    /// <summary>
    /// Создать новый заказ.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<PurchaseOrder> CreateAsync(PurchaseOrder order);

    /// <summary>
    /// Обновить существующий заказ.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<PurchaseOrder> UpdateAsync(PurchaseOrder order);

    /// <summary>
    /// Удалить заказ.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<bool> DeleteAsync(Guid id);
}
