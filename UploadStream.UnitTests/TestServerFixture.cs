using System;
using System.Net.Http;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace UploadStream.UnitTests {

    public class TestServerFixture : IDisposable {

        readonly TestServer _testServer;
        public HttpClient Client { get; }

        public TestServerFixture() {
            var builder = new WebHostBuilder()
                   .UseEnvironment("testing")
                   .UseStartup<Startup>(); 

            _testServer = new TestServer(builder);
            Client = _testServer.CreateClient();
        }

        public void Dispose() {
            Client.Dispose();
            _testServer.Dispose();
        }
    }
}
