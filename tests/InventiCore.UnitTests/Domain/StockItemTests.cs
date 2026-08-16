using FluentAssertions;
using InventiCore.Domain.Entities;
using InventiCore.Domain.Exceptions;
using Xunit;

namespace InventiCore.UnitTests.Domain;

public class StockItemTests
{
    [Fact]
    public void AddQuantity_ShouldIncreaseQuantity_WhenAmountIsPositive()
    {
        // Arrange
        var stockItem = new StockItem { Quantity = 10 };

        // Act
        stockItem.AddQuantity(5);

        // Assert
        stockItem.Quantity.Should().Be(15);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void AddQuantity_ShouldThrowArgumentException_WhenAmountIsInvalid(int amount)
    {
        // Arrange
        var stockItem = new StockItem { Quantity = 10 };

        // Act
        Action act = () => stockItem.AddQuantity(amount);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("A quantidade a ser adicionada deve ser maior que zero. (Parameter 'amount')");
    }

    [Fact]
    public void RemoveQuantity_ShouldDecreaseQuantity_WhenSufficientStock()
    {
        // Arrange
        var stockItem = new StockItem { Quantity = 20 };

        // Act
        stockItem.RemoveQuantity(5);

        // Assert
        stockItem.Quantity.Should().Be(15);
    }

    [Fact]
    public void RemoveQuantity_ShouldThrowInsufficientStockException_WhenInsufficientStock()
    {
        // Arrange
        var product = new Product { Name = "Notebook" };
        var stockItem = new StockItem { Quantity = 5, Product = product };

        // Act
        Action act = () => stockItem.RemoveQuantity(10);

        // Assert
        var exception = act.Should().Throw<InsufficientStockException>().And;
        exception.ProductName.Should().Be("Notebook");
        exception.Requested.Should().Be(10);
        exception.Available.Should().Be(5);
    }
}
