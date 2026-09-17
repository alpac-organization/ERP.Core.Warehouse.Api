using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using ERP.Core.Database.Domain.Enums;
using ERP.Core.Warehouse.Api.Test.Common;
using ERP.Core.Warehouse.Api.Test.Common.Utils;

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
        Guid userId = await CreateUser("Adonis José Luis Carlos Rodriguez Pérez Aguilar");

        var bearerToken = AuthManager.GenerateJwtToken(EnvironmentManager.JwtKey, userId);

        var company = await _unitOfWork.Companies.Entities
            .Where(company => company.IsActive)
            .Where(company => company.Alias == companyAlias)
            .FirstOrDefaultAsync(default);


        var warehouse = await _unitOfWork.Warehouses.Entities
            .Where(warehouse => warehouse.IsActive)
            .Where(warehouse => warehouse.WarehouseType != WarehouseType.Granel)
            .FirstOrDefaultAsync(default) ?? throw new InvalidOperationException
            ("No se encontró un warehouse activo que no sea tipo Granel.");

        var section = await _unitOfWork.Sections.Entities
            .Where(section => section.IsActive)
            .Where(section => section.WarehouseId == warehouse.Id)
            .Where(section => section.SectionType == SectionType.Storage)
            .Where(section => section.SectionStorageType == SectionStorageType.Lots)
            .FirstOrDefaultAsync(default) ?? throw new InvalidOperationException
            ($"No se encontró una sección de tipo Tramos en la Bodega {warehouse.Code}");

        var module = await _unitOfWork.Modules.Entities
            .Where(module => module.IsActive)
            .Where(module => module.Code == "ALM-MAN-2KE4")
            .FirstAsync();

        var moduleCode = module.Code;

        var payload = new
        {
            quantity = 8,
            nominalRows = 4,
            nominalColumns = 5,
            width = 10.00,
            length = 15.00
        };

        //capturar respuesta de la peticion
        var response = await SendRequestAsync(HttpMethod.Post, RegisterLotsBaseUrl
            (company!.Id, moduleCode!, warehouse.Id, section.Id), bearerToken, payload);

        //confirmar que el resultado sea el correcto
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Created));

        //verificar que las cantidades de lotes creadas sean correctas
        var registeredLots = await _unitOfWork.Lots.Entities
            .Where(lot => lot.SectionId == section.Id)
            .OrderBy(lot => lot.Code)
            .ToListAsync();

        Assert.That(registeredLots.Count, Is.EqualTo(payload.quantity));

        var lotIds = registeredLots.Select(l => l.Id).ToList();

        var capacities = await _unitOfWork.LotsCapacities.Entities
            .Where(cap => lotIds.Contains(cap.LotsId))
            .ToListAsync();

        Assert.That(capacities.Count, Is.EqualTo(payload.quantity));

        var positionPerLot = payload.nominalColumns * payload.nominalRows;
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
            Assert.That(lot.NominalColumns, Is.EqualTo(payload.nominalColumns));
            Assert.That(lot.NominalRows, Is.EqualTo(payload.nominalRows));
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