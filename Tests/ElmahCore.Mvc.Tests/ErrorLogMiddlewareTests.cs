using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Options = Microsoft.Extensions.Options.Options;
using Xunit;

namespace ElmahCore.Mvc.Tests;

public class ErrorLogMiddlewareTests
{
    private readonly RequestDelegate _requestDelegate;
    private readonly ErrorLog _errorLog;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOptions<ElmahOptions> _options;

    public ErrorLogMiddlewareTests()
    {
        _requestDelegate = _ => Task.CompletedTask;
        _options = Substitute.For<IOptions<ElmahOptions>>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _errorLog = new MemoryErrorLog();
    }

    [Fact]
    public void WhenInitMiddlewareSetsStaticExtension()
    {
        _ = new ErrorLogMiddleware(_requestDelegate, _errorLog, _loggerFactory, _options);
        ElmahExtensions.LogMiddleware.Should().NotBeNull();
    }

    [Fact]
    public void RiseErrorOkWhenMiddlewareInitialized()
    {
        _ = new ErrorLogMiddleware(_requestDelegate, _errorLog, _loggerFactory, _options);
        var act = async () => await ElmahExtensions.RaiseError(new DefaultHttpContext(), new Exception());
        act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogExceptionWithStatusCodeOverridesDefault()
    {
        var errorLog = new MemoryErrorLog();
        var options = Options.Create(new ElmahOptions());
        var middleware = new ErrorLogMiddleware(_requestDelegate, errorLog, _loggerFactory, options);
        var context = new DefaultHttpContext();

        var id = await middleware.LogException(
            new InvalidOperationException("Test"),
            context,
            (ctx, error) => Task.CompletedTask,
            statusCode: 404);

        id.Should().NotBeNullOrEmpty();
        var entry = errorLog.GetError(id);
        entry.Should().NotBeNull();
        entry.Error.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task LogExceptionWithStatusCodeAndCallbackOverridesDefault()
    {
        var errorLog = new MemoryErrorLog();
        var options = Options.Create(new ElmahOptions());
        var middleware = new ErrorLogMiddleware(_requestDelegate, errorLog, _loggerFactory, options);
        var context = new DefaultHttpContext();
        var callbackInvoked = false;

        var id = await middleware.LogException(
            new InvalidOperationException("Test"),
            context,
            (ctx, error) =>
            {
                callbackInvoked = true;
                return Task.CompletedTask;
            },
            statusCode: 422);

        id.Should().NotBeNullOrEmpty();
        var entry = errorLog.GetError(id);
        entry.Should().NotBeNull();
        entry.Error.StatusCode.Should().Be(422);
        callbackInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task LogExceptionWithoutStatusCodeUsesErrorDefault()
    {
        var errorLog = new MemoryErrorLog();
        var options = Options.Create(new ElmahOptions());
        var middleware = new ErrorLogMiddleware(_requestDelegate, errorLog, _loggerFactory, options);
        var context = new DefaultHttpContext();

        var id = await middleware.LogException(
            new InvalidOperationException("Test"),
            context,
            (ctx, error) => Task.CompletedTask);

        id.Should().NotBeNullOrEmpty();
        var entry = errorLog.GetError(id);
        entry.Should().NotBeNull();
        // With DefaultHttpContext (no connection), Error defaults to 0
        // With a real HTTP context, it would default to 500
        entry.Error.StatusCode.Should().Be(0);
    }
}