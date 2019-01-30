module Tests

open Xunit
open BitBankApi
open System.IO
open System
open Newtonsoft.Json.Linq

[<Fact>]
let ``Should be able to use public api`` () =
    let date = Some "20181226"
    let resp = PublicApi.GetTransactions "btc_jpy" date
    Assert.NotNull(resp)

    let getTickerResp = PublicApi.GetTicker "btc_jpy"
    Assert.NotNull(getTickerResp)

    let getDepthResp = PublicApi.GetDepth "btc_jpy"
    Assert.NotNull(getDepthResp)

    let getCandleStickResp = PublicApi.GetCandleStick "btc_jpy" "1hour" "20181225"
    Assert.NotNull(getCandleStickResp)

let getPrivate () =
    let conf = JObject.Parse(File.ReadAllText("../../../../BitBankApi/config.json"))
    let apiKey = (conf.GetValue("ApiKey") :?> JValue).Value :?> string
    let apiSecret = (conf.GetValue("ApiSecret") :?> JValue).Value :?> string
    new PrivateApi(apiKey, apiSecret)

[<Fact>]
let `` Should get Assets `` () =
    let api = getPrivate()
    let resp = api.GetAssets()
    Assert.NotNull(resp.Data.Assets)
    ()

[<Fact>]
let `` Shuold get Order `` () =
  let api = getPrivate()
  let resp = api.GetOrder("")
  Assert.True(false, "needs update")
  ()