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


type GetAssetsResponse = JsonProvider<"""
    {"success":1,"data":{ "code": 1, "assets":[{"asset":"jpy","amount_precision":4,"onhand_amount":"0.0000","locked_amount":"0.0000","free_amount":"0.0000","stop_deposit":false,"stop_withdrawal":false,"withdrawal_fee":{"threshold":"30000.0000","under":"540.0000","over":"756.0000"}},{"asset":"btc","amount_precision":8,"onhand_amount":"0.00000000","locked_amount":"0.00000000","free_amount":"0.00000000","stop_deposit":false,"stop_withdrawal":false,"withdrawal_fee":"0.00100000"},{"asset":"ltc","amount_precision":8,"onhand_amount":"0.00000000","locked_amount":"0.00000000","free_amount":"0.00000000","stop_deposit":false,"stop_withdrawal":false,"withdrawal_fee":"0.00100000"},{"asset":"xrp","amount_precision":6,"onhand_amount":"0.000000","locked_amount":"0.000000","free_amount":"0.000000","stop_deposit":false,"stop_withdrawal":false,"withdrawal_fee":"0.150000"},{"asset":"eth","amount_precision":8,"onhand_amount":"0.00000000","locked_amount":"0.00000000","free_amount":"0.00000000","stop_deposit":false }]}}
    """>
type OrderResponse = JsonProvider<"""
    {
     "success": 1,
     "data": {
       "code": 0 ,
       "order_id": 0,
       "pair": "btc_jpy",
       "side": "buy",
       "type": "market",
       "start_amount": "0.1000",
       "remaining_amount": "0.1000",
       "executed_amount": "0.1000",
       "price": "0.1000",
       "average_price": "0.1000",
       "ordered_at": 1400000000,
       "status": "good"
     }}
    """>

type OrdersResponse = JsonProvider<"""
  {
    "success": 1,
    "orders": [
    {
     "data": {
       "code": 0 ,
       "order_id": 0,
       "pair": "btc_jpy",
       "side": "buy",
       "type": "market",
       "start_amount": "0.1000",
       "remaining_amount": "0.1000",
       "executed_amount": "0.1000",
       "price": "0.1000",
       "average_price": "0.1000",
       "ordered_at": 1400000000,
       "status": "good"
     }}
     ]
    }""">

type TradeHistoryResponse = JsonProvider<"""
  {
    "success": 1,
    "trades": [
      {"trade_id": 1111111},
      {"pair": "btc_jpy"},
      {"order_id": 100000},
      {"side": "buy"},
      {"type": "market"},
      {"amount": "10000"},
      {"price": "0.1000"},
      {"marker_taker": "maker"},
      {"fee_amount_base": "0.001"},
      {"fee_amount_quote": "0.001"},
      {"executed_at": 1111111}
    ]
  }""">

type WithdrawalAccountResponse = JsonProvider<"""
 {
   "success": 1,
   "code": 10001,
   "uuid": "37195a40-3d70-11e8-9c3c-2bd004e45303",
   "label": "foobar",
   "address": "3FcxyAjrC5fumngYg4LeNJAqPqq6QP3fKK"
 }
""">

type WithdrawalResponse = JsonProvider<"""
  {
     "success": 1,
     "code": 10001,
     "uuid": "37195a40-3d70-11e8-9c3c-2bd004e45303",
     "asset": "btc",
     "amount": 0.01,
     "account_uuid": "37195a40-3d70-11e8-9c3c-2bd004e45303",
     "fee": "0.00001",
     "status": "active",
     "label": "foobar",
     "txid": "47123199c08715c4375ed44796b80857be4b1fc5d145dd11f9192a988ddcd3d0",
     "address": "3FcxyAjrC5fumngYg4LeNJAqPqq6QP3fKK"
  }
  """>

[<AutoOpen>]
type PrivateApi(apiKey: string, apiSecret: string) =
  let hash = new HMACSHA256(Encoding.Default.GetBytes(apiSecret))
  let mutable nonce = UnixTimeNow()

  let getHttpRequestCustomizer stringToCommit =
    nonce <- nonce + 100L // the api does not recognize if only increment one here.
    let utf8Enc = System.Text.UTF8Encoding()
    let message = utf8Enc.GetBytes(nonce.ToString() + stringToCommit)
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

  interface IDisposable with
    member this.Dispose() = hash.Dispose()

  member this.GetAssets() =
    None
      |> get "/user/assets"
      |> GetAssetsResponse.Parse

  member this.GetOrder (orderId: int, pair: PathPair) =
    Some [("pair", pair :> obj); ("order_id", orderId :> obj)]
      |> get "/user/spot/order"
      |> OrderResponse.Parse

  member this.PostOrder(pair: PathPair, amount: string, side: string, orderType: string, ?price: int) =
    let dict = new Dictionary<string, obj>()
    dict.Add("pair", pair :> obj)
    dict.Add("amount", amount :> obj)
    dict.Add("side", side :> obj)
    dict.Add("type", orderType :> obj)
    if price <> None then dict.Add("price", optToString price)
    dict
      |> post "/user/spot/order"
      |> OrderResponse.Parse

  member this.CancelOrder(orderId: int, pair: PathPair) =
    let dict = new Dictionary<string, obj>()
    dict.Add("pair", pair)
    dict.Add("order_id", orderId)
    dict
      |> post "/user/spot/cancel_order"
      |> OrderResponse.Parse

  member this.CancelOrders(orderIds: int[], pair: PathPair) =
    let dict = new Dictionary<string, obj>()
    dict.Add("pair", pair :> obj)
    dict.Add("order_id", orderIds)
    dict
      |> post "/user/spot/cancel_orders"
      |> OrdersResponse.Parse

  member this.GetOrdersInfo(orderIds: int[], pair: PathPair) =
    Some [("order_ids", orderIds :> obj); ("pair", pair :> obj)]
      |> get "/user/spot/orders_info"
      |> OrdersResponse.Parse

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
      |> OrdersResponse.Parse

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
      |> TradeHistoryResponse.Parse

  member this.GetWithdrawalAccount(asset: string) =
    Some [("asset", asset :> obj)]
      |> get "/user/withdrawal_account"
      |> WithdrawalAccountResponse.Parse

  member this.RequestWithdrawal(asset: string, amount: string, uuid: string, ?otpToken: string, ?smsToken: string) =
    let dict = new Dictionary<string, obj>()
    dict.Add("asset", asset)
    dict.Add("amount", amount)
    dict.Add("uuid", uuid)
    if otpToken <> None then dict.Add("otp_token", optToString otpToken)
    if smsToken <> None then dict.Add("sms_token", optToString smsToken)
    dict
      |> post "/user/request_withdrawal"
      |> WithdrawalResponse.Parse