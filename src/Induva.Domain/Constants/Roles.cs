namespace Induva.Domain.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string FactoryOwner = "FactoryOwner";
    public const string Supplier = "Supplier";
    public const string Investor = "Investor";
    public const string Startup = "Startup";
    public const string Buyer = "Buyer";

    public static readonly string[] All = [Admin, FactoryOwner, Supplier, Investor, Startup, Buyer];
    public static readonly string[] SelfRegistrable = [FactoryOwner, Supplier, Investor, Startup, Buyer];
}

