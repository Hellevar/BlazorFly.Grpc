using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using BlazorFly.Grpc.Utils;
using Grpc.Core;
using Microsoft.AspNetCore.Components;

namespace BlazorFly.Grpc.Extensions
{
    internal static class MethodBuilderExtensions
    {
        private static ConstructorInfo UnaryCallFuncConstructorMethod(Type requestType, Type responseType) => typeof(Func<,,>)
            .MakeGenericType(new[] { requestType, typeof(CallOptions), typeof(AsyncUnaryCall<>).MakeGenericType(responseType) })
            .GetConstructors()[0];

        private static ConstructorInfo ClientStreamingFuncConstructorMethod(Type requestType, Type responseType) => typeof(Func<,>)
            .MakeGenericType(new[] { typeof(CallOptions), typeof(AsyncClientStreamingCall<,>).MakeGenericType(requestType, responseType) })
            .GetConstructors()[0];

        private static ConstructorInfo ServerStreamingFuncConstructorMethod(Type requestType, Type responseType) => typeof(Func<,,>)
            .MakeGenericType(new[] { requestType, typeof(CallOptions), typeof(AsyncServerStreamingCall<>).MakeGenericType(responseType) })
            .GetConstructors()[0];

        private static ConstructorInfo DuplexStreamingFuncConstructorMethod(Type requestType, Type responseType) => typeof(Func<,>)
            .MakeGenericType(new[] { typeof(CallOptions), typeof(AsyncDuplexStreamingCall<,>).MakeGenericType(requestType, responseType) })
            .GetConstructors()[0];

        private static readonly ConstructorInfo ActionConstructorMethod = typeof(Action)
            .GetConstructors()[0];

        private static readonly ConstructorInfo FuncConstructorMethod = typeof(Func<string, Task>)
            .GetConstructors()[0];

        private static readonly ConstructorInfo FuncFromStringConstructorMethod = typeof(Func<string>)
            .GetConstructors()[0];

        private static MethodInfo UtilsSendUnaryCallMethod(Type requestType, Type responseType) => typeof(GrpcClientInvoker)
            .GetMethod(nameof(GrpcClientInvoker.DoUnaryCall))
            .MakeGenericMethod(new[] { requestType, responseType });

        private static MethodInfo UtilsSendClientStreamingMethod(Type requestType, Type responseType) => typeof(GrpcClientInvoker)
            .GetMethod(nameof(GrpcClientInvoker.DoClientStreaming))
            .MakeGenericMethod(new[] { requestType, responseType });

        private static MethodInfo UtilsSendServerStreamingMethod(Type requestType, Type responseType) => typeof(GrpcClientInvoker)
            .GetMethod(nameof(GrpcClientInvoker.DoServerStreaming))
            .MakeGenericMethod(new[] { requestType, responseType });

        private static MethodInfo UtilsSendDuplexStreamingMethod(Type requestType, Type responseType) => typeof(GrpcClientInvoker)
            .GetMethod(nameof(GrpcClientInvoker.DoDuplexStreaming))
            .MakeGenericMethod(new[] { requestType, responseType });

        private static readonly MethodInfo CancellationTokenSourceCancelMethod = typeof(CancellationTokenSource)
            .GetMethod(nameof(CancellationTokenSource.Cancel),
            Type.EmptyTypes);

        private static readonly ConstructorInfo CancellationTokenSourceConstructorMethod = typeof(CancellationTokenSource)
            .GetConstructors()[0];

        private static readonly FieldInfo StringEmptyField = typeof(string)
            .GetField(nameof(string.Empty));

        private static readonly MethodInfo StateHasChangedMethod = typeof(ComponentBase)
            .GetMethod("StateHasChanged",
            BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly MethodInfo InvokeAsyncMethod = typeof(ComponentBase)
            .GetMethod("InvokeAsync",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { typeof(Action) },
            null);

        public static MethodBuilder ImplementProcessingMethod(
            this MethodBuilder method,
            PropertyInfo clientProperty,
            PropertyInfo requestProperty,
            FieldInfo cancellationTokenSourceField,
            PropertyInfo optionsProperty,
            PropertyInfo responseProperty,
            ClientMethodType methodType,
            MethodInfo clientRequestMethod,
            MethodInfo setResponseMethod,
            Type requestType,
            Type responseType,
            PropertyInfo metadataProviderProperty,
            MethodInfo clearResponseMethod)
        {
            var generator = method.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, clearResponseMethod);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, clientProperty.GetGetMethod());
            generator.Emit(OpCodes.Dup);
            generator.Emit(OpCodes.Ldvirtftn, clientRequestMethod);

