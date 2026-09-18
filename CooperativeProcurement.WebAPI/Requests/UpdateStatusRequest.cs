using CooperativeProcurement.Core.Models;

namespace CooperativeProcurement.WebAPI.Requests;

/// <summary>
/// Запрос на обновление статуса.
/// </summary>
public class UpdateStatusRequest
{
    public OrderStatus Status { get; set; }
}
