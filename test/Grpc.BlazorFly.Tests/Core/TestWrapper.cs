using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Grpc.BlazorFly.Tests.Core
{
    public class TestWrapper : ComponentBase
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