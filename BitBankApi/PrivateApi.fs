namespace BitBankApi
open Utils
open FSharp.Data
open System.Security.Cryptography
open System
open System.Net
open System.Text
open System.Web
open System.Collections.Generic
open Utf8Json
open Utf8Json.Resolvers
open System.Threading.Tasks
open System.Runtime.InteropServices

type BitBankApiException(code: int) =
  inherit Exception(sprintf "Error code was %s. See %s for details" (code.ToString()) ErrorCodeDescriptionUrl)
  member val Code = code with get

[<CLIMutable>]
type Response<'T> = {
  Success: int
  Data: 'T
}

[<CLIMutable>]
type ErrorCode = { Code: int }

[<CLIMutable>]
type ErrorResponse = {
  Success: int
  Data: ErrorCode
}


[<CLIMutable>]
type WithdrawalFee = private {
  Threshold: string
  Under: string
  Over: string
}

[<CLIMutable>]
type AssetRecord = {
  Asset: string
  AmountPrecision: int
  OnhandAmount: string
  LockedAmount: string
  FreeAmount: string
  StopDeposit: bool
  StopWithdrawal: bool
  WithdrawalFee: WithdrawalFee
}

[<CLIMutable>]
type AssetsRecord = {
  Assets: array<AssetRecord>
}

[<CLIMutable>]
type OrderRecord = {
  OrderId: int
  Pair: PathPair
  Side: string
  Type: string
  StartAmount: string
  RemainingAmount: string
  ExecutedAmount: string
  Price: string
  AveragePrice: string
  OrderedAt: string
  Status: string
}

[<CLIMutable>]
type OrdersRecord =  {
  Orders: array<OrderRecord>
}

[<CLIMutable>]
type TradeHistoryRecord = {
   TradeId: int 
   Pair: PathPair
   OrderId: int
   Side: string
   Type: string
   Amount: string
   Price: string
   MakerTaker: string
   FeeAmountBase: string
   FeeAmountQuote: string
   ExecutedAt: string
}

[<CLIMutable>]
type TradeHistorysRecord = {
  Trades: array<TradeHistoryRecord>
}
[<CLIMutable>]
type WithdrawalAccountRecord = {
  Uuid: string
  Label: string
  Address: string
}

[<CLIMutable>]
type WithdrawalRecord = {
  Uuid: string
  Asset: string
  Amount : int
  AccountUuid: string
  Fee: string
  Status: string
  Label: string
  Txid: string
  Address: string
}

