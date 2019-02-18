namespace BitBankApi
open System.IO
open System

module Utils =

  /// --------- public apis ----------
  [<Literal>]
  let BaseUrl = "https://public.bitbank.cc"

  [<Literal>]
  let PrivateBaseUrl = "https://api.bitbank.cc/v1"

  [<Literal>]
  let ErrorCodeDescriptionUrl = "https://docs.bitbank.cc/error_code/"

  [<Literal>]
  let StubPair = "/btc_jpy"

  [<Literal>]
  let GetTransactionsUrl = BaseUrl + StubPair + "/transactions"
 
  [<Literal>]
  let GetDailyTransactionsUrl = BaseUrl + StubPair + "/transactions/20181225"

  [<Literal>]
  let GetTickerUrl = BaseUrl + StubPair + "/ticker"

  [<Literal>]
  let GetDepthUrl = BaseUrl + StubPair + "/depth"

  [<Literal>]
  let GetCandleStickUrl = BaseUrl + StubPair + "/candlestick/1hour/20181225"

  /// --------- private apis ----------
  [<Literal>]
  let GetAssetsUrl = BaseUrl + "/v1" + "/user/assets"

  /// --------- else -------------
  type CandleType = string

  type PathPair = string

  let UnixTimeNow () =
    let genesis = new DateTime(1970, 1, 1)
    DateTime.UtcNow.Subtract(genesis).Ticks

  let byteToHex (bytes: seq<byte>) =
   let sb = System.Text.StringBuilder()
   bytes
       |> Seq.iter (fun b -> b.ToString("x2") |> sb.Append |> ignore)
   string sb
