using BlazorFly.Grpc;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorApp.Pages
{
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
}