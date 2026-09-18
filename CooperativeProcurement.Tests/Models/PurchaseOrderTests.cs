using Xunit;
using FluentAssertions;
using CooperativeProcurement.Core.Models;

namespace CooperativeProcurement.Tests.Models;

/// <summary>
/// Тесты для <see cref="PurchaseOrder"/>.
/// </summary>
public class PurchaseOrderTests
{
    /// <summary>
    /// Тест: TotalAmount должен корректно суммировать стоимость всех позиций.
    /// </summary>
    [Fact]
    public void TotalAmount_MultipleItems_ReturnsCorrectSum()
    {
        // Arrange
        var order = new PurchaseOrder
        {
            Items = new List<OrderItem>
            {
                new() { Name = "Молоко", Quantity = 10, UnitPrice = 85.50m }, // 855.00
                new() { Name = "Хлеб", Quantity = 20, UnitPrice = 45.00m },   // 900.00
                new() { Name = "Сыр", Quantity = 5, UnitPrice = 350.00m }     // 1750.00
            }
        };

        // Act
        var total = order.TotalAmount;

        // Assert
        total.Should().Be(3505.00m);
    }

    /// <summary>
    /// Тест: TotalAmount для пустого заказа должен быть 0.
    /// </summary>
    [Fact]
    public void TotalAmount_EmptyItems_ReturnsZero()
    {
        // Arrange
        var order = new PurchaseOrder
        {
            Items = new List<OrderItem>()
        };

        // Act
        var total = order.TotalAmount;

        // Assert
        total.Should().Be(0m);
    }

    /// <summary>
    /// Тест: IsValid для корректного заказа должен вернуть true.
    /// </summary>
    [Fact]
    public void IsValid_ValidOrder_ReturnsTrue()
    {
        // Arrange
        var order = new PurchaseOrder
        {
            SupplierName = "ООО Ромашка",
            Items = new List<OrderItem>
            {
                new() { Name = "Молоко", Quantity = 10, UnitPrice = 85.50m }
            }
        };

        // Act
        var isValid = order.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    /// <summary>
    /// Тест: IsValid для заказа без поставщика должен вернуть false.
    /// </summary>
    [Fact]
    public void IsValid_EmptySupplierName_ReturnsFalse()
    {
        // Arrange
        var order = new PurchaseOrder
        {
            SupplierName = "",
            Items = new List<OrderItem>
            {
                new() { Name = "Молоко", Quantity = 10, UnitPrice = 85.50m }
            }
        };

        // Act
        var isValid = order.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    /// <summary>
    /// Тест: IsValid для заказа без товаров должен вернуть false.
    /// </summary>
    [Fact]
    public void IsValid_EmptyItems_ReturnsFalse()
    {
        // Arrange
        var order = new PurchaseOrder
        {
            SupplierName = "ООО Ромашка",
            Items = new List<OrderItem>()
        };

        // Act
        var isValid = order.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    /// <summary>
    /// Тест: IsValid для заказа с отрицательным количеством должен вернуть false.
    /// </summary>
    [Fact]
    public void IsValid_NegativeQuantity_ReturnsFalse()
    {
        // Arrange
        var order = new PurchaseOrder
        {
            SupplierName = "ООО Ромашка",
            Items = new List<OrderItem>
            {
                new() { Name = "Молоко", Quantity = -5, UnitPrice = 85.50m }
            }
        };

        // Act
        var isValid = order.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }
}
