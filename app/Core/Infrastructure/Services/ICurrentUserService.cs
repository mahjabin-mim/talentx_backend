namespace Infrastructure.Services;

public interface ICurrentUserService
{
    Guid GetRequiredUserId();
}
