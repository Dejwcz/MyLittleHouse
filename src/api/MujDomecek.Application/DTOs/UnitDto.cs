namespace MujDomecek.Application.DTOs;

public sealed record UnitDto(
    Guid Id,
    Guid PropertyId,
    Guid? ParentUnitId,
    string Name,
    string? Description,
    string UnitType,
    int ChildCount,
    int ZaznamCount,
    Guid? CoverMediaId,
    string? CoverUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public sealed record CreateUnitRequest(
    Guid PropertyId,
    Guid? ParentUnitId,
    string Name,
    string? Description,
    string UnitType
);

public sealed record UpdateUnitRequest(
    string? Name,
    string? Description,
    string? UnitType,
    Guid? ParentUnitId
);
