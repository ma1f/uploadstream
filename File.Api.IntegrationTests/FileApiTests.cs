using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace File.Api.IntegrationTests {
    using System.IO;

    public class FileApiTests : IClassFixture<TestServerFixture> {

        readonly TestServerFixture _fixture;
        readonly static string[] filenames = new[] { "xs.png", "sm.jpg", "md.jpg", "lg.jpg", "xl.exe" };
        const int LOAD_LIMIT = 3;

        static readonly string BaseDir = Path.Combine(AppContext.BaseDirectory, "resources");

        public FileApiTests(TestServerFixture fixture) {
            _fixture = fixture;
        }

        public readonly static StringContent[] Base64Files;
        public readonly static MultipartFormDataContent[] Files;
        public readonly static MultipartFormDataContent[] LoadFiles;

        public static IEnumerable<object[]> JsonPostData {
            get {
                var base64Files = filenames
                    .Select(x => {
                        var path = Path.Combine(BaseDir, x);
                        var base64 = Convert.ToBase64String(File.ReadAllBytes(path));
                        return new[] { new StringContent($"{{\"name\":\"name\",\"description\":\"description\",\"base64\":\"{base64}\"}}", Encoding.UTF8, "application/json") };
                    })
                    .ToArray();

                return base64Files;
            }
        }

        public static IEnumerable<object[]> PostData {
            get {
                var files = filenames.Select(x => {
                    var path = Path.Combine(BaseDir, x);
                    var multicontent = new MultipartFormDataContent();
                    ByteArrayContent bytes = new ByteArrayContent(File.ReadAllBytes(path));
                    bytes.Headers.Add("Content-Type", x.EndsWith(".png") ? "image/png" : x.EndsWith(".jpg") ? "image/jpeg" : "application/octet-stream");
                    multicontent.Add(bytes, "files", x);
                    multicontent.Add(new StringContent("name"), "name");
                    multicontent.Add(new StringContent("description"), "description");
                    return new[] { multicontent };
                })
                .ToArray();

                return files;
            }
        }

        public static IEnumerable<object[]> PostDataLoad {
            get {
                var data = Enumerable.Range(0, LOAD_LIMIT)
                    .Select(i => PostData.Select(x => x[0] as MultipartFormDataContent).ToArray())
                    .ToArray();

                var load = new object[data.First().Count()];
                return load.Select((x, i) => {
                    return data.Select(d => d[i]).ToArray();
                });
            }
        }

        public static IEnumerable<object[]> PostCollectionData {
            get {
                var files = filenames.Select(x => {
                    var path = Path.Combine(BaseDir, x);
                    var multicontent = new MultipartFormDataContent();
                    var bytes = File.ReadAllBytes(path);
                    for (int i = 0; i < LOAD_LIMIT; i++) {
                        var byteContent = new ByteArrayContent(bytes);
                        byteContent.Headers.Add("Content-Type", x.EndsWith(".png") ? "image/png" : x.EndsWith(".jpg") ? "image/jpeg" : "application/octet-stream");
                        multicontent.Add(byteContent, "files", x);
                    }
                    multicontent.Add(new StringContent("name"), "name");
                    multicontent.Add(new StringContent("description"), "description");
                    return new[] { multicontent };
                })
                .ToArray();

                return files;
            }
        }

        [Theory]
        [MemberData(nameof(JsonPostData), DisableDiscoveryEnumeration = true)]
        public async void PostBase64File(StringContent postData) {
            // ARRANGE

            // ACT
            var response = await _fixture.Client.PostAsync("api/base64", postData);

            // ASSERT
            response.EnsureSuccessStatusCode();

            var responseStr = await response.Content.ReadAsStringAsync();
            responseStr.Should().Contain("name");
            responseStr.Should().Contain("description");
        }

        [Theory]
        [MemberData(nameof(PostData), DisableDiscoveryEnumeration = true)]
        public async void PostFile(HttpContent postData) {
            // ARRANGE

            // ACT
            var response = await _fixture.Client.PostAsync("api/upload", postData);

            // ASSERT
            response.EnsureSuccessStatusCode();

            var responseStr = await response.Content.ReadAsStringAsync();
            responseStr.Should().Contain("name");
            responseStr.Should().Contain("description");
            responseStr.Should().Contain("1");
        }

        [Theory]
        [MemberData(nameof(PostCollectionData), DisableDiscoveryEnumeration = true)]
        public async void PostCollectionFiles(HttpContent postData) {
            // ARRANGE

            // ACT
            var response = await _fixture.Client.PostAsync("api/upload", postData);

            // ASSERT
            response.EnsureSuccessStatusCode();

            var responseStr = await response.Content.ReadAsStringAsync();
            responseStr.Should().Contain("name");
            responseStr.Should().Contain("description");
            responseStr.Should().Contain(LOAD_LIMIT.ToString());
        }

        [Theory]
        [MemberData(nameof(PostDataLoad), DisableDiscoveryEnumeration = true)]
        public async void PostFileLoad(params HttpContent[] postData) {
            // ARRANGE
            var results = new HttpResponseMessage[LOAD_LIMIT];
            
            // ACT
            Parallel.For(0, LOAD_LIMIT, (i, s) => results[i] = _fixture.Client.PostAsync("api/upload", postData[i]).Result);

            // ASSERT
            foreach (var response in results) {
                response.EnsureSuccessStatusCode();

                var responseStr = await response.Content.ReadAsStringAsync();
                responseStr.Should().Contain("name");
                responseStr.Should().Contain("description");
                responseStr.Should().Contain("1");
            }
        }

        [Theory]
        [MemberData(nameof(PostData), DisableDiscoveryEnumeration = true)]
        public async void StreamFile(HttpContent postData) {
            // ARRANGE

            // ACT
            var response = await _fixture.Client.PostAsync("api/stream", postData);

            // ASSERT
            response.EnsureSuccessStatusCode();

            var responseStr = await response.Content.ReadAsStringAsync();
            var model = JsonConvert.DeserializeObject<HttpRequestResult>(responseStr);

            model.Model.Name.Should().Contain("name");
            model.Model.Description.Should().Contain("description");
            model.Files.Count().Should().Be(1);
        }

        [Theory]
        [MemberData(nameof(PostCollectionData), DisableDiscoveryEnumeration = true)]
        public async void StreamCollectionFile(HttpContent postData) {
            // ARRANGE

            // ACT
            var response = await _fixture.Client.PostAsync("api/stream", postData);

            // ASSERT
            response.EnsureSuccessStatusCode();

            var responseStr = await response.Content.ReadAsStringAsync();
            var model = JsonConvert.DeserializeObject<HttpRequestResult>(responseStr);

            model.Model.Name.Should().Contain("name");
            model.Model.Description.Should().Contain("description");
            model.Files.Count().Should().Be(3);
        }

        [Theory]
        [MemberData(nameof(PostDataLoad), DisableDiscoveryEnumeration = true)]
        public async void StreamFileLoad(params HttpContent[] postData) {
            // ARRANGE
            var results = new HttpResponseMessage[LOAD_LIMIT];
            // ACT
            Parallel.For(0, LOAD_LIMIT, (i, s) => results[i] = _fixture.Client.PostAsync("api/stream", postData[i]).Result);

            // ASSERT
            foreach (var response in results) {
                response.EnsureSuccessStatusCode();

                var responseStr = await response.Content.ReadAsStringAsync();
                var model = JsonConvert.DeserializeObject<HttpRequestResult>(responseStr);

                model.Model.Name.Should().Contain("name");
                model.Model.Description.Should().Contain("description");
                model.Files.Count().Should().Be(1);
            }
        }
    }

    class HttpRequestResult {
        public ModelResult Model { get; set; }
        public IEnumerable<FormFileResult> Files { get; set; }
    }
    class ModelResult {
        public string Name { get; set; }
        public string Description{ get; set; }
    }
    class FormFileResult {
        public string Name { get; set; }
        public string FileName { get; set; }
        public long Length { get; set; }
        public string ContentType { get; set; }
        public string ContentDisposition { get; set; }
        public Dictionary<string,string[]> Headers{ get; set; }

    }
}
