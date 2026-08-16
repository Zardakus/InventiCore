using FluentValidation;
using InventiCore.Application.DTOs;
using InventiCore.Application.Common.Mappings;
using InventiCore.Domain.Entities;
using InventiCore.Domain.Interfaces;
using MediatR;

namespace InventiCore.Application.Features.Products.Commands.CreateProduct;

// ── Command ──────────────────────────────────────────────────
public record CreateProductCommand : IRequest<ProductDto>
{
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Category { get; init; }
    public decimal CostPrice { get; init; }
    public decimal SellingPrice { get; init; }
    public int MinimumStock { get; init; }
}

// ── Validator ────────────────────────────────────────────────
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
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
    }
}

// ── Handler ──────────────────────────────────────────────────
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly InventiCore.Application.Common.Interfaces.ICurrentUserService _currentUserService;

    public CreateProductCommandHandler(IUnitOfWork unitOfWork, InventiCore.Application.Common.Interfaces.ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId 
            ?? throw new UnauthorizedAccessException("Tenant não identificado no token.");

        // Regra de negócio: SKU deve ser único por Tenant
        var existingProduct = await _unitOfWork.Products
            .GetBySkuAsync(request.Sku, tenantId, cancellationToken);

        if (existingProduct is not null)
            throw new InvalidOperationException($"Já existe um produto com o SKU '{request.Sku}' para este Tenant.");

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Sku = request.Sku,
            Description = request.Description,
            Category = request.Category,
            CostPrice = request.CostPrice,
            SellingPrice = request.SellingPrice,
            MinimumStock = request.MinimumStock,
            TenantId = tenantId,
            IsActive = true
        };

        await _unitOfWork.Products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ProductMapper.ToDto(product);
    }
}
