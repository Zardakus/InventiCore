using FluentValidation;
using InventiCore.Application.Common.Mappings;
using InventiCore.Application.DTOs;
using InventiCore.Domain.Interfaces;
using MediatR;

namespace InventiCore.Application.Features.Tenants.Commands.UpdateTenant;

public record UpdateTenantCommand(Guid Id, string Name, string Document, bool IsActive) : IRequest<TenantDto>;

public class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id é obrigatório.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Document)
            .NotEmpty().WithMessage("Documento (CNPJ) é obrigatório.")
            .MaximumLength(20).WithMessage("Documento deve ter no máximo 20 caracteres.");
    }
}

public class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, TenantDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTenantCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TenantDto> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Tenant com Id '{request.Id}' não encontrado.");

        if (tenant.Document != request.Document)
        {
            var existing = await _unitOfWork.Tenants.GetByDocumentAsync(request.Document, cancellationToken);
            if (existing is not null)
                throw new InvalidOperationException($"Já existe um tenant com o documento '{request.Document}'.");
        }

        tenant.Name = request.Name;
        tenant.Document = request.Document;
        tenant.IsActive = request.IsActive;

        _unitOfWork.Tenants.Update(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TenantMapper.ToDto(tenant);
    }
}
