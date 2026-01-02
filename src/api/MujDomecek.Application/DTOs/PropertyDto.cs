namespace MujDomecek.Application.DTOs;

public sealed record PropertyDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    string Name,
    string? Description,
    decimal? Latitude,
    decimal? Longitude,
    int GeoRadius,
    int UnitCount,
    int ZaznamCount,
    decimal TotalCost,
    string MyRole,
    bool IsShared,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public sealed record PropertyListResponse(
    IReadOnlyList<PropertyDto> Items,
    int Total
);

public sealed record PropertyStatsResponse(
    decimal TotalCost,
    int ZaznamCount,
    int DraftCount,
    int DocumentCount,
    IReadOnlyList<CostByMonth> CostByMonth,
    IReadOnlyList<CostByYear> CostByYear
);

public sealed record CostByMonth(string Month, decimal Cost);

public sealed record CostByYear(int Year, decimal Cost);

public sealed record CreatePropertyRequest(
    Guid ProjectId,
    string Name,
    string? Description,
    decimal? Latitude,
    decimal? Longitude,
    int? GeoRadius
);

public sealed record UpdatePropertyRequest(
    string? Name,
    string? Description,
    decimal? Latitude,
    decimal? Longitude,
    int? GeoRadius
);
