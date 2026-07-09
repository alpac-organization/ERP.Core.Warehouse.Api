using MediatR;
using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Domain.Entities.Auth;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;

namespace ERP.Core.Manager.Api.Application.Commons.Bases
{
    public class AccessValidationResult<T>
    {
        public bool IsSuccess { get; set; }
        public Role? Role { get; set; }
        public T? ErrorResponse { get; set; }
    }

    public abstract class AlpacBaseHandler<TRequest, TResponse>(IUnitOfWork unitOfWork, IErrorManager errorManager) : IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        protected readonly IUnitOfWork _unitOfWork = unitOfWork;
        protected readonly IErrorManager _errorManager = errorManager;

        public abstract Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);

        protected async Task<AccessValidationResult<TResponse>> ValidateAccessAsync(Guid userId, Guid companyId, string moduleCode, CancellationToken ct)
        {
            // 1. Validar Usuario
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user is null)
            {
                return new AccessValidationResult<TResponse> { 
                    IsSuccess = false, 
                    ErrorResponse = _errorManager.ThrowBadRequest<TResponse>("Este usuario no existe!", "ERP:003") 
                };
            }

            // 2. Validar Perfil
            var profile = await _unitOfWork.Profiles.FirstOrDefaultAsync(p => p.UserId == userId && p.CompanyId == companyId, ct);

            if (profile is null)
            {
                return new AccessValidationResult<TResponse> { 
                    IsSuccess = false, 
                    ErrorResponse = _errorManager.ThrowBadRequest<TResponse>("No existe un perfil asociado a esta empresa", "ERP:004") 
                };
            }

            // 3. Validar Módulo
            var module = await _unitOfWork.UserModules.FirstOrDefaultAsync(m => m.ModuleCode == moduleCode && m.UserProfileId == profile.Id, ct);

            if (module is null)
            {
                return new AccessValidationResult<TResponse> { 
                    IsSuccess = false, 
                    ErrorResponse = _errorManager.ThrowBadRequest<TResponse>("No tienes acceso a este módulo", "ERP:005") 
                };
            }

            // 4. Obtener Rol
            var role = await _unitOfWork.Roles.FirstOrDefaultAsync(r => r.Id == module.RoleId, ct);
            
            if (role is null)
            {
                return new AccessValidationResult<TResponse> { 
                    IsSuccess = false, 
                    ErrorResponse = _errorManager.ThrowBadRequest<TResponse>("El rol asignado no es válido", "ERP:006") 
                };
            }

            return new AccessValidationResult<TResponse> { IsSuccess = true, Role = role };
        }
    }
}
