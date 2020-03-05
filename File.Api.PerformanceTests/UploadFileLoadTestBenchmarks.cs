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
    public class UploadFileLoadTestBenchmarks {
        
        public HttpClient Client;
        public static int LOAD_LIMIT;
        [GlobalSetup]
        public void GlobalSetup() {
            var builder = new WebHostBuilder().UseStartup<Startup>();
            var testServer = new TestServer(builder);
            Client = testServer.CreateClient();

            LOAD_LIMIT = 20;
        }
        
        public static MultipartFormDataContent[] MultipartContent;

        [IterationSetup]
        public void IterationSetup() {
            var baseDir = Path.Combine(AppContext.BaseDirectory, "resources");
            var path = Path.Combine(baseDir, Filename);
            var content = new ByteArrayContent(File.ReadAllBytes(path));
            content.Headers.Add("Content-Type", Filename.EndsWith(".png") ? "image/png" : Filename.EndsWith(".jpg") ? "image/jpeg" : "application/octet-stream");

            MultipartContent = new MultipartFormDataContent[LOAD_LIMIT];
            for (int i = 0; i < LOAD_LIMIT; i++) {
                var multicontent = new MultipartFormDataContent {
                    { content, "files", Filename },
                    { new StringContent("name"), "name" },
                    { new StringContent("description"), "description" }
                };
                MultipartContent[i] = multicontent;
            }
        }

        [Params("xs.png", "sm.jpg", "md.jpg", "lg.jpg", "xl.exe")]
        public string Filename;

        [Benchmark]
        public void UploadFile() => Parallel.For(0, LOAD_LIMIT, (i, s) => Client.PostAsync("api/upload", MultipartContent[i]).Wait());
    }
}
