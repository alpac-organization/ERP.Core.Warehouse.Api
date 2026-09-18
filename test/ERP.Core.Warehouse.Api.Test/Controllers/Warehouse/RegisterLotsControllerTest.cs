using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Test.Common;
using ERP.Core.Warehouse.Api.Test.Common.Utils;
using ERP.Core.Database.Domain.Entities.Auth;

namespace ERP.Core.Warehouse.Api.Test.Controllers.Warehouse;

[TestFixture]
public class RegisterLotsControllerTest : IntegrationTestUtilsBase
{
    private static string RegisterLotsBaseUrl
        (Guid companyId, string moduleCode, Guid warehouseId, Guid sectionId)
            => $"api/v1/companies/{companyId}/modules/{moduleCode}/warehouses/{warehouseId}/sections/{sectionId}/lots";

    [Test]
    [TestCase("ALPAC")]
    public async Task RegisterLotsWhenIsSuccess(string companyAlias)
    {
        var company = await _unitOfWork.Companies.Entities
            .Where(company => company.IsActive && company.DeletedAt == null)
            .Where(company => company.Alias == companyAlias)
            .FirstAsync();

        var profile = await _unitOfWork.Profiles.Entities
            .Where(p => p.CompanyId == company.Id)
            .FirstAsync();
        
        var bearerToken = AuthManager.GenerateJwtToken(EnvironmentManager.JwtKey, profile.UserId);
        
        var role = await _unitOfWork.Roles.Entities
            .FirstAsync(r => r.RoleType == RoleType.Administrator);
        
        var lotsModule = await _unitOfWork.Modules.Entities
            .FirstAsync(m => m.Code == "ALM-MAN-2KE4");

        await _unitOfWork.UserModules.AssignRolesModule(new UserModuleRoles
        {
           Id = Guid.NewGuid(),
           RoleId = role.Id,
           UserProfileId = profile.Id,
           ModuleId = lotsModule.Id,
           ModuleCode = lotsModule.Code,
           IsActive = true 
        });

        await _unitOfWork.SaveChangesAsync(default);

        var section = await _unitOfWork.Sections.Entities
            .Where(section => section.IsActive && section.DeletedAt == null)
            .Where(section => section.SectionStorageType == SectionStorageType.Lots)
            .FirstAsync();

        var warehouse = await _unitOfWork.Warehouses.Entities
            .FirstAsync(w => w.Id == section.WarehouseId);

        var moduleCode = lotsModule.Code;

        var existingLotIds = await _unitOfWork.Lots.Entities
            .Where(lot => lot.SectionId == section.Id)
            .Select(lot => lot.Id)
            .ToListAsync();

        var payload = new
        {
            quantity = 8,
            nominal_rows = 4,
            nominal_columns = 5,
            width = 10.00,
            length = 15.00
        };

        //capturar respuesta de la peticion
        var response = await SendRequestAsync(HttpMethod.Post, RegisterLotsBaseUrl
            (company.Id, moduleCode!, warehouse.Id, section.Id), bearerToken, payload);

        //confirmar que el resultado sea el correcto
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));

        //verificar que las cantidades de lotes creadas sean correctas
        var registeredLots = await _unitOfWork.Lots.Entities
            .Where(lot => lot.SectionId == section.Id)
            .Where(lot => !existingLotIds.Contains(lot.Id))
            .OrderBy(lot => lot.Code)
            .ToListAsync();

        Assert.That(registeredLots.Count, Is.EqualTo(payload.quantity));

        var lotIds = registeredLots.Select(l => l.Id).ToList();

        var capacities = await _unitOfWork.LotsCapacities.Entities
            .Where(cap => lotIds.Contains(cap.LotsId))
            .ToListAsync();

        Assert.That(capacities.Count, Is.EqualTo(payload.quantity));

        var positionPerLot = payload.nominal_columns * payload.nominal_rows;
        var expectedTotalPositions = payload.quantity * positionPerLot;

        var positions = await _unitOfWork.LotsPositions.Entities
            .Where(pos => lotIds.Contains(pos.LotId))
            .ToListAsync();

        Assert.That(positions.Count, Is.EqualTo(expectedTotalPositions));

        var positionsByLot = positions
            .GroupBy(p => p.LotId)
            .ToDictionary(g => g.Key, g => g.Count());

        //validar que los campos en cada lot sean correctos
        foreach (var lot in registeredLots)
        {
            Assert.That(lot.NominalColumns, Is.EqualTo(payload.nominal_columns));
            Assert.That(lot.NominalRows, Is.EqualTo(payload.nominal_rows));
            Assert.That(lot.Status, Is.EqualTo(RackStatus.Available));

            Assert.That(positionsByLot.ContainsKey(lot.Id), Is.True,
                $"El lote {lot.Code} no tiene posiciones registradas.");

            Assert.That(positionsByLot[lot.Id], Is.EqualTo(positionPerLot),
                $"El lote {lot.Code} no tiene la cantidad esperada de posiciones.");
        }

        foreach (var capacity in capacities)
        {
            Assert.That(capacity.Width, Is.EqualTo(payload.width));
            Assert.That(capacity.Length, Is.EqualTo(payload.length));
            Assert.That(capacity.TotalAreaM2, Is.EqualTo(payload.length * payload.width));
        }
    }
}