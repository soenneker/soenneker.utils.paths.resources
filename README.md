[![](https://img.shields.io/nuget/v/soenneker.utils.paths.resources.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.utils.paths.resources/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.paths.resources/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.utils.paths.resources/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.utils.paths.resources.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.utils.paths.resources/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.paths.resources/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.utils.paths.resources/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Utils.Paths.Resources
A utility library for retrieving the Resources path across environments.

## Installation

```bash
dotnet add package Soenneker.Utils.Paths.Resources
```

## Quick start

```csharp
using Soenneker.Utils.Paths.Resources.Registrars;

services.AddResourcesPathUtilAsSingleton();
```

Then inject `IResourcesPathUtil` wherever you need it.

## Common operations

- `Get()` - Returns the absolute path to the "Resources" directory according to the resolution order.
- `GetResourceFilePath()` - Absolute path to a file under /Resources.
