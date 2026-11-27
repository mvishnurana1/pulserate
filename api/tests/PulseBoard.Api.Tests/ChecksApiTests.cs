using System.Net;
using System.Net.Http.Json;
using PulseBoard.Api.Tests.Infrastructure;
using PulseBoard.Monitoring.Application.Checks;

namespace PulseBoard.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class ChecksApiTests(PulseBoardApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Creating_a_check_returns_201_with_its_location()
    {
        var url = TestHelpers.UniqueUrl();

        var response = await _client.PostAsJsonAsync("/api/checks", new { name = "Homepage", url, intervalSeconds = 300, failureThreshold = 2 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var check = await response.Content.ReadFromJsonAsync<CheckResponse>();
        Assert.NotNull(check);
        Assert.Equal("Homepage", check.Name);
        Assert.Equal("Pending", check.Status);
        Assert.Equal(300, check.IntervalSeconds);
        Assert.Equal($"/api/checks/{check.Id}", response.Headers.Location?.AbsolutePath, ignoreCase: true);
    }

    [Theory]
    [InlineData("ftp://example.com", 60, "Check.InvalidUrl")]
    [InlineData("not a url", 60, "Check.InvalidUrl")]
    [InlineData("https://example.com", 5, "Check.IntervalOutOfRange")]
    public async Task Invalid_input_returns_400_with_the_error_code(string url, int intervalSeconds, string expectedCode)
    {
        var response = await _client.PostAsJsonAsync("/api/checks", new { name = "Bad", url, intervalSeconds, failureThreshold = 3 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(expectedCode, await response.ReadErrorCodeAsync());
    }

    [Fact]
    public async Task Unknown_check_returns_404_problem()
    {
        var response = await _client.GetAsync($"/api/checks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Check.NotFound", await response.ReadErrorCodeAsync());
    }

    [Fact]
    public async Task Pausing_twice_returns_409_and_resume_brings_it_back()
    {
        var check = await _client.CreateCheckAsync();

        var first = await _client.PostAsync($"/api/checks/{check.Id}/pause", null);
        var second = await _client.PostAsync($"/api/checks/{check.Id}/pause", null);
        var resumed = await _client.PostAsync($"/api/checks/{check.Id}/resume", null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("Check.AlreadyPaused", await second.ReadErrorCodeAsync());
        Assert.Equal("Pending", (await resumed.Content.ReadFromJsonAsync<CheckResponse>())?.Status);
    }

    [Fact]
    public async Task Update_changes_the_settings_and_delete_removes_the_check()
    {
        var check = await _client.CreateCheckAsync();

        var update = await _client.PutAsJsonAsync($"/api/checks/{check.Id}", new
        {
            name = "Renamed",
            url = check.Url,
            intervalSeconds = 900,
            failureThreshold = 5,
        });
        var updated = await update.Content.ReadFromJsonAsync<CheckResponse>();

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Renamed", updated?.Name);
        Assert.Equal(900, updated?.IntervalSeconds);

        var delete = await _client.DeleteAsync($"/api/checks/{check.Id}");
        var afterDelete = await _client.GetAsync($"/api/checks/{check.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task Running_a_check_records_a_result_in_its_history()
    {
        var check = await _client.CreateCheckAsync();

        var run = await factory.RunCheckAsync(check.Id);
        var results = await _client.GetFromJsonAsync<List<CheckResultResponse>>($"/api/checks/{check.Id}/results");
        var refreshed = await _client.GetFromJsonAsync<CheckResponse>($"/api/checks/{check.Id}");

        Assert.True(run.IsSuccess);
        var result = Assert.Single(results!);
        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("Up", refreshed?.Status);
        Assert.NotNull(refreshed?.LastCheckedAt);
    }
}
