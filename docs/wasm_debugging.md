## Debugging Blazor standalone app with hot-reload

Before we start debugging the wasm app, our aspire + api has to be running. With VS/VSCode open solution without web and launch startup project.

The approach which was working the best was to add the wasm to Aspire dashboard just for development, when hot reload doesn't work, but debugging works well. If you want also add hot reload, you have to use dotnet watch and use solution without the web project. Then debugging and hot reload works, but because web solution is excluded from the solution, the debugging experience lacks c# language service for the web, so the autocomplete is limited.

There are three key actions we need to set in order to have debugging working.
- The application has to be started under [debugging proxy server](https://learn.microsoft.com/en-us/aspnet/core/blazor/debug?view=aspnetcore-8.0&tabs=visual-studio). VS starts it automatically by target Sdk and in VS Code we can use `dotnet watch`.
- The browser used shall be chrome or edge and needs to be started with remote debugging enabled. VS handles it automatically by detecting sdk and looking at launchSettings.json.
- The debugging IDE has to be attached to the browser with remote debugging. VS does it automatically and VSCode can be configured to use a right debugging extension in the launch.json.

## The dotnet watch
dotnet watch can be started with project reference and the launch-profile has to be specified to a profile from ana.Web which has debugging `inspectUri` configured, opens right ports and has set environment variable "ASPNETCORE_ENVIRONMENT" to "Development".
Because it is compiling the application and creates debugging symbols which are sent to the browser, the project cannot be compiled by other method and should be excluded from the main solution. For ana web, it can be started using:
```
dotnet watch --project .\ana.Web.csproj --configuration Debug --launch-profile "BlazorWeb"
``` 
If it opens a browser, that is not the browser configured with remote debugging and you can close it.

in my current version seems to be a bug, which after detaching the IDE from the browser, and re-attaching again, dotnet watch throws an exception, so in this case I was restarting the dotnet watch. The error reported after reconnecting is:
```
DevToolsProxy.Run: System.Net.WebSockets.WebSocketException (0x80004005): Unable to connect to the remote server
       ---> System.Net.Http.HttpRequestException: No connection could be made because the target machine actively refused it. (127.0.0.1:62749)
```

## Browser setup
To launch browser manually, from powershell you can execute:
```
& "C:\Program Files\Google\Chrome\Application\chrome.exe" `
  --remote-debugging-port=9222 `
  --remote-debugging-address=0.0.0.0 `
  --user-data-dir="C:\chrome-debug-profile" `
  --no-first-run
```
Anyway with VS Code the launch action `blazorwasm` seems to be ignoring request attach and opens its own browser, so you probably don't need to start it explicitly.
To verify whether browser is started with debugging support, you can open Blazor remote debugging console using Shift Alt D. If you see an error that remote debugging is not enabled, check the right browser and command line arguments, which shall have `remote-debugging-port=9222`.

## Attaching the IDE
VS does it automatically to browser it launched. In VSCode, we can use  `blazorwasm`, or `pwa-chrome` in the launch.json configuration.
Unfortunatelly the `blazorwasm` extension doesn't support run on WSL (https://github.com/dotnet/runtime/issues/112860). I haven't yet tested `pwa-chrome` but it doesn't seem to work in WSL.

With aspire the application does not fully support hot-reload in the wasm application. [https://github.com/dotnet/aspire/issues/7695]Also Aspire doesn't inject the environment variables in the wwwroot/appSettings.json. This is from where Blazor wasm app reads the configuration settings. During compilation of wasm, blazor picks the file and attach it to resulting wasm, so no changes after build can be done. Because of this limitation, I have configured launch profiles for both Api and Web to static ports, because it needs to configure identity server with Web and provide the Web with the url of the Api.


The simplest way how to debug and have hot reload in place is to run Aspi+Api and Wasm application side by side. You can either start Visual Studio and start debug over the Wasm project explicitly, which will open separate browser with remote debugging, but application is not integrated with Aspire.
With VSCode you have to start dotnet watch from the folder where wasm application is located (ana.Web) with arguments to use Debug and lanuch-profile which opens the app on a static port, which Api's identity server is configured to trust:
```
dotnet watch --project .\ana.Web.csproj --configuration Debug --launch-profile "BlazorWeb"
```
Also the launch.json action has to be blazorwasm with cwd and webRoot to ana.Web/.

Due to the issue in dotnet watch or debugging proxy, after detaching, mentioned above, dotnet watch has to be restarted.

Also, when using WSL on Windows, the browser opened is a windows version of browser and blazorwasm doesn't allow to specify the path or the browser arguments. Unfortunatelly Chrome is only listening on 127.0.0.1, which is not exposed to WSL.


