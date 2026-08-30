using CooperativeProcurement.Core.Models;

namespace CooperativeProcurement.WebAPI.Requests
{
    /// <summary>
    /// Запрос на создание заказа
    /// </summary>
    public class CreateOrderRequest
    {
        public string SupplierName { get; set; } = string.Empty;
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

}
