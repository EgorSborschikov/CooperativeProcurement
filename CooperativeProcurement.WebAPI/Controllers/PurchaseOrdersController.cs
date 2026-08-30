using CooperativeProcurement.Core.Models;
using CooperativeProcurement.Core.Services;
using CooperativeProcurement.Infrastructure.Repositories;
using CooperativeProcurement.WebAPI.Requests;
using Microsoft.AspNetCore.Mvc;

namespace CooperativeProcurement.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly PurchaseOrderService _service;

        public PurchaseOrdersController()
        {
            // В реальном проекте используется DI, но для простоты создаем напрямую
            var repository = new JsonPurchaseOrderRepository("orders.json");
            _service = new PurchaseOrderService(repository);
        }

        /// <summary>
        /// Получить все заказы
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PurchaseOrder>>> GetOrders()
        {
            try
            {
                var orders = await _service.GetAllOrdersAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при получении заказов: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить заказ по ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PurchaseOrder>> GetOrder(Guid id)
        {
            try
            {
                var order = await _service.GetOrderByIdAsync(id);
                if (order == null)
                    return NotFound($"Заказ с ID {id} не найден");

                return Ok(order);
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
        /// Создать новый заказ
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PurchaseOrder>> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                var order = await _service.CreateOrderAsync(request.SupplierName, request.Items);
                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
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
        /// Обновить статус заказа
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<ActionResult<PurchaseOrder>> UpdateOrderStatus(Guid id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var order = await _service.UpdateOrderStatusAsync(id, request.Status);
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
        /// Удалить заказ
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteOrder(Guid id)
        {
            try
            {
                var result = await _service.DeleteOrderAsync(id);
                if (!result)
                    return NotFound($"Заказ с ID {id} не найден");

                return NoContent();
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
        /// Получить общую сумму всех заказов
        /// </summary>
        [HttpGet("total")]
        public async Task<ActionResult<decimal>> GetTotalAmount()
        {
            try
            {
                var total = await _service.GetTotalAmountAllOrdersAsync();
                return Ok(new { TotalAmount = total });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при подсчете суммы: {ex.Message}");
            }
        }
    }
}
