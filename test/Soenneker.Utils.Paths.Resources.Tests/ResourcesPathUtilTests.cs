using Soenneker.Tests.HostedUnit;

namespace Soenneker.Utils.Paths.Resources.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class ResourcesPathUtilTests : HostedUnitTest
{
    public ResourcesPathUtilTests(Host host) : base(host)
    {
    }

    [Test]
    public void Default()
    {

    }
}
