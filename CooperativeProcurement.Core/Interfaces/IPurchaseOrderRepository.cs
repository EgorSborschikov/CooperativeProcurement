using CooperativeProcurement.Core.Models;

namespace CooperativeProcurement.Core.Interfaces
{
    /// <summary>
    /// Интерфейс репозитория для работы с заказами
    /// </summary>
    public interface IPurchaseOrderRepository
    {
        /// <summary>
        /// Получить все заказы
        /// </summary>
        Task<IEnumerable<PurchaseOrder>> GetAllAsync();

        /// <summary>
        /// Получить заказ по идентификатору
        /// </summary>
        Task<PurchaseOrder?> GetByIdAsync(Guid id);

        /// <summary>
        /// Создать новый заказ
        /// </summary>
        Task<PurchaseOrder> CreateAsync(PurchaseOrder order);

        /// <summary>
        /// Обновить существующий заказ
        /// </summary>
        Task<PurchaseOrder> UpdateAsync(PurchaseOrder order);

        /// <summary>
        /// Удалить заказ
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
    }
}
