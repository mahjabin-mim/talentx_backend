using Domain.Enums;

namespace Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; }
    public string? Fullname { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? ProfileImageURL { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public bool EmailConfirmed { get; set; }
    public string? EmailVerificationCode { get; set; }
    public DateTimeOffset? EmailVerificationCodeExpiryTime { get; set; }
    public string? ForgetPasswordVerificationCode { get; set; }
    public DateTimeOffset? ForgotPasswordVerificationCodeExpiryTime { get; set; }

    public string? RefreshToken { get; set; }
    public DateTimeOffset? RefreshTokenExpiryTime { get; set; }

    public string? PreviousRefreshToken { get; set; }
    public DateTimeOffset? PreviousRefreshTokenExpiryTime { get; set; }

    public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? LastModifiedOn { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }
    public bool Deleted { get; set; }
    public UserRole? UserRole { get; set; }
    public int TokenVersion { get; set; }

    public Employer? Employer { get; set; }
}
