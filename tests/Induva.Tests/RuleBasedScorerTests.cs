using Induva.Application.AI.Scoring;
using Induva.Domain.Enums;
using Xunit;

namespace Induva.Tests;

public class RuleBasedScorerTests
{
    [Fact]
    public void FactoryToSupplier_calculates_coverage_and_proximity()
    {
        // Factory in Cairo (30.04, 31.23) needs materials {1, 2}
        var factory = new GeoPoint(30.04, 31.23);
        var factoryNeeds = new HashSet<int> { 1, 2 };

        // Supplier 1 in Giza (~10 km away) provides {1, 2} (100% coverage)
        var supNear = new GeoPoint(30.01, 31.21);
        var sup1Materials = new HashSet<int> { 1, 2 };

        var scoreNear = RuleBasedScorer.FactoryToSupplier(factoryNeeds, factory, sup1Materials, supNear);

        // Supplier 2 in Aswan (~700 km away) provides only {1} (50% coverage)
        var supFar = new GeoPoint(24.08, 32.89);
        var sup2Materials = new HashSet<int> { 1 };

        var scoreFar = RuleBasedScorer.FactoryToSupplier(factoryNeeds, factory, sup2Materials, supFar);

        // Score near should be significantly higher than far
        Assert.True(scoreNear > 90.0);
        Assert.True(scoreFar < 50.0);
    }

    [Fact]
    public void FactoryToDemand_evaluates_tag_overlap_and_proximity()
    {
        var factory = new GeoPoint(30.04, 31.23);
        var factoryTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "injection-molding", "assembly" };

        var buyerNear = new GeoPoint(30.05, 31.24);
        var demandTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "injection-molding" };

        var score = RuleBasedScorer.FactoryToDemand(
            factoryTags, factory, demandTags, buyerNear,
            monthlyCapacityHint: 10000, requestedQuantity: 5000, deadlineUtc: DateTime.UtcNow.AddDays(45));

        Assert.True(score > 70.0);
    }

    [Fact]
    public void FactoryToInvestor_rewards_sector_match_and_idle_capacity()
    {
        // Plastics factory with 40% utilization (60% idle)
        var scoreMatched = RuleBasedScorer.FactoryToInvestor(
            Sector.Plastics, utilizationPercent: 40, hasCurrentIdleWindow: true,
            investorSectors: new HashSet<Sector> { Sector.Plastics, Sector.Chemicals });

        var scoreUnmatched = RuleBasedScorer.FactoryToInvestor(
            Sector.Plastics, utilizationPercent: 95, hasCurrentIdleWindow: false,
            investorSectors: new HashSet<Sector> { Sector.Textiles });

        Assert.True(scoreMatched > 85.0);
        Assert.True(scoreUnmatched < 15.0);
    }

    [Fact]
    public void StartupToFactory_rewards_tag_overlap_and_available_capacity()
    {
        var startup = new GeoPoint(30.04, 31.23);
        var startupTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "cnc-machining" };

        var factory = new GeoPoint(30.06, 31.25);
        var factoryTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "cnc-machining", "welding" };

        var score = RuleBasedScorer.StartupToFactory(
            startupTags, startup, factoryTags, factory, factoryUtilization: 45, hasIdleWindow: true);

        Assert.True(score > 85.0);
    }

    [Fact]
    public void StartupToInvestor_evaluates_sector_stage_and_ticket_fit()
    {
        var scorePerfect = RuleBasedScorer.StartupToInvestor(
            Sector.Packaging,
            StartupStage.Prototype,
            fundingNeededEgp: 500000,
            investorSectors: new HashSet<Sector> { Sector.Packaging },
            investorStages: new HashSet<StartupStage> { StartupStage.Prototype },
            minTicketEgp: 200000,
            maxTicketEgp: 1000000);

        var scorePoor = RuleBasedScorer.StartupToInvestor(
            Sector.Plastics,
            StartupStage.Idea,
            fundingNeededEgp: 10000000, // Exceeds max ticket
            investorSectors: new HashSet<Sector> { Sector.FoodProcessing },
            investorStages: new HashSet<StartupStage> { StartupStage.Growth },
            minTicketEgp: 200000,
            maxTicketEgp: 2000000);

        Assert.Equal(100.0, scorePerfect);
        Assert.True(scorePoor <= 20.0);
    }
}

