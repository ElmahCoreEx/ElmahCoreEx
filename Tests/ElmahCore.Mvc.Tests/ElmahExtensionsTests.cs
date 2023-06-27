using System;
using ElmahCore.Mvc.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace ElmahCore.Mvc.Tests
{
    public class ElmahExtensionsTests
    {
        [Fact]
        public void RiseErrorExceptionWhenMiddlewareNotInitialised()
        {
            var act = async () => await ElmahExtensions.RaiseError(new DefaultHttpContext(), new Exception());
            act.Should().ThrowAsync<MiddlewareNotInitializedException>();
        }
    }
}