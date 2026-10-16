using Microsoft.AspNetCore.Http;
using MediatR;
using Shared.Dtos.Applications;

namespace Modules.Applications.Create;

public sealed record CreateApplicationCommand(
    Guid JobId,
    string Name,
    string Email,
    string ContactNumber,
    string Education,
    string Department,
    string CGPA,
    string LinkedInUrl,
    string AdditionalUrl,
    string Location,
    string CoverLetter,
    decimal ExpectedSalary,
    int Experience,
    IFormFile Resume) : IRequest<CreateApplicationDto>;
