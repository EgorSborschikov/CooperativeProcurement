using Moq;
using Xunit;
using FluentAssertions;
using CooperativeProcurement.Core.Interfaces;
using CooperativeProcurement.Core.Models;
using CooperativeProcurement.Core.Services;
using System.ComponentModel.DataAnnotations;

namespace CooperativeProcurement.Tests.Services
{
    /// <summary>
    /// Тесты для <see cref="PurchaseOrderService"/>.
    /// </summary>
    public class PurchaseOrderServiceTests
    {
        private readonly Mock<IPurchaseOrderRepository> _repositoryMock;
        private readonly PurchaseOrderService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="PurchaseOrderServiceTests"/> class.
        /// </summary>
        public PurchaseOrderServiceTests()
        {
            this._repositoryMock = new Mock<IPurchaseOrderRepository>();
            this._service = new PurchaseOrderService(this._repositoryMock.Object);
        }

        /// <summary>
        /// Тест: создание заказа с валидными данными должно вернуть созданный заказ.
        /// </summary>
        [Fact]
        public async Task CreateOrderAsync_ValidData_ReturnsCreatedOrder()
        {
            // Arrange
            var supplierName = "ООО Ромашка";
            var items = new List<OrderItem>
            {
                new() { Name = "Молоко", Quantity = 10, UnitPrice = 85.50m },
                new() { Name = "Хлеб", Quantity = 20, UnitPrice = 45.00m }
            };

            var expectedOrder = new PurchaseOrder
            {
                SupplierName = supplierName,
                Items = items,
                Status = OrderStatus.Draft,
            };

            this._repositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<PurchaseOrder>()))
                .ReturnsAsync(expectedOrder);

            // Act
            var result = await this._service.CreateOrderAsync(supplierName, items);

            // Assert
            result.Should().NotBeNull();
            result.SupplierName.Should().Be(supplierName);
            result.Items.Should().HaveCount(2);
            result.Status.Should().Be(OrderStatus.Draft);

            this._repositoryMock.Verify(
                r => r.CreateAsync(It.IsAny<PurchaseOrder>()),
                Times.Once);
        }

        /// <summary>
        /// Тест: создание заказа с пустым именем поставщика должно выбросить ArgumentException.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task CreateOrderAsync_EmptySupplierName_ThrowsArgumentException(string? supplierName)
        {
            // Arrange
            var items = new List<OrderItem>
        {
            new() { Name = "Молоко", Quantity = 10, UnitPrice = 85.50m }
        };

            // Act
            Func<Task> act = async () => await this._service.CreateOrderAsync(supplierName!, items);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*поставщика*");

            this._repositoryMock.Verify(
                r => r.CreateAsync(It.IsAny<PurchaseOrder>()),
                Times.Never);
        }

        /// <summary>
        /// Тест: создание заказа с пустым списком товаров должно выбросить ArgumentException.
        /// </summary>
        [Fact]
        public async Task CreateOrderAsync_EmptyItems_ThrowsArgumentException()
        {
            // Arrange
            var supplierName = "ООО Ромашка";
            var items = new List<OrderItem>();

            // Act
            Func<Task> act = async () => await this._service.CreateOrderAsync(supplierName, items);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*хотя бы одну позицию*");
        }

        /// <summary>
        /// Тест: создание заказа с отрицательным количеством должно выбросить ArgumentException.
        /// </summary>
        [Fact]
        public async Task CreateOrderAsync_NegativeQuantity_ThrowsArgumentException()
        {
            // Arrange
            var supplierName = "ООО Ромашка";
            var items = new List<OrderItem>
        {
            new() { Name = "Молоко", Quantity = -5, UnitPrice = 85.50m }
        };

            // Act
            Func<Task> act = async () => await this._service.CreateOrderAsync(supplierName, items);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*положительным*");
        }

        /// <summary>
        /// Тест: создание заказа с отрицательной ценой должно выбросить ArgumentException.
        /// </summary>
        [Fact]
        public async Task CreateOrderAsync_NegativePrice_ThrowsArgumentException()
        {
            // Arrange
            var supplierName = "ООО Ромашка";
            var items = new List<OrderItem>
        {
            new() { Name = "Молоко", Quantity = 10, UnitPrice = -85.50m }
        };

            // Act
            Func<Task> act = async () => await this._service.CreateOrderAsync(supplierName, items);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*отрицательной*");
        }

        /// <summary>
        /// Тест: получение заказа по несуществующему ID должно вернуть null.
        /// </summary>
        [Fact]
        public async Task GetOrderByIdAsync_NonExistentId_ReturnsNull()
        {
            // Arrange
            var id = Guid.NewGuid();
            this._repositoryMock
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync((PurchaseOrder?)null);

            // Act
            var result = await this._service.GetOrderByIdAsync(id);

            // Assert
            result.Should().BeNull();
        }

        /// <summary>
        /// Тест: получение заказа с пустым ID должно выбросить ArgumentException.
        /// </summary>
        [Fact]
        public async Task GetOrderByIdAsync_EmptyId_ThrowsArgumentException()
        {
            // Arrange
            var id = Guid.Empty;

            // Act
            Func<Task> act = async () => await this._service.GetOrderByIdAsync(id);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        /// <summary>
        /// Тест: удаление заказа с пустым ID должно выбросить ArgumentException.
        /// </summary>
        [Fact]
        public async Task DeleteOrderAsync_EmptyId_ThrowsArgumentException()
        {
            // Arrange
            var id = Guid.Empty;

            // Act
            Func<Task> act = async () => await this._service.DeleteOrderAsync(id);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        /// <summary>
        /// Тест: общая сумма всех заказов должна быть корректно рассчитана.
        /// </summary>
        [Fact]
        public async Task GetTotalAmountAllOrdersAsync_MultipleOrders_ReturnsCorrectSum()
        {
            // Arrange
            var orders = new List<PurchaseOrder>
        {
            new()
            {
                SupplierName = "ООО Ромашка",
                Items = new List<OrderItem>
                {
                    new() { Name = "Молоко", Quantity = 10, UnitPrice = 85.50m } // 855
                }
            },
            new()
            {
                SupplierName = "ООО Василёк",
                Items = new List<OrderItem>
                {
                    new() { Name = "Хлеб", Quantity = 20, UnitPrice = 45.00m } // 900
                }
            }
        };

            this._repositoryMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(orders);

            // Act
            var total = await this._service.GetTotalAmountAllOrdersAsync();

            // Assert
            total.Should().Be(1755.00m);
        }

        /// <summary>
        /// Тест: обновление статуса несуществующего заказа должно выбросить InvalidOperationException.
        /// </summary>
        [Fact]
        public async Task UpdateOrderStatusAsync_NonExistentOrder_ThrowsInvalidOperationException()
        {
            // Arrange
            var id = Guid.NewGuid();
            this._repositoryMock
                .Setup(r => r.GetByIdAsync(id))
                .ReturnsAsync((PurchaseOrder?)null);

            // Act
            Func<Task> act = async () => await this._service.UpdateOrderStatusAsync(id, OrderStatus.Submitted);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"*{id}*");
        }
    }
}
