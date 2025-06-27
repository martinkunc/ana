## Installation
## Requirements

Azd (Azure Developer CLI)

Windows:
```
winget install microsoft.azd
```

Create/update resources:
```
azd up
```

Deployment
```
azd deploy
```

az cli
Windows:
```
winget install --exact --id Microsoft.AzureCLI
```

Bicep:
```
winget install -e --id Microsoft.Bicep
```



## CosmosDb Emulator configuration
This application requires Windows based Cosmos Db. The best way to run it on MacOs or linux is to use a Windows VM. Fortunatelly the emulator works on Windows ARM.
The limitation is because EF query with contains for Identity Role is not parsed correctly by Linux based Preview emulator.
To connect in either user Port forwarding forwarding from local 58081 (or other) to 8081 in machine, or setup Bridged networking. I was using Parallels port forwarding.


CosmosDb emulator in the Windows machine has to be set for remote access.
First a key file has to be generated. In Powershell:
& "C:\Program Files\Azure Cosmos DB Emulator\Microsoft.Azure.Cosmos.Emulator.exe" /GenKeyFile=$env:USERPROFILE\CosmosEmulatorKey


Second step to to remove existing CosmosDB local data from $env:LOCALAPPDATA\CosmosDBEmulator. The emulator has to be stopped.
It can be done using:
```
rm -Recurse $env:LOCALAPPDATA\CosmosDBEmulator
```

Then it can be started:
```
& "C:\Program Files\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe" `
    /KeyFile=$env:USERPROFILE\CosmosEmulatorKey `
    /AllowNetworkAccess `
    /EnablePreviewFeatures
```

The start takes some time and the explorer in browser should then open. Unfortunately once we set another generated key, the explorer still reports the build in key and cannot connect to itself.

We can use VSCode with CosmosDb extension which uses connection string to connect. VSCode has to be set to relax self signed certificate policy to trust them.
Is VsCode settings under User there is `http.proxyStrictSSL` which should be unchecked and the studio would need to be restarted to take effect.

To create a connection string we would need content of the earlier generated CosmosEmulatorKey file. In my case it looked like:
```
+Jc58XdOA1ukucCS0Vg6LIfasG+sAZVbuEOPlFv5XXpwSYGdVdjy9y9bzkm4HKDJJdvukG3K/ugUpcePYPowNg==
```
Then full connection string with port forwarding is similar to this example, where you have to replace the key with yours and please note port I used for forwarding.
```
AccountEndpoint=https://localhost:58081/;AccountKey=+Jc58XdOA1ukucCS0Vg6LIfasG+sAZVbuEOPlFv5XXpwSYGdVdjy9y9bzkm4HKDJJdvukG3K/ugUpcePYPowNg==
```

Finally under Azure Side Bar button, Workspace, CosmosDb the connection to NoSql CosmosDb can be created.

The connection string has to be set to application using these commands, replace with your connection string with access key.

```
COSMOS_CONN_STR="AccountEndpoint=https://localhost:58081/;AccountKey=+Jc58XdOA1ukucCS0Vg6LIfasG+sAZVbuEOPlFv5XXpwSYGdVdjy9y9bzkm4HKDJJdvukG3K/ugUpcePYPowNg=="
cd ana.AppHost
dotnet user-secrets set "ConnectionStrings:cosmos-db" "$COSMOS_CONN_STR"
cd ..
```




## Default user login
Default user is admin. His password shall be set locally by using this command, where instead of "123", specify the password.

```
cd ana.AppHost
dotnet user-secrets set "DefaultAdminPassword" "123" -p ana.AppHost/ana.AppHost.csproj
cd ..
```

## Add Issuer signing key to app's secrets
We need to set the key used to validate Identity server's generate JWT token among clients and Api and for this we need to generate secret key. Issue these commands:
```
ISSUER_SECRET_KEY=$(openssl rand -base64 32)
cd ana.AppHost
dotnet user-secrets set "issuer-signing-key" "$ISSUER_SECRET_KEY"
cd ..
```

## Other secrets
cd ana.AppHost
FROM_EMAIL="email@domain.com"
dotnet user-secrets set "from-email" "$FROM_EMAIL"

FROM_EMAIL="email@domain.com"
dotnet user-secrets set "from-email" "$FROM_EMAIL"


## Viewing the secrets
Setup of .net user secrets for .net Aspire provisioning in dev mode
ana.AppHost is configured to use user secret `03fad75d-f8ce-4083-8bf8-cb8ef785cf37`
You can see the secrets using.
```
code %APPDATA%\Microsoft\UserSecrets\03fad75d-f8ce-4083-8bf8-cb8ef785cf37\secrets.js
```
On Linux and Mac:
```
code ~/.microsoft/usersecrets/03fad75d-f8ce-4083-8bf8-cb8ef785cf37/secrets.json
```