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
    [InlineData("Flotsam & Jetsam", "Flotsam and Jetsam", true)]
    [InlineData("Flotsam and Jetsam", "Flotsam & Jetsam", true)]
    [InlineData("Simon & Garfunkel", "Simon and Garfunkel", true)]
    [InlineData("Kool & The Gang", "Kool and the Gang", true)]
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

    [Theory]
    [InlineData("When The Storm Comes Down (Japan)", "When The Storm Comes Down")]
    [InlineData("When The Storm Comes Down [Japan]", "When The Storm Comes Down")]
    [InlineData("When The Storm Comes Down (Japan) (1990)", "When The Storm Comes Down")]
    [InlineData("When The Storm Comes Down (Japan) [Remastered]", "When The Storm Comes Down")]
    [InlineData("When The Storm Comes Down - (Japan)", "When The Storm Comes Down")]
    [InlineData("Album Title (Deluxe Edition)", "Album Title")]
    [InlineData("Album Title [Bonus Tracks]", "Album Title")]
    [InlineData("(Untitled)", "(Untitled)")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void StripTitleAnnotations_StripsTrailingAnnotations(string? input, string expected)
    {
        var result = StringExtensions.StripTitleAnnotations(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("JP", "Japan", true)]
    [InlineData("Japan", "JP", true)]
    [InlineData("US", "United States", true)]
    [InlineData("UK", "GB", true)]
    [InlineData("Europe", "XE", true)]
    [InlineData("Germany", "DE", true)]
    [InlineData("JP", "US", false)]
    [InlineData(null, "Japan", false)]
    public void MatchesCountry_EvaluatesCorrectly(string? country1, string? country2, bool expected)
    {
        var result = StringExtensions.MatchesCountry(country1, country2);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MatchesSourceCountry_MatchesCountryFromAlbumAnnotation()
    {
        var sourceData = new CueData
        {
            Artist = "Flotsam & Jetsam",
            Album = "When The Storm Comes Down (Japan)"
        };

        Assert.True(StringExtensions.MatchesSourceCountry("JP", sourceData));
        Assert.True(StringExtensions.MatchesSourceCountry("Japan", sourceData));
        Assert.False(StringExtensions.MatchesSourceCountry("US", sourceData));
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

    [Fact]
    public async Task DiscogsProvider_SearchAsync_FallsBackToTextSearch_AndStrippedTitle_WhenCatNoReturnsEmpty()
    {
        var releaseJson = """
        {
            "id": 7131871,
            "title": "When The Storm Comes Down",
            "year": 1990,
            "artists": [
                {
                    "name": "Flotsam And Jetsam",
                    "anv": "",
                    "id": 106065
                }
            ],
            "labels": [
                {
                    "name": "MCA Records",
                    "catno": "WMC5-78"
                }
            ]
        }
        """;

        var emptySearchJson = """{ "pagination": { "page": 1, "pages": 1, "per_page": 50, "items": 0, "urls": {} }, "results": [] }""";
        var matchedSearchJson = """
        {
            "pagination": { "page": 1, "pages": 1, "per_page": 50, "items": 1, "urls": {} },
            "results": [
                {
                    "country": "Japan",
                    "year": 1990,
                    "id": 7131871,
                    "catno": "WMC5-78",
                    "title": "Flotsam And Jetsam - When The Storm Comes Down",
                    "resource_url": "https://api.discogs.com/releases/7131871"
                }
            ]
        }
        """;

        var handler = new TestHttpMessageHandler((request) =>
        {
            var uri = request.RequestUri!.ToString();
            if (uri.Contains("releases/7131871"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(releaseJson, Encoding.UTF8, "application/json")
                };
            }

            // If searching with original title with (Japan), return 0 results
            if (uri.Contains("Japan"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(emptySearchJson, Encoding.UTF8, "application/json")
                };
            }

            // Stripped title search matches!
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(matchedSearchJson, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler);
        var provider = new DiscogsProvider("fakeKey", "fakeSecret", "fakeToken", httpClient);

        var sourceData = new CueData
        {
            Artist = "Flotsam & Jetsam",
            Album = "When The Storm Comes Down (Japan)",
            CatalogNumber = "WMCP-78" // Mismatched catno that returns nothing
        };

        var results = await provider.SearchAsync(sourceData);

        Assert.Single(results);
        Assert.Equal("Flotsam And Jetsam", results[0].Artist);
        Assert.Equal("When The Storm Comes Down", results[0].Album);
        Assert.Equal("WMC5-78", results[0].CatalogNumber);
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
