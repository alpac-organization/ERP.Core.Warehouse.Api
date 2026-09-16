using Microsoft.EntityFrameworkCore;

using ERP.Core.Database.Domain.Enums;

namespace ERP.Core.Warehouse.Api.Test.Common
{

    /// <summary>
    /// Todos los metodos reutilizables, puedes agregarlos aqui
    /// </summary>
    public class IntegrationTestUtilsBase : IntegrationTestBase
    {
        public async Task<Guid> CreateUser(string fullname, Guid? areaId = null)
        {
            Guid workAreaId;

            if (areaId.HasValue) workAreaId = areaId.Value;
            else
            {
                var area = await _unitOfWork.WorkAreas.Entities
                    .Where(workArea => workArea.IsActive)
                    .Where(workArea => workArea.WorkAreaCode == 10)
                    .FirstOrDefaultAsync(default);
                    
                workAreaId = area!.Id;
            }

            var newUserId = Guid.NewGuid();

            await _unitOfWork.Users.CreateNewUser(new()
            {
                Id = newUserId,
                UserStatus = UserStatus.Active,
                UserType = UserType.StandardUser,
                UserName = "user.name",
                PasswordHash = "$hashpassoword",
                Fullname = fullname,
                Email = "testing@domain.com",
                AreaId = workAreaId,
                IdentificationNumber = "0010101011052A",
            });

            var branch = await _unitOfWork.Branches.Entities
                .Where(branch => branch.IsActive)
                .Where(branch => branch.BranchCode == "ALPAC-01")
                .FirstOrDefaultAsync(default);

            await _unitOfWork.Profiles.CreateNewUserProfile(new()
            {
                Id = Guid.NewGuid(),
                UserId = newUserId,
                BranchId = branch!.Id,
                IsActive = true,
                CompanyId = branch.CompanyId
            });

            await _unitOfWork.SaveChangesAsync(default);
            
            return newUserId;
        }
    }
}