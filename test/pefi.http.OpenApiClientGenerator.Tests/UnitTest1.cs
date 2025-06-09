
namespace pefi.http.OpenApiClientGenerator.Tests
{
    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            var url = "http://192.168.0.5:5550/swagger/v1/swagger.json";
            var sourceCode = await  ClientGenerator.Execute("testNameSpace", "foo", url, CancellationToken.None);
            Assert.NotNull(sourceCode);
            Assert.NotEmpty(sourceCode);    
        }
    }
}