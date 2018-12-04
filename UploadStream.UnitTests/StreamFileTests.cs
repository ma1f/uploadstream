using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net.Http;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using FluentAssertions;
using Moq;
using Xunit;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Threading;
using System.Threading.Tasks;
using Jil;
using System.Net;

namespace UploadStream.UnitTests {
    public class StreamFileTests : IClassFixture<TestServerFixture> {

        readonly static string BaseDir = Path.Combine(AppContext.BaseDirectory, "resources");
        readonly TestServerFixture _fixture;

        public StreamFileTests(TestServerFixture fixture) {
            _fixture = fixture;
        }

        public static IEnumerable<object[]> PostData {
            get {
                var filename = "xs.png";
                var path = Path.Combine(BaseDir, filename);
                var multicontent = new MultipartFormDataContent();
                ByteArrayContent bytes = new ByteArrayContent(File.ReadAllBytes(path));
                bytes.Headers.Add("Content-Type", "image/png");
                multicontent.Add(bytes, "files", filename);
                multicontent.Add(new StringContent("42"), "id");
                multicontent.Add(new StringContent("mr-x"), "name");
                multicontent.Add(new StringContent("description"), "description");
                return new[] { new[] { multicontent } };
            }
        }

        [Fact]
        public async void HttpRequestShouldStreamFilesAndModel() {
            // arrange
            var httpContext = new Mock<HttpContext>();
            var httpRequest = new Mock<HttpRequest>();
            httpContext.Setup(x => x.Request).Returns(httpRequest.Object);
            httpRequest.Setup(x => x.ContentType).Returns("multipart/form-data; boundary=----");

            var filename = "xs.png";
            var multicontent = new MultipartFormDataContent("----");
            var bytes = System.IO.File.ReadAllBytes(Path.Combine(BaseDir, filename));
            ByteArrayContent byteContent = new ByteArrayContent(bytes);
            byteContent.Headers.Add("Content-Type", "image/png");
            multicontent.Add(byteContent, "files", filename);
            multicontent.Add(new StringContent("3"), "id");
            multicontent.Add(new StringContent("x"), "name");
            var multipartStream = await multicontent.ReadAsStreamAsync();

            httpRequest.Setup(x => x.Body).Returns(await multicontent.ReadAsStreamAsync());

            var controller = new TestController {
                ControllerContext = new ControllerContext {
                    HttpContext = httpContext.Object
                }
            };

            // action
            var result = await controller.Request.StreamFilesModel(file => {
                // assert
                file.Name.Should().Be("files");
                file.FileName.Should().Be("xs.png");
                file.ContentType.Should().Be("image/png");
                file.Length.Should().Be(0);
                while (file.OpenReadStream().ReadByte() != -1) ;
                file.Length.Should().Be(bytes.Length);

                return Task.CompletedTask;
            });

            // assert
            var dict = result.GetKeysFromPrefix("");
            dict.ContainsKey("id").Should().BeTrue();
            dict.ContainsKey("name").Should().BeTrue();
            result.GetValue("id").Values[0].Should().Be("3");
            result.GetValue("name").Values[0].Should().Be("x");
        }

        [Fact]
        public async void NullUploadShouldNotExecute() {
            // ARRANGE
            var content = new MultipartFormDataContent();

            // ACT
            var response = await _fixture.Client.PostAsync("api/null", content);

            // ASSERT
            response.EnsureSuccessStatusCode();

            var responseStr = await response.Content.ReadAsStringAsync();
            var expected = new { IsValid = true };
            var responseObj = JsonConvert.DeserializeObject(responseStr, expected.GetType());
            responseObj.Should().BeEquivalentTo(expected);
        }

        [Theory]
        [MemberData(nameof(PostData), DisableDiscoveryEnumeration = true)]
        public async void DefaultUploadShouldExecute(HttpContent postData) {
            // ACT
            var response = await _fixture.Client.PostAsync("api/default", postData);

            // ASSERT
            response.EnsureSuccessStatusCode();

            var responseStr = await response.Content.ReadAsStringAsync();
            var expected = new {
                Model = new {
                    Id = 42,
                    Name = "mr-x",
                    Files = new[] {
                        new {
                            ContentDisposition = "form-data; name=files; filename=xs.png; filename*=utf-8''xs.png",
                            ContentType = "image/png",
                            FileName = "xs.png",
                            Length = 6086,
                            Name= "files"
                        }
                    }
                },
                IsValid = true
            };
            var responseObj = JsonConvert.DeserializeObject(responseStr, expected.GetType());
            responseObj.Should().BeEquivalentTo(expected);
        }

        [Theory]
        [MemberData(nameof(PostData), DisableDiscoveryEnumeration = true)]
        public async void DefaultUploadShouldNotBindWhenModelBindingDisabled(HttpContent postData) {
            // ACT
            var response = await _fixture.Client.PostAsync("api/default/nobinding", postData);

            // ASSERT
            response.EnsureSuccessStatusCode();

            var responseStr = await response.Content.ReadAsStringAsync();
            var expected = new {
                Model = new {
                    Files = new[] {
                        new {
                            ContentDisposition = "form-data; name=files; filename=xs.png; filename*=utf-8''xs.png",
                            ContentType = "image/png",
                            FileName = "xs.png",
                            Length = 6086,
                            Name= "files"
                        }
                    }
                },
                IsValid = true
            };
            var responseObj = JsonConvert.DeserializeObject(responseStr, expected.GetType());
            responseObj.Should().BeEquivalentTo(expected);
        }

