using System;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace File.Api.PerformanceTests {
    using System.IO;

    [ConfidenceIntervalErrorColumn, MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class UploadFileBenchmarks {

        public HttpClient Client;
        [GlobalSetup]
        public void GlobalSetup() {
            var builder = new WebHostBuilder().UseStartup<Startup>();
            var testServer = new TestServer(builder);
            Client = testServer.CreateClient();
        }

        public static MultipartFormDataContent MultipartContent;

        [IterationSetup]
        public void IterationSetup() {
            var baseDir = Path.Combine(AppContext.BaseDirectory, "resources");
            var path = Path.Combine(baseDir, Filename);
            var content = new ByteArrayContent(File.ReadAllBytes(path));
            content.Headers.Add("Content-Type", Filename.EndsWith(".png") ? "image/png" : Filename.EndsWith(".jpg") ? "image/jpeg" : "application/octet-stream");

            MultipartContent = new MultipartFormDataContent {
                { content, "files", Filename },
                { new StringContent("name"), "name" },
                { new StringContent("description"), "description" }
            };
        }

        [Params("xs.png", "sm.jpg", "md.jpg", "lg.jpg", "xl.exe")]
        public string Filename;

        [Benchmark]
        public async Task UploadFile() => await Client.PostAsync("api/upload", MultipartContent).Result.Content.ReadAsStringAsync();
    }
}
