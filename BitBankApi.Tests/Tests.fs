module Tests

open Xunit
open BitBankApi
open System.IO
open Newtonsoft.Json.Linq
open Xunit.Abstractions

type PublicApiTests() =
  [<Fact>]
  let ``Should be able to use public api`` () =
    let date = Some "20181226"
    let api = new PublicApi()
    let resp = api.GetTransactions("btc_jpy", date)
    Assert.NotNull(resp)

    let getTickerResp = api.GetTicker("btc_jpy")
    Assert.NotNull(getTickerResp)

    let getDepthResp = api.GetDepth("btc_jpy")
    Assert.NotNull(getDepthResp)

    let getCandleStickResp = api.GetCandleStick("btc_jpy", "1hour", "20181225")
    Assert.NotNull(getCandleStickResp)

let getPrivate () =
    let conf = JObject.Parse(File.ReadAllText("../../../../BitBankApi/config.json"))
    let apiKey = (conf.GetValue("ApiKey") :?> JValue).Value :?> string
    let apiSecret = (conf.GetValue("ApiSecret") :?> JValue).Value :?> string
    new PrivateApi(apiKey, apiSecret)

let private failureTest f expectedErrorCode =
  try
    f() |> ignore
    Assert.True(false, "did not raise exception")
  with
  | :? BitBankApiException as ex -> Assert.True(ex.Code = expectedErrorCode, sprintf "error code was different from the one expected %s" (ex.Code.ToString()))
  ()

let getAssetsTest(api: PrivateApi) =
    let resp: Response<AssetsRecord> = api.GetAssets()
    Assert.NotNull(resp.Data.Assets)
    Assert.True(1 = resp.Success, sprintf "failed to get Assets. Result was %s" (resp.ToString()))

let getOrderTest(api: PrivateApi) =
    let resp = api.GetOrder(14541507, "btc_jpy")
    Assert.NotNull(resp)
    Assert.True(1 = resp.Success, sprintf "failed to get Orders. Result was %s" (resp.ToString()))

let getActiveOrderTest (api: PrivateApi) =
    let resp = api.GetActiveOrders()
    Assert.NotNull(resp)
    Assert.True(1 = resp.Success, sprintf "failed to get Active Orders. Result was %s" (resp.ToString()))

let postOrderTest (api: PrivateApi) =
    let func = fun () -> api.PostOrder(pair = "btc_jpy", amount = "0.01", price = 1000, side = "buy", orderType = "market")
    failureTest func 60002

let cancelOrderTest(api: PrivateApi) =
    let func = fun () -> api.CancelOrder(1, "btc_jpy")
    failureTest func 50010

let cancelOrdersTest (api: PrivateApi) =
    let func = fun () -> api.CancelOrders([| 1; 2 |], "btc_jpy")
    failureTest func 30007

let getWithdrawalTest (api: PrivateApi) =
    let resp = api.GetWithdrawalAccount("btc")
    Assert.NotNull(resp)
    Assert.True(1 = resp.Success, sprintf "failed to get withdrawal account. Result was %s" (resp.ToString()))

let requestWithdrawalTest (api: PrivateApi) =
    let func = fun ()  -> api.RequestWithdrawal("jpy", "10", "37195a40-3d70-11e8-9c3c-2bd004e45303", "652036")
    failureTest func 20011

type PrivateApiTests(output: ITestOutputHelper) =

  [<Fact>]
  member this.`` Should be able to use private api properly `` () =
    use api = getPrivate()
    // get
    output.WriteLine("testing GetAssets")
    getAssetsTest(api)
    output.WriteLine("testing GetOrder")
    getOrderTest(api)
    output.WriteLine("testing GetActiveOrder")
    getActiveOrderTest(api)
    output.WriteLine("testing GetWithdrawalOrder")
    getWithdrawalTest(api)

    // post
    output.WriteLine("testing RequestWithdrawal")
    requestWithdrawalTest(api)
    output.WriteLine("testing CancelOrder")
    cancelOrderTest(api)
    output.WriteLine("testing CancelOrders")
    cancelOrdersTest(api)
    output.WriteLine("testing PostOrder")
    postOrderTest(api)