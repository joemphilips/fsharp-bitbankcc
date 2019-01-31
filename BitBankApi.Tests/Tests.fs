module Tests

open Xunit
open BitBankApi
open System.IO
open System
open Newtonsoft.Json.Linq
open Xunit.Abstractions

type PublicApiTests() =
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

type PrivateApiTests(output: ITestOutputHelper) =
  [<Fact>]
  member this.`` Should get assets `` () =
    let resp = getPrivate().GetAssets()
    Assert.NotNull(resp.Data.Assets)
    Assert.Equal(1, resp.Success)
    ()

  [<Fact>]
  member this.`` Should get order `` () =
    let resp = getPrivate().GetOrder(14541507, "btc_jpy")
    Assert.NotNull(resp)
    Assert.Equal(1, resp.Success)
    ()

  [<Fact>]
  member this.`` Should get active order `` () =
    let resp = getPrivate().GetActiveOrders()
    Assert.NotNull(resp)
    Assert.Equal(1, resp.Success)
    ()

  [<Fact>]
  member this.`` Should post, get and cancel order `` () =
    let api = getPrivate()
    let pair = "btc_jpy"
    let postResp1 = api.PostOrder(pair, amount = "0.01", price = 1000, side = "buy", orderType = "market")
    let postResp2 = api.PostOrder(pair, amount = "0.01", price = 1001, side = "buy", orderType = "market")
    let postResp3 = api.PostOrder(pair, amount = "0.01", price = 1002, side = "buy", orderType = "market")
    Assert.NotNull(postResp1)
    Assert.NotEqual(1, postResp1.Success)
    let resp = api.GetOrder(postResp1.Data.OrderId, pair)
    Assert.NotNull(resp)
    Assert.NotEqual(1, resp.Success)
    let cancelResp = api.CancelOrder(postResp1.Data.OrderId, pair)
    Assert.NotNull(cancelResp)
    Assert.NotEqual(1, cancelResp.Success)

    let cancelsResp = api.CancelOrders([|postResp2.Data.OrderId; postResp3.Data.OrderId|], pair)
    Assert.NotNull(cancelsResp)
    Assert.NotEqual(1, cancelsResp.Success)
    ()

  [<Fact>]
  member this.`` Should get withdrawal account``() =
    let resp = getPrivate().GetWithdrawalAccount("jpy")
    Assert.NotNull(resp)
    printf "resp was %s" (resp.JsonValue.ToString())
    Assert.Equal(1, resp.Success)

  [<Fact>]
  member this.`` Should request withdrawal``() =
    let resp = getPrivate().RequestWithdrawal("jpy", "10", "37195a40-3d70-11e8-9c3c-2bd004e45303")
    Assert.NotNull(resp)
    Assert.Equal(1, resp.Success)
