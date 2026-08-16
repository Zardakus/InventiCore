using FluentValidation;
using InventiCore.Application.DTOs;
using InventiCore.Application.Common.Mappings;
using InventiCore.Domain.Interfaces;
using MediatR;

namespace InventiCore.Application.Features.Products.Commands.UpdateProduct;

// ── Command ──────────────────────────────────────────────────
public record UpdateProductCommand : IRequest<ProductDto>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Category { get; init; }
    public decimal CostPrice { get; init; }
    public decimal SellingPrice { get; init; }
    public int MinimumStock { get; init; }
    public bool IsActive { get; init; }
    public Guid TenantId { get; init; }
}

// ── Validator ────────────────────────────────────────────────
public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id do produto é obrigatório.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome do produto é obrigatório.")
            .MaximumLength(300).WithMessage("Nome deve ter no máximo 300 caracteres.");

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU é obrigatório.")
            .MaximumLength(50).WithMessage("SKU deve ter no máximo 50 caracteres.");

        RuleFor(x => x.CostPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Preço de custo não pode ser negativo.");

        RuleFor(x => x.SellingPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Preço de venda não pode ser negativo.");

        RuleFor(x => x.MinimumStock)
            .GreaterThanOrEqualTo(0).WithMessage("Estoque mínimo não pode ser negativo.");

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId é obrigatório.");
    }
}

// ── Handler ──────────────────────────────────────────────────
public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Produto com Id '{request.Id}' não encontrado.");

        // Regra de negócio: se o SKU mudou, verificar unicidade por Tenant
        if (!string.Equals(product.Sku, request.Sku, StringComparison.OrdinalIgnoreCase))
        {
            var existingProduct = await _unitOfWork.Products
                .GetBySkuAsync(request.Sku, request.TenantId, cancellationToken);

            if (existingProduct is not null)
                throw new InvalidOperationException($"Já existe um produto com o SKU '{request.Sku}' para este Tenant.");
        }

        product.Name = request.Name;
        product.Sku = request.Sku;
        product.Description = request.Description;
        product.Category = request.Category;
        product.CostPrice = request.CostPrice;
        product.SellingPrice = request.SellingPrice;
        product.MinimumStock = request.MinimumStock;
        product.IsActive = request.IsActive;

        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ProductMapper.ToDto(product);
    }
}
