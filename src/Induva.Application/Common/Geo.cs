namespace Induva.Application.Common;

public static class Geo
{
    public static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth radius in km
        double dLat = Rad(lat2 - lat1);
        double dLon = Rad(lon2 - lon1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * R * Math.Asin(Math.Sqrt(a));
    }

    private static double Rad(double d) => d * Math.PI / 180;

    /// <summary>
    /// 0..1 — 1 = same place, 0 = 600 km or more (roughly Alexandria → Aswan is ~900 km).
    /// </summary>
    public static double ProximityScore(double km) => Math.Max(0, 1 - km / 600.0);
}

