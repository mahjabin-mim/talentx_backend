using System.Text;
using Amazon.Extensions.NETCore.Setup;
using Amazon.S3;
using System.Security.Claims;
using Domain.Repositories;
using Infrastructure.Data;
using Infrastructure.Options;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.AiScoring;
using Infrastructure.Services.Auth;
using Infrastructure.Services.EmailTemplates;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using Shared.Services;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection connection string is missing.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.MigrationsAssembly("PostgreSQL")
            ));

        var awsOptions = configuration.GetAWSOptions();
        // var awsOptions = configuration.GetAWSOptions("AWS");
        // var accessKey = configuration["AWS:AccessKey"];
        // var secretKey = configuration["AWS:SecretKey"];

        // if (!string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey))
        // {
        //     awsOptions.Credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
        // }

        services.AddDefaultAWSOptions(awsOptions);
        services.AddAWSService<IAmazonS3>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<S3Options>(configuration.GetSection(S3Options.SectionName));
        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));
        services.Configure<AiScoringOptions>(configuration.GetSection(AiScoringOptions.SectionName));
        services.AddHttpClient<IResumeExtractionService, GeminiResumeExtractionService>();
        services.AddHttpClient<IJobDescriptionGenerationService, GeminiJobDescriptionGenerationService>();
        services.AddHttpClient<IAiScoreCalculationService, AiScoreCalculationService>();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT options are missing.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)

            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("JwtValidation");
                        var userIdClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? context.Principal?.FindFirstValue("sub");
                        var tokenVersionClaim = context.Principal?.FindFirstValue("token_version");

                        if (!Guid.TryParse(userIdClaim, out var userId))
                        {
                            logger?.LogInformation("JWT validation failed: Invalid or missing userId claim. userIdClaim={UserIdClaim}", userIdClaim);
                            context.Fail("Invalid token claims: userId.");
                            return;
                        }
                        if (!int.TryParse(tokenVersionClaim, out var tokenVersion))
                        {
                            logger?.LogInformation("JWT validation failed: Invalid or missing token_version claim. tokenVersionClaim={TokenVersionClaim}", tokenVersionClaim);
                            context.Fail("Invalid token claims: token_version.");
                            return;
                        }

                        var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                        var user = await dbContext.Users
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x => x.Id == userId && !x.Deleted, context.HttpContext.RequestAborted);

                        if (user is null)
                        {
                            logger?.LogInformation("JWT validation failed: User not found. userId={UserId}", userId);
                            context.Fail("Token is no longer valid: user not found.");
                            return;
                        }
                        if (!user.IsActive)
                        {
                            logger?.LogInformation("JWT validation failed: User is inactive. userId={UserId}", userId);
                            context.Fail("Token is no longer valid: user inactive.");
                            return;
                        }
                        if (user.TokenVersion != tokenVersion)
                        {
                            logger?.LogInformation("JWT validation failed: Token version mismatch. userId={UserId}, tokenVersion={TokenVersion}, user.TokenVersion={UserTokenVersion}", userId, tokenVersion, user.TokenVersion);
                            context.Fail("Token is no longer valid: token version mismatch.");
                            return;
                        }
                        logger?.LogInformation("JWT validation succeeded for userId={UserId}", userId);
                    }
                };
            });

        services.AddAuthorization();
        services.AddSingleton<EmailTemplateRenderer>();
        services.AddScoped<IAuthEmailService, AuthEmailService>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IApplicationAccountEmailService, ApplicationAccountEmailService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IResumeStorageService, S3ResumeStorageService>();
        services.AddScoped<IAiScoreInvalidationService, AiScoreInvalidationService>();
        services.AddScoped<ISecretHashService, SecretHashService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEmployerRepository, EmployerRepository>();
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<ITalentProfileRepository, TalentProfileRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddHostedService<AiScoreBackgroundService>();

        return services;
    }
}
