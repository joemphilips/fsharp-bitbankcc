namespace BitBankApi
open Utils
open FSharp.Data
open System.Security.Cryptography
open System
open System.Net
open System.Text
open System.Web
open Newtonsoft.Json

type GetAssetsResponse = JsonProvider<"""
    {"success":1,"data":{"assets":[{"asset":"jpy","amount_precision":4,"onhand_amount":"0.0000","locked_amount":"0.0000","free_amount":"0.0000","stop_deposit":false,"stop_withdrawal":false,"withdrawal_fee":{"threshold":"30000.0000","under":"540.0000","over":"756.0000"}},{"asset":"btc","amount_precision":8,"onhand_amount":"0.00000000","locked_amount":"0.00000000","free_amount":"0.00000000","stop_deposit":false,"stop_withdrawal":false,"withdrawal_fee":"0.00100000"},{"asset":"ltc","amount_precision":8,"onhand_amount":"0.00000000","locked_amount":"0.00000000","free_amount":"0.00000000","stop_deposit":false,"stop_withdrawal":false,"withdrawal_fee":"0.00100000"},{"asset":"xrp","amount_precision":6,"onhand_amount":"0.000000","locked_amount":"0.000000","free_amount":"0.000000","stop_deposit":false,"stop_withdrawal":false,"withdrawal_fee":"0.150000"},{"asset":"eth","amount_precision":8,"onhand_amount":"0.00000000","locked_amount":"0.00000000","free_amount":"0.00000000","stop_deposit":false }]}}
    """>
type GetOrderResponse = JsonProvider<"""
    {"success": 1,"data":{ "code": 0 }}
    """>

[<AutoOpen>]
type PrivateApi(apiKey: string, apiSecret: string) =
  let hash = new HMACSHA256(Encoding.Default.GetBytes(apiSecret))
  let nonce = UnixTimeNow()

  let getHttpRequestCustomizer absPath stringToCommit =
    let utf8Enc = System.Text.UTF8Encoding()
    let message = utf8Enc.GetBytes(nonce + stringToCommit)
    let signature = byteToHex (hash.ComputeHash(message))
    let customizer (req: HttpWebRequest) =
      req.ContentType <- "application/json"
      req.Headers.Add("ACCESS-KEY", apiKey)
      req.Headers.Add("ACCESS-SIGNATURE", signature)
      req.Headers.Add("ACCESS-NONCE", nonce)
      req
    customizer

  let encodeQueryString (items: #seq<string * _>): string =
      "?" + String.concat "&" [ for k, v in items -> HttpUtility.UrlPathEncode k + "=" + HttpUtility.UrlPathEncode v]

  let post path query =
    let absPath = PrivateBaseUrl + path
    let customizer = getHttpRequestCustomizer absPath query
    Http.Request(absPath, httpMethod = "POST", customizeHttpRequest = customizer)

  let getWithQuery path query =
    let queryString = query |> encodeQueryString
    let absPath = PrivateBaseUrl + path + queryString
    let customizer = getHttpRequestCustomizer absPath ("/v1" + path + queryString)
    printf "Going to request to %s \n" absPath
    printf "with query string %s \n" queryString
    Http.Request(absPath, httpMethod = "GET", customizeHttpRequest = customizer)

  let getWithNoneQuery path =
    let absPath = PrivateBaseUrl + path
    let customizer = getHttpRequestCustomizer absPath ("/v1" + path)
    Http.Request(absPath, httpMethod = "GET", customizeHttpRequest = customizer)

  let get path (query: (string * string) list option) =
    match query with
    | Some q -> getWithQuery path q
    | None -> getWithNoneQuery path

  let formatBody response =
    let respString = response.Body.ToString()
    let tmp = respString.Replace("Text\n", "")
                        .Replace("Text ", "")
                        .Replace(" ", "")
    let json = tmp.Substring(1, tmp.Length - 2)
    json

  let encodeBody (items: Map<string, _>): string =
    let jtw = new JsonTextWriter(new System.IO.StringWriter())
    jtw.WriteStartObject()
    for i in items do
      jtw.WritePropertyName(i.Key)
      jtw.WriteValue(i.Value.ToString())
    jtw.WriteEndObject()
    jtw.Flush()
    jtw.ToString()

  interface IDisposable with
    member this.Dispose() = hash.Dispose()

  member this.GetAssets() =
    get "/user/assets" None |> formatBody |> GetAssetsResponse.Parse

  member this.GetOrder (orderId, pair: PathPair) =
    Some [("pair", pair); ("order_id", orderId)]
      |> get "/user/spot/order"
      |> formatBody
      |> GetOrderResponse.Parse
