using System;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace ElmahCore.Tests;

public class LogAllXmlTests
{
    [Fact]
    public void ElmahOptions_LogAllXml_DefaultsToTrue()
    {
        var options = new ElmahOptions();

        options.LogAllXml.Should().BeTrue();
    }

    [Fact]
    public void ElmahOptions_LogAllXml_CanBeSetToFalse()
    {
        var options = new ElmahOptions();

        options.LogAllXml = false;

        options.LogAllXml.Should().BeFalse();
    }
}
