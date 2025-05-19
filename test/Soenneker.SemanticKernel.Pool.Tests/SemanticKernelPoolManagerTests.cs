using Soenneker.SemanticKernel.Pool.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.SemanticKernel.Pool.Tests;

[Collection("Collection")]
public class SemanticKernelPoolManagerTests : FixturedUnitTest
{
    private readonly ISemanticKernelPool _manager;

    public SemanticKernelPoolManagerTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _manager = Resolve<ISemanticKernelPool>(true);
    }

    [Fact]
    public void Default()
    {

    }
}
