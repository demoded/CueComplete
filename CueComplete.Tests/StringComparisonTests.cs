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
    [InlineData("MP", "Metal Priest", true)]
    [InlineData("Metal Priest", "MP", true)]
    [InlineData("MP (22)", "Metal Priest", true)]
    [InlineData("Metal Priest", "MP (22)", true)]
    [InlineData("M.P.", "Metal Priest", true)]
    [InlineData("Metal Priest", "M.P.", true)]
    [InlineData("ELO", "Electric Light Orchestra", true)]
    [InlineData("Electric Light Orchestra", "ELO", true)]
    [InlineData("OMD", "Orchestral Manoeuvres in the Dark", true)]
    [InlineData("BOC", "Blue Öyster Cult", true)]
    [InlineData("ELP", "Emerson, Lake & Palmer", true)]
    [InlineData("ЧК", "Чёрный Кофе", true)]
    [InlineData("MP", "Michael Jackson", false)]
    [InlineData("MP", "Madonna", false)]
    [InlineData("War", "We Are Right", false)]
    [InlineData("Metallica", "Megadeth", false)]
    [InlineData("Judas Priest", "Metal Priest", false)]
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
    [InlineData("Metal Priest", "MP")]
    [InlineData("Electric Light Orchestra", "ELO")]
    [InlineData("Orchestral Manoeuvres in the Dark", "OMD")]
    [InlineData("Crosby, Stills, Nash & Young", "CSNY")]
    [InlineData("Blue Öyster Cult", "BÖC")]
    [InlineData("Madonna", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void GetAcronym_EvaluatesCorrectly(string? input, string? expected)
    {
        var result = StringExtensions.GetAcronym(input);
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
    [InlineData("Get It Now", "Get It Now", true)]
    [InlineData("Get It Now [Unofficial]", "Get It Now", true)]
    [InlineData("Get It Now", "Get It Now (Deluxe)", true)]
    [InlineData("Everything's Eventual", "Everything's Eventual (2003)", true)]
    [InlineData("When The Storm Comes Down (Japan)", "When The Storm Comes Down", true)]
    [InlineData("Rock & Roll", "Rock and Roll", true)]
    [InlineData("Get It Now", "Yo Hey! The Bass", false)]
    [InlineData("Get It Now", "Left Of Blue", false)]
    [InlineData(null, "Get It Now", false)]
    [InlineData("Get It Now", null, false)]
    public void MatchesAlbum_EvaluatesCorrectly(string? candidate, string? target, bool expected)
    {
        var result = StringExtensions.MatchesAlbum(candidate, target);
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
    public async Task DiscogsProvider_CatNoSearch_FindsMatchBeyondFirst5Items()
    {
        // Simulate Discogs search returning 11 results where items 0-9 are other artists and item 10 is the target match
        var searchJson = """
        {
            "pagination": { "page": 1, "pages": 1, "per_page": 50, "items": 11, "urls": {} },
            "results": [
                { "id": 1, "title": "Artist One - Album 1", "catno": "MOG002", "resource_url": "https://api.discogs.com/releases/1" },
                { "id": 2, "title": "Artist Two - Album 2", "catno": "SMOG002", "resource_url": "https://api.discogs.com/releases/2" },
                { "id": 3, "title": "Artist Three - Album 3", "catno": "MOG 002", "resource_url": "https://api.discogs.com/releases/3" },
                { "id": 4, "title": "Artist Four - Album 4", "catno": "SMOG 002", "resource_url": "https://api.discogs.com/releases/4" },
                { "id": 5, "title": "Artist Five - Album 5", "catno": "SMOG 002", "resource_url": "https://api.discogs.com/releases/5" },
                { "id": 6, "title": "Artist Six - Album 6", "catno": "SMOG 002", "resource_url": "https://api.discogs.com/releases/6" },
                { "id": 7, "title": "Artist Seven - Album 7", "catno": "SMOG002LP", "resource_url": "https://api.discogs.com/releases/7" },
                { "id": 8, "title": "Artist Eight - Album 8", "catno": "MOG 001", "resource_url": "https://api.discogs.com/releases/8" },
                { "id": 9, "title": "Artist Nine - Album 9", "catno": "12\" MOG 001", "resource_url": "https://api.discogs.com/releases/9" },
                { "id": 10, "title": "Artist Ten - Album 10", "catno": "MOG-002", "resource_url": "https://api.discogs.com/releases/10" },
                {
                    "id": 26057443,
                    "title": "Marco Garau's Magic Opera - Battle Of Ice",
                    "catno": "MOG002",
                    "country": "Italy",
                    "year": "2023",
                    "resource_url": "https://api.discogs.com/releases/26057443"
                }
            ]
        }
        """;

        var releaseJson = """
        {
            "id": 26057443,
            "title": "Battle Of Ice",
            "year": 2023,
            "artists": [
                {
                    "name": "Marco Garau's Magic Opera",
                    "anv": "",
                    "id": 123456
                }
            ],
            "labels": [
                {
                    "name": "Self-released",
                    "catno": "MOG002"
                }
            ]
        }
        """;

        var requestedReleaseUrls = new List<string>();
        var handler = new TestHttpMessageHandler((request) =>
        {
            var uri = request.RequestUri!.ToString();
            if (uri.Contains("releases/"))
            {
                requestedReleaseUrls.Add(uri);
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
            Artist = "Marco Garau's Magic Opera",
            Album = "Battle of Ice",
            CatalogNumber = "MOG002"
        };

        var results = new List<CueData>();
        await provider.PerformDiscogsCatNoSearchAsync(sourceData.CatalogNumber, sourceData, results);

        Assert.Single(results);
        Assert.Equal("Marco Garau's Magic Opera", results[0].Artist);
        Assert.Equal("Battle Of Ice", results[0].Album);
        Assert.Equal("MOG002", results[0].CatalogNumber);

        // Verify only the matching release was fetched, avoiding wasteful requests for the other 10 candidates
        Assert.Single(requestedReleaseUrls);
        Assert.Contains("releases/26057443", requestedReleaseUrls[0]);
    }

    [Fact]
    public async Task DiscogsProvider_BarcodeSearch_ProcessesAllResultsBeyondFirst5Items_WhenArtistNotSpecified()
    {
        // 8 releases returned by barcode search with no artist specified in sourceData
        var searchJson = """
        {
            "pagination": { "page": 1, "pages": 1, "per_page": 50, "items": 8, "urls": {} },
            "results": [
                { "id": 1, "title": "Band A - Album A", "catno": "CAT1", "resource_url": "https://api.discogs.com/releases/1" },
                { "id": 2, "title": "Band B - Album B", "catno": "CAT2", "resource_url": "https://api.discogs.com/releases/2" },
                { "id": 3, "title": "Band C - Album C", "catno": "CAT3", "resource_url": "https://api.discogs.com/releases/3" },
                { "id": 4, "title": "Band D - Album D", "catno": "CAT4", "resource_url": "https://api.discogs.com/releases/4" },
                { "id": 5, "title": "Band E - Album E", "catno": "CAT5", "resource_url": "https://api.discogs.com/releases/5" },
                { "id": 6, "title": "Band F - Album F", "catno": "CAT6", "resource_url": "https://api.discogs.com/releases/6" },
                { "id": 7, "title": "Band G - Album G", "catno": "CAT7", "resource_url": "https://api.discogs.com/releases/7" },
                { "id": 8, "title": "Band H - Album H", "catno": "CAT8", "resource_url": "https://api.discogs.com/releases/8" }
            ]
        }
        """;

        var requestedReleaseUrls = new List<string>();
        var handler = new TestHttpMessageHandler((request) =>
        {
            var uri = request.RequestUri!.ToString();
            if (uri.Contains("releases/"))
            {
                requestedReleaseUrls.Add(uri);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{ \"id\": 0, \"title\": \"Test\" }", Encoding.UTF8, "application/json")
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
            Barcode = "123456789012"
        };

        var results = new List<CueData>();
        await provider.PerformDiscogsBarcodeSearchAsync(sourceData.Barcode, sourceData, results);

        // All 8 items are processed because the 5-item limit was removed
        Assert.Equal(8, results.Count);
        Assert.Equal(8, requestedReleaseUrls.Count);
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

    [Fact]
    public async Task DiscogsProvider_CatNoSearch_DoesNotFireRequestsForNonMatchingResults_WhenZeroMatchArtist()
    {
        // Results returned for catno=flac, all by different artists
        var searchJson = """
        {
            "pagination": { "page": 1, "pages": 1, "per_page": 50, "items": 5, "urls": {} },
            "results": [
                { "id": 1, "title": "Mo'Vez Lang - Héritiers De La Rue", "catno": "flac", "resource_url": "https://api.discogs.com/releases/1" },
                { "id": 2, "title": "Leyland Kirby - Bonus Tracks 1", "catno": "flac", "resource_url": "https://api.discogs.com/releases/2" },
                { "id": 3, "title": "Mac Gayden - Come Along", "catno": "flac", "resource_url": "https://api.discogs.com/releases/3" },
                { "id": 4, "title": "Can - More Live Music 1975", "catno": "flac", "resource_url": "https://api.discogs.com/releases/4" },
                { "id": 5, "title": "Shaun Williams - Rocket To The Moon", "catno": "flac", "resource_url": "https://api.discogs.com/releases/5" }
            ]
        }
        """;

        var requestedReleaseUrls = new List<string>();
        var handler = new TestHttpMessageHandler((request) =>
        {
            var uri = request.RequestUri!.ToString();
            if (uri.Contains("releases/"))
            {
                requestedReleaseUrls.Add(uri);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{ \"id\": 0, \"title\": \"Test\" }", Encoding.UTF8, "application/json")
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
            Artist = "Appleton",
            Album = "Everything's Eventual (2003)",
            CatalogNumber = "flac"
        };

        var results = new List<CueData>();
        await provider.PerformDiscogsCatNoSearchAsync(sourceData.CatalogNumber, sourceData, results);

        Assert.Empty(results);
        // Verify NO detail requests were fired for non-matching artists
        Assert.Empty(requestedReleaseUrls);
    }

    [Fact]
    public async Task MetadataService_FastSearch_FallsBackToArtistAndAlbum_WhenCatNoReturnsZeroMatches()
    {
        // When searching catno=flac, return 0 matching results (other artists)
        var catNoSearchJson = """
        {
            "pagination": { "page": 1, "pages": 1, "per_page": 50, "items": 1, "urls": {} },
            "results": [
                { "id": 1, "title": "Other Band - Other Album", "catno": "flac", "resource_url": "https://api.discogs.com/releases/1" }
            ]
        }
        """;

        // When searching text "Appleton Everything's Eventual", return the matching release
        var textSearchJson = """
        {
            "pagination": { "page": 1, "pages": 1, "per_page": 50, "items": 1, "urls": {} },
            "results": [
                {
                    "id": 161474,
                    "title": "Appleton - Everything's Eventual",
                    "catno": "589 609-2",
                    "country": "UK",
                    "year": "2003",
                    "resource_url": "https://api.discogs.com/releases/161474"
                }
            ]
        }
        """;

        var releaseJson = """
        {
            "id": 161474,
            "title": "Everything's Eventual",
            "year": 2003,
            "artists": [
                { "name": "Appleton", "anv": "", "id": 1234 }
            ],
            "labels": [
                { "name": "Polydor", "catno": "589 609-2" }
            ]
        }
        """;

        var handler = new TestHttpMessageHandler((request) =>
        {
            var uri = request.RequestUri!.ToString();
            if (uri.Contains("releases/161474"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(releaseJson, Encoding.UTF8, "application/json")
                };
            }
            if (uri.Contains("catno=flac"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(catNoSearchJson, Encoding.UTF8, "application/json")
                };
            }
            if (uri.Contains("Everything%27s+Eventual") || uri.Contains("Everythings+Eventual"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(textSearchJson, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{ "results": [] }""", Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler);
        var discogsProvider = new DiscogsProvider("fakeKey", "fakeSecret", "fakeToken", httpClient);
        var metadataService = new MetadataService(discogsProvider);

        var sourceData = new CueData
        {
            Artist = "Appleton",
            Album = "Everything's Eventual (2003)",
            CatalogNumber = "flac"
        };

        var results = await metadataService.SearchReleasesAsync(sourceData, deepSearch: false);

        Assert.Single(results);
        Assert.Equal("Appleton", results[0].Artist);
        Assert.Equal("Everything's Eventual", results[0].Album);
        Assert.Equal("589 609-2", results[0].CatalogNumber);
    }

    [Fact]
    public async Task DiscogsProvider_CatNoSearch_MatchesArtistAcronym_MP_ForMetalPriest()
    {
        var searchJson = """
        {
            "pagination": { "page": 1, "pages": 1, "per_page": 50, "items": 3, "urls": {} },
            "results": [
                { "id": 1, "title": "DJ RPM - Yo Hey! The Bass", "catno": "73053", "resource_url": "https://api.discogs.com/releases/1" },
                { "id": 2, "title": "Dave Eggar - Left Of Blue", "catno": "73053", "resource_url": "https://api.discogs.com/releases/2" },
                {
                    "id": 4877242,
                    "title": "MP (22) - Get It Now",
                    "catno": "73053",
                    "country": "Russia",
                    "resource_url": "https://api.discogs.com/releases/4877242"
                }
            ]
        }
        """;

        var releaseJson = """
        {
            "id": 4877242,
            "title": "Get It Now",
            "year": 1987,
            "country": "Russia",
            "artists": [
                {
                    "name": "MP (22)",
                    "anv": "",
                    "id": 6147288
                }
            ],
            "labels": [
                {
                    "name": "Steps Records",
                    "catno": "73053"
                }
            ]
        }
        """;

        var handler = new TestHttpMessageHandler((request) =>
        {
            if (request.RequestUri!.ToString().Contains("releases/4877242"))
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
            Artist = "Metal Priest",
            Album = "Get It Now",
            CatalogNumber = "73053"
        };

        var results = new List<CueData>();
        await provider.PerformDiscogsCatNoSearchAsync(sourceData.CatalogNumber, sourceData, results);

        Assert.Single(results);
        Assert.Equal("MP", results[0].Artist);
        Assert.Equal("Get It Now", results[0].Album);
        Assert.Equal("73053", results[0].CatalogNumber);
        Assert.Equal("Steps Records", results[0].Label);
    }

    [Fact]
    public async Task MetadataService_FastSearch_FallsBackToAcronym_WhenTextSearchYieldsZeroMatches()
    {
        var textSearchNoMatchJson = """
        {
            "pagination": { "page": 1, "pages": 1, "per_page": 50, "items": 1, "urls": {} },
            "results": [
                { "id": 999, "title": "Various - Masters Of Metal", "catno": "HAM 172", "resource_url": "https://api.discogs.com/releases/999" }
            ]
        }
        """;

        var acronymSearchJson = """
        {
            "pagination": { "page": 1, "pages": 1, "per_page": 50, "items": 1, "urls": {} },
            "results": [
                {
                    "id": 4877242,
                    "title": "MP (22) - Get It Now",
                    "catno": "73053",
                    "country": "Russia",
                    "resource_url": "https://api.discogs.com/releases/4877242"
                }
            ]
        }
        """;

        var releaseJson = """
        {
            "id": 4877242,
            "title": "Get It Now",
            "year": 1987,
            "artists": [
                {
                    "name": "MP (22)",
                    "anv": "",
                    "id": 6147288
                }
            ],
            "labels": [
                {
                    "name": "Steps Records",
                    "catno": "73053"
                }
            ]
        }
        """;

        var handler = new TestHttpMessageHandler((request) =>
        {
            var uri = request.RequestUri!.ToString();
            if (uri.Contains("releases/4877242"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(releaseJson, Encoding.UTF8, "application/json")
                };
            }
            if (uri.Contains("MP+Get+It+Now") || uri.Contains("MP%20Get%20It%20Now"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(acronymSearchJson, Encoding.UTF8, "application/json")
                };
            }
            if (uri.Contains("Metal+Priest+Get+It+Now") || uri.Contains("Metal%20Priest%20Get%20It%20Now"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(textSearchNoMatchJson, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{ "results": [] }""", Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler);
        var discogsProvider = new DiscogsProvider("fakeKey", "fakeSecret", "fakeToken", httpClient);
        var metadataService = new MetadataService(discogsProvider);

        var sourceData = new CueData
        {
            Artist = "Metal Priest",
            Album = "Get It Now"
        };

        var results = await metadataService.SearchReleasesAsync(sourceData, deepSearch: false);

        Assert.Single(results);
        Assert.Equal("MP", results[0].Artist);
        Assert.Equal("Get It Now", results[0].Album);
        Assert.Equal("73053", results[0].CatalogNumber);
    }

    [Fact]
    public async Task DiscogsProvider_CatNoSearch_MatchesAlbum_WhenArtistDiffers()
    {
        var searchJson = """
        {
            "pagination": { "page": 1, "pages": 1, "per_page": 50, "items": 3, "urls": {} },
            "results": [
                { "id": 1, "title": "DJ RPM - Yo Hey! The Bass", "catno": "73053", "resource_url": "https://api.discogs.com/releases/1" },
                { "id": 2, "title": "Dave Eggar - Left Of Blue", "catno": "73053", "resource_url": "https://api.discogs.com/releases/2" },
                {
                    "id": 9999,
                    "title": "Totally Different Artist - Get It Now",
                    "catno": "73053",
                    "resource_url": "https://api.discogs.com/releases/9999"
                }
            ]
        }
        """;

        var releaseJson = """
        {
            "id": 9999,
            "title": "Get It Now",
            "year": 1987,
            "artists": [
                {
                    "name": "Totally Different Artist",
                    "anv": ""
                }
            ],
            "labels": [
                {
                    "name": "Steps Records",
                    "catno": "73053"
                }
            ]
        }
        """;

        var handler = new TestHttpMessageHandler((request) =>
        {
            if (request.RequestUri!.ToString().Contains("releases/9999"))
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
            Artist = "Metal Priest",
            Album = "Get It Now",
            CatalogNumber = "73053"
        };

        var results = new List<CueData>();
        await provider.PerformDiscogsCatNoSearchAsync(sourceData.CatalogNumber, sourceData, results);

        Assert.Single(results);
        Assert.Equal("Totally Different Artist", results[0].Artist);
        Assert.Equal("Get It Now", results[0].Album);
        Assert.Equal("73053", results[0].CatalogNumber);
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
