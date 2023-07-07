using FluentAssertions;
using Xunit;

namespace ElmahCore.Mvc.Tests;

public class UserAgentHelperTests
{
    [Fact]
    public void IsMobileFor3Chars()
    {
        var x = UserAgentHelper.IsMobile("zzz");
        x.Should().Be(false);
    }
}