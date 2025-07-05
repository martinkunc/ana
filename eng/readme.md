## Installation
## Requirements

### Azd (Azure Developer CLI)

Windows:
```
winget install microsoft.azd
```

Linux has instructions at https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd?tabs=winget-windows%2Cbrew-mac%2Cscript-linux&pivots=os-linux

### Azd configuration
When azd is used to deploy Azure Kubernets Apps with custom domains set previously in Portal, the custom domain gets overwritten by the deployment. This can be prevented by configuring azd using:
```
azd config set alpha.aca.persistDomains on
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

Linux has instructions on https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-linux?view=azure-cli-latest&pivots=apt

To authenticate then:
```
az auth
```


Bicep (optional):
```
winget install -e --id Microsoft.Bicep
```

Node:
Use install instructions from: https://nodejs.org/en/download
I tested it with v22.17.0.
```
cd ana.react
npm ci
```



## CosmosDb Emulator configuration
This application requires Windows based Cosmos Db. The best way to run it on MacOs or Linux is to use a Windows VM. Fortunatelly the emulator works on Windows ARM.
The limitation is because EF query with contains for Identity Role is not parsed correctly by Linux based Preview emulator.
To connect in either user Port forwarding forwarding from local 58081 (or other) to 8081 in machine, or setup Bridged networking. I was using port forwarding with Paralels on MacOs and Port forwarding in VirtualBox on Linux.


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
dotnet user-secrets set "ana-default-admin-password" "123" -p ana.AppHost/ana.AppHost.csproj
```

If the default admin password should be empty, it needs to be set explicitly using:
```
dotnet user-secrets set "ana-default-admin-password-is-empty" "true" -p ana.AppHost/ana.AppHost.csproj
```

## Add Issuer signing key to app's secrets
We need to generate secrets for Identity server clients blazor and webapp. Issue these commands:
```
WEBAPP_SECRET_KEY=$(openssl rand -base64 32)
dotnet user-secrets set "ana-webapp-clientsecret" "$WEBAPP_SECRET_KEY" -p ana.AppHost/ana.AppHost.csproj

BLAZOR_SECRET_KEY=$(openssl rand -base64 32)
dotnet user-secrets set "ana-blazor-clientsecret" "$BLAZOR_SECRET_KEY" -p ana.AppHost/ana.AppHost.csproj
```

## SendGrid secrets
```
FROM_EMAIL="email@domain.com"
dotnet user-secrets set "ana-from-email" "$FROM_EMAIL" -p ana.AppHost/ana.AppHost.csproj

SENDGRID_KEY="SG.WY-xxxxx"
dotnet user-secrets set "ana-sendgrid-key" "$SENDGRID_KEY" -p ana.AppHost/ana.AppHost.csproj
```

## Twilio WhatsApp secrets
```
TWILIO_ACCOUNTSID="AC-xxxxx"
dotnet user-secrets set "ana-twilio-accountsid" "$TWILIO_ACCOUNTSID" -p ana.AppHost/ana.AppHost.csproj

TWILIO_ACCOUNTTOKEN="ccxxxxxx"
dotnet user-secrets set "ana-twilio-accounttoken" "$TWILIO_ACCOUNTTOKEN" -p ana.AppHost/ana.AppHost.csproj

WHATSAPP_FROM="whatsapp:+xxx"
dotnet user-secrets set "ana-whatsapp-from" "$WHATSAPP_FROM" -p ana.AppHost/ana.AppHost.csproj

```


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

## Making the self signed development certificate trusted for local web development
Execute:
```
dotnet dev-certs https --trust
```

If using CHrome, then import the certificate to Chrome using:
1. Export the .NET dev certificate:
```
dotnet dev-certs https --export-path ~/aspnet-dev-cert.crt --format PEM
```
2. Open Chrome settings:
Go to Settings → Privacy and security → Security → Manage certificates → Authorities.

3. Import the certificate:

Click “Import” and select ~/aspnet-dev-cert.crt.
Trust it for identifying websites.

## Received email notifications
The email notifications are for development purposes send from an gmail account, which by default
is mostly classified as a spam. Please check your spam folder an if you don't see a notification you should.

## Whatsapp sandbox
My Twilio subscription is still being verified, so, it can only send messages to verified numbers, which joined a sandbox.

