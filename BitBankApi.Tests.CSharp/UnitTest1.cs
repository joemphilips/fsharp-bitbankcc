using System;
using Xunit;
using BitBankApi;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading.Tasks;

namespace BitBankApi.Tests.CSharp
{
    public class BitBankApiTest1
    {
        [Fact]
        public void PublicApiTest1()
        {
            var api = new PublicApi();
            var ticker = api.GetTicker("btc_jpy");
            Console.WriteLine(ticker.ToString());
            Assert.NotNull(ticker);
        }

        [Fact]
        public void PrivateApiTest1()
        {
            using (var api = GetPrivate())
            {
                var resp = api.GetAssets();
                Assert.True(resp.Success == 1);
                Assert.NotNull(resp.Data.Assets);
            }
        }

        [Fact]
        public async void PrivateAsyncApiTest1()
        {
            using (var api = GetPrivate())
            {
                var resp = await api.GetAssetsAsync();
                Assert.True(resp.Success == 1);
                Assert.NotNull(resp.Data.Assets);

                var resp2 = await api.GetTradeHistoryAsync();
                Assert.True(resp2.Success == 1);
                Assert.NotNull(resp2.Data);

            }
        }

        private static BitBankApi.PrivateApi GetPrivate()
        {

            var conf = JObject.Parse(File.ReadAllText("../../../../BitBankApi/config.json"));
            var apiKey = (string)(conf.GetValue("ApiKey"));

            var apiSecret = (string)conf.GetValue("ApiSecret");

            return new PrivateApi(apiKey, apiSecret);
        }
    }
}
