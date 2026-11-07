namespace Induva.Application.Dtos;

public sealed record GovernorateDto(int Id, string NameEn, string NameAr, double Latitude, double Longitude);
public sealed record RawMaterialDto(int Id, string NameEn, string NameAr, string Category);

