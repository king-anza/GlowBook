using GlowBook.Domain;

namespace GlowBook.UnitTests;

public sealed class ArchitectureTests
{
    [Fact]
    public void DomainAssemblyHasExpectedName()
    {
        Assert.Equal("GlowBook.Domain", typeof(AssemblyReference).Assembly.GetName().Name);
    }
}
