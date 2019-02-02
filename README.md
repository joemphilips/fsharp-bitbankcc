# bitbank.cc api client for dotnet

## How to use

```fsharp
open BitBankApi

// for public api
let api = new PublicApi()
api.GetTicker("btc_jpy")

// for private api
let apiKey = "..."
let apiSecret = "..."
let api = new PrivateApi(apiKey, apiSecret)
let response = api.GetAssets()
```

and it is mostly the same in C#.

```csharp
using BitBankApi

// ...
  var api = new PrivateApi("YourApiKey", "YourApiSecret");
  var resp = api.GetAssets()
```

async methods are prefixed with `Async` , and returns `Task<T>` (instead of F#'s `Async<T>`).
Be careful when you use async methods that the order of executions is fixed.
i.e. If you try something like `Task.WaitAll([api.GetAssetsAsync(), api.GetOrderAsync()])` you may encounter wierd error.
This is because the api requires to include a nonce to the request, and the nonce has to be incremented in order.
