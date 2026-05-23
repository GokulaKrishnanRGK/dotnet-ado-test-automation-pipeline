using FluentAssertions;
using OpsLedger.Core;

namespace OpsLedger.UnitTests;

public sealed class SolutionSkeletonTests
{
    [Fact]
    public void Core_project_is_referenced()
    {
        typeof(AssemblyMarker).Namespace.Should().Be("OpsLedger.Core");
    }
}
