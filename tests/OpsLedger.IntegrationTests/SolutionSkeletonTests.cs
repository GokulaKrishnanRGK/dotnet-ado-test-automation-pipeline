using FluentAssertions;
using OpsLedger.Infrastructure;

namespace OpsLedger.IntegrationTests;

public sealed class SolutionSkeletonTests
{
    [Fact]
    public void Infrastructure_project_is_referenced()
    {
        typeof(AssemblyMarker).Namespace.Should().Be("OpsLedger.Infrastructure");
    }
}
