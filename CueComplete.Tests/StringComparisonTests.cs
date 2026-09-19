using System.Net;
using System.Text;
using CueComplete.Core;
using CueComplete.Core.Metadata;
using Xunit;

namespace CueComplete.Tests;

public class StringComparisonTests
{
    [Theory]
    [InlineData("Черный Кофе", "Чёрный Кофе", true)]
    [InlineData("Чёрный Кофе", "Черный Кофе", true)]
    [InlineData("Черный Кофе", "чёрный", true)]
    [InlineData("Чёрный Кофе", "черный", true)]
    [InlineData("Motörhead", "motorhead", true)]
    [InlineData("Motorhead", "motörhead", true)]
    [InlineData("Beyoncé", "beyonce", true)]
    [InlineData("Beyonce", "beyoncé", true)]
    [InlineData("Mötley Crüe", "motley crue", true)]
    [InlineData("Sigur Rós", "sigur ros", true)]
    [InlineData("Metallica", "Megadeth", false)]
    [InlineData("Joan Jett", "Daevid Allen", false)]
    public void ContainsIgnoreCaseAndDiacritics_EvaluatesCorrectly(string source, string target, bool expected)
    {
        var result = source.ContainsIgnoreCaseAndDiacritics(target);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Чёрный Кофе", "Черный Кофе", true)]
    [InlineData("Черный Кофе", "Чёрный Кофе", true)]
    [InlineData("Чёрный Кофе", "Черный Кофе = Black Coffee", true)]
    [InlineData("Черный Кофе = Black Coffee", "Чёрный Кофе", true)]
    [InlineData("Joan Jett & the Blackhearts", "Joan Jett", true)]
    [InlineData("Joan Jett", "Joan Jett & the Blackhearts", true)]
    [InlineData("Motörhead", "Motorhead", true)]
    [InlineData("Daevid Allen", "Joan Jett", false)]
    [InlineData(null, "Черный Кофе", false)]
    [InlineData("Черный Кофе", null, false)]
    [InlineData("", "", false)]
    public void MatchesArtist_EvaluatesCorrectly(string? candidate, string? target, bool expected)
    {
        var result = StringExtensions.MatchesArtist(candidate, target);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task DiscogsProvider_CatNoSearch_DoesNotFilterOut_CyrillicYoDifference()
    {
        // Simulate Discogs search returning release 28920571 with artist "Чёрный Кофе"
        // while sourceData has artist "Черный Кофе" (with 'е')
        var searchJson = """
        {
            "pagination": { "page": 1, "pages": 1, "per_page": 50, "items": 1, "urls": {} },
            "results": [
                {
                    "country": "Russia",
                    "year": "2023",
                    "format": ["CD", "Album", "Reissue", "Remastered"],
                    "label": ["Moroz Records"],
                    "type": "release",
                    "id": 28920571,
                    "catno": "MR 23143 CD",
                    "title": "Чёрный Кофе - Переступи Порог",
                    "resource_url": "https://api.discogs.com/releases/28920571"
                }
            ]
        }
        """;

        var releaseJson = """
        {
            "id": 28920571,
            "title": "Переступи Порог",
            "year": 2023,
            "artists": [
                {
                    "name": "Чёрный Кофе",
                    "anv": "",
                    "id": 484543
                }
            ],
            "labels": [
                {
                    "name": "Moroz Records",
                    "catno": "MR 23143 CD"
                }
            ]
        }
        """;

        var handler = new TestHttpMessageHandler((request) =>
        {
            if (request.RequestUri!.ToString().Contains("releases/28920571"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(releaseJson, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(searchJson, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler);
        var provider = new DiscogsProvider("fakeKey", "fakeSecret", "fakeToken", httpClient);

        var sourceData = new CueData
        {
            Artist = "Черный Кофе", // With 'е'
            Album = "Переступи Порог",
            CatalogNumber = "MR 23143 CD"
        };

        var results = new List<CueData>();
        await provider.PerformDiscogsCatNoSearchAsync(sourceData.CatalogNumber, sourceData, results);

        Assert.Single(results);
        Assert.Equal("Чёрный Кофе", results[0].Artist);
        Assert.Equal("MR 23143 CD", results[0].CatalogNumber);
    }

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }
}
