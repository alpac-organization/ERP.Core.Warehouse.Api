using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Application.Commons.Interfaces.Bases;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using ERP.Core.Database.Domain.Entities.Warehouse;
using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Application.Features.WarehouseAssignments.v1.Handlers;

public record WarehouseDocumentContext(
    Guid DocumentId,
    DocumentType DocumentType,
    EntranceDucats? Ducat,
    CustomsDeclarations? CustomsDeclaration,
    RecordEntrance RecordEntrance);

public static class WarehouseDocumentLookup
{
    public static async Task<WarehouseDocumentContext?> FindDocumentAsync(
        IUnitOfWork unitOfWork,
        Guid documentId,
        DocumentType documentType,
        CancellationToken cancellationToken)
    {
        if (documentType == DocumentType.DUCA)
        {
            var ducat = await unitOfWork.EntranceDucats.Entities
                .Include(d => d.RecordEntrance!)
                    .ThenInclude(r => r.ReceptionEntrance)
                .Include(d => d.RecordEntrance!)
                    .ThenInclude(r => r.DucatRegistry)
                .Include(d => d.RecordEntrance!)
                    .ThenInclude(r => r.ExecutionLogs)
                .Include(d => d.RegistryDetail)
                .FirstOrDefaultAsync(d => d.Id == documentId && d.DeletedAt == null, cancellationToken);

            if (ducat?.RecordEntrance == null) return null;
            return new WarehouseDocumentContext(documentId, documentType, ducat, null, ducat.RecordEntrance);
        }

        if (documentType == DocumentType.CustomsDeclaration)
        {
            var declaration = await unitOfWork.CustomsDeclarations.Entities
                .Include(c => c.RecordEntrance!)
                    .ThenInclude(r => r.ReceptionEntrance)
                .Include(c => c.RecordEntrance!)
                    .ThenInclude(r => r.DucatRegistry)
                .Include(c => c.RecordEntrance!)
                    .ThenInclude(r => r.ExecutionLogs)
                .Include(c => c.Details)
                .FirstOrDefaultAsync(c => c.Id == documentId && c.DeletedAt == null, cancellationToken);

            if (declaration?.RecordEntrance == null) return null;
            return new WarehouseDocumentContext(documentId, documentType, null, declaration, declaration.RecordEntrance);
        }

        return null;
    }

    public static string GetDocumentNumber(WarehouseDocumentContext context)
    {
        return context.DocumentType == DocumentType.DUCA
            ? context.Ducat!.DucatNumber
            : context.CustomsDeclaration!.CustomsDeclarationNumber;
    }

    public static string? GetServiceOrderCode(WarehouseDocumentContext context)
    {
        return context.DocumentType == DocumentType.DUCA
            ? context.Ducat!.ServiceOrderCode
            : context.CustomsDeclaration!.ServiceOrderCode;
    }

    public static string? GetMerchandiseName(WarehouseDocumentContext context)
    {
        return context.DocumentType == DocumentType.DUCA
            ? context.Ducat!.RegistryDetail?.MerchandiseName
            : context.CustomsDeclaration!.Details?.Product;
    }

    public static int? GetTotalBultos(WarehouseDocumentContext context)
    {
        return context.DocumentType == DocumentType.DUCA
            ? context.Ducat!.RegistryDetail?.TotalBultos
            : context.CustomsDeclaration!.Details?.Packages;
    }

    public static decimal? GetTotalWeight(WarehouseDocumentContext context)
    {
        return context.DocumentType == DocumentType.DUCA
            ? context.Ducat!.RegistryDetail?.TotalWeight
            : null;
    }

    public static string? GetRemitente(WarehouseDocumentContext context)
    {
        return context.DocumentType == DocumentType.DUCA
            ? context.Ducat!.RegistryDetail?.Remitente
            : context.CustomsDeclaration!.Details?.Customer;
    }

    public static string? GetContainerNumber(WarehouseDocumentContext context)
    {
        return context.DocumentType == DocumentType.DUCA
            ? context.RecordEntrance.DucatRegistry?.ContainerNumber
            : context.CustomsDeclaration!.Details?.ContainerNumber;
    }
}