            switch (methodType)
            {
                case ClientMethodType.UnaryCall:
                    {
                        generator.Emit(OpCodes.Newobj, UnaryCallFuncConstructorMethod(requestType, responseType));
                        break;
                    }
                case ClientMethodType.ClientStreaming:
                    {
                        generator.Emit(OpCodes.Newobj, ClientStreamingFuncConstructorMethod(requestType, responseType));
                        break;
                    }
                case ClientMethodType.ServerStreaming:
                    {
                        generator.Emit(OpCodes.Newobj, ServerStreamingFuncConstructorMethod(requestType, responseType));
                        break;
                    }
                default:
                    {
                        generator.Emit(OpCodes.Newobj, DuplexStreamingFuncConstructorMethod(requestType, responseType));
                        break;
                    }
            }

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, requestProperty.GetGetMethod());
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Newobj, CancellationTokenSourceConstructorMethod);
            generator.Emit(OpCodes.Stfld, cancellationTokenSourceField);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, cancellationTokenSourceField);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, optionsProperty.GetGetMethod());

            if (methodType == ClientMethodType.ServerStreaming || methodType == ClientMethodType.DuplexStreaming)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldftn, responseProperty.GetGetMethod());
                generator.Emit(OpCodes.Newobj, FuncFromStringConstructorMethod);
            }

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldftn, setResponseMethod);
            generator.Emit(OpCodes.Newobj, FuncConstructorMethod);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, metadataProviderProperty.GetGetMethod());

            switch (methodType)
            {
                case ClientMethodType.UnaryCall:
                    {
                        generator.EmitCall(OpCodes.Call, UtilsSendUnaryCallMethod(requestType, responseType), null);
                        break;
                    }
                case ClientMethodType.ClientStreaming:
                    {
                        generator.EmitCall(OpCodes.Call, UtilsSendClientStreamingMethod(requestType, responseType), null);
                        break;
                    }
                case ClientMethodType.ServerStreaming:
                    {
                        generator.EmitCall(OpCodes.Call, UtilsSendServerStreamingMethod(requestType, responseType), null);
                        break;
                    }
                default:
                    {
                        generator.EmitCall(OpCodes.Call, UtilsSendDuplexStreamingMethod(requestType, responseType), null);
                        break;
                    }
            }

            generator.Emit(OpCodes.Ret);

            return method;
        }

        public static MethodBuilder ImplementCancelProcessing(this MethodBuilder method, FieldInfo cancellationTokenSourceField)
        {
            var generator = method.GetILGenerator();
            var toReturn = generator.DefineLabel();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, cancellationTokenSourceField);
            generator.Emit(OpCodes.Brfalse, toReturn);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, cancellationTokenSourceField);
            generator.Emit(OpCodes.Callvirt, CancellationTokenSourceCancelMethod);
            generator.MarkLabel(toReturn);
            generator.Emit(OpCodes.Ret);

            return method;
        }

        public static MethodBuilder ImplementClearResponse(this MethodBuilder method, MethodInfo cancelProcessingMethod, PropertyInfo responseProperty)
        {
            var generator = method.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, cancelProcessingMethod);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldsfld, StringEmptyField);
            generator.Emit(OpCodes.Call, responseProperty.GetSetMethod());
            generator.Emit(OpCodes.Ret);

            return method;
        }

        public static MethodBuilder ImplementInvertBlockVisibility(this MethodBuilder method, PropertyInfo visibilityProperty)
        {
            var generator = method.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, visibilityProperty.GetGetMethod());
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Ceq);
            generator.Emit(OpCodes.Call, visibilityProperty.GetSetMethod());
            generator.Emit(OpCodes.Ret);

            return method;
        }

        public static MethodBuilder ImplementSetWithNotify(this MethodBuilder method, PropertyBuilder responseProperty)
        {
            var generator = method.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Call, responseProperty.GetSetMethod());
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldftn, StateHasChangedMethod);
            generator.Emit(OpCodes.Newobj, ActionConstructorMethod);
            generator.Emit(OpCodes.Call, InvokeAsyncMethod);
            generator.Emit(OpCodes.Ret);

            return method;
        }
    }
}