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

