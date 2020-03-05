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
    public class UploadBase64LoadTestBenchmarks {
        public HttpClient Client;
        public static int LOAD_LIMIT;

        [GlobalSetup]
        public void GlobalSetup() {
            var builder = new WebHostBuilder().UseStartup<Startup>();
            var testServer = new TestServer(builder);
            Client = testServer.CreateClient();

            LOAD_LIMIT = 20;
        }

        public static string Content;
        public static StringContent[] StringContent;

        [IterationSetup]
        public void IterationSetup() {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "resources", Filename);
            var base64 = Convert.ToBase64String(File.ReadAllBytes(path));
            Content = $"{{\"name\":\"name\",\"description\":\"description\",\"base64\":\"{base64}\"}}";
            StringContent = new StringContent[LOAD_LIMIT];
            for (int i = 0; i < LOAD_LIMIT; i++)
                StringContent[i] = new StringContent(Content, Encoding.UTF8, "application/json");
        }

        [Params("xs.png", "sm.jpg", "md.jpg", "lg.jpg", "xl.exe")]
        public string Filename;

        [Benchmark]
        public void UploadBase64() => Parallel.For(0, LOAD_LIMIT, (i, s) => Client.PostAsync("api/base64", StringContent[i]).Wait());

    }
}
