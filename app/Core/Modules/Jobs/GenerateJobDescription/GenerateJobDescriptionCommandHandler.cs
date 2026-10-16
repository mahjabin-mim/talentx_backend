using Domain.Entities;
using Domain.Repositories;
using Domain.Specifications;
using Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Dtos.Jobs;
using Shared.Exceptions;

namespace Modules.Jobs.GenerateJobDescription;

public sealed class GenerateJobDescriptionCommandHandler(
    IRepository<User> userRepository,
    IJobDescriptionGenerationService jobDescriptionGenerationService,
    ILogger<GenerateJobDescriptionCommandHandler> logger)
    : IRequestHandler<GenerateJobDescriptionCommand, GenerateJobDescriptionResponseDto>
{
    private readonly IRepository<User> _userRepository = userRepository;
    private readonly IJobDescriptionGenerationService _jobDescriptionGenerationService = jobDescriptionGenerationService;
    private readonly ILogger<GenerateJobDescriptionCommandHandler> _logger = logger;

    public async Task<GenerateJobDescriptionResponseDto> Handle(
        GenerateJobDescriptionCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.FirstOrDefaultAsync(new ActiveEmployerUserByIdSpec(request.UserId), cancellationToken);

        if (user is null)
        {
            _logger.LogInformation("GenerateJobDescription denied: Employer account unavailable. UserId: {UserId}", request.UserId);
            throw new UnauthorizedException("Employer account is unavailable.");
        }

        var description = await _jobDescriptionGenerationService.GenerateAsync(
            new GenerateJobDescriptionRequestDto
            {
                JobId = request.JobId,
                Title = request.Title,
                Description = request.Description,
                Responsibilities = request.Responsibilities,
                Skills = request.Skills,
                EmploymentType = request.EmploymentType,
                WorkArrangement = request.WorkArrangement,
                ExperienceLevel = request.ExperienceLevel,
                ExperienceMinYears = request.ExperienceMinYears,
                ExperienceMaxYears = request.ExperienceMaxYears,
                SalaryMin = request.SalaryMin,
                SalaryMax = request.SalaryMax,
                CountryCode = request.CountryCode,
                City = request.City,
                Timezone = request.Timezone,
                CurrencyCode = request.CurrencyCode,
                NumberOfPositions = request.NumberOfPositions,
                ApplicationDeadline = request.ApplicationDeadline
            },
            cancellationToken);

        return new GenerateJobDescriptionResponseDto
        {
            Description = description,
            Message = "Job description generated successfully."
        };
    }
}
