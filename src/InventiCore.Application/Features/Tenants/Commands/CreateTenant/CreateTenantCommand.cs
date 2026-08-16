using FluentValidation;
using InventiCore.Application.Common.Mappings;
using InventiCore.Application.DTOs;
using InventiCore.Domain.Entities;
using InventiCore.Domain.Interfaces;
using MediatR;

namespace InventiCore.Application.Features.Tenants.Commands.CreateTenant;

public record CreateTenantCommand(string Name, string Document) : IRequest<TenantDto>;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Document)
            .NotEmpty().WithMessage("Documento (CNPJ) é obrigatório.")
            .MaximumLength(20).WithMessage("Documento deve ter no máximo 20 caracteres.");
    }
}

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, TenantDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateTenantCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TenantDto> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.Tenants.GetByDocumentAsync(request.Document, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"Já existe um tenant com o documento '{request.Document}'.");

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Document = request.Document,
            IsActive = true
        };

        await _unitOfWork.Tenants.AddAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TenantMapper.ToDto(tenant);
    }
}
