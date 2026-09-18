using System.Text.Json;
using CooperativeProcurement.Core.Interfaces;
using CooperativeProcurement.Core.Models;

namespace CooperativeProcurement.Infrastructure.Repositories;

/// <summary>
/// Реализация репозитория с хранением данных в JSON-файле.
/// </summary>
public class JsonPurchaseOrderRepository : IPurchaseOrderRepository
{
    private static readonly JsonSerializerOptions _jsonOptions = new ()
    {
        WriteIndented = true,
    };

    private readonly string _filePath;
    private readonly object _lock = new object();

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonPurchaseOrderRepository"/> class.
    /// </summary>
    /// <param name="filePath">Путь к JSON-файлу. По умолчанию — "orders.json".</param>
    public JsonPurchaseOrderRepository(string filePath = "orders.json")
    {
        this._filePath = filePath;
        this.EnsureFileExists();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PurchaseOrder>> GetAllAsync()
        => await Task.Run(() => this.LoadOrders().AsEnumerable());

    /// <inheritdoc/>
    public async Task<PurchaseOrder?> GetByIdAsync(Guid id)
        => await Task.Run(() => this.LoadOrders().FirstOrDefault(o => o.Id == id));

    /// <inheritdoc/>
    public async Task<PurchaseOrder> CreateAsync(PurchaseOrder order)
    {
        return await Task.Run(() =>
        {
            List<PurchaseOrder> orders = this.LoadOrders();
            order.Id = Guid.NewGuid();
            order.OrderDate = DateTime.Now;
            orders.Add(order);
            this.SaveOrders(orders);
            return order;
        });
    }

    /// <inheritdoc/>
    public async Task<PurchaseOrder> UpdateAsync(PurchaseOrder order)
    {
        return await Task.Run(() =>
        {
            List<PurchaseOrder> orders = this.LoadOrders();
            int index = orders.FindIndex(o => o.Id == order.Id);
            if (index == -1)
            {
                throw new InvalidOperationException($"Заказ с ID {order.Id} не найден");
            }

            orders[index] = order;
            this.SaveOrders(orders);
            return order;
        });
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id)
    {
        return await Task.Run(() =>
        {
            List<PurchaseOrder> orders = this.LoadOrders();
            int removed = orders.RemoveAll(o => o.Id == id);
            if (removed > 0)
            {
                this.SaveOrders(orders);
                return true;
            }

            return false;
        });
    }

    private void EnsureFileExists()
    {
        if (!File.Exists(this._filePath))
        {
            var initialData = new List<PurchaseOrder>();
            string json = JsonSerializer.Serialize(initialData, _jsonOptions);
            File.WriteAllText(this._filePath, json);
        }
    }

    private List<PurchaseOrder> LoadOrders()
    {
        lock (this._lock)
        {
            string json = File.ReadAllText(this._filePath);
            return JsonSerializer.Deserialize<List<PurchaseOrder>>(json) ?? [];
        }
    }

    private void SaveOrders(List<PurchaseOrder> orders)
    {
        lock (this._lock)
        {
            string json = JsonSerializer.Serialize(orders, _jsonOptions);
            File.WriteAllText(this._filePath, json);
        }
    }
}
