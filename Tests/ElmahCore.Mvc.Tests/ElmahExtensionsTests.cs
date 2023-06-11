﻿using System;
using System.Threading.Tasks;
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
            var httpContext = new DefaultHttpContext();
            var act = async () => await ElmahExtensions.RaiseError(httpContext, new Exception());
            act.Should().ThrowAsync<MiddlewareNotInitializedException>();
        }
    }
}