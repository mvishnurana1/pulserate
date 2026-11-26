using System.Reflection;
using NetArchTest.Rules;

namespace PulseBoard.Architecture.Tests;

/// <summary>
/// Turns the architecture diagram into failing builds. If someone takes a shortcut across a
/// module boundary or a layer, these tests say exactly which types broke the rule.
/// </summary>
public sealed class ModuleBoundaryTests
{
    [Theory]
    [InlineData("PulseBoard.Monitoring.Domain")]
    [InlineData("PulseBoard.Incidents.Domain")]
    public void Domain_does_not_depend_on_persistence_or_web(string assemblyName)
    {
        var result = Types.InAssembly(Assembly.Load(assemblyName))
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Npgsql", "Microsoft.AspNetCore")
            .GetResult();

        AssertPassed(result);
    }

    [Theory]
    [InlineData("Monitoring")]
    [InlineData("Incidents")]
    public void Application_does_not_depend_on_infrastructure_or_web(string module)
    {
        var result = Types.InAssembly(Assembly.Load($"PulseBoard.{module}.Application"))
            .ShouldNot()
            .HaveDependencyOnAny($"PulseBoard.{module}.Infrastructure", "Npgsql", "Microsoft.AspNetCore")
            .GetResult();

        AssertPassed(result);
    }

    [Fact]
    public void Monitoring_does_not_depend_on_Incidents()
    {
        var result = Types.InAssemblies(ModuleAssemblies("Monitoring"))
            .ShouldNot()
            .HaveDependencyOn("PulseBoard.Incidents")
            .GetResult();

        AssertPassed(result);
    }

    [Fact]
    public void Incidents_only_sees_the_public_contracts_of_Monitoring()
    {
        var result = Types.InAssemblies(ModuleAssemblies("Incidents"))
            .ShouldNot()
            .HaveDependencyOnAny(
                "PulseBoard.Monitoring.Domain",
                "PulseBoard.Monitoring.Application",
                "PulseBoard.Monitoring.Infrastructure")
            .GetResult();

        AssertPassed(result);
    }

    [Theory]
    [InlineData("Monitoring")]
    [InlineData("Incidents")]
    public void Handlers_are_internal_to_their_module(string module)
    {
        var result = Types.InAssembly(Assembly.Load($"PulseBoard.{module}.Application"))
            .That()
            .HaveNameEndingWith("Handler")
            .Should()
            .NotBePublic()
            .GetResult();

        AssertPassed(result);
    }

    private static Assembly[] ModuleAssemblies(string module) =>
    [
        Assembly.Load($"PulseBoard.{module}.Contracts"),
        Assembly.Load($"PulseBoard.{module}.Domain"),
        Assembly.Load($"PulseBoard.{module}.Application"),
        Assembly.Load($"PulseBoard.{module}.Infrastructure"),
    ];

    private static void AssertPassed(TestResult result) =>
        Assert.True(
            result.IsSuccessful,
            $"Rule broken by: {string.Join(", ", result.FailingTypeNames ?? [])}");
}
