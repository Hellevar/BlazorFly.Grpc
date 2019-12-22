using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BlazorFly.Grpc.Extensions;
using BlazorFly.Grpc.Internal;
using BlazorFly.Grpc.Utils;
using Grpc.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorFly.Grpc.Builders
{
    internal class ComponentBuilder
    {
        public Type Build(ICollection<Type> clientTypes)
        {
            var typeBuilder = CreateComponentType();

            var implementedMethods = 0;
            var sharedLines = 0;

            var jsonOptionsProperty = CreateJsonOptionsProperty(typeBuilder);

            var constructorBuilder = typeBuilder
                .DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes)
                .StartBuilding(jsonOptionsProperty.GetSetMethod());

            var metadataProviderProperty = CreateInjectedProperty(typeBuilder, typeof(IGrpcMetadataProvider));

            var renderMethod = typeBuilder.DefineMethod(
                "BuildRenderTree",
                MethodAttributes.Public | MethodAttributes.Virtual,
                typeof(void), new[] { typeof(RenderTreeBuilder) });

            var renderMethodBuilder = new RenderMethodBuilder(renderMethod.GetILGenerator());
            StartSharedRenderBlock(renderMethodBuilder, ref sharedLines);

            foreach (var clientType in clientTypes)
            {
                StartClientRenderBlock(renderMethodBuilder, clientType, ref sharedLines);
                var grpcClientProperty = CreateInjectedProperty(typeBuilder, clientType);
                var clientMethods = ExtractGrpcMethods(clientType);

                foreach (var method in clientMethods)
                {
                    BuildMethodBlock(typeBuilder, renderMethodBuilder, grpcClientProperty, jsonOptionsProperty, implementedMethods, method, constructorBuilder, metadataProviderProperty);
                    implementedMethods++;
                }

                FinishClientRenderBlock(renderMethodBuilder, ref sharedLines);
            }            

            FinishSharedRenderBlock(renderMethodBuilder, ref sharedLines);

            renderMethodBuilder.Build();
            constructorBuilder.FinishBuilding();

            return typeBuilder.CreateType();
        }

        private List<MethodInfo> ExtractGrpcMethods(Type clientType)
        {
            var grpcReturnTypes = new[]
            {
                typeof(AsyncUnaryCall<>),
                typeof(AsyncClientStreamingCall<,>),
                typeof(AsyncServerStreamingCall<>),
                typeof(AsyncDuplexStreamingCall<,>)
            };

            var clientMethods = clientType
                .GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public)
                .Where(method => method.ReturnType.IsGenericType && grpcReturnTypes.Contains(method.ReturnType.GetGenericTypeDefinition()))
                .GroupBy(method => new { ReturnTypeName = method.ReturnType.Name, MethodName = method.Name })
                .Select(groupedMethods => groupedMethods.Where(method => method.GetParameters().Count() == groupedMethods.Min(method => method.GetParameters().Count())).FirstOrDefault())
                .ToList();

            if (!clientMethods.Any())
            {
                throw new ArgumentException($"{clientType} probably is not gRPC client type, please check registered client types!", nameof(clientType));
            }

            return clientMethods;
        }

        private TypeBuilder CreateComponentType()
        {
            var assemblyName = new AssemblyName("BlazorFly.Grpc.GeneratedAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("BlazorFly.Grpc.GeneratedModule");
            var typeBuilder = moduleBuilder.DefineType("BlazorFly.Grpc.GeneratedComponent", TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed);
            typeBuilder.SetParent(typeof(ComponentBase));

            return typeBuilder;
        }

        private void StartSharedRenderBlock(RenderMethodBuilder renderMethodBuilder, ref int sharedLines)
        {
            renderMethodBuilder.AddMarkupContent(sharedLines++, "<style>\r\n    .hidden {\r\n        visibility: collapse;\r\n        height: 0px;\r\n        padding: 0px !important;\r\n    }\r\n\r\n    .visible {\r\n        visibility: visible;\r\n        height: 100%;\r\n        transition: height 5s;\r\n    }\r\n\r\n    .clients {\r\n        display: grid;\r\n        grid-row-gap: 40px;\r\n    }\r\n\r\n    .clientHeader {\r\n        font-weight: bold;\r\n        display: inline;\r\n    }\r\n\r\n    .clientDescription {\r\n        display: inline;\r\n    }\r\n\r\n    .clientMethods {\r\n        margin-top: 20px;\r\n        display: grid;\r\n        grid-row-gap: 20px;\r\n    }\r\n\r\n    .methodData {\r\n        border-radius: 5px;\r\n        border: 1px solid;\r\n    }\r\n\r\n    .methodData.unaryCall {\r\n        background: rgba(60, 145, 230, 0.1);\r\n        border-color: rgba(60, 145, 230, 1);\r\n    }\r\n\r\n    .methodData.unaryCall .methodBody {\r\n        border-color: rgba(60, 145, 230, 1);\r\n    }\r\n\r\n    .methodData.clientStreaming {\r\n        background: rgba(159, 211, 86, 0.1);\r\n        border-color: rgba(159, 211, 86, 1);\r\n    }\r\n\r\n    .methodData.clientStreaming .methodBody {\r\n        border-color: rgba(159, 211, 86, 1);\r\n    }\r\n\r\n    .methodData.serverStreaming {\r\n        background: rgba(255, 190, 11, 0.1);\r\n        border-color: rgba(255, 190, 11, 1);\r\n    }\r\n\r\n    .methodData.serverStreaming .methodBody {\r\n        border-color: rgba(255, 190, 11, 1);\r\n    }\r\n\r\n    .methodData.duplexStreaming {\r\n        background: rgba(251, 86, 7, 0.1);\r\n        border-color: rgba(251, 86, 7, 1);\r\n    }\r\n\r\n    .methodData.duplexStreaming .methodBody {\r\n        border-color: rgba(251, 86, 7, 1);\r\n    }\r\n\r\n    .methodHeader {\r\n        cursor: pointer;\r\n        display: inline-block;\r\n        width: 100%;\r\n    }\r\n\r\n    .methodType {\r\n        text-transform: uppercase;\r\n        text-align: center;\r\n        width: 200px;\r\n        color: whitesmoke;\r\n        border-radius: 5px;\r\n        display: inherit;\r\n        margin: 5px;\r\n        padding: 5px;\r\n    }\r\n\r\n    .methodType.unaryCall {\r\n        background-color: rgba(60, 145, 230, 1);\r\n    }\r\n\r\n    .methodType.clientStreaming {\r\n        background-color: rgba(159, 211, 86, 1);\r\n    }\r\n\r\n    .methodType.serverStreaming {\r\n        background-color: rgba(255, 190, 11, 1);\r\n    }\r\n\r\n    .methodType.duplexStreaming {\r\n        background-color: rgba(251, 86, 7, 1);\r\n    }\r\n\r\n    .methodDescription {\r\n        display: inherit;\r\n    }\r\n\r\n    .methodBody {\r\n        padding: 20px;\r\n        display: grid;\r\n        border-top: 1px solid;\r\n        grid-template-columns: 1fr 1fr 1fr;\r\n        grid-template-rows: 200px 50px 200px;\r\n        grid-row-gap: 20px;\r\n        grid-column-gap: 20px;\r\n    }\r\n\r\n    .requestLabel {\r\n        grid-row-start: 1;\r\n        grid-row-end: 2;\r\n        grid-column-start: 1;\r\n        grid-column-end: 2;\r\n    }\r\n\r\n    .requestValue {\r\n        grid-row-start: 1;\r\n        grid-row-end: 2;\r\n        grid-column-start: 2;\r\n        grid-column-end: 4;\r\n        border-radius: 5px;\r\n        border: 1px solid;\r\n    }\r\n\r\n    .requestValue.unaryCall {\r\n        border-color: rgba(60, 145, 230, 1);\r\n    }\r\n\r\n    .requestValue.clientStreaming {\r\n        border-color: rgba(159, 211, 86, 1);\r\n    }\r\n\r\n    .requestValue.serverStreaming {\r\n        border-color: rgba(255, 190, 11, 1);\r\n    }\r\n\r\n    .requestValue.duplexStreaming {\r\n        border-color: rgba(251, 86, 7, 1);\r\n    }\r\n\r\n    .requestButton {\r\n        grid-row-start: 2;\r\n        grid-row-end: 3;\r\n        text-align: center;\r\n        text-transform: uppercase;\r\n        border: none;\r\n        color: white;\r\n        border-radius: 5px;\r\n    }\r\n\r\n    .requestButton.execute {\r\n        grid-column-start: 1;\r\n        grid-column-end: 2;\r\n    }\r\n\r\n    .requestButton.clear {\r\n        grid-column-start: 2;\r\n        grid-column-end: 3;\r\n    }\r\n\r\n    .requestButton.cancel {\r\n        grid-column-start: 3;\r\n        grid-column-end: 4;\r\n    }\r\n\r\n    .requestButton.unaryCall {\r\n        background-color: rgba(60, 145, 230, 1);\r\n    }\r\n\r\n    .requestButton.clientStreaming {\r\n        background-color: rgba(159, 211, 86, 1);\r\n    }\r\n\r\n    .requestButton.serverStreaming {\r\n        background-color: rgba(255, 190, 11, 1);\r\n    }\r\n\r\n    .requestButton.duplexStreaming {\r\n        background-color: rgba(251, 86, 7, 1);\r\n    }\r\n\r\n    .responseLabel {\r\n        grid-row-start: 3;\r\n        grid-row-end: 4;\r\n        grid-column-start: 1;\r\n        grid-column-end: 2;\r\n    }\r\n\r\n    .responseValue {\r\n        grid-row-start: 3;\r\n        grid-row-end: 4;\r\n        grid-column-start: 2;\r\n        grid-column-end: 4;\r\n        background-color: white;\r\n        border-radius: 5px;\r\n        border: 1px solid;\r\n    }\r\n\r\n    .responseValue.unaryCall {\r\n        border-color: rgba(60, 145, 230, 1);\r\n    }\r\n\r\n    .responseValue.clientStreaming {\r\n        border-color: rgba(159, 211, 86, 1);\r\n    }\r\n\r\n    .responseValue.serverStreaming {\r\n        border-color: rgba(255, 190, 11, 1);\r\n    }\r\n\r\n    .responseValue.duplexStreaming {\r\n        border-color: rgba(251, 86, 7, 1);\r\n    }\r\n</style>\r\n\r\n")
                .OpenElement(sharedLines++, "div")
                .AddAttribute(sharedLines++, "class", "clients")
                .AddMarkupContent(sharedLines++, "\r\n");
        }

        private void StartClientRenderBlock(RenderMethodBuilder renderMethodBuilder, Type clientType, ref int sharedLines)
        {
            renderMethodBuilder.OpenElement(sharedLines++, "div")
                .AddAttribute(sharedLines++, "class", "client")
                .AddMarkupContent(sharedLines++, "\r\n")
                .AddMarkupContent(sharedLines++, $"<div class=\"clientHeader\">{clientType.Name}</div>\r\n")
                .AddMarkupContent(sharedLines++, "<div class=\"clientDescription\">Contains all mapped gRPC methods</div>\r\n")
                .OpenElement(sharedLines++, "div")
                .AddAttribute(sharedLines++, "class", "clientMethods")
                .AddMarkupContent(sharedLines++, "\r\n");
        }

        private void FinishClientRenderBlock(RenderMethodBuilder renderMethodBuilder, ref int sharedLines)
        {
            renderMethodBuilder.AddMarkupContent(sharedLines++, "\r\n")
                .CloseElement()
                .AddMarkupContent(sharedLines++, "\r\n")
                .CloseElement();
        }

        private void FinishSharedRenderBlock(RenderMethodBuilder renderMethodBuilder, ref int sharedLines)
        {
            renderMethodBuilder.AddMarkupContent(sharedLines++, "\r\n")
                .CloseElement();
        }

        private void BuildMethodBlock(
            TypeBuilder typeBuilder,
            RenderMethodBuilder renderMethodBuilder,
            PropertyBuilder grpcClientProperty,
            PropertyBuilder jsonOptionsProperty,
            int implementedMethods,
            MethodInfo method,
            ConstructorBuilder constructorBuilder,
            PropertyBuilder metadataProviderProperty)
        {
            var clientMethodType = method.GetClientMethodType();
            var methodTypeName = clientMethodType.ToString();
            var requestType = method.GetRequestTypeFromMethod(clientMethodType);
            var responseType = method.GetResponseTypeFromMethod(clientMethodType);

            var requestProperty = CreateRequestProperty(typeBuilder, methodTypeName, implementedMethods);
            var responseProperty = CreateResponseProperty(typeBuilder, methodTypeName, implementedMethods);
            var blockVisibleProperty = CreateBlockVisiblePropertyProperty(typeBuilder, methodTypeName, implementedMethods);
            var cancellationTokenSourceField = CreateСancellationTokenSourceField(typeBuilder, methodTypeName, implementedMethods);

            constructorBuilder.InitializeBoolValue(blockVisibleProperty);
            constructorBuilder.InitializeStringValue(
                    requestType,
                    jsonOptionsProperty,
                    requestProperty,
                    clientMethodType == ClientMethodType.ClientStreaming || clientMethodType == ClientMethodType.DuplexStreaming);

            var cancelProcessingMethod = typeBuilder
                .DefineMethod($"Cancel{methodTypeName}Processing_{implementedMethods}", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), Type.EmptyTypes)
                .ImplementCancelProcessing(cancellationTokenSourceField);

            var clearResponseMethod = typeBuilder
                .DefineMethod($"Clear{methodTypeName}Response_{implementedMethods}", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), Type.EmptyTypes)
                .ImplementClearResponse(cancelProcessingMethod, responseProperty);

            var invertBlockVisibilityMethod = typeBuilder
                .DefineMethod($"Invert{methodTypeName}BlockVisibility_{implementedMethods}", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), Type.EmptyTypes)
                .ImplementInvertBlockVisibility(blockVisibleProperty);

            var setResponseWithNotifyMethod = typeBuilder
                .DefineMethod($"SetResponseFor{methodTypeName}_{implementedMethods}", MethodAttributes.Public | MethodAttributes.Virtual, typeof(Task), new[] { typeof(string) })
                .ImplementSetWithNotify(responseProperty);

            var processingMethod = typeBuilder
                .DefineMethod($"Process{methodTypeName}Request_{implementedMethods}", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void), Type.EmptyTypes)
                .ImplementProcessingMethod(
                    grpcClientProperty,
                    requestProperty,
                    cancellationTokenSourceField,
                    jsonOptionsProperty,
                    responseProperty,
                    clientMethodType,
                    method,
                    setResponseWithNotifyMethod,
                    requestType,
                    responseType,
                    metadataProviderProperty,
                    clearResponseMethod);

            var methodType = methodTypeName.Substring(0, 1).ToLowerInvariant() + methodTypeName.Substring(1, methodTypeName.Length - 1);

            renderMethodBuilder.OpenRegion(implementedMethods)
                .OpenElement(0, "div")
                .AddAttribute(1, "class", $"methodData {methodType}")
                .AddMarkupContent(2, "\r\n")
                .OpenElement(3, "div")
                .AddAttribute(4, "class", "methodHeader")
                .AddAttribute(5, "onclick", invertBlockVisibilityMethod)
                .AddMarkupContent(6, "\r\n")
                .AddMarkupContent(7, $"<div class=\"methodType {methodType}\">{clientMethodType.GetNormalizedName()}</div>\r\n")
                .AddMarkupContent(8, $"<div class=\"methodDescription\">{method.Name} method</div>\r\n")
                .CloseElement()
                .AddMarkupContent(9, "\r\n")
                .OpenElement(10, "div")
                .AddAttribute(11, "class", $"methodBody {methodType} ", blockVisibleProperty.GetGetMethod(), "hidden")
                .AddMarkupContent(12, "\r\n")
                .AddMarkupContent(13, "<p class=\"requestLabel\">Request values: </p>\r\n")
                .OpenElement(14, "textarea")
                .AddAttribute(15, "class", $"requestValue {methodType}")
                .AddAttribute(16, "value", requestProperty)
                .AddAttribute(17, "oninput", requestProperty.GetSetMethod(), requestProperty.GetGetMethod())
                .SetUpdatesAttributeName("value")
                .CloseElement()
                .AddMarkupContent(18, "\r\n\r\n")
                .OpenElement(19, "button")
                .AddAttribute(20, "class", $"requestButton execute {methodType}")
                .AddAttribute(21, "onclick", processingMethod)
                .AddMarkupContent(22, "\r\nExecute\r\n")
                .CloseElement()
                .AddMarkupContent(23, "\r\n")
                .OpenElement(24, "button")
                .AddAttribute(25, "class", $"requestButton clear {methodType}")
                .AddAttribute(26, "onclick", clearResponseMethod)
                .AddMarkupContent(27, "\r\nClear\r\n")
                .CloseElement()
                .AddMarkupContent(28, "\r\n")
                .OpenElement(29, "button")
                .AddAttribute(30, "class", $"requestButton cancel {methodType}")
                .AddAttribute(31, "onclick", cancelProcessingMethod)
                .AddMarkupContent(32, "\r\nCancel\r\n")
                .CloseElement()
                .AddMarkupContent(33, "\r\n\r\n\r\n")
                .AddMarkupContent(34, "<p class=\"responseLabel\">Response value: </p>\r\n")
                .OpenElement(35, "pre")
                .AddAttribute(36, "class", $"responseValue {methodType}")
                .AddContent(37, responseProperty)
                .CloseElement()
                .AddMarkupContent(38, "\r\n")
                .CloseElement()
                .AddMarkupContent(39, "\r\n")
                .CloseElement()
                .AddMarkupContent(40, "\r\n")
                .CloseRegion();
        }

        private PropertyBuilder CreateJsonOptionsProperty(TypeBuilder typeBuilder)
        {
            var jsonOptionsPropertyType = typeof(JsonSerializerOptions);
            var jsonOptionsPropertyName = "Options";
            var jsonOptionsField = typeBuilder.DefineField($"_{jsonOptionsPropertyName}", jsonOptionsPropertyType, FieldAttributes.Private);
            var jsonOptionsProperty = typeBuilder
                .DefineProperty(jsonOptionsPropertyName, PropertyAttributes.None, jsonOptionsPropertyType, Type.EmptyTypes)
                .ImplementSetter(typeBuilder, jsonOptionsField)
                .ImplementGetter(typeBuilder, jsonOptionsField);

            return jsonOptionsProperty;
        }

        private PropertyBuilder CreateInjectedProperty(TypeBuilder typeBuilder, Type propertyType)
        {
            var propertyName = propertyType.Name;
            var field = typeBuilder.DefineField($"_{propertyName}", propertyType, FieldAttributes.Private);
            var grpcClientProperty = typeBuilder
                .DefineProperty(propertyName, PropertyAttributes.None, propertyType, Type.EmptyTypes)
                .ImplementSetter(typeBuilder, field)
                .ImplementGetter(typeBuilder, field);
            grpcClientProperty.SetCustomAttribute(new CustomAttributeBuilder(typeof(InjectAttribute).GetConstructor(Type.EmptyTypes), Array.Empty<object>()));

            return grpcClientProperty;
        }

        private PropertyBuilder CreateRequestProperty(TypeBuilder typeBuilder, string methodTypeName, int implementedMethods)
        {
            var requestPropertyType = typeof(string);
            var requestPropertyName = $"{methodTypeName}Request_{implementedMethods}";
            var requestField = typeBuilder.DefineField($"_{requestPropertyName}", requestPropertyType, FieldAttributes.Private);
            var requestProperty = typeBuilder
                .DefineProperty(requestPropertyName, PropertyAttributes.None, requestPropertyType, Type.EmptyTypes)
                .ImplementGetter(typeBuilder, requestField)
                .ImplementSetter(typeBuilder, requestField);

            return requestProperty;
        }

        private PropertyBuilder CreateResponseProperty(TypeBuilder typeBuilder, string methodTypeName, int implementedMethods)
        {
            var responsePropertyType = typeof(string);
            var responsePropertyName = $"{methodTypeName}Response_{implementedMethods}";
            var responseField = typeBuilder.DefineField($"_{responsePropertyName}", responsePropertyType, FieldAttributes.Private);
            var responseProperty = typeBuilder
                .DefineProperty(responsePropertyName, PropertyAttributes.None, responsePropertyType, Type.EmptyTypes)
                .ImplementGetter(typeBuilder, responseField)
                .ImplementSetter(typeBuilder, responseField);

            return responseProperty;
        }

        private PropertyBuilder CreateBlockVisiblePropertyProperty(TypeBuilder typeBuilder, string methodTypeName, int implementedMethods)
        {
            var blockVisiblePropertyType = typeof(bool);
            var blockVisiblePropertyName = $"{methodTypeName}BlockVisible_{implementedMethods}";
            var blockVisibleField = typeBuilder.DefineField($"_{blockVisiblePropertyName}", blockVisiblePropertyType, FieldAttributes.Private);
            var blockVisibleProperty = typeBuilder
                .DefineProperty(blockVisiblePropertyName, PropertyAttributes.None, blockVisiblePropertyType, Type.EmptyTypes)
                .ImplementGetter(typeBuilder, blockVisibleField)
                .ImplementSetter(typeBuilder, blockVisibleField);

            return blockVisibleProperty;
        }

        private FieldBuilder CreateСancellationTokenSourceField(TypeBuilder typeBuilder, string methodTypeName, int implementedMethods)
        {
            var cancellationTokenSourceFieldType = typeof(CancellationTokenSource);
            var cancellationTokenSourceFieldName = $"_{methodTypeName}CancellationTokenSource_{implementedMethods}";
            var cancellationTokenSourceField = typeBuilder.DefineField(cancellationTokenSourceFieldName, cancellationTokenSourceFieldType, FieldAttributes.Private);

            return cancellationTokenSourceField;
        }
    }
}