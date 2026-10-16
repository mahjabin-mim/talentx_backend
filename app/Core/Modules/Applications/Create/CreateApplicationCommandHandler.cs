using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.AiScoring;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Dtos.Applications;
using System.Security.Cryptography;
using FluentValidation;
using Domain.Specifications;

namespace Modules.Applications.Create;

public sealed class CreateApplicationCommandHandler(
    IUserRepository userRepository,
    IRepository<Application> applicationRepository,
    IRepository<Job> jobRepository,
    IRepository<TalentProfile> talentProfileRepository,
    IApplicationAccountEmailService _applicationAccountEmailService,
    IPasswordService passwordService,
    IResumeStorageService resumeStorageService,
    IUnitOfWork unitOfWork,
    IAiScoreInvalidationService aiScoreInvalidationService,
    ILogger<CreateApplicationCommandHandler> logger) : IRequestHandler<CreateApplicationCommand, CreateApplicationDto>
{
    private readonly IRepository<Application> _applicationRepository = applicationRepository;
    private readonly IRepository<Job> _jobRepository = jobRepository;
    private readonly IRepository<TalentProfile> _talentProfileRepository = talentProfileRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordService _passwordService = passwordService;
    private readonly IResumeStorageService _resumeStorageService = resumeStorageService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAiScoreInvalidationService _aiScoreInvalidationService = aiScoreInvalidationService;
    private readonly ILogger<CreateApplicationCommandHandler> _logger = logger;

    public async Task<CreateApplicationDto> Handle(CreateApplicationCommand request, CancellationToken cancellationToken)
    {
        var jobExists = await _jobRepository.AnyAsync(
            new JobAvailableSpec(request.JobId, DateTimeOffset.UtcNow),
            cancellationToken);

        if (!jobExists)
        {
            throw new ValidationException("Job is not available for applications.");
        }

        var email = request.Email.Trim().ToLowerInvariant();

        await using var resumeStream = request.Resume.OpenReadStream();

        var resumeUploadResult = await _resumeStorageService.UploadAsync(
            resumeStream,
            request.Resume.FileName,
            request.Resume.ContentType,
            cancellationToken);

        var existingUser = await _userRepository.GetByEmailAsync(email, cancellationToken);

        var existingUserFound = existingUser is not null;

        if (existingUser is null)
        {
            var generatedPassword = GenerateTemporaryPassword();

            existingUser = new User
            {
                Fullname = request.Name.Trim(),
                Email = email,
                Password = _passwordService.HashPassword(generatedPassword),
                PhoneNumber = request.ContactNumber.Trim(),
                IsActive = true,
                EmailConfirmed = true,
                UserRole = UserRole.Talent,
                CreatedBy = "application-submission",
                CreatedOn = DateTimeOffset.UtcNow,
                Deleted = false,
                TokenVersion = 0
            };

            _userRepository.Add(existingUser);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _applicationAccountEmailService.SendNewAccountPasswordAsync(
                email,
                request.Name.Trim(),
                generatedPassword);
        }

        var talentProfile = await _talentProfileRepository.FirstOrDefaultAsync(
            new TalentProfileByUserIdSpec(existingUser.Id),
            cancellationToken);

        if (talentProfile is null)
        {
            var createdAt = DateTimeOffset.UtcNow;

            talentProfile = new TalentProfile
            {
                Id = Guid.NewGuid(),
                UserId = existingUser.Id,
                CreatedAt = createdAt,
                ResumeUrl = resumeUploadResult.ObjectUrl,
                ResumeFileName = Path.GetFileName(request.Resume.FileName),
                Location = request.Location.Trim(),
                Linkedin = request.LinkedInUrl.Trim(),
                AdditionalUrl = request.AdditionalUrl.Trim(),
                Experience = request.Experience
            };

            await _talentProfileRepository.AddAsync(talentProfile, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        var application = new Application
        {
            JobId = request.JobId,
            TalentId = talentProfile.Id,
            Name = request.Name.Trim(),
            Email = email,
            ContactNumber = request.ContactNumber.Trim(),
            Education = request.Education.Trim(),
            Department = request.Department.Trim(),
            CGPA = request.CGPA.Trim(),
            LinkedInUrl = request.LinkedInUrl.Trim(),
            AdditionalUrl = request.AdditionalUrl.Trim(),
            Location = request.Location.Trim(),
            CoverLetter = request.CoverLetter.Trim(),
            ResumeUrl = resumeUploadResult.ObjectUrl,
            ExpectedSalary = request.ExpectedSalary,
            Experience = request.Experience,
            Status = ApplicationStatus.Applied,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _applicationRepository.AddAsync(application, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await _aiScoreInvalidationService.EnqueueScoreForPairAsync(
                application.JobId,
                application.TalentId,
                "application-submitted",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to queue AI score refresh after application submission. JobId: {JobId}, TalentId: {TalentId}",
                application.JobId,
                application.TalentId);
        }

        _logger.LogInformation(
            "Application created. ApplicationId: {ApplicationId}, Email: {Email}",
            application.Id,
            application.Email);

        return new CreateApplicationDto
        {
            ExistingUserFound = existingUserFound,
            Name = application.Name ?? string.Empty,
            Email = application.Email ?? string.Empty,
            ContactNumber = application.ContactNumber ?? string.Empty,
            Education = application.Education ?? string.Empty,
            Department = application.Department ?? string.Empty,
            CGPA = application.CGPA ?? string.Empty,
            LinkedInUrl = application.LinkedInUrl ?? string.Empty,
            AdditionalUrl = application.AdditionalUrl ?? string.Empty,
            Location = application.Location ?? string.Empty,
            CoverLetter = application.CoverLetter ?? string.Empty,
            ExpectedSalary = application.ExpectedSalary,
            Experience = application.Experience
        };
    }

    private static string GenerateTemporaryPassword()
    {
        const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lowercase = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "!@#$%&*?";
        var allChars = uppercase + lowercase + digits + symbols;

        Span<char> passwordChars = stackalloc char[12];

        passwordChars[0] = uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)];
        passwordChars[1] = lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)];
        passwordChars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        passwordChars[3] = symbols[RandomNumberGenerator.GetInt32(symbols.Length)];

        for (var i = 4; i < passwordChars.Length; i++)
        {
            passwordChars[i] = allChars[RandomNumberGenerator.GetInt32(allChars.Length)];
        }

        for (var i = passwordChars.Length - 1; i > 0; i--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(i + 1);
            (passwordChars[i], passwordChars[swapIndex]) =
                (passwordChars[swapIndex], passwordChars[i]);
        }

        return new string(passwordChars);
    }
}
