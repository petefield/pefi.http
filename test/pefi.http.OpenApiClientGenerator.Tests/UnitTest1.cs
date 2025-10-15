
namespace pefi.http.OpenApiClientGenerator.Tests
{
    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            var url = "C:\\source\\personal\\pefi.http\\test\\pefi.http.OpenApiClientGenerator.Tests\\client_config\\service_mgr_openapi.json";

            var s = System.IO.File.ReadAllText(url);
            
            var sourceCode = await  ClientGenerator.Execute("testNameSpace", "foo", s, CancellationToken.None);
            Assert.NotNull(sourceCode);
            Assert.NotEmpty(sourceCode);    
        }
    }
}