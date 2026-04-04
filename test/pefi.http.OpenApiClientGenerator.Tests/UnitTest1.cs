
namespace pefi.http.OpenApiClientGenerator.Tests
{
    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            var specPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client_config", "service_mgr_openapi.json");

            var s = System.IO.File.ReadAllText(specPath);
            
            var sourceCode = await ClientGenerator.Execute("testNameSpace", "foo", s, CancellationToken.None);
            Assert.NotNull(sourceCode);
            Assert.NotEmpty(sourceCode);
        }
    }
}