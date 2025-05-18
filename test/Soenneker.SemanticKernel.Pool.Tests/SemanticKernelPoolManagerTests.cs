using Soenneker.SemanticKernel.Pool.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.SemanticKernel.Pool.Tests;

[Collection("Collection")]
public class SemanticKernelPoolManagerTests : FixturedUnitTest
{
    private readonly IKernelPoolManager _manager;

    public SemanticKernelPoolManagerTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _manager = Resolve<IKernelPoolManager>(true);
    }

    [Fact]
    public void Default()
    {

    }
}
