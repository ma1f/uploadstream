using BenchmarkDotNet.Running;

namespace File.Api.PerformanceTests {
    public class Benchmarks {
        
        public static void Main() {
            BenchmarkRunner.Run<UploadBase64Benchmarks>();
            BenchmarkRunner.Run<UploadFileBenchmarks>();
            BenchmarkRunner.Run<UploadStreamBenchmarks>();

            BenchmarkRunner.Run<UploadStreamModelBenchmarks>();

            BenchmarkRunner.Run<UploadBase64LoadTestBenchmarks>();
            BenchmarkRunner.Run<UploadFileLoadTestBenchmarks>();
            BenchmarkRunner.Run<UploadStreamLoadTestBenchmarks>();

            BenchmarkRunner.Run<UploadFileLoadTestMultipartBenchmarks>();
            BenchmarkRunner.Run<UploadStreamLoadTestMultipartBenchmarks>();

            BenchmarkRunner.Run<DownloadBenchmarks>();
        }

    }
}
