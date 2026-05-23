using FluentAssertions;
using OpsLedger.Core;

namespace OpsLedger.BddTests;

public sealed class SolutionSkeletonTests
{
    [Fact]
    public void Bdd_project_can_reference_core_domain()
    {
        typeof(AssemblyMarker).Assembly.GetName().Name.Should().Be("OpsLedger.Core");
    }
}
