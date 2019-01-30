namespace BitBankApi
open FSharp.Data
open BitBankApi.Utils
open System.Net.Http

module PublicApi =
  type GetTransactionReponse = JsonProvider<GetTransactionsUrl>

  type GetDailyTransactionReponse = JsonProvider<GetDailyTransactionsUrl>

  type GetTickerResponse = JsonProvider<GetTickerUrl>

  type GetDepthResponse = JsonProvider<GetDepthUrl>

  type GetCandleStickResponse = JsonProvider<GetCandleStickUrl>

  let client = new HttpClient()

  let path pair = BaseUrl + "/" + pair

  let GetTransactions (pair: PathPair) (yyyymmdd: string option) =
    let endpoint = match yyyymmdd with
                   | None -> path pair + "/transactions"
                   | Some date -> path pair + "/transactions" + "/" + date
    GetTransactionReponse.Load(endpoint)
  
  let GetTicker (pair: PathPair) =
    GetTickerResponse.Load(path pair + "/ticker")

  let GetDepth (pair: PathPair) =
    GetDepthResponse.Load(path pair + "/depth")

  let GetCandleStick (pair: PathPair) (candleType: CandleType) (yyyymmdd: string) =
    GetCandleStickResponse.Load(path pair + "/candlestick/" + candleType + "/" + yyyymmdd)