using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Test.Common;
using ERP.Core.Warehouse.Api.Test.Common.Utils;

namespace ERP.Core.Warehouse.Api.Test.Controllers.Warehouse;

[TestFixture]
public class DeleteLotsControllerTest : IntegrationTestUtilsBase
{
    private static string RegisterLotsBaseUrl
        (Guid companyId, string moduleCode, Guid warehouseId, Guid sectionId)
            => $"api/v1/companies/{companyId}/modules/{moduleCode}/warehouses/{warehouseId}/sections/{sectionId}/lots";

    [Test]
    [TestCase("ALPAC")]
    public async Task DeleteLotWhenIsSuccess(string companyAlias)
    {
        var company = await _unitOfWork.Companies.Entities
            .Where(company => company.IsActive && company.DeletedAt == null)
            .Where(company => company.Alias == companyAlias)
            .FirstAsync();

        var profile = await _unitOfWork.Profiles.Entities
            .Where(p => p.CompanyId == company.Id)
            .FirstAsync();

        var bearerToken = AuthManager.GenerateJwtToken(EnvironmentManager.JwtKey, profile.UserId);

        var moduleCode = "ALM-MAN-2KE4";

        await GrantModuleAccessAsync(profile.UserId, company.Id, RoleType.Administrator, moduleCode);

        var section = await GetOrCreateSectionToLotsAsync();

        var warehouseId = section.WarehouseId;

        var payload = new
        {
            quantity = 8,
            nominal_rows = 4,
            nominal_columns = 5,
            width = 10.00,
            length = 15.00
        };

        var registerResponse = await SendRequestAsync(HttpMethod.Post, RegisterLotsBaseUrl
            (company.Id, moduleCode, warehouseId, section.Id), bearerToken, payload);

        Assert.That(registerResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));

        var lotToDelete = await _unitOfWork.Lots.Entities
            .Where(lot => lot.SectionId == section.Id && lot.DeletedAt == null)
            .OrderBy(lot => lot.Code)
            .FirstAsync();

        var expectedPositionsPerLot = payload.nominal_columns * payload.nominal_rows;

        var deleteResponse = await SendRequestAsync(HttpMethod.Delete, $"{RegisterLotsBaseUrl
            (company.Id, moduleCode, warehouseId, section.Id)}/{lotToDelete.Id}", bearerToken);

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NoContent));

        var deletedLot = await _unitOfWork.Lots.Entities
            .Include(lot => lot.LotsCapacity)
            .FirstAsync(lot => lot.Id == lotToDelete.Id);

        Assert.That(deletedLot.DeletedAt, Is.Not.Null);
        Assert.That(deletedLot.LotsCapacity, Is.Not.Null);
        Assert.That(deletedLot.LotsCapacity!.DeletedAt, Is.Not.Null);

        var positions = await _unitOfWork.LotsPositions.Entities
            .Where(position => position.LotId == lotToDelete.Id)
            .ToListAsync();

        Assert.That(positions, Has.Count.EqualTo(expectedPositionsPerLot));
        Assert.That(positions.All(position => position.DeletedAt != null), Is.True);

        var remainingActiveLots = await _unitOfWork.Lots.Entities
            .CountAsync(lot => lot.SectionId == section.Id && lot.DeletedAt == null);

        Assert.That(remainingActiveLots, Is.EqualTo(payload.quantity - 1));

        var sectionCapacity = await _unitOfWork.SectionCapacities.Entities
            .FirstAsync(cap => cap.SectionId == section.Id);

        var expectedCapacityRestante = (payload.quantity - 1) * payload.width * payload.length;
        Assert.That(sectionCapacity.AvailableAreaWithMarginM2, Is.EqualTo(expectedCapacityRestante));
    }
}