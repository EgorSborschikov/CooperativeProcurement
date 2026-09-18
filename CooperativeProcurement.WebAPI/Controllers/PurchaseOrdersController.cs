using CooperativeProcurement.Core.Models;
using CooperativeProcurement.Core.Services;
using CooperativeProcurement.Infrastructure.Repositories;
using CooperativeProcurement.WebAPI.Requests;
using Microsoft.AspNetCore.Mvc;

namespace CooperativeProcurement.WebAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PurchaseOrdersController : ControllerBase
{
    private readonly PurchaseOrderService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="PurchaseOrdersController"/> class.
    /// </summary>
    public PurchaseOrdersController()
    {
        // В реальном проекте используется DI, но для простоты создаем напрямую
        var repository = new JsonPurchaseOrderRepository("orders.json");
        this._service = new PurchaseOrderService(repository);
    }

    /// <summary>
    /// Получить все заказы.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PurchaseOrder>>> GetOrders()
    {
        try
        {
            IEnumerable<PurchaseOrder> orders = await this._service.GetAllOrdersAsync();
            return Ok(orders);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при получении заказов: {ex.Message}");
        }
    }

    /// <summary>
    /// Получить заказ по ID.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<PurchaseOrder>> GetOrder(Guid id)
    {
        try
        {
            PurchaseOrder? order = await this._service.GetOrderByIdAsync(id);
            return order == null ? (ActionResult<PurchaseOrder>)NotFound($"Заказ с ID {id} не найден") : (ActionResult<PurchaseOrder>)Ok(order);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при получении заказа: {ex.Message}");
        }
    }

    /// <summary>
    /// Создать новый заказ.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpPost]
    public async Task<ActionResult<PurchaseOrder>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            PurchaseOrder order = await this._service.CreateOrderAsync(request.SupplierName, request.Items);
            return CreatedAtAction(nameof(this.GetOrder), new { id = order.Id }, order);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при создании заказа: {ex.Message}");
        }
    }

    /// <summary>
    /// Обновить статус заказа.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpPut("{id}/status")]
    public async Task<ActionResult<PurchaseOrder>> UpdateOrderStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            PurchaseOrder order = await this._service.UpdateOrderStatusAsync(id, request.Status);
            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при обновлении статуса: {ex.Message}");
        }
    }

    /// <summary>
    /// Удалить заказ.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteOrder(Guid id)
    {
        try
        {
            bool result = await this._service.DeleteOrderAsync(id);
            return !result ? NotFound($"Заказ с ID {id} не найден") : NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при удалении заказа: {ex.Message}");
        }
    }

    /// <summary>
    /// Получить общую сумму всех заказов.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpGet("total")]
    public async Task<ActionResult<decimal>> GetTotalAmount()
    {
        try
        {
            decimal total = await this._service.GetTotalAmountAllOrdersAsync();
            return Ok(new { TotalAmount = total });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Ошибка при подсчете суммы: {ex.Message}");
        }
    }
}
