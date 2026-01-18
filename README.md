# OpenDMA REST client for Python

Please see [OpenDMA API for C#](https://github.com/OpenDMA/opendma-csharp-api)
for an introduction to OpenDMA and it's C# API.

Provides an OpenDMA Adaptor consuming the [OpenDMA REST API](https://github.com/OpenDMA/opendma-spec/blob/main/opendma-rest-api-spec.yaml).

## Installation

This library is available via [NuGet](https://nuget.org/). You can add OpenDMA.Remote to any .NET using...

#### ...the .NET CLI
```
dotnet add package OpenDMA.Remote
```

#### ...Visual Studio
1. Right-click your project → **Manage NuGet Packages...**
2. Search for **OpenDMA.Remote**
3. Click **Install**

## Usage

Use the `RemoteSessionFactory.Connect` method to establish an `IOdmaSession`
with a REST-ful OpenDMA service:

```csharp
using OpenDMA.Api;
using OpenDMA.Remote;

var session = RemoteSessionFactory.Connect(
    endpoint: "http://127.0.0.1:8080/opendma",
    username: "user",
    password: "secret");
```

## Example

Run the tutorial REST service docker container:
```
docker run -p 8080:8080 ghcr.io/opendma/tutorial-xmlrepo:0.8
```

It will provide the tutorial xml repository. Make sure that this service is available
by opening  
http://localhost:8080/opendma  
in a web browser.

<details>
  <summary>Troubleshooting</summary>
  
  If the GitHub Container Registry (ghcr.io) is blocked in your environment, you can use our mirror on Docker Hub:
  ```
  docker run -p 8080:8080 opendma/tutorial-xmlrepo:latest
  ```

  If the local port 8080 is already in use, you can map to a different port, e.g. 8090:
  ```
  docker run -p 8090:8080 ghcr.io/opendma/tutorial-xmlrepo:latest
  ```

  To run in deamon mode in the background:
  ```
  docker run -d --name opendma-tutorial-xmlrepo -p 8080:8080 ghcr.io/opendma/tutorial-xmlrepo:latest
  ```

</details>

Check out this repository, open the `OpenDMA.Remote.Example` project and run `OpenDMAExample`.