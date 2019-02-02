# bitbank.cc api client for dotnet

```fsharp
open BitBankApi
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