type PrivateApi(apiKey: string, apiSecret: string, [<Optional; DefaultParameterValue(PrivateBaseUrl)>] apiUrl: string) =
  let hash = new HMACSHA256(Encoding.Default.GetBytes(apiSecret))
  let mutable nonce = UnixTimeNow()
  let utf8 = System.Text.UTF8Encoding()

  let getHttpRequestCustomizer stringToCommit =
    nonce <- nonce + 100L // the api does not recognize if only increment one here.
    let message = utf8.GetBytes(nonce.ToString() + stringToCommit)
    let signature = byteToHex (hash.ComputeHash(message))
    let customizer (req: HttpWebRequest) =
      req.ContentType <- "application/json"
      req.Headers.Add("ACCESS-KEY", apiKey)
      req.Headers.Add("ACCESS-SIGNATURE", signature)
      req.Headers.Add("ACCESS-NONCE", nonce.ToString())
      req
    customizer


  let rec urlItemEncode (k: string) (v: obj) =
    match v with
    | :? (array<obj>) as a -> [for i in a -> urlItemEncode k i] |> String.concat "&"
    | _ -> HttpUtility.UrlPathEncode k + "=" + HttpUtility.UrlPathEncode (v.ToString())

  let encodeQueryString (items: (string * _) list): string =
    match items with
    | [] -> ""
    | _ ->  "?" + String.concat "&" [ for k, v in items -> urlItemEncode k v]

  let rec encodeBody (items: obj): string =
    JsonSerializer.ToJsonString(items)

  let formatBody response =
    let respString = response.Body.ToString()
    let tmp = respString.Replace("Text\n", "")
                        .Replace("Text ", "")
                        .Replace(" ", "")
    let json = tmp.Substring(1, tmp.Length - 2)
    json

  let post path body =
    let jsonBodyString = body |> encodeBody
    let absPath = apiUrl + path
    let requestBody = TextRequest jsonBodyString
    let customizer = getHttpRequestCustomizer jsonBodyString
    async {
      let! resp = Http.AsyncRequest(absPath, httpMethod = "POST", body = requestBody, customizeHttpRequest = customizer)
      return formatBody resp
    }

  let getWithQuery path query =
    let queryString = query |> encodeQueryString
    let absPath = apiUrl  + path + queryString
    let customizer = getHttpRequestCustomizer ("/v1" + path + queryString)
    async {
      let! resp = Http.AsyncRequest(absPath, httpMethod = "GET", customizeHttpRequest = customizer)
      return formatBody resp
    }

  let getWithNoneQuery path =
    let absPath = apiUrl + path
    let customizer = getHttpRequestCustomizer ("/v1" + path)
    async {
      let! resp = Http.AsyncRequest(absPath, httpMethod = "GET", customizeHttpRequest = customizer)
      return formatBody resp
    }

  let get path (query: (string * _) list option) =
    match query with
    | Some q -> getWithQuery path q
    | None -> getWithNoneQuery path

  let optToString = function
                  | Some i -> i.ToString() :> obj
                  | None -> "" :> obj

  let failIfError (json: string) =
    let errorResponse = JsonSerializer.Deserialize<ErrorResponse>(json, StandardResolver.CamelCase)
    if errorResponse.Success = 0 then raise(BitBankApiException errorResponse.Data.Code)
    json

  interface IDisposable with
    member this.Dispose() = hash.Dispose()

  member this.GetAssetsAsync() =
    (async {
      let! resp = None |> get "/user/assets"
      let resp2 = failIfError resp
      return JsonSerializer.Deserialize<Response<AssetsRecord>>(resp2, StandardResolver.CamelCase)
    } |> Async.StartAsTask).ConfigureAwait(false)

  member this.GetAssets() =
    this.GetAssetsAsync().GetAwaiter().GetResult()

  member this.GetOrderAcync (orderId: int, pair: PathPair) =
    (async {
      let! resp =  get "/user/spot/order" (Some [("pair", pair :> obj); ("order_id", orderId :> obj)])
      let resp2 = failIfError resp
      return JsonSerializer.Deserialize<Response<OrderRecord>>(resp2, StandardResolver.CamelCase)
    } |> Async.StartAsTask).ConfigureAwait(false)

  member this.GetOrder (orderId: int, pair: PathPair) =
    this.GetOrderAcync(orderId, pair).GetAwaiter().GetResult()

  member this.PostOrderAsync(pair: PathPair, amount: string, side: string, orderType: string, [<Optional>] ?price: int) =
    let dict = new Dictionary<string, obj>()
    dict.Add("pair", pair :> obj)
    dict.Add("amount", amount :> obj)
    dict.Add("side", side :> obj)
    dict.Add("type", orderType :> obj)
    if price <> None then dict.Add("price", optToString price)
    (async {
      let! resp = post "/user/spot/order" dict
      failIfError resp |> ignore
      return JsonSerializer.Deserialize<Response<OrderRecord>>(resp, StandardResolver.CamelCase)
    } |> Async.StartAsTask).ConfigureAwait(false)

  member this.PostOrder(pair: PathPair, amount: string, side: string, orderType: string, [<Optional>] ?price: int) =
    match price with
    | Some i -> this.PostOrderAsync(pair, amount, side, orderType, i).GetAwaiter().GetResult()
    | None -> this.PostOrderAsync(pair, amount, side, orderType).GetAwaiter().GetResult()

  member this.CancelOrderAsync(orderId: int, pair: PathPair) =
    let dict = new Dictionary<string, obj>()
    dict.Add("pair", pair)
    dict.Add("order_id", orderId)
    (async {
      let! resp = post "/user/spot/cancel_order" dict
      failIfError resp |> ignore
      return JsonSerializer.Deserialize<Response<OrderRecord>>(resp, StandardResolver.CamelCase)
    } |> Async.StartAsTask).ConfigureAwait(false)

  member this.CancelOrder(orderId: int, pair: PathPair) =
    this.CancelOrderAsync(orderId, pair).GetAwaiter().GetResult()

  member this.CancelOrdersAsync(orderIds: array<int>, pair: PathPair) =
    let dict = new Dictionary<string, obj>()
    dict.Add("pair", pair)
    dict.Add("order_ids", orderIds)
    (async {
      let! resp = post "/user/spot/cancel_orders" dict
      failIfError resp |> ignore
      return JsonSerializer.Deserialize<Response<OrdersRecord>>(resp, StandardResolver.CamelCase)
    } |> Async.StartAsTask).ConfigureAwait(false)

  member this.CancelOrders(orderIds: array<int>, pair: PathPair) =
    this.CancelOrdersAsync(orderIds, pair).GetAwaiter().GetResult()

  member this.GetOrdersInfoAsync(orderIds: array<int>, pair: PathPair) =
    let arg = Some [("order_ids", orderIds :> obj); ("pair", pair :> obj)]
    (async {
      let! resp = get "/user/spot/orders_info" arg
      failIfError resp |> ignore
      return JsonSerializer.Deserialize<Response<OrdersRecord>>(resp, StandardResolver.CamelCase)
    } |> Async.StartAsTask).ConfigureAwait(false)

  member this.GetOrdersInfo(orderIds: array<int>, pair: PathPair) =
    this.GetOrdersInfoAsync(orderIds, pair).GetAwaiter().GetResult()

  member private this.GetActiveOrdersAsyncPrivate(pair: PathPair option, count: int option, fromId: int option, endId: int option, since: int option, endDate: int option) =
    let list = [
        ("pair", optToString pair);
        ("count", optToString count);
        ("from_id", optToString fromId);
        ("end_id", optToString endId);
        ("since", optToString since);
        ("end", optToString endDate);
      ]
    let arg = list |> List.filter(fun tup -> snd tup <> ("" :> obj))
                   |> Some
    (async {
      let! resp =  get "/user/spot/active_orders"arg
      failIfError resp |> ignore
      return JsonSerializer.Deserialize<Response<OrdersRecord>>(resp, StandardResolver.CamelCase)
    } |> Async.StartAsTask).ConfigureAwait(false)

  member this.GetActiveOrdersAsync([<Optional>] ?pair: PathPair,
                                   [<Optional>] ?count: int,
                                   [<Optional>] ?fromId: int,
                                   [<Optional>] ?endId: int,
                                   [<Optional>] ?since: int,
                                   [<Optional>] ?endDate: int) =
    this.GetActiveOrdersAsyncPrivate(pair, count, fromId, endId, since, endDate)

  member this.GetActiveOrders([<Optional>] ?pair: PathPair,
                              [<Optional>] ?count: int,
                              [<Optional>] ?fromId: int,
                              [<Optional>] ?endId: int,
                              [<Optional>] ?since: int,
                              [<Optional>] ?endDate: int) =
    this.GetActiveOrdersAsyncPrivate(pair, count, fromId, endId, since, endDate).GetAwaiter().GetResult()

  member private this.GetTradeHistoryAsyncPrivate(pair: PathPair option, count: int option, orderId: int option, since: int option, endDate: int option) =
    let list = [
      ("pair", optToString pair);
      ("count", optToString count);
      ("order_id", optToString orderId);
      ("since", optToString since);
      ("end", optToString endDate);
    ]
    let arg = list |> List.filter(fun tup -> snd tup <> ("" :> obj))
                   |> Some
    (async {
      let! resp = get "/user/spot/trade_history" arg
      failIfError resp |> ignore
      return JsonSerializer.Deserialize<Response<TradeHistorysRecord>>(resp, StandardResolver.CamelCase)
    } |> Async.StartAsTask).ConfigureAwait(false)

  member this.GetTradeHistoryAsync([<Optional>] ?pair: PathPair,
                                   [<Optional>] ?count: int,
                                   [<Optional>] ?orderId: int,
                                   [<Optional>] ?since: int, 
                                   [<Optional>] ?endDate: int) =
    this.GetTradeHistoryAsyncPrivate(pair, count, orderId, since, endDate)

  member this.GetTradeHistory([<Optional>] ?pair: PathPair,
                              [<Optional>] ?count: int,
                              [<Optional>] ?orderId: int,
                              [<Optional>] ?since: int,
                              [<Optional>] ?endDate: int) =
    this.GetTradeHistoryAsyncPrivate(pair, count, orderId, since, endDate).GetAwaiter().GetResult()

  member this.GetWithdrawalAccountAsync(asset: string) =
    let arg = Some [("asset", asset :> obj)]
    (async {
      let! resp = get "/user/withdrawal_account" arg
      failIfError resp |> ignore
      return JsonSerializer.Deserialize<Response<WithdrawalAccountRecord>>(resp, StandardResolver.CamelCase)
    } |> Async.StartAsTask).ConfigureAwait(false)

  member this.GetWithdrawalAccount(asset: string) =
    this.GetWithdrawalAccountAsync(asset).GetAwaiter().GetResult()

  member private this.RequestWithdrawalAsyncPrivate(asset: string, amount: string, uuid: string, otpToken: string option, smsToken: string option) =
    let dict = new Dictionary<string, obj>()
    dict.Add("asset", asset)
    dict.Add("amount", amount)
    dict.Add("uuid", uuid)
    if otpToken <> None then dict.Add("otp_token", optToString otpToken)
    if smsToken <> None then dict.Add("sms_token", optToString smsToken)
    (async {
      let! resp =  post "/user/request_withdrawal" dict
      failIfError resp |> ignore
      return JsonSerializer.Deserialize<Response<WithdrawalRecord>>(resp, StandardResolver.CamelCase)
    } |> Async.StartAsTask).ConfigureAwait(false)

  member this.RequestWithdrawalAsync(asset: string, amount: string, uuid: string, [<Optional>] ?otpToken: string, [<Optional>] ?smsToken: string) =
    this.RequestWithdrawalAsyncPrivate(asset, amount, uuid, otpToken, smsToken)

  member this.RequestWithdrawal(asset: string, amount: string, uuid: string, [<Optional>] ?otpToken: string, [<Optional>] ?smsToken: string) =
    this.RequestWithdrawalAsyncPrivate(asset, amount, uuid, otpToken, smsToken).GetAwaiter().GetResult()