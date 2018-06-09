using System;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using BenchmarkDotNet.Order;

namespace File.Api.PerformanceTests {
    using System.IO;

    [ConfidenceIntervalErrorColumn, MemoryDiagnoser, OrderProvider(SummaryOrderPolicy.FastestToSlowest)]
    public class UploadStreamLoadTestMultipartBenchmarks {
        
        public HttpClient Client;
        public static int LOAD_LIMIT;
        [GlobalSetup]
        public void GlobalSetup() {
            var builder = new WebHostBuilder().UseStartup<Startup>();
            var testServer = new TestServer(builder);
            Client = testServer.CreateClient();
            LOAD_LIMIT = 20;
        }

        public static MultipartFormDataContent MultipartContent;

        [IterationSetup]
        public void IterationSetup() {
            var baseDir = Path.Combine(AppContext.BaseDirectory, "resources");
            var path = Path.Combine(baseDir, Filename);
            var content = new ByteArrayContent(File.ReadAllBytes(path));
            content.Headers.Add("Content-Type", Filename.EndsWith(".png") ? "image/png" : Filename.EndsWith(".jpg") ? "image/jpeg" : "application/octet-stream");
            
            var multicontent = new MultipartFormDataContent {
                { content, "files", Filename },
                { new StringContent("name"), "name" },
                { new StringContent("description"), "description" }
            };
            for (int i = 0; i < LOAD_LIMIT; i++)
                multicontent.Add(content, "files", Filename);

            MultipartContent = multicontent;
        }

        [Params("xs.png", "sm.jpg", "md.jpg", "lg.jpg", "xl.exe")]
        public string Filename;

        [Benchmark]
        public async Task UploadStream() => await Client.PostAsync("api/stream", MultipartContent).Result.Content.ReadAsStringAsync();
    }
}
