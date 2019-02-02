using System;
using Xunit;
using BitBankApi;
using Newtonsoft.Json.Linq;
using System.IO;

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

    private static BitBankApi.PrivateApi GetPrivate()
    {

      var conf = JObject.Parse(File.ReadAllText("../../../../BitBankApi/config.json"));
      var apiKey = (string)(conf.GetValue("ApiKey"));

      var apiSecret = (string)conf.GetValue("ApiSecret");

      return new PrivateApi(apiKey, apiSecret);
    }
  }
}
