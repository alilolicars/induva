using Induva.Domain.Constants;
using Induva.Domain.Entities;
using Induva.Domain.Enums;
using Induva.Infrastructure.Identity;
using Induva.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Induva.Infrastructure.Seeding;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (db.Database.IsRelational())
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        // 1) roles
        foreach (var r in Roles.All)
        {
            if (!await roles.RoleExistsAsync(r))
                await roles.CreateAsync(new IdentityRole(r));
        }

        // 2) admin account
        var adminEmail = cfg["Seed:AdminEmail"] ?? "admin@induva.com";
        var adminPwd = cfg["Seed:AdminPassword"] ?? cfg["Seed:DemoPassword"] ?? "Admin@123456";
        await EnsureUserAsync(users, adminEmail, "Platform Administrator", Roles.Admin, adminPwd);

        // 3) lookups: 27 Egyptian Governorates
        if (!await db.Governorates.AnyAsync())
        {
            db.Governorates.AddRange(
                G("Cairo", "القاهرة", 30.0444, 31.2357),
                G("Giza", "الجيزة", 30.0131, 31.2089),
                G("Alexandria", "الإسكندرية", 31.2001, 29.9187),
                G("Qalyubia", "القليوبية", 30.4660, 31.1850),
                G("Port Said", "بورسعيد", 31.2653, 32.3019),
                G("Suez", "السويس", 29.9668, 32.5498),
                G("Ismailia", "الإسماعيلية", 30.5965, 32.2715),
                G("Damietta", "دمياط", 31.4165, 31.8133),
                G("Dakahlia", "الدقهلية", 31.0409, 31.3785),
                G("Sharqia", "الشرقية", 30.5877, 31.5020),
                G("Gharbia", "الغربية", 30.7865, 31.0004),
                G("Kafr El Sheikh", "كفر الشيخ", 31.1107, 30.9388),
                G("Monufia", "المنوفية", 30.5972, 30.9876),
                G("Beheira", "البحيرة", 31.0341, 30.4682),
                G("Fayoum", "الفيوم", 29.3084, 30.8428),
                G("Beni Suef", "بني سويف", 29.0661, 31.0994),
                G("Minya", "المنيا", 28.1099, 30.7503),
                G("Assiut", "أسيوط", 27.1809, 31.1837),
                G("Sohag", "سوهاج", 26.5591, 31.6948),
                G("Qena", "قنا", 26.1551, 32.7160),
                G("Luxor", "الأقصر", 25.6872, 32.6396),
                G("Aswan", "أسوان", 24.0889, 32.8998),
                G("Red Sea", "البحر الأحمر", 27.2579, 33.8116),
                G("New Valley", "الوادي الجديد", 25.4390, 30.5586),
                G("Matrouh", "مطروح", 31.3543, 27.2373),
                G("North Sinai", "شمال سيناء", 31.1321, 33.7984),
                G("South Sinai", "جنوب سيناء", 28.2410, 33.6220));

            await db.SaveChangesAsync();
        }

        // 4) lookups: Raw Materials
        if (!await db.RawMaterials.AnyAsync())
        {
            db.RawMaterials.AddRange(
                M("Limestone", "حجر جيري", "Mineral"),
                M("Gypsum", "جبس", "Mineral"),
                M("Silica sand", "رمل سيليكا", "Mineral"),
                M("Clay", "طفلة", "Mineral"),
                M("Kaolin", "كاولين", "Mineral"),
                M("Feldspar", "فلسبار", "Mineral"),
                M("Marble", "رخام", "Mineral"),
                M("Granite", "جرانيت", "Mineral"),
                M("Basalt", "بازلت", "Mineral"),
                M("Phosphate", "فوسفات", "Mineral"),
                M("Iron ore", "خام الحديد", "Mineral"),
                M("Salt", "ملح", "Mineral"),
                M("Cotton", "قطن", "Agricultural"),
                M("Sugarcane", "قصب السكر", "Agricultural"),
                M("Sugar beet", "بنجر السكر", "Agricultural"),
                M("Tomatoes", "طماطم", "Agricultural"),
                M("Citrus", "موالح", "Agricultural"),
                M("Dates", "بلح", "Agricultural"),
                M("Olives", "زيتون", "Agricultural"),
                M("Wheat", "قمح", "Agricultural"),
                M("Flax", "كتان", "Agricultural"),
                M("Polymers (resin)", "راتنج بوليمر", "Chemical"),
                M("Recycled plastic", "بلاستيك مُعاد تدويره", "Chemical"));

            await db.SaveChangesAsync();
        }

        // 5) Golden Demo Data (Roadmap & Demo-Day Safety)
        var seedDemo = cfg.GetValue<bool?>("Seed:DemoData") ?? true;
        if (seedDemo && !await db.Factories.AnyAsync())
        {
            await SeedGoldenDemoDataAsync(db, users, cfg);
        }
    }

    private static async Task SeedGoldenDemoDataAsync(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IConfiguration cfg)
    {
        var demoPwd = cfg["Seed:DemoPassword"] ?? "Demo@123456";

        // 1. Demo User Accounts
        var uDelta = await EnsureUserAsync(userManager, "delta.tomato@induva.com", "Delta Tomato Canning Co.", Roles.FactoryOwner, demoPwd);
        var uMahalla = await EnsureUserAsync(userManager, "mahalla.textiles@induva.com", "Mahalla Garment Works", Roles.FactoryOwner, demoPwd);
        var uPlastics = await EnsureUserAsync(userManager, "tenth.plastics@induva.com", "Tenth Plastics Ltd.", Roles.FactoryOwner, demoPwd);
        var uCeramics = await EnsureUserAsync(userManager, "sokhna.ceramics@induva.com", "Ain Sokhna Ceramics", Roles.FactoryOwner, demoPwd);
        var uStone = await EnsureUserAsync(userManager, "shaq.stone@induva.com", "Shaq El Thoban Stone", Roles.FactoryOwner, demoPwd);
        var uSugar = await EnsureUserAsync(userManager, "komombo.sugar@induva.com", "Kom Ombo Sugar Mill", Roles.FactoryOwner, demoPwd);
        var uPartner = await EnsureUserAsync(userManager, "alilolicars@gmail.com", "Ali Loli (Industry Partner)", Roles.FactoryOwner, demoPwd);

        var uSupPolymers = await EnsureUserAsync(userManager, "supplier.polymers@induva.com", "Cairo Polymer Distribution", Roles.Supplier, demoPwd);
        var uSupCotton = await EnsureUserAsync(userManager, "supplier.cotton@induva.com", "Nile Cotton Ginning", Roles.Supplier, demoPwd);
        var uSupTomato = await EnsureUserAsync(userManager, "supplier.tomato@induva.com", "Beheira Tomato Growers Co-op", Roles.Supplier, demoPwd);
        var uSupClay = await EnsureUserAsync(userManager, "supplier.clay@induva.com", "Red Sea Minerals & Kaolin", Roles.Supplier, demoPwd);
        var uSupStone = await EnsureUserAsync(userManager, "supplier.stone@induva.com", "Minya Stone & Marble Quarries", Roles.Supplier, demoPwd);
        var uSupRecycled = await EnsureUserAsync(userManager, "supplier.recycled@induva.com", "Delta Recycled Flakes", Roles.Supplier, demoPwd);

        var uStartPack = await EnsureUserAsync(userManager, "startup.packaging@induva.com", "EcoPack Circular Solutions", Roles.Startup, demoPwd);
        var uStartPaper = await EnsureUserAsync(userManager, "startup.agriwaste@induva.com", "Nile Agro-Fiber Paper", Roles.Startup, demoPwd);
        var uStartLeather = await EnsureUserAsync(userManager, "startup.leather@induva.com", "Kemet Crafted Leather", Roles.Startup, demoPwd);
        var uStartSauces = await EnsureUserAsync(userManager, "startup.sauces@induva.com", "Qaha Gourmet Sauces", Roles.Startup, demoPwd);

        var uInvNile = await EnsureUserAsync(userManager, "investor.nile@induva.com", "Nile Valley Agribusiness Ventures", Roles.Investor, demoPwd);
        var uInvCairo = await EnsureUserAsync(userManager, "investor.cairo@induva.com", "Cairo Industrial Growth Fund", Roles.Investor, demoPwd);
        var uInvMfg = await EnsureUserAsync(userManager, "investor.mfg@induva.com", "Misr Manufacturing Capital", Roles.Investor, demoPwd);
        var uInvAngel = await EnsureUserAsync(userManager, "investor.angel@induva.com", "Delta Angel Syndicate", Roles.Investor, demoPwd);

        var govs = await db.Governorates.ToDictionaryAsync(g => g.NameEn);
        var mats = await db.RawMaterials.ToDictionaryAsync(m => m.NameEn);

        // 2. Governorate Resources (Indicative Regional Endowments)
        db.GovernorateResources.AddRange(
            new GovernorateResource { GovernorateId = govs["Sharqia"].Id, RawMaterialId = mats["Tomatoes"].Id, EstimatedAnnualTons = 150000, Notes = "Extensive fertile cultivation across Belbeis and Faqous" },
            new GovernorateResource { GovernorateId = govs["Sharqia"].Id, RawMaterialId = mats["Polymers (resin)"].Id, EstimatedAnnualTons = 80000, Notes = "Industrial petrochemical distribution hub in 10th of Ramadan" },
            new GovernorateResource { GovernorateId = govs["Gharbia"].Id, RawMaterialId = mats["Cotton"].Id, EstimatedAnnualTons = 75000, Notes = "Delta long-staple cotton central belt" },
            new GovernorateResource { GovernorateId = govs["Cairo"].Id, RawMaterialId = mats["Marble"].Id, EstimatedAnnualTons = 200000, Notes = "Shaq El Thoban industrial stone cluster" },
            new GovernorateResource { GovernorateId = govs["Cairo"].Id, RawMaterialId = mats["Polymers (resin)"].Id, EstimatedAnnualTons = 120000, Notes = "Wholesale chemical and resin storage facilities" },
            new GovernorateResource { GovernorateId = govs["Suez"].Id, RawMaterialId = mats["Clay"].Id, EstimatedAnnualTons = 300000, Notes = "Ain Sokhna mineral basin" },
            new GovernorateResource { GovernorateId = govs["Suez"].Id, RawMaterialId = mats["Kaolin"].Id, EstimatedAnnualTons = 90000, Notes = "High-purity kaolin deposits" },
            new GovernorateResource { GovernorateId = govs["Suez"].Id, RawMaterialId = mats["Silica sand"].Id, EstimatedAnnualTons = 500000, Notes = "Industrial-grade glass and ceramic silica" },
            new GovernorateResource { GovernorateId = govs["Minya"].Id, RawMaterialId = mats["Limestone"].Id, EstimatedAnnualTons = 1200000, Notes = "Eastern desert quarry corridor" },
            new GovernorateResource { GovernorateId = govs["Minya"].Id, RawMaterialId = mats["Marble"].Id, EstimatedAnnualTons = 400000, Notes = "Alabaster and Sunny marble formations" },
            new GovernorateResource { GovernorateId = govs["Beheira"].Id, RawMaterialId = mats["Tomatoes"].Id, EstimatedAnnualTons = 250000, Notes = "Nubaria agricultural reclamation zones" },
            new GovernorateResource { GovernorateId = govs["Beheira"].Id, RawMaterialId = mats["Citrus"].Id, EstimatedAnnualTons = 180000, Notes = "Export orange and lemon groves" },
            new GovernorateResource { GovernorateId = govs["Beheira"].Id, RawMaterialId = mats["Cotton"].Id, EstimatedAnnualTons = 60000, Notes = "Northern Delta agricultural cooperatives" },
            new GovernorateResource { GovernorateId = govs["Aswan"].Id, RawMaterialId = mats["Sugarcane"].Id, EstimatedAnnualTons = 800000, Notes = "Kom Ombo irrigation basin" },
            new GovernorateResource { GovernorateId = govs["Aswan"].Id, RawMaterialId = mats["Granite"].Id, EstimatedAnnualTons = 150000, Notes = "Aswan red and black monumental granite" },
            new GovernorateResource { GovernorateId = govs["Red Sea"].Id, RawMaterialId = mats["Kaolin"].Id, EstimatedAnnualTons = 120000, Notes = "Safaga-Quseir mineral belt" },
            new GovernorateResource { GovernorateId = govs["Red Sea"].Id, RawMaterialId = mats["Feldspar"].Id, EstimatedAnnualTons = 95000, Notes = "Pegmatite veins suitable for ceramics" },
            new GovernorateResource { GovernorateId = govs["Red Sea"].Id, RawMaterialId = mats["Phosphate"].Id, EstimatedAnnualTons = 600000, Notes = "Phosphate rock mining zones" },
            new GovernorateResource { GovernorateId = govs["Qalyubia"].Id, RawMaterialId = mats["Citrus"].Id, EstimatedAnnualTons = 90000, Notes = "Qaha and Toukh orchards" },
            new GovernorateResource { GovernorateId = govs["Qalyubia"].Id, RawMaterialId = mats["Recycled plastic"].Id, EstimatedAnnualTons = 35000, Notes = "Industrial and post-consumer sorting hubs" }
        );

        // 3. Golden Factories (from DOCS/INDUVA_04_ROADMAP_AND_RISKS.md §4)
        var fDelta = new Factory
        {
            OwnerUserId = uDelta.Id,
            Name = "Delta Tomato Canning Co.",
            Sector = Sector.FoodProcessing,
            GovernorateId = govs["Sharqia"].Id,
            Latitude = 30.5877,
            Longitude = 31.5020,
            UtilizationPercent = 60,
            Description = "Established agro-industrial processing plant with complete washing, concentration, hot-break pulping, automated canning, and retort autoclave sterilization.",
            IsPublic = true,
            Machines = new List<Machine>
            {
                new() { Name = "Industrial Vegetable Washing Line", MachineType = "Washer", Quantity = 1, CapacityPerHour = 8000, CapacityUnit = "kg/hr", Description = "Dual-stage immersion and spray wash" },
                new() { Name = "Hot-Break Pulper & Finisher", MachineType = "Pulper", Quantity = 2, CapacityPerHour = 6000, CapacityUnit = "kg/hr", Description = "Pectic enzyme thermal deactivator" },
                new() { Name = "Triple-Effect Vacuum Evaporator", MachineType = "Evaporator", Quantity = 1, CapacityPerHour = 4000, CapacityUnit = "kg/hr", Description = "Low-temp concentration to 30 Brix" },
                new() { Name = "Rotary Automatic Can Seamer", MachineType = "Seamer", Quantity = 2, CapacityPerHour = 7200, CapacityUnit = "cans/hr", Description = "Double-seam hermetic canning" },
                new() { Name = "Industrial Retort Sterilizer Autoclave", MachineType = "Sterilizer", Quantity = 3, CapacityPerHour = 1500, CapacityUnit = "cans/cycle", Description = "Overpressure steam-water autoclave" },
                new() { Name = "Glass-Jar Vacuum Capping & Filling Line", MachineType = "Filling", Quantity = 1, CapacityPerHour = 4800, CapacityUnit = "jars/hr", Description = "Twist-off vacuum capper" }
            },
            IdleWindows = new List<IdleWindow>
            {
                new() { StartMonth = 10, EndMonth = 2, AvailableCapacityPercent = 80, Note = "Post tomato harvest off-peak window; available for contract processing" }
            },
            Products = new List<FactoryProduct>
            {
                new() { Name = "Tomato Paste 370g Tin", Category = "Canned Goods" },
                new() { Name = "Canned Peeled Plum Tomatoes 800g", Category = "Canned Goods" }
            },
            MaterialNeeds = new List<FactoryMaterialNeed>
            {
                new() { RawMaterialId = mats["Tomatoes"].Id, MonthlyTons = 500 },
                new() { RawMaterialId = mats["Salt"].Id, MonthlyTons = 15 }
            }
        };

        var fMahalla = new Factory
        {
            OwnerUserId = uMahalla.Id,
            Name = "Mahalla Garment Works",
            Sector = Sector.Textiles,
            GovernorateId = govs["Gharbia"].Id,
            Latitude = 30.7865,
            Longitude = 31.0004,
            UtilizationPercent = 70,
            Description = "Integrated textile apparel production unit equipped with multi-needle industrial sewing stations, automated fabric cutting, and finishing steam presses.",
            IsPublic = true,
            Machines = new List<Machine>
            {
                new() { Name = "Lockstitch Industrial Sewing Machines", MachineType = "Sewing", Quantity = 120, CapacityPerHour = 120, CapacityUnit = "garments/hr", Description = "High-speed precision lockstitch" },
                new() { Name = "5-Thread Overlock Machines", MachineType = "Overlock", Quantity = 30, CapacityPerHour = 60, CapacityUnit = "garments/hr", Description = "Edge seaming & trimming" },
                new() { Name = "Computerized Spreading & Cutting Table", MachineType = "Cutter", Quantity = 4, CapacityPerHour = 250, CapacityUnit = "pieces/hr", Description = "Multi-ply CNC fabric cutting" },
                new() { Name = "Industrial Steam Press Carousel", MachineType = "Finishing", Quantity = 6, CapacityPerHour = 100, CapacityUnit = "garments/hr", Description = "Final shaping and press" }
            },
            IdleWindows = new List<IdleWindow>
            {
                new() { StartMonth = 1, EndMonth = 2, AvailableCapacityPercent = 30, Note = "Winter collection transition window" }
            },
            Products = new List<FactoryProduct>
            {
                new() { Name = "100% Egyptian Cotton Crewneck T-Shirts", Category = "Apparel" },
                new() { Name = "Heavy Duty Industrial Workwear Uniforms", Category = "Workwear" }
            },
            MaterialNeeds = new List<FactoryMaterialNeed>
            {
                new() { RawMaterialId = mats["Cotton"].Id, MonthlyTons = 40 }
            }
        };

        var fPlastics = new Factory
        {
            OwnerUserId = uPlastics.Id,
            Name = "Tenth Plastics Ltd.",
            Sector = Sector.Plastics,
            GovernorateId = govs["Sharqia"].Id,
            Latitude = 30.5877,
            Longitude = 31.5020,
            UtilizationPercent = 75,
            Description = "Precision injection molding facility in 10th of Ramadan industrial city with 150T to 250T clamping machines and polymer extrusion lines.",
            IsPublic = true,
            Machines = new List<Machine>
            {
                new() { Name = "Hydraulic Injection Molding Machine 150T", MachineType = "Injection", Quantity = 4, CapacityPerHour = 180, CapacityUnit = "cycles/hr", Description = "Automated sprue picker" },
                new() { Name = "High-Precision Injection Molding Machine 250T", MachineType = "Injection", Quantity = 2, CapacityPerHour = 100, CapacityUnit = "cycles/hr", Description = "Large part clamping force" },
                new() { Name = "Single-Screw Profile Extruder", MachineType = "Extruder", Quantity = 1, CapacityPerHour = 60, CapacityUnit = "kg/hr", Description = "Continuous profile line" },
                new() { Name = "Mold Storage Overhead Crane & Preheater", MachineType = "Tooling", Quantity = 1, Description = "5-ton rapid mold swap crane" }
            },
            IdleWindows = new List<IdleWindow>
            {
                new() { StartMonth = 12, EndMonth = 2, AvailableCapacityPercent = 25, Note = "Annual tooling overhaul and excess mold capacity" }
            },
            Products = new List<FactoryProduct>
            {
                new() { Name = "Polypropylene Food Storage Containers", Category = "Packaging" },
                new() { Name = "Heavy Duty Stackable Plastic Crates", Category = "Logistics" }
            },
            MaterialNeeds = new List<FactoryMaterialNeed>
            {
                new() { RawMaterialId = mats["Polymers (resin)"].Id, MonthlyTons = 60 },
                new() { RawMaterialId = mats["Recycled plastic"].Id, MonthlyTons = 30 }
            }
        };

        var fCeramics = new Factory
        {
            OwnerUserId = uCeramics.Id,
            Name = "Ain Sokhna Ceramics",
            Sector = Sector.Ceramics,
            GovernorateId = govs["Suez"].Id,
            Latitude = 29.9668,
            Longitude = 32.5498,
            UtilizationPercent = 60,
            Description = "Modern continuous tile manufacturing facility producing porcelain and ceramic wall and floor tiles with automated glazing and roller kilns.",
            IsPublic = true,
            Machines = new List<Machine>
            {
                new() { Name = "Continuous Ball Mill 40-Ton", MachineType = "Milling", Quantity = 2, CapacityPerHour = 15000, CapacityUnit = "kg/hr", Description = "Wet slip grinding" },
                new() { Name = "Industrial Spray Dryer Tower", MachineType = "Dryer", Quantity = 1, CapacityPerHour = 8000, CapacityUnit = "kg/hr", Description = "Granule moisture control" },
                new() { Name = "Hydraulic Tile Press 3500T", MachineType = "Press", Quantity = 2, CapacityPerHour = 300, CapacityUnit = "m²/hr", Description = "High-density tile forming" },
                new() { Name = "Single-Layer Roller Tunnel Kiln", MachineType = "Kiln", Quantity = 1, CapacityPerHour = 450, CapacityUnit = "m²/hr", Description = "1200°C fast firing line" },
                new() { Name = "Automated Digital Glaze & Inkjet Line", MachineType = "Glazing", Quantity = 1, CapacityPerHour = 500, CapacityUnit = "m²/hr", Description = "High-definition surface print" }
            },
            IdleWindows = new List<IdleWindow>
            {
                new() { StartMonth = 1, EndMonth = 2, AvailableCapacityPercent = 40, Note = "Scheduled kiln relining and refractory maintenance" }
            },
            Products = new List<FactoryProduct>
            {
                new() { Name = "Glazed Ceramic Wall Tiles 30x60", Category = "Tiles" },
                new() { Name = "Polished Porcelain Floor Tiles 60x60", Category = "Tiles" }
            },
            MaterialNeeds = new List<FactoryMaterialNeed>
            {
                new() { RawMaterialId = mats["Clay"].Id, MonthlyTons = 200 },
                new() { RawMaterialId = mats["Kaolin"].Id, MonthlyTons = 50 },
                new() { RawMaterialId = mats["Feldspar"].Id, MonthlyTons = 80 },
                new() { RawMaterialId = mats["Silica sand"].Id, MonthlyTons = 120 }
            }
        };

        var fStone = new Factory
        {
            OwnerUserId = uStone.Id,
            Name = "Shaq El Thoban Stone",
            Sector = Sector.Construction,
            GovernorateId = govs["Cairo"].Id,
            Latitude = 30.0444,
            Longitude = 31.2357,
            UtilizationPercent = 85,
            Description = "Heavy stone sawing, calibrating, and surface polishing works serving domestic building projects and international export shipments.",
            IsPublic = true,
            Machines = new List<Machine>
            {
                new() { Name = "Multi-Blade Diamond Gang Saw", MachineType = "Saw", Quantity = 3, CapacityPerHour = 25, CapacityUnit = "m²/hr", Description = "Primary block cutting" },
                new() { Name = "5-Axis CNC Bridge Saw", MachineType = "CNC", Quantity = 2, CapacityPerHour = 15, CapacityUnit = "m²/hr", Description = "Precision architectural profiling" },
                new() { Name = "Continuous 16-Head Slab Polishing Line", MachineType = "Polisher", Quantity = 1, CapacityPerHour = 40, CapacityUnit = "m²/hr", Description = "Mirror polish finishing" },
                new() { Name = "High-Pressure Abrasive Waterjet Cutter", MachineType = "Waterjet", Quantity = 1, CapacityPerHour = 8, CapacityUnit = "m²/hr", Description = "Intricate medallion inlay cutting" }
            },
            Products = new List<FactoryProduct>
            {
                new() { Name = "Polished Galala Marble Slabs 2cm", Category = "Slabs" },
                new() { Name = "Honed Aswan Red Granite Tiles 40x40", Category = "Tiles" }
            },
            MaterialNeeds = new List<FactoryMaterialNeed>
            {
                new() { RawMaterialId = mats["Marble"].Id, MonthlyTons = 180 },
                new() { RawMaterialId = mats["Granite"].Id, MonthlyTons = 100 }
            }
        };

        var fSugar = new Factory
        {
            OwnerUserId = uSugar.Id,
            Name = "Kom Ombo Sugar Mill",
            Sector = Sector.FoodProcessing,
            GovernorateId = govs["Aswan"].Id,
            Latitude = 24.0889,
            Longitude = 32.8998,
            UtilizationPercent = 50,
            Description = "Historic Upper Egypt cane sugar refining complex with heavy extraction tandem mills, juice clarifiers, vacuum pans, and high-speed centrifuges.",
            IsPublic = true,
            Machines = new List<Machine>
            {
                new() { Name = "Heavy Sugarcane Milling Tandem (5 Mills)", MachineType = "Mill", Quantity = 2, CapacityPerHour = 25000, CapacityUnit = "kg/hr", Description = "Juice extraction and bagasse separation" },
                new() { Name = "Multi-Effect Juice Evaporators", MachineType = "Evaporator", Quantity = 4, CapacityPerHour = 15000, CapacityUnit = "kg/hr", Description = "Concentration to syrup" },
                new() { Name = "Batch Sugar Centrifuges", MachineType = "Centrifuge", Quantity = 6, CapacityPerHour = 5000, CapacityUnit = "kg/hr", Description = "Crystal separation from mother liquor" },
                new() { Name = "Bagasse-Fired High-Pressure Steam Boilers", MachineType = "Boiler", Quantity = 3, Description = "Cogeneration energy unit" }
            },
            IdleWindows = new List<IdleWindow>
            {
                new() { StartMonth = 6, EndMonth = 12, AvailableCapacityPercent = 90, Note = "Sugarcane seasonal dormant window (summer/autumn off-peak)" }
            },
            Products = new List<FactoryProduct>
            {
                new() { Name = "Refined White Granulated Cane Sugar 50kg", Category = "Sugar" },
                new() { Name = "Industrial Blackstrap Cane Molasses", Category = "Byproduct" }
            },
            MaterialNeeds = new List<FactoryMaterialNeed>
            {
                new() { RawMaterialId = mats["Sugarcane"].Id, MonthlyTons = 2000 }
            }
        };

        db.Factories.AddRange(fDelta, fMahalla, fPlastics, fCeramics, fStone, fSugar);
        await db.SaveChangesAsync();

        // Pre-warm AI capability profile on Delta Tomato Canning Co. for immediate demo readiness
        db.FactoryCapabilityProfiles.Add(new FactoryCapabilityProfile
        {
            FactoryId = fDelta.Id,
            Summary = "Delta Tomato Canning Co. operates an integrated vegetable processing facility in Sharqia, capable of industrial washing, vacuum concentration, automated canning, glass-jar filling, and retort sterilization for shelf-stable food products.",
            TagsCsv = "canning,fruit-vegetable-processing,food-packaging,beverage-bottling",
            ResultJson = """
            {
              "processes": [
                { "name": "Cleaning & Sorting", "machines": ["washing line"], "materialsProcessable": ["Tomatoes", "Citrus", "Vegetables"], "capacityNote": "High throughput pre-treatment" },
                { "name": "Thermal Pulverization", "machines": ["hot-break pulper"], "materialsProcessable": ["Fresh Produce"], "capacityNote": "Inactivates pectic enzymes" },
                { "name": "Vacuum Concentration", "machines": ["vacuum evaporator"], "materialsProcessable": ["Tomato Pulp", "Fruit Purees"], "capacityNote": "Low-temperature evaporation" },
                { "name": "Hermetic Packaging", "machines": ["can seamer", "glass-jar filling line"], "materialsProcessable": ["Tin Cans", "Glass Jars"], "capacityNote": "Dual packaging lines" },
                { "name": "Thermal Sterilization", "machines": ["retort sterilizer"], "materialsProcessable": ["Sealed Cans", "Sealed Jars"], "capacityNote": "Commercial sterility compliant" }
              ],
              "strengths": [
                "Complete end-to-end hot-break and concentration line",
                "Dual packaging flexibility (both tin cans and glass jars)",
                "Retort autoclave guarantees long shelf life without preservatives",
                "Proximity to major tomato farming clusters in Sharqia and Delta"
              ],
              "constraints": [
                "High thermal energy consumption for steam boilers",
                "Severe seasonality outside the Egyptian winter tomato harvest window",
                "Equipment cleaning turnaround required between different food categories"
              ],
              "underusedAssets": [
                "Glass-jar filling line during October to February idle window",
                "Retort sterilizer capacity can be rented for contract sterilization"
              ],
              "missingInformation": [
                "Boiler steam capacity (tons/hour)",
                "Exact can diameter / jar volume specifications"
              ],
              "confidence": 0.88
            }
            """,
            GeneratedAtUtc = DateTime.UtcNow
        });

        // 4. Suppliers (6 from DOCS/INDUVA_04_ROADMAP_AND_RISKS.md §4)
        var sPolymers = new Supplier
        {
            OwnerUserId = uSupPolymers.Id,
            CompanyName = "Cairo Polymer Distribution",
            GovernorateId = govs["Cairo"].Id,
            Latitude = 30.0444,
            Longitude = 31.2357,
            Description = "Distributor of prime-grade injection polymers and engineering resins.",
            IsPublic = true,
            Materials = new List<SupplierMaterial>
            {
                new() { RawMaterialId = mats["Polymers (resin)"].Id, MinOrderTons = 5, PriceNote = "Bulk pellet delivery" },
                new() { RawMaterialId = mats["Recycled plastic"].Id, MinOrderTons = 2, PriceNote = "Washed regrind" }
            }
        };

        var sCotton = new Supplier
        {
            OwnerUserId = uSupCotton.Id,
            CompanyName = "Nile Cotton Ginning",
            GovernorateId = govs["Beheira"].Id,
            Latitude = 31.0341,
            Longitude = 30.4682,
            Description = "Ginned long-staple cotton bales supplied directly to Egyptian spinning mills.",
            IsPublic = true,
            Materials = new List<SupplierMaterial>
            {
                new() { RawMaterialId = mats["Cotton"].Id, MinOrderTons = 10, PriceNote = "Graded Giza 94 bales" }
            }
        };

        var sTomato = new Supplier
        {
            OwnerUserId = uSupTomato.Id,
            CompanyName = "Beheira Tomato Growers Co-op",
            GovernorateId = govs["Beheira"].Id,
            Latitude = 31.0341,
            Longitude = 30.4682,
            Description = "Agricultural union supplying industrial processing tomatoes on contract farming terms.",
            IsPublic = true,
            Materials = new List<SupplierMaterial>
            {
                new() { RawMaterialId = mats["Tomatoes"].Id, MinOrderTons = 20, PriceNote = "Field-gate delivered in crates" }
            }
        };

        var sClay = new Supplier
        {
            OwnerUserId = uSupClay.Id,
            CompanyName = "Red Sea Minerals & Kaolin",
            GovernorateId = govs["Red Sea"].Id,
            Latitude = 27.2579,
            Longitude = 33.8116,
            Description = "Quarry operators and mineral processors supplying ceramic and refractory factories.",
            IsPublic = true,
            Materials = new List<SupplierMaterial>
            {
                new() { RawMaterialId = mats["Kaolin"].Id, MinOrderTons = 25, PriceNote = "Washed and micronized" },
                new() { RawMaterialId = mats["Silica sand"].Id, MinOrderTons = 30, PriceNote = "High silica 99.2%" },
                new() { RawMaterialId = mats["Feldspar"].Id, MinOrderTons = 20, PriceNote = "Sodium and potassium grades" }
            }
        };

        var sStone = new Supplier
        {
            OwnerUserId = uSupStone.Id,
            CompanyName = "Minya Stone & Marble Quarries",
            GovernorateId = govs["Minya"].Id,
            Latitude = 28.1099,
            Longitude = 30.7503,
            Description = "Direct quarry extractors supplying rough blocks of limestone and Egyptian marble.",
            IsPublic = true,
            Materials = new List<SupplierMaterial>
            {
                new() { RawMaterialId = mats["Limestone"].Id, MinOrderTons = 30, PriceNote = "High purity calcium carbonate blocks" },
                new() { RawMaterialId = mats["Marble"].Id, MinOrderTons = 20, PriceNote = "Rough quarried blocks" }
            }
        };

        var sRecycled = new Supplier
        {
            OwnerUserId = uSupRecycled.Id,
            CompanyName = "Delta Recycled Flakes",
            GovernorateId = govs["Qalyubia"].Id,
            Latitude = 30.4660,
            Longitude = 31.1850,
            Description = "Cleaned, hot-washed HDPE and PP regrind flakes for circular packaging manufacturing.",
            IsPublic = true,
            Materials = new List<SupplierMaterial>
            {
                new() { RawMaterialId = mats["Recycled plastic"].Id, MinOrderTons = 2, PriceNote = "Uniform melt flow index" }
            }
        };

        db.Suppliers.AddRange(sPolymers, sCotton, sTomato, sClay, sStone, sRecycled);

        // 5. Demand Requests (8 RFQs from DOCS/INDUVA_04_ROADMAP_AND_RISKS.md §4)
        db.DemandRequests.AddRange(
            new DemandRequest
            {
                RequesterUserId = uDelta.Id,
                Title = "Tomato Paste Bulk 40 Tons",
                ProductName = "Concentrated Tomato Paste (28-30 Brix)",
                Sector = Sector.FoodProcessing,
                RequiredTagsCsv = "fruit-vegetable-processing,canning",
                Quantity = 40,
                Unit = "tons",
                TargetPriceEgp = 1800000,
                GovernorateId = govs["Cairo"].Id,
                Status = "Open",
                Description = "Aseptic drum packaged tomato paste required for national food distributor network."
            },
            new DemandRequest
            {
                RequesterUserId = uStartPack.Id,
                Title = "500 ml Food Containers 200,000 Pcs",
                ProductName = "Rigid Polypropylene Food Containers",
                Sector = Sector.Plastics,
                RequiredTagsCsv = "injection-molding,food-packaging",
                Quantity = 200000,
                Unit = "pcs",
                TargetPriceEgp = 600000,
                GovernorateId = govs["Giza"].Id,
                Status = "Open",
                Description = "Microwave-safe transparent takeaway food containers with tamper-evident lids."
            },
            new DemandRequest
            {
                RequesterUserId = uMahalla.Id,
                Title = "School Uniform Sets 20,000 Units",
                ProductName = "Secondary School Uniform Apparel",
                Sector = Sector.Textiles,
                RequiredTagsCsv = "apparel-assembly,textile-spinning",
                Quantity = 20000,
                Unit = "sets",
                TargetPriceEgp = 4000000,
                GovernorateId = govs["Alexandria"].Id,
                Status = "Open",
                Description = "Standardized cotton blend school uniforms for upcoming academic year tender."
            },
            new DemandRequest
            {
                RequesterUserId = uCeramics.Id,
                Title = "Floor Tiles 60x60 10,000 m²",
                ProductName = "Glazed Vitrified Porcelain Tiles",
                Sector = Sector.Ceramics,
                RequiredTagsCsv = "kiln-firing,glazing",
                Quantity = 10000,
                Unit = "m²",
                TargetPriceEgp = 1500000,
                GovernorateId = govs["Giza"].Id,
                Status = "Open",
                Description = "First-choice anti-slip commercial porcelain tiles for administrative center project."
            },
            new DemandRequest
            {
                RequesterUserId = uPlastics.Id,
                Title = "Plastic Crates 5,000 Pcs",
                ProductName = "Perforated Agricultural Produce Crates",
                Sector = Sector.Plastics,
                RequiredTagsCsv = "injection-molding",
                Quantity = 5000,
                Unit = "pcs",
                TargetPriceEgp = 350000,
                GovernorateId = govs["Dakahlia"].Id,
                Status = "Open",
                Description = "Heavy duty stackable crates for field harvesting and cold room transportation."
            },
            new DemandRequest
            {
                RequesterUserId = uDelta.Id,
                Title = "Export Canned Vegetables Pallet Batch",
                ProductName = "Export Canned Artichokes and Legumes",
                Sector = Sector.FoodProcessing,
                RequiredTagsCsv = "fruit-vegetable-processing,canning,packaging",
                Quantity = 50,
                Unit = "tons",
                TargetPriceEgp = 2500000,
                GovernorateId = govs["Port Said"].Id,
                IsExport = true,
                Status = "Open",
                Description = "EU-certified canned vegetables for export shipping through Port Said Mediterranean hub."
            },
            new DemandRequest
            {
                RequesterUserId = uStone.Id,
                Title = "Egyptian Marble Slabs 300 m²",
                ProductName = "Polished Sunny Light Marble",
                Sector = Sector.Construction,
                RequiredTagsCsv = "cnc-machining,waterjet-cutting",
                Quantity = 300,
                Unit = "m²",
                TargetPriceEgp = 450000,
                GovernorateId = govs["Cairo"].Id,
                Status = "Open",
                Description = "Pre-cut calibrated marble wall cladding for embassy refurbishment."
            },
            new DemandRequest
            {
                RequesterUserId = uStartPack.Id,
                Title = "Reusable Polypropylene Packaging 50,000 Pcs",
                ProductName = "Foldable Circular Logistics Boxes",
                Sector = Sector.Plastics,
                RequiredTagsCsv = "injection-molding,plastic-recycling",
                Quantity = 50000,
                Unit = "pcs",
                TargetPriceEgp = 250000,
                GovernorateId = govs["Cairo"].Id,
                Status = "Open",
                Description = "Recycled polymer returnable delivery packaging for e-commerce supermarket chains."
            }
        );

        // 6. Startups (4 from DOCS/INDUVA_04_ROADMAP_AND_RISKS.md §4)
        db.Startups.AddRange(
            new Startup
            {
                OwnerUserId = uStartPack.Id,
                Name = "EcoPack Circular Solutions",
                Sector = Sector.Plastics,
                Stage = StartupStage.Prototype,
                GovernorateId = govs["Sharqia"].Id,
                ProductIdea = "Reusable circular delivery containers fabricated from locally recycled polymers",
                Description = "Innovating returnable grocery tote boxes using 100% recycled Egyptian plastic flakes.",
                FundingNeededEgp = 1200000,
                RequiredTagsCsv = "injection-molding,plastic-recycling"
            },
            new Startup
            {
                OwnerUserId = uStartPaper.Id,
                Name = "Nile Agro-Fiber Paper",
                Sector = Sector.Packaging,
                Stage = StartupStage.Prototype,
                GovernorateId = govs["Qalyubia"].Id,
                ProductIdea = "Biodegradable industrial packaging cardboard from sugarcane bagasse and agricultural residue",
                Description = "Upcycling sugarcane waste from Upper Egypt into eco-friendly molded pulp packaging.",
                FundingNeededEgp = 2500000,
                RequiredTagsCsv = "paper-converting"
            },
            new Startup
            {
                OwnerUserId = uStartLeather.Id,
                Name = "Kemet Crafted Leather",
                Sector = Sector.Textiles,
                Stage = StartupStage.Mvp,
                GovernorateId = govs["Cairo"].Id,
                ProductIdea = "Export-grade vegetable-tanned leather protective footwear and accessories",
                Description = "Partnering with Robbiki leather city tanneries for sustainable industrial safety footwear.",
                FundingNeededEgp = 800000,
                RequiredTagsCsv = "leather-processing"
            },
            new Startup
            {
                OwnerUserId = uStartSauces.Id,
                Name = "Qaha Gourmet Sauces",
                Sector = Sector.FoodProcessing,
                Stage = StartupStage.EarlyRevenue,
                GovernorateId = govs["Qalyubia"].Id,
                ProductIdea = "Premium ready-to-eat artisanal sauces and dips packaged in recyclable glass jars",
                Description = "Small-batch culinary sauces using locally sourced herbs and sun-ripened tomatoes.",
                FundingNeededEgp = 3000000,
                RequiredTagsCsv = "fruit-vegetable-processing,canning"
            }
        );

        // 7. Investors (4 profiles from DOCS/INDUVA_04_ROADMAP_AND_RISKS.md §4)
        db.InvestorProfiles.AddRange(
            new InvestorProfile
            {
                OwnerUserId = uInvNile.Id,
                DisplayName = "Nile Valley Agribusiness Ventures",
                SectorsCsv = "FoodProcessing,Packaging",
                MinTicketEgp = 500000,
                MaxTicketEgp = 3000000,
                PreferredStagesCsv = "Prototype,Mvp,EarlyRevenue",
                Bio = "Dedicated to agro-industrial processing, food packaging, and cold-chain scale-ups across the Delta region."
            },
            new InvestorProfile
            {
                OwnerUserId = uInvCairo.Id,
                DisplayName = "Cairo Industrial Growth Fund",
                SectorsCsv = "Plastics,Construction,Automotive",
                MinTicketEgp = 2000000,
                MaxTicketEgp = 10000000,
                PreferredStagesCsv = "EarlyRevenue,Growth",
                Bio = "Growth-stage private equity focusing on industrial manufacturing, polymers, and export substitution."
            },
            new InvestorProfile
            {
                OwnerUserId = uInvMfg.Id,
                DisplayName = "Misr Manufacturing Capital",
                SectorsCsv = "Textiles,Ceramics,Chemicals",
                MinTicketEgp = 5000000,
                MaxTicketEgp = 20000000,
                PreferredStagesCsv = "Growth",
                Bio = "Major industrial fund financing heavy manufacturing, textiles, and ceramics modernisation programs."
            },
            new InvestorProfile
            {
                OwnerUserId = uInvAngel.Id,
                DisplayName = "Delta Angel Syndicate",
                SectorsCsv = "Plastics,Packaging,Electronics",
                MinTicketEgp = 100000,
                MaxTicketEgp = 1000000,
                PreferredStagesCsv = "Idea,Prototype,Mvp",
                Bio = "Early-stage angel syndicate supporting innovative industrial tech and circular economy startups."
            }
        );

        await db.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string displayName,
        string role,
        string password)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, role))
                await userManager.AddToRoleAsync(existing, role);

            return existing;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName
        };

        var res = await userManager.CreateAsync(user, password);
        if (!res.Succeeded)
        {
            var err = string.Join("; ", res.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to seed user {email}: {err}");
        }

        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    private static Governorate G(string en, string ar, double lat, double lng) =>
        new() { NameEn = en, NameAr = ar, Latitude = lat, Longitude = lng };

    private static RawMaterial M(string en, string ar, string cat) =>
        new() { NameEn = en, NameAr = ar, Category = cat };
}
