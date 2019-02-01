module Tests

open Xunit
open BitBankApi
open System.IO
open System
open System.Threading.Tasks
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

let getAssetsTest(api: PrivateApi) =
    let resp = api.GetAssets()
    Assert.NotNull(resp.Data.Assets)
    Assert.True(1 = resp.Success, sprintf "failed to get Assets. Result was %s" (resp.JsonValue.ToString()))
    ()

let getOrderTest(api: PrivateApi) =
    let resp = api.GetOrder(14541507, "btc_jpy")
    Assert.NotNull(resp)
    Assert.True(1 = resp.Success, sprintf "failed to get Orders. Result was %s" (resp.JsonValue.ToString()))
    ()

let getActiveOrderTest (api: PrivateApi) =
    let resp = api.GetActiveOrders()
    Assert.NotNull(resp)
    Assert.True(1 = resp.Success, sprintf "failed to get Active Orders. Result was %s" (resp.JsonValue.ToString()))

let postOrderTest (api: PrivateApi) =
    let resp = api.PostOrder(pair = "btc_jpy", amount = "0.01", price = 1000, side = "buy", orderType = "market")
    Assert.NotNull(resp)
    Assert.True(1 = resp.Success, sprintf "failed to post order. Result was %s" (resp.JsonValue.ToString()))

let cancelOrderTest(api: PrivateApi) =
    let resp = api.CancelOrder(1, "btc_jpy")
    Assert.NotNull(resp)
    Assert.True(resp.Data.Code = 50010, sprintf "failed to cancel order. Result was %s" (resp.JsonValue.ToString()))

let cancelOrdersTest (api: PrivateApi) =
    let resp = api.CancelOrders([| 1; 2 |], "btc_jpy")
    Assert.NotNull(resp)
    Assert.True(resp.Orders = Array.empty, sprintf "failed to cancel orders. Result was %s" (resp.JsonValue.ToString()))

let getWithdrawalTest (api: PrivateApi) =
    let resp = api.GetWithdrawalAccount("btc")
    Assert.NotNull(resp)
    Assert.True(1 = resp.Success, sprintf "failed to get withdrawal account. Result was %s" (resp.JsonValue.ToString()))

let requestWithdrawalTest (api: PrivateApi) =
    let resp = api.RequestWithdrawal("jpy", "10", "37195a40-3d70-11e8-9c3c-2bd004e45303", "652036")
    Assert.NotNull(resp)
    Assert.True(1 = resp.Success, sprintf "failed to request withdrawal. Result was %s" (resp.JsonValue.ToString()))

type PrivateApiTests(output: ITestOutputHelper) =

  [<Fact>]
  member this.`` Should be able to use private api properly `` () =
    let api = getPrivate()
    // get
    getAssetsTest(api)
    getOrderTest(api)
    getActiveOrderTest(api)
    getWithdrawalTest(api)

    // post
    cancelOrderTest(api)
    cancelOrdersTest(api)
    postOrderTest(api)
    requestWithdrawalTest(api)