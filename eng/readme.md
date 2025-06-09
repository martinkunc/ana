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

Setup of .net user secrets for .net Aspire provisioning in dev mode
ana.AppHost is configured to use user secret `03fad75d-f8ce-4083-8bf8-cb8ef785cf37`
To store your Azure credentials, edit your:
```
code %APPDATA%\Microsoft\UserSecrets\03fad75d-f8ce-4083-8bf8-cb8ef785cf37\secrets.js
```
and add this section:
```
  "Azure": {
    "CredentialSource": "AzureCli",
    "SubscriptionId": "YOUR-AZURE-SUBSCRIPTION-ID",
    "ResourceGroupPrefix": "rg_ana",
    "Location": "Germany West Central"
  }
```
and add your subscription id, which can be found as id in output of the following command:
```
az account show
```

## CosmosDb Emulator configuration
This application requires Windows based Cosmos Db. The best way to run it on MacOs or linux is to use a Windows VM. Fortunatelly the emulator works on Windows ARM.
The limitation is because EF query with contains for Identity Role is not parsed correctly by Linux based Preview emulator.
To connect in either user Port forwarding forwarding from local 58081 (or other) to 8081 in machine, or setup Bridged networking. I was using Parallels port forwarding.


CosmosDb emulator has to be set for remote access.
First a key file has to be generated. In Powershell:
& "C:\Program Files\Azure Cosmos DB Emulator\Microsoft.Azure.Cosmos.Emulator.exe" /GenKeyFile=$env:USERPROFILE\CosmosEmulatorKey


Second step to to remove existing CosmosDB local data from $env:LOCALAPPDATA\CosmosDBEmulator. The emulator has to be stopped.
It can be done using:
```
rm -Recurse $env:LOCALAPPDATA\CosmosDBEmulator
```

Then it can be started:
& "C:\Program Files\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe" `
    /KeyFile=$env:USERPROFILE\CosmosEmulatorKey `
    /AllowNetworkAccess `
    /EnablePreviewFeatures

The start takes some time and the explorer in browser should then open. Unfortunatelly once we set another generated key, the explorer still reports the build in key and cannot connect to itself.

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



## Default user login
User Admin, Password Admin123!


