using Xunit;
using FluentAssertions;
using CooperativeProcurement.Core.Models;

namespace CooperativeProcurement.Tests.Models;

/// <summary>
/// Тесты для <see cref="OrderItem"/>.
/// </summary>
public class OrderItemTests
{
    /// <summary>
    /// Тест: TotalPrice должен корректно рассчитываться.
    /// </summary>
    [Theory]
    [InlineData(10, 85.50, 855.00)]
    [InlineData(1, 100.00, 100.00)]
    [InlineData(0, 50.00, 0.00)]
    [InlineData(100, 0.01, 1.00)]
    public void TotalPrice_ValidData_ReturnsCorrectValue(int quantity, decimal unitPrice, decimal expected)
    {
        // Arrange
        var item = new OrderItem
        {
            Name = "Тестовый товар",
            Quantity = quantity,
            UnitPrice = unitPrice
        };

        // Act
        var total = item.TotalPrice;

        // Assert
        total.Should().Be(expected);
    }

    /// <summary>
    /// Тест: Id должен генерироваться автоматически.
    /// </summary>
    [Fact]
    public void Id_NewOrderItem_IsGenerated()
    {
        // Arrange & Act
        var item = new OrderItem();

        // Assert
        item.Id.Should().NotBe(Guid.Empty);
    }
}
