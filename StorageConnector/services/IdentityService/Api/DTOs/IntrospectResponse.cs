namespace IdentityService.Api.DTOs;

public sealed record IntrospectResponse(
    bool Active,
    string? UserId = null,
    string? Email = null,
    long? Exp = null,
    string? Issuer = null
);
