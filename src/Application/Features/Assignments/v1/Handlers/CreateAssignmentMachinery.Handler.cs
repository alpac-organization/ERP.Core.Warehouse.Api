using ERP.Core.Application.Commons.Interfaces;
using ERP.Core.Database.Application.Commons.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace ERP.Core.Warehouse.Api.Application.Features.Assignments.v1.Handlers;

public class CreateAssignmentMachineryHandler(IUnitOfWork _unitOfWork, IErrorManager _errorManager,
    ILogger<CreateAssignmentMachineryHandler> _logger)