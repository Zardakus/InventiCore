using FluentAssertions;
using InventiCore.Application.Common.Interfaces;
using InventiCore.Application.Features.Stock.Commands.TransferStock;
using InventiCore.Domain.Entities;
using InventiCore.Domain.Interfaces;
using Moq;
using Xunit;

namespace InventiCore.UnitTests.Application;

public class TransferStockCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMessagePublisher> _publisherMock;
    private readonly TransferStockCommandHandler _handler;

    public TransferStockCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>() { DefaultValue = DefaultValue.Mock };
        _publisherMock = new Mock<IMessagePublisher>();
        _handler = new TransferStockCommandHandler(_unitOfWorkMock.Object, _publisherMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldTransferStock_WhenAllConditionsAreMet()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var sourceWarehouseId = Guid.NewGuid();
        var destWarehouseId = Guid.NewGuid();

        var product = new Product { Id = productId, Name = "Product", TenantId = tenantId };
        var sourceWarehouse = new Warehouse { Id = sourceWarehouseId, TenantId = tenantId };
        var destWarehouse = new Warehouse { Id = destWarehouseId, TenantId = tenantId };
        
        var sourceStock = new StockItem { Id = Guid.NewGuid(), ProductId = productId, WarehouseId = sourceWarehouseId, Quantity = 20 };
        var destStock = new StockItem { Id = Guid.NewGuid(), ProductId = productId, WarehouseId = destWarehouseId, Quantity = 5 };

        _unitOfWorkMock.Setup(u => u.Products.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _unitOfWorkMock.Setup(u => u.Warehouses.GetByIdAsync(sourceWarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceWarehouse);
        _unitOfWorkMock.Setup(u => u.Warehouses.GetByIdAsync(destWarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destWarehouse);
        
        _unitOfWorkMock.Setup(u => u.StockItems.GetByProductAndWarehouseAsync(productId, sourceWarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceStock);
        _unitOfWorkMock.Setup(u => u.StockItems.GetByProductAndWarehouseAsync(productId, destWarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destStock);

        var command = new TransferStockCommand(productId, sourceWarehouseId, destWarehouseId, 10, "Transfer", "User");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        sourceStock.Quantity.Should().Be(10);
        destStock.Quantity.Should().Be(15);
        _unitOfWorkMock.Verify(u => u.StockItems.Update(sourceStock), Times.Once);
        _unitOfWorkMock.Verify(u => u.StockItems.Update(destStock), Times.Once);
        _unitOfWorkMock.Verify(u => u.StockMovements.AddAsync(It.IsAny<StockMovement>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenTenantsAreDifferent()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var sourceWarehouseId = Guid.NewGuid();
        var destWarehouseId = Guid.NewGuid();

        var product = new Product { Id = productId, Name = "Product", TenantId = Guid.NewGuid() };
        var sourceWarehouse = new Warehouse { Id = sourceWarehouseId, TenantId = Guid.NewGuid() }; // Tenant A
        var destWarehouse = new Warehouse { Id = destWarehouseId, TenantId = Guid.NewGuid() }; // Tenant B

        _unitOfWorkMock.Setup(u => u.Products.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _unitOfWorkMock.Setup(u => u.Warehouses.GetByIdAsync(sourceWarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceWarehouse);
        _unitOfWorkMock.Setup(u => u.Warehouses.GetByIdAsync(destWarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destWarehouse);

        var command = new TransferStockCommand(productId, sourceWarehouseId, destWarehouseId, 10, "Transfer", "User");

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Não é permitido transferir estoque entre Tenants diferentes.");
        
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
