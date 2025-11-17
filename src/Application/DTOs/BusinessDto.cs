namespace ClaudePlayground.Application.DTOs;

public record AddressDto(
    string? Street,
    string? City,
    string? State,
    string? ZipCode,
    string? Country
);

public record BusinessDto(
    string Id,
    string TenantId,
    string Name,
    string? Description,
    AddressDto? Address,
    string? PhoneNumber,
    string? Email,
    string? Website,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateBusinessDto(
    string Name,
    string? Description,
    AddressDto? Address,
    string? PhoneNumber,
    string? Email,
    string? Website
);

public record UpdateBusinessDto(
    string Name,
    string? Description,
    AddressDto? Address,
    string? PhoneNumber,
    string? Email,
    string? Website,
    bool IsActive
);

public record CreateBusinessWithUserDto(
    string Name,
    string? Description,
    AddressDto? Address,
    string? PhoneNumber,
    string? Email,
    string? Website,
    string UserEmail,
    string UserPassword,
    string? UserFirstName,
    string? UserLastName
);

public record BusinessWithUserDto(
    BusinessDto Business,
    string UserId,
    string UserEmail,
    string Token,
    string RefreshToken
);
