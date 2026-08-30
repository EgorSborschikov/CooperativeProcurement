namespace CooperativeProcurement.Core.Models
{
    /// <summary>
    /// Статус заказа
    /// </summary>
    public enum OrderStatus
    {
        Draft,      // Черновик
        Submitted,  // Отправлен поставщику
        Received    // Получен
    }
}
