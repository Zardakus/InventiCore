using FluentValidation;
using InventiCore.Application.Common.Mappings;
using InventiCore.Application.DTOs;
using InventiCore.Domain.Entities;
using InventiCore.Domain.Interfaces;
using MediatR;

namespace InventiCore.Application.Features.Warehouses.Commands.CreateWarehouse;

public record CreateWarehouseCommand(string Name, string Location, Guid TenantId) : IRequest<WarehouseDto>;

public class CreateWarehouseCommandValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Location)
            .MaximumLength(500).WithMessage("Localização deve ter no máximo 500 caracteres.");

        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId é obrigatório.");
    }
}

public class CreateWarehouseCommandHandler : IRequestHandler<CreateWarehouseCommand, WarehouseDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateWarehouseCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<WarehouseDto> Handle(CreateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var tenantExists = await _unitOfWork.Tenants.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenantExists is null)
            throw new KeyNotFoundException($"Tenant com Id '{request.TenantId}' não encontrado.");

        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Location = request.Location,
            TenantId = request.TenantId,
            IsActive = true
        };

        await _unitOfWork.Warehouses.AddAsync(warehouse, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return WarehouseMapper.ToDto(warehouse);
    }
}
