using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Test.Common;
using ERP.Core.Warehouse.Api.Test.Common.Utils;

namespace ERP.Core.Warehouse.Api.Test.Controllers.Warehouse;

[TestFixture]
public class UpdateLotsControllerTest : IntegrationTestUtilsBase
{
    private static string LotsBaseUrl
        (Guid companyId, string moduleCode, Guid warehouseId, Guid sectionId)
            => $"api/v1/companies/{companyId}/modules/{moduleCode}/warehouses/{warehouseId}/sections/{sectionId}/lots";

    [Test]
    [TestCase("ALPAC")]
    public async Task UpdateLotWhenIsSuccess(string companyAlias)
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

        var existingLotIds = await _unitOfWork.Lots.Entities
            .Where(lot => lot.SectionId == section.Id)
            .Select(lot => lot.Id)
            .ToListAsync();

        var createPayload = new
        {
            quantity = 1,
            nominal_rows = 4,
            nominal_columns = 5,
            width = 10.00,
            length = 15.00
        };

        var createResponse = await SendRequestAsync(HttpMethod.Post, LotsBaseUrl
            (company.Id, moduleCode, warehouseId, section.Id), bearerToken, createPayload);

        Assert.That(createResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));

        var lotToUpdate = await _unitOfWork.Lots.Entities
            .Where(lot => lot.SectionId == section.Id)
            .Where(lot => !existingLotIds.Contains(lot.Id))
            .FirstAsync();

        var updatePayload = new
        {
            nominal_rows = 3,
            nominal_columns = 6,
            width_metres = 12.00,
            length_metres = 20.00
        };

        var updateResponse = await SendRequestAsync(HttpMethod.Patch, $"{LotsBaseUrl
            (company.Id, moduleCode, warehouseId, section.Id)}/{lotToUpdate.Id}", bearerToken, updatePayload);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));

        var updatedLot = await _unitOfWork.Lots.Entities
            .Include(lot => lot.LotsCapacity)
            .FirstAsync(lot => lot.Id == lotToUpdate.Id);

        Assert.That(updatedLot.NominalRows, Is.EqualTo(updatePayload.nominal_rows));
        Assert.That(updatedLot.NominalColumns, Is.EqualTo(updatePayload.nominal_columns));

        Assert.That(updatedLot.LotsCapacity, Is.Not.Null);
        Assert.That(updatedLot.LotsCapacity!.Width, Is.EqualTo(updatePayload.width_metres));
        Assert.That(updatedLot.LotsCapacity!.Length, Is.EqualTo(updatePayload.length_metres));
        Assert.That(updatedLot.LotsCapacity!.TotalAreaM2,
            Is.EqualTo(updatePayload.length_metres * updatePayload.width_metres));

        var positions = await _unitOfWork.LotsPositions.Entities
            .Where(position => position.LotId == lotToUpdate.Id)
            .ToListAsync();

        var expectedPositionCount = updatePayload.nominal_columns * updatePayload.nominal_rows;
        Assert.That(positions, Has.Count.EqualTo(expectedPositionCount));

        var expectedPositionCodes = new List<string>();
        for (int row = 1; row <= updatePayload.nominal_rows; row++)
        {
            for (int column = 1; column <= updatePayload.nominal_columns; column++)
            {
                expectedPositionCodes.Add($"{updatedLot.Code}-F{row}C{column}");
            }
        }

        var actualPositionCodes = positions.Select(p => p.PositionCode).ToList();
        Assert.That(actualPositionCodes, Is.EquivalentTo(expectedPositionCodes));

        var sectionCapacity = await _unitOfWork.SectionCapacities.Entities
            .FirstAsync(cap => cap.SectionId == section.Id);

        var expectedSectionArea = updatePayload.length_metres * updatePayload.width_metres;
        Assert.That(sectionCapacity.AvailableAreaWithMarginM2, Is.EqualTo(expectedSectionArea));
    }
}