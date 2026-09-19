using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
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

    [Fact]
    public async Task InvokeAsyncDoesNotThrowWhenBodyIsShorterThanDeclaredContentLength()
    {
        var errorLog = new MemoryErrorLog();
        var options = Options.Create(new ElmahOptions { LogRequestBody = true });
        var middleware = new ErrorLogMiddleware(_requestDelegate, errorLog, _loggerFactory, options);

        var context = new DefaultHttpContext();
        var actualBody = Encoding.UTF8.GetBytes("field=short");
        context.Request.ContentType = "application/x-www-form-urlencoded";
        // Simulates a truncated/malformed request (e.g. a bot that disconnects mid-body):
        // the client declares more bytes than it actually sends.
        context.Request.ContentLength = actualBody.Length + 100;
        context.Request.Body = new MemoryStream(actualBody);

        var act = async () => await middleware.InvokeAsync(context);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InvokeAsyncCapturesTruncatedBodyInErrorInsteadOfThrowingEndOfStreamException()
    {
        var errorLog = new MemoryErrorLog();
        var options = Options.Create(new ElmahOptions { LogRequestBody = true });
        var actualBody = Encoding.UTF8.GetBytes("field=short");
        RequestDelegate throwingDelegate = _ => throw new InvalidOperationException("boom");
        var middleware = new ErrorLogMiddleware(throwingDelegate, errorLog, _loggerFactory, options);

        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.ContentLength = actualBody.Length + 100;
        context.Request.Body = new MemoryStream(actualBody);

        var act = async () => await middleware.InvokeAsync(context);

        await act.Should().ThrowAsync<InvalidOperationException>();

        var entries = new List<ErrorLogEntry>();
        errorLog.GetErrors(0, 10, entries);
        entries.Should().ContainSingle();
        entries[0].Error.Body.Should().Be(Encoding.UTF8.GetString(actualBody));
    }

    [Fact]
    public async Task InvokeAsyncDoesNotLogErrorWhenBodyReadThrowsIOException()
    {
        // Kestrel's BadHttpRequestException ("Unexpected end of request content") and
        // ConnectionResetException both derive from IOException.
        var errorLog = await InvokeWithUnreadableBody(new ThrowingStream(new IOException("Connection reset")));

        await errorLog.DidNotReceiveWithAnyArgs().LogAsync(default, default);
    }

    [Fact]
    public async Task InvokeAsyncDoesNotLogErrorWhenRequestIsAbortedDuringBodyRead()
    {
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();

        var errorLog = await InvokeWithUnreadableBody(new MemoryStream(Encoding.UTF8.GetBytes("field=value")),
            aborted.Token);

        await errorLog.DidNotReceiveWithAnyArgs().LogAsync(default, default);
    }

    // MemoryErrorLog shares its entries across instances, so use a substitute to observe logging.
    private async Task<ErrorLog> InvokeWithUnreadableBody(Stream body, CancellationToken requestAborted = default)
    {
        var errorLog = Substitute.For<ErrorLog>();
        var options = Options.Create(new ElmahOptions { LogRequestBody = true });
        var middleware = new ErrorLogMiddleware(_requestDelegate, errorLog, _loggerFactory, options);

        var context = new DefaultHttpContext { RequestAborted = requestAborted };
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.ContentLength = 5000;
        context.Request.Body = body;

        var act = async () => await middleware.InvokeAsync(context);
        await act.Should().NotThrowAsync();

        return errorLog;
    }

    // Simulates a Kestrel request body whose client disconnected mid-body.
    private sealed class ThrowingStream(Exception exception) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => throw exception;

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            ValueTask.FromException<int>(exception);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            Task.FromException<int>(exception);

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}