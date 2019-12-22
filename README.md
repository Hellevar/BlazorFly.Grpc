# BlazorFly.Grpc ![GitHub Workflow Status (branch)](https://img.shields.io/github/workflow/status/Hellevar/BlazorFly.Grpc/Deploy%20package%20to%20Nuget/master)
BlazorFly.Grpc is a library for manual testing of your gRPC services through Blazor-based generated UI. The main goal is to provide easy to install and integrate library with simple UI and intuitive usage.

## Notes
To use this library in your project you need to enable Blazor support. For more info see [official documentation](https://docs.microsoft.com/en-us/aspnet/core/blazor/?view=aspnetcore-3.1).
This library uses Server-side Blazor, for now it cannot be used with Client-side Blazor due to gRPC usage problems itself. 
Do not try to use in Client-side apps!

## Setup
### Requirements
.Net Core 3.0+

### Installation
* Install the [BlazorFly.Grpc](https://www.nuget.org/packages/BlazorFly.Grpc/) package.
* Install the [Grpc.Net.ClientFactory](https://www.nuget.org/packages/Grpc.Net.ClientFactory) package.

### Registration
* Register BlazorFly services in the Startup
```csharp
public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddServerSideBlazor();
            
            // Add BlazorFly services and specify your gRPC client
            services.AddBlazorFlyGrpc(typeof(TestService.TestServiceClient));
            
            // There is also an overloaded version with collection of clients
            services.AddBlazorFlyGrpc(
                new List<Type> 
                { 
                  typeof(TestService.TestServiceClient),
                  typeof(AnotherService.AnotherServiceClient) 
                });
            
            // If you want to pass default metadata to all of you 
            // requests - additionally register metadata provider:
            services.AddBlazorFlyGrpc(typeof(TestService.TestServiceClient), () =>
            {
                var metadata = new Metadata();
                metadata.Add(new Metadata.Entry("testkey", "testvalue"));
                return metadata;
            });
            
            // Register you gRPC clients in DI via HttpClientFactory integration,
            // so generated component can get access and use them later
            services.AddGrpcClient<TestService.TestServiceClient>(options =>
            {
                options.Address = new Uri("https://localhost:5001");
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
```

### Class-based Blazor component integration
* BlazorFly registrates IGrpcViewTypeProvider service in the DI, so you should inject it to your 'wrapper' component, like this:
```csharp
[Route("grpcView")]
    public class GrpcView : ComponentBase
    {
        [Inject]
        public IGrpcViewTypeProvider TypeProvider { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent(0, TypeProvider.GetGrpcViewType());
            builder.CloseComponent();
        }
    }
```

### Markdown-based Blazor component integration
* If you don't want to create class-based 'wrapper' component as described above, you can use it in your another Blazor component, like this:
- Add using to your _Imports.razor
```csharp
@using BlazorFly.Grpc;
```
- Create GrpcView.razor component or use existing component and made appropriate changes like described below:
```csharp
@page "/grpcView"

@GrpcViewComponent

@code {
    [Inject] IGrpcViewTypeProvider GrpcViewTypeProvider { get; set; }

    public RenderFragment GrpcViewComponent { get; set; }

    protected override void OnInitialized()
    {
        GrpcViewComponent = builder =>
        {
            builder.OpenComponent(0, GrpcViewTypeProvider.GetGrpcViewType());
            builder.CloseComponent();
        };   
        
        base.OnInitialized();
    }
}
```

### Component route registration
* 'Wrapper' component is registered at 'grpcView' route, so change you router to make it accessible:
```html
<NavLink class="nav-link" href="grpcView">
    <span class="oi oi-plus" aria-hidden="true"></span> Grpc View
</NavLink>
```

## Examples
You can find [example projects](https://github.com/Hellevar/BlazorFly.Grpc/tree/master/examples) which contains more detailed and practical example of setup and usage.

## Tests
You can find basic tests in the [test project](https://github.com/Hellevar/BlazorFly.Grpc/tree/master/test). It heavily uses cloned repo of this [prototype testing library](https://github.com/SteveSandersonMS/BlazorUnitTestingPrototype).

## Authors
* **Hellevar** - *Initial work* - [Hellevar](https://github.com/Hellevar)

## Versions
* 0.9.0 - added ability to register and use several clients, added metadata provider
* 0.8.0 - initial work done, ability to register and use single client per page