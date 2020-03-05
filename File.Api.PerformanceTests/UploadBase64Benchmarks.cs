using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace File.Api.PerformanceTests {
    using System.IO;

    [ConfidenceIntervalErrorColumn, MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class UploadBase64Benchmarks {
        
        public HttpClient Client;
        [GlobalSetup]
        public void GlobalSetup() {
            var builder = new WebHostBuilder().UseStartup<Startup>();
            var testServer = new TestServer(builder);
            Client = testServer.CreateClient();
        }

        public static StringContent StringContent;

        [IterationSetup]
        public void IterationSetup() {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "resources", Filename);
            var base64 = Convert.ToBase64String(File.ReadAllBytes(path));
            var content = $"{{\"name\":\"name\",\"description\":\"description\",\"base64\":\"{base64}\"}}";
            StringContent = new StringContent(content, Encoding.UTF8, "application/json");
        }

        [Params("xs.png", "sm.jpg", "md.jpg", "lg.jpg", "xl.exe")]
        public string Filename;

        [Benchmark]
        public async Task UploadBase64() => await Client.PostAsync("api/base64", StringContent).Result.Content.ReadAsStringAsync();
    }
}
