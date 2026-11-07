namespace Induva.Application.Dtos;

public sealed record MapMarkerDto(
    string Type,
    int Id,
    string Title,
    string Subtitle,
    double Lat,
    double Lng,
    string Url);