        [Theory]
        [MemberData(nameof(PostData), DisableDiscoveryEnumeration = true)]
        public async void UploadNoModelShouldReturnFiles(HttpContent postData) {
            // ACT
            var response = await _fixture.Client.PostAsync("api/nomodel", postData);

            // ASSERT
            response.EnsureSuccessStatusCode();

            var responseStr = await response.Content.ReadAsStringAsync();

            var expected = new {
                Files = new[] {
                    new {
                        ContentDisposition = "form-data; name=files; filename=xs.png; filename*=utf-8''xs.png",
                        ContentType = "image/png",
                        FileName = "xs.png",
                        Length = 6086,
                        Name= "files"
                    }
                },
                IsValid = true
            };
            var responseObj = JsonConvert.DeserializeObject(responseStr, expected.GetType());
            responseObj.Should().BeEquivalentTo(expected);
        }

        [Theory]
        [MemberData(nameof(PostData), DisableDiscoveryEnumeration = true)]
        public async void UploadModelShouldReturnFilesAndModel(HttpContent postData) {
            // ARRANGE

            // ACT
            var response = await _fixture.Client.PostAsync("api/model", postData);

            // ASSERT
            response.EnsureSuccessStatusCode();

            var responseStr = await response.Content.ReadAsStringAsync();
            var expected = new {
                Model = new {
                    Id = 42,
                    Name = "mr-x"
                },
                Files = new[] {
                    new {
                        ContentDisposition = "form-data; name=files; filename=xs.png; filename*=utf-8''xs.png",
                        ContentType = "image/png",
                        FileName = "xs.png",
                        Length = 6086,
                        Name= "files"
                    }
                },
                IsValid = false
            };
            var responseObj = JsonConvert.DeserializeObject(responseStr, expected.GetType());
            responseObj.Should().BeEquivalentTo(expected);
        }

        [Theory]
        [MemberData(nameof(PostData), DisableDiscoveryEnumeration = true)]
        public async void UploadModelShouldReturnFilesAndValidatedModel(HttpContent postData) {
            // ARRANGE

            // ACT
            var response = await _fixture.Client.PostAsync("api/model/validation", postData);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var responseStr = await response.Content.ReadAsStringAsync();
            var expected = new { Name = new[] { "The field Name must be a string or array type with a minimum length of '5'." } };
            var responseObj = JsonConvert.DeserializeObject(responseStr, expected.GetType());
            responseObj.Should().BeEquivalentTo(expected);
        }

        [Theory]
        [MemberData(nameof(PostData), DisableDiscoveryEnumeration = true)]
        public async void UploadModelWithModelBindingShouldReturnModels(HttpContent postData) {
            // ACT
            var response = await _fixture.Client.PostAsync("api/model/bindingenabled", postData);

            // ASSERT
            response.EnsureSuccessStatusCode();

            var responseStr = await response.Content.ReadAsStringAsync();
            var expected = new {
                Model = new {
                    Id = 42,
                    Name = "mr-x"
                },
                BindingModel = new {
                    Id = 42,
                    Name = "mr-x",
                    Files = new[] {
                        new {
                            ContentDisposition = "form-data; name=files; filename=xs.png; filename*=utf-8''xs.png",
                            ContentType = "image/png",
                            FileName = "xs.png",
                            Length = 6086,
                            Name= "files"
                        }
                    }
                },
                Files = new[] {
                    new {
                        ContentDisposition = "form-data; name=files; filename=xs.png; filename*=utf-8''xs.png",
                        ContentType = "image/png",
                        FileName = "xs.png",
                        Length = 6086,
                        Name= "files"
                    }
                },
                IsValid = true
            };
            var responseObj = JsonConvert.DeserializeObject(responseStr, expected.GetType());
            responseObj.Should().BeEquivalentTo(expected);
        }
        [Theory]
        [MemberData(nameof(PostData), DisableDiscoveryEnumeration = true)]
        public async void UploadModelWithNoModelBindingShouldReturnNullBindingModel(HttpContent postData) {
            // ACT
            var response = await _fixture.Client.PostAsync("api/model/bindingdisabled", postData);

            // ASSERT
            response.EnsureSuccessStatusCode();

            var responseStr = await response.Content.ReadAsStringAsync();
            var expected = new {
                Model = new {
                    Id = 42,
                    Name = "mr-x"
                },
                BindingModel = new {
                    Id = default(int),
                    Name = default(string),
                    Files = new[] {
                        new {
                            ContentDisposition = "form-data; name=files; filename=xs.png; filename*=utf-8''xs.png",
                            ContentType = "image/png",
                            FileName = "xs.png",
                            Length = 6086,
                            Name= "files"
                        }
                    }
                },
                Files = new[] {
                    new {
                        ContentDisposition = "form-data; name=files; filename=xs.png; filename*=utf-8''xs.png",
                        ContentType = "image/png",
                        FileName = "xs.png",
                        Length = 6086,
                        Name= "files"
                    }
                },
                IsValid = true
            };
            var responseObj = JsonConvert.DeserializeObject(responseStr, expected.GetType());
            responseObj.Should().BeEquivalentTo(expected);
        }

    }
}


