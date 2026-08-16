using FluentValidation;
using InventiCore.Application.Common.Mappings;
using InventiCore.Application.DTOs;
using InventiCore.Domain.Interfaces;
using MediatR;

namespace InventiCore.Application.Features.Warehouses.Commands.UpdateWarehouse;

public record UpdateWarehouseCommand(Guid Id, string Name, string Location, bool IsActive, Guid TenantId) : IRequest<WarehouseDto>;

public class UpdateWarehouseCommandValidator : AbstractValidator<UpdateWarehouseCommand>
{
    public UpdateWarehouseCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id é obrigatório.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Location)
            .MaximumLength(500).WithMessage("Localização deve ter no máximo 500 caracteres.");

        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId é obrigatório.");
    }
}

public class UpdateWarehouseCommandHandler : IRequestHandler<UpdateWarehouseCommand, WarehouseDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateWarehouseCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<WarehouseDto> Handle(UpdateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Warehouse com Id '{request.Id}' não encontrado.");

        if (warehouse.TenantId != request.TenantId)
            throw new InvalidOperationException("Não é permitido alterar o TenantId de um Warehouse existente.");

        warehouse.Name = request.Name;
        warehouse.Location = request.Location;
        warehouse.IsActive = request.IsActive;

        _unitOfWork.Warehouses.Update(warehouse);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return WarehouseMapper.ToDto(warehouse);
    }
}
