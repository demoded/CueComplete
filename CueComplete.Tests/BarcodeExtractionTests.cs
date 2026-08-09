using System.Net;
using System.Text.Json;
using CueComplete.Core;
using CueComplete.Core.Metadata;
using Xunit;

namespace CueComplete.Tests;

public class BarcodeExtractionTests
{
    [Theory]
    [InlineData("075992388422", true)]
    [InlineData("0 7599-23884-2 2", true)]
    [InlineData("5099909462228", true)]
    [InlineData("602537863170", true)]
    [InlineData("Matrix / Runout: 1 23884-2 SRC+02", false)]
    [InlineData("IFPI L551", false)]
    [InlineData("Mastering SID Code: IFPI L123", false)]
    [InlineData("Mould SID Code: IFPI 94K2", false)]
    [InlineData("JASRAC R-123456", false)]
    [InlineData("Short12", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Test_IsValidBarcodeCandidate(string? input, bool expected)
    {
        bool actual = DiscogsProvider.IsValidBarcodeCandidate(input);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task Test_FetchAndApplyDiscogsDataAsync_ExtractsOnlyBarcodeIdentifier()
    {
        // Arrange: Fake Discogs release JSON response containing Matrix / Runout AND Barcode identifiers
        var releaseJson = @"
        {
            ""title"": ""Bad Reputation"",
            ""country"": ""US"",
            ""released"": ""1981"",
            ""identifiers"": [
                { ""type"": ""Matrix / Runout"", ""value"": ""1 23884-2 SRC+02"" },
                { ""type"": ""Mastering SID Code"", ""value"": ""IFPI L551"" },
                { ""type"": ""Barcode"", ""value"": ""075992388422"" }
            ]
        }";

        var handler = new MockHttpMessageHandler(releaseJson);
        var httpClient = new HttpClient(handler);
        var provider = new DiscogsProvider("key", "secret", "token", httpClient);
        var cueData = new CueData();

        // Act
        await provider.FetchAndApplyDiscogsDataAsync(cueData, "https://api.discogs.com/releases/12345", isDirectReleaseUrl: true);

        // Assert
        Assert.Equal("075992388422", cueData.Barcode);
    }

    [Fact]
    public async Task Test_PerformDiscogsTextSearchAsync_FiltersOutMatrixRunoutInSearchResults()
    {
        // Arrange: Fake Discogs search result JSON where barcode array contains Matrix/Runout first
        var searchJson = @"
        {
            ""results"": [
                {
                    ""title"": ""Joan Jett - Bad Reputation"",
                    ""country"": ""US"",
                    ""catno"": ""VICP-5173"",
                    ""barcode"": [
                        ""Matrix / Runout: 1 23884-2 SRC+02"",
                        ""075992388422""
                    ]
                }
            ]
        }";

        var handler = new MockHttpMessageHandler(searchJson);
        var httpClient = new HttpClient(handler);
        var provider = new DiscogsProvider("key", "secret", "token", httpClient);
        var results = new List<CueData>();

        // Act
        await provider.PerformDiscogsTextSearchAsync("Bad Reputation", new CueData { Artist = "Joan Jett" }, results);

        // Assert
        Assert.NotEmpty(results);
        Assert.Equal("075992388422", results[0].Barcode);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseContent;

        public MockHttpMessageHandler(string responseContent)
        {
            _responseContent = responseContent;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseContent)
            };
            return Task.FromResult(response);
        }
    }
}
