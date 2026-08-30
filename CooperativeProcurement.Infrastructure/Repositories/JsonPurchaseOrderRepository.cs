using CooperativeProcurement.Core.Interfaces;
using CooperativeProcurement.Core.Models;
using System.Text.Json;

namespace CooperativeProcurement.Infrastructure.Repositories
{
    /// <summary>
    /// Реализация репозитория с хранением данных в JSON-файле
    /// </summary>
    public class JsonPurchaseOrderRepository : IPurchaseOrderRepository
    {
        private readonly string _filePath;
        private readonly object _lock = new object();

        public JsonPurchaseOrderRepository(string filePath = "orders.json")
        {
            _filePath = filePath;
            EnsureFileExists();
        }

        private void EnsureFileExists()
        {
            if (!File.Exists(_filePath))
            {
                // Создаем пустой массив в файле
                var initialData = new List<PurchaseOrder>();
                var json = JsonSerializer.Serialize(initialData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
        }

        private List<PurchaseOrder> LoadOrders()
        {
            lock (_lock)
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<PurchaseOrder>>(json) ?? new List<PurchaseOrder>();
            }
        }

        private void SaveOrders(List<PurchaseOrder> orders)
        {
            lock (_lock)
            {
                var json = JsonSerializer.Serialize(orders, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
        }

        public async Task<IEnumerable<PurchaseOrder>> GetAllAsync()
        {
            return await Task.Run(() => LoadOrders().AsEnumerable());
        }

        public async Task<PurchaseOrder?> GetByIdAsync(Guid id)
        {
            return await Task.Run(() => LoadOrders().FirstOrDefault(o => o.Id == id));
        }

        public async Task<PurchaseOrder> CreateAsync(PurchaseOrder order)
        {
            return await Task.Run(() =>
            {
                var orders = LoadOrders();
                order.Id = Guid.NewGuid();
                order.OrderDate = DateTime.Now;
                orders.Add(order);
                SaveOrders(orders);
                return order;
            });
        }

        public async Task<PurchaseOrder> UpdateAsync(PurchaseOrder order)
        {
            return await Task.Run(() =>
            {
                var orders = LoadOrders();
                var index = orders.FindIndex(o => o.Id == order.Id);
                if (index == -1)
                    throw new InvalidOperationException($"Заказ с ID {order.Id} не найден");

                orders[index] = order;
                SaveOrders(orders);
                return order;
            });
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            return await Task.Run(() =>
            {
                var orders = LoadOrders();
                var removed = orders.RemoveAll(o => o.Id == id);
                if (removed > 0)
                {
                    SaveOrders(orders);
                    return true;
                }
                return false;
            });
        }
    }
}
