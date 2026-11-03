using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PenangGoApi.Services;
using Xunit;

namespace PenangGoApi.Tests;

public class ApiResponsesTests
{
    private static DefaultHttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider()
        };
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    [Fact]
    public async Task Problem_Returns400WithMessageAndField()
    {
        var result = ApiResponses.Problem("Name is required", field: "name");
        var httpContext = CreateHttpContext();

        await result.ExecuteAsync(httpContext);

        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);

        httpContext.Response.Body.Position = 0;
        using var doc = JsonDocument.Parse(httpContext.Response.Body);
        var root = doc.RootElement;
        Assert.Equal("Invalid request", root.GetProperty("title").GetString());
        Assert.Equal("Name is required", root.GetProperty("message").GetString());
        Assert.Equal("name", root.GetProperty("field").GetString());
    }

    [Fact]
    public async Task Problem_UsesProvidedStatusCode()
    {
        var result = ApiResponses.Problem("Event is full", StatusCodes.Status409Conflict);
        var httpContext = CreateHttpContext();

        await result.ExecuteAsync(httpContext);

        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
    }
}