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

## Resolve the Resources directory

```csharp
string resourcesDirectory = await resourcesPathUtil.Get(cancellationToken);
string templatePath = await resourcesPathUtil.GetResourceFilePath(
    Path.Combine("Templates", "invoice.html"),
    cancellationToken);
```

`GetResourceFilePath` accepts a relative file or nested path and guarantees the normalized result
stays beneath the resolved Resources directory. Rooted paths and `..` traversal outside that
directory throw `InvalidOperationException`. The method only constructs a path; it does not require
the file to exist.

The directory is resolved in this order:

1. Existing directory from `RESOURCES_DIR`.
2. `Resources` beside the running application's base directory.
3. Azure `HOME/site/wwwroot/Resources` for Functions or App Service.
4. A `Resources` directory found from the current directory within a GitHub Actions workspace, then `<GITHUB_WORKSPACE>/Resources`.
5. A `Resources` directory found by walking upward from the current directory.
6. `HOME/site/wwwroot/Resources` outside Azure.
7. The application-base `Resources` path as a last resort, even when it does not exist.

Resolution never creates the directory. The first result is cached by each utility instance, so
changes to environment variables, the current directory, or the filesystem are not observed by
later calls on that instance. Singleton registration shares that cached decision application-wide;
scoped registration resolves once per scope.
