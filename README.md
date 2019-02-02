# bitbank.cc api client for dotnet

Currently only support syncronous API, since the api requries to increment the nonce for each request,
And async method may screw up the order of requests.

Write your own wrapper if you want to use async mehtods, and you are sure that it won't break the order of nonce.

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
