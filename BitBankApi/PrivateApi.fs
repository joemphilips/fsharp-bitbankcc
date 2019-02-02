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
  Assets: AssetRecord[]
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
  Orders: OrderRecord[]
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
  Trades: TradeHistoryRecord[]
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

/// Intentially not supporting async method (since nonce might not increment properly when it's used in an async method)
/// This might have space for improvement.
type PrivateApi(apiKey: string, apiSecret: string) =
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
    | :? (obj[]) as a -> [for i in a -> urlItemEncode k i] |> String.concat "&"
    | _ -> HttpUtility.UrlPathEncode k + "=" + HttpUtility.UrlPathEncode (v.ToString())

  let encodeQueryString (items: (string * _) list): string =
    match items with
    | [] -> ""
    | _ ->  "?" + String.concat "&" [ for k, v in items -> urlItemEncode k v]

  let encodeBody (items: obj): string =
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
    let absPath = PrivateBaseUrl + path
    let requestBody = TextRequest jsonBodyString
    let customizer = getHttpRequestCustomizer jsonBodyString
    Http.Request(absPath, httpMethod = "POST", body = requestBody, customizeHttpRequest = customizer) |> formatBody

  let getWithQuery path query =
    let queryString = query |> encodeQueryString
    let absPath = PrivateBaseUrl  + path + queryString
    let customizer = getHttpRequestCustomizer ("/v1" + path + queryString)
    Http.Request(absPath, httpMethod = "GET", customizeHttpRequest = customizer) |> formatBody

  let getWithNoneQuery path =
    let absPath = PrivateBaseUrl + path
    let customizer = getHttpRequestCustomizer ("/v1" + path)
    Http.Request(absPath, httpMethod = "GET", customizeHttpRequest = customizer) |> formatBody

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

  member this.GetAssets() =
    None
      |> get "/user/assets"
      |> failIfError
      |> fun res -> JsonSerializer.Deserialize<Response<AssetsRecord>>(res, StandardResolver.CamelCase)

  member this.GetOrder (orderId: int, pair: PathPair) =
    Some [("pair", pair :> obj); ("order_id", orderId :> obj)]
      |> get "/user/spot/order"
      |> failIfError
      |> fun res -> JsonSerializer.Deserialize<Response<OrderRecord>>(res, StandardResolver.CamelCase)

  member this.PostOrder(pair: PathPair, amount: string, side: string, orderType: string, ?price: int) =
    let dict = new Dictionary<string, obj>()
    dict.Add("pair", pair :> obj)
    dict.Add("amount", amount :> obj)
    dict.Add("side", side :> obj)
    dict.Add("type", orderType :> obj)
    if price <> None then dict.Add("price", optToString price)
    dict
      |> post "/user/spot/order"
      |> fun x -> printf "post order result was %s" x; x
      |> failIfError
      |> fun res -> JsonSerializer.Deserialize<Response<OrderRecord>>(res, StandardResolver.CamelCase)

  member this.CancelOrder(orderId: int, pair: PathPair) =
    let dict = new Dictionary<string, obj>()
    dict.Add("pair", pair)
    dict.Add("order_id", orderId)
    dict
      |> post "/user/spot/cancel_order"
      |> failIfError
      |> fun res -> JsonSerializer.Deserialize<Response<OrderRecord>>(res, StandardResolver.CamelCase)

  member this.CancelOrders(orderIds: int[], pair: PathPair) =
    let dict = new Dictionary<string, obj>()
    dict.Add("pair", pair :> obj)
    dict.Add("order_id", orderIds)
    dict
      |> post "/user/spot/cancel_orders"
      |> failIfError
      |> fun res -> JsonSerializer.Deserialize<Response<OrdersRecord>>(res, StandardResolver.CamelCase)

  member this.GetOrdersInfo(orderIds: int[], pair: PathPair) =
    Some [("order_ids", orderIds :> obj); ("pair", pair :> obj)]
      |> get "/user/spot/orders_info"
      |> failIfError
      |> fun res -> JsonSerializer.Deserialize<Response<OrdersRecord>>(res, StandardResolver.CamelCase)

  member this.GetActiveOrders(?pair: PathPair, ?count: int, ?fromId: int, ?endId: int, ?since: int, ?endDate: int) =
    [
      ("pair", optToString pair);
      ("count", optToString count);
      ("from_id", optToString fromId);
      ("end_id", optToString endId);
      ("since", optToString since);
      ("end", optToString endDate);
    ] |> List.filter(fun tup -> snd tup <> ("" :> obj))
      |> Some
      |> get "/user/spot/active_orders"
      |> failIfError
      |> fun res -> JsonSerializer.Deserialize<Response<OrdersRecord>>(res, StandardResolver.CamelCase)

  member this.GeTradeHistory(?pair: PathPair, ?count: int, ?orderId: int, ?since: int, ?endDate: int, ?order: string) =
    [
      ("pair", optToString pair);
      ("count", optToString count);
      ("order_id", optToString orderId);
      ("since", optToString since);
      ("end", optToString endDate);
    ] |> List.filter(fun tup -> snd tup <> ("" :> obj))
      |> Some
      |> get "/user/spot/trade_history"
      |> failIfError
      |> fun res -> JsonSerializer.Deserialize<Response<TradeHistorysRecord>>(res, StandardResolver.CamelCase)

  member this.GetWithdrawalAccount(asset: string) =
    Some [("asset", asset :> obj)]
      |> get "/user/withdrawal_account"
      |> failIfError
      |> fun res -> JsonSerializer.Deserialize<Response<WithdrawalAccountRecord>>(res, StandardResolver.CamelCase)

  member this.RequestWithdrawal(asset: string, amount: string, uuid: string, ?otpToken: string, ?smsToken: string) =
    let dict = new Dictionary<string, obj>()
    dict.Add("asset", asset)
    dict.Add("amount", amount)
    dict.Add("uuid", uuid)
    if otpToken <> None then dict.Add("otp_token", optToString otpToken)
    if smsToken <> None then dict.Add("sms_token", optToString smsToken)
    dict
      |> post "/user/request_withdrawal"
      |> failIfError
      |> fun res -> JsonSerializer.Deserialize<Response<WithdrawalRecord>>(res, StandardResolver.CamelCase)