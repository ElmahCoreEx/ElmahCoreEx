using System;
using FluentAssertions;
using Xunit;

namespace ElmahCore.Tests;

public class ErrorTests
{
    [Fact]
    public void Error_DefaultConstructor_InitializesWithDefaults()
    {
        var error = new Error();

        error.Message.Should().BeEmpty();
        error.Type.Should().BeEmpty();
        error.Source.Should().BeEmpty();
        error.Detail.Should().BeEmpty();
        error.ApplicationName.Should().BeEmpty();
        error.StatusCode.Should().Be(0);
        error.Exception.Should().BeNull();
    }

    [Fact]
    public void Error_FromException_CapturesExceptionDetails()
    {
        var exception = new InvalidOperationException("Test error message");

        var error = new Error(exception);

        error.Message.Should().Be("Test error message");
        error.Type.Should().Be("System.InvalidOperationException");
        error.Exception.Should().Be(exception);
        error.Detail.Should().Contain("Test error message");
    }

    [Fact]
    public void Error_FromNestedExceptions_UsesBaseException()
    {
        var innerException = new ArgumentException("Inner error");
        var outerException = new InvalidOperationException("Outer error", innerException);

        var error = new Error(outerException);

        error.Message.Should().Be("Inner error");
        error.Type.Should().Be("System.ArgumentException");
    }

    [Fact]
    public void Error_Clone_CreatesIndependentCopy()
    {
        var original = new Error(new InvalidOperationException("Original error"));
        original.ApplicationName = "TestApp";
        original.ServerVariables.Add("Key1", "Value1");
        original.QueryString.Add("param", "value");

        var clone = original.Clone();

        clone.Should().NotBeSameAs(original);
        clone.Message.Should().Be(original.Message);
        clone.Type.Should().Be(original.Type);
        clone.ApplicationName.Should().Be(original.ApplicationName);
        clone.ServerVariables["Key1"].Should().Be("Value1");
        clone.QueryString["param"].Should().Be("value");
    }

    [Fact]
    public void Error_Clone_ModifyingCloneDoesNotAffectOriginal()
    {
        var original = new Error();
        original.ServerVariables.Add("Key1", "Value1");

        var clone = original.Clone();
        clone.ServerVariables.Add("Key2", "Value2");

        original.ServerVariables["Key2"].Should().BeNull();
        clone.ServerVariables["Key2"].Should().Be("Value2");
    }

    [Fact]
    public void Error_HostName_HasDefaultValue()
    {
        var error = new Error();

        // On most platforms, the hostname should be available
        // but allow for edge cases where it might not be
        if (!string.IsNullOrEmpty(Environment.MachineName))
        {
            error.HostName.Should().NotBeNullOrEmpty();
        }
        else
        {
            error.HostName.Should().NotBeNull();
        }
    }

    [Fact]
    public void Error_Time_IsSetOnConstruction()
    {
        var before = DateTime.Now;
        var error = new Error(new Exception("Test"));
        var after = DateTime.Now;

        error.Time.Should().BeOnOrAfter(before);
        error.Time.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Error_ToString_ReturnsMessage()
    {
        var error = new Error(new InvalidOperationException("Test message"));

        error.ToString().Should().Be("Test message");
    }

    [Fact]
    public void Error_Collections_AreLazilyInitialized()
    {
        var error = new Error();

        error.ServerVariables.Should().NotBeNull();
        error.QueryString.Should().NotBeNull();
        error.Form.Should().NotBeNull();
        error.Cookies.Should().NotBeNull();
    }

    [Fact]
    public void Error_MessageLogs_AreInitializedEmpty()
    {
        var error = new Error();

        error.MessageLog.Should().NotBeNull();
        error.MessageLog.Should().BeEmpty();
        error.SqlLog.Should().NotBeNull();
        error.SqlLog.Should().BeEmpty();
        error.Params.Should().NotBeNull();
        error.Params.Should().BeEmpty();
    }

    [Fact]
    public void Error_ApplicationName_CanBeSetOnce()
    {
        var error = new Error();
        error.ApplicationName = "MyApp";

        error.ApplicationName.Should().Be("MyApp");
    }

    [Fact]
    public void Error_Properties_AreSettable()
    {
        var error = new Error
        {
            Message = "Custom message",
            Type = "CustomType",
            Source = "CustomSource",
            Detail = "Custom detail",
            User = "TestUser",
            StatusCode = 500,
            WebHostHtmlMessage = "<html>Error</html>"
        };

        error.Message.Should().Be("Custom message");
        error.Type.Should().Be("CustomType");
        error.Source.Should().Be("CustomSource");
        error.Detail.Should().Be("Custom detail");
        error.User.Should().Be("TestUser");
        error.StatusCode.Should().Be(500);
        error.WebHostHtmlMessage.Should().Be("<html>Error</html>");
    }

    [Fact]
    public void Error_FromNullException_HandlesGracefully()
    {
        var error = new Error(null);

        error.Message.Should().BeEmpty();
        error.Type.Should().BeEmpty();
        error.Exception.Should().BeNull();
    }
}