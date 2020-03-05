using System;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace File.Api.PerformanceTests {

    [ConfidenceIntervalErrorColumn, MemoryDiagnoser, Orderer(SummaryOrderPolicy.FastestToSlowest)]
    public class DownloadBenchmarks {

        public HttpClient Client;
        [GlobalSetup]
        public void GlobalSetup() {
            var builder = new WebHostBuilder().UseWebRoot(AppContext.BaseDirectory).UseStartup<Startup>();
            var testServer = new TestServer(builder);
            Client = testServer.CreateClient();
        }

        [Params("xs.png", "sm.jpg", "md.jpg", "lg.jpg", "xl.exe")]
        public string Filename { get; set; }

        [Benchmark(Baseline = true)]
        public async Task Ping() {
            using var stream = await Client.GetAsync("api/ping").Result.Content.ReadAsStreamAsync();
            while (stream.ReadByte() != -1) ;
        }

        [Benchmark]
        public async Task DownloadBase64() {
            using var stream = await Client.GetAsync($"api/base64/{Filename}").Result.Content.ReadAsStreamAsync();
            while (stream.ReadByte() != -1) ;
        }

        [Benchmark]
        public async Task DownloadBase64Memory() {
            using var stream = await Client.GetAsync($"api/base64/{Filename}/memory").Result.Content.ReadAsStreamAsync();
                while (stream.ReadByte() != -1) ;
        }

        [Benchmark]
        public async Task DownloadFile() {
            using var stream = await Client.GetAsync($"api/file/{Filename}").Result.Content.ReadAsStreamAsync();
            while (stream.ReadByte() != -1) ;
        }

        [Benchmark]
        public async Task DownloadFileMemory() {
            using var stream = await Client.GetAsync($"api/file/{Filename}/memory").Result.Content.ReadAsStreamAsync();
            while (stream.ReadByte() != -1) ;
        }

        [Benchmark]
        public async Task DownloadFileFileStream() {
            using var stream = await Client.GetAsync($"api/file/{Filename}/filestream").Result.Content.ReadAsStreamAsync();
            while (stream.ReadByte() != -1) ;
        }

        [Benchmark]
        public async Task DownloadStream() {
            using var stream = await Client.GetAsync($"api/stream/{Filename}").Result.Content.ReadAsStreamAsync();
            while (stream.ReadByte() != -1) ;
        }

        [Benchmark]
        public async Task DownloadStreamMemory() {
            using var stream = await Client.GetAsync($"api/stream/{Filename}/memory").Result.Content.ReadAsStreamAsync();
            while (stream.ReadByte() != -1) ;
        }
    }
}
