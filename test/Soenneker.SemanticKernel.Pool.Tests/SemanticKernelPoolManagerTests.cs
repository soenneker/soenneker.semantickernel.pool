using Soenneker.SemanticKernel.Pool.Abstract;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.SemanticKernel.Pool.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public class SemanticKernelPoolManagerTests : HostedUnitTest
{
    private readonly ISemanticKernelPool _manager;

    public SemanticKernelPoolManagerTests(Host host) : base(host)
    {
        _manager = Resolve<ISemanticKernelPool>(true);
    }

    [Test]
    public void Default()
    {

    }
}
