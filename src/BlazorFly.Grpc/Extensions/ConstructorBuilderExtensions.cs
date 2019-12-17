using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using BlazorFly.Grpc.Utils;
using Microsoft.AspNetCore.Components;

namespace BlazorFly.Grpc.Extensions
{
    internal static class ConstructorBuilderExtensions
    {
        private static readonly ConstructorInfo JsonOptionsConstructor = typeof(JsonSerializerOptions)
            .GetConstructor(Type.EmptyTypes);

        private static readonly PropertyInfo JsonOptionsWriteIntendedProperty = typeof(JsonSerializerOptions)
            .GetProperty(nameof(JsonSerializerOptions.WriteIndented));

        private static readonly ConstructorInfo ComponentBaseConstructor = typeof(ComponentBase)
            .GetConstructor(Type.EmptyTypes);

        private static MethodInfo InitializeStringValueMethod(Type type) => typeof(GrpcClientInvoker)
            .GetMethod(nameof(GrpcClientInvoker.InitializeStringValue),
            1,
            new[] { typeof(JsonSerializerOptions), typeof(bool) })
            .MakeGenericMethod(type);

        private static readonly MethodInfo OnInitializedMethod = typeof(ComponentBase)
            .GetMethod("OnInitialized",
            BindingFlags.NonPublic | BindingFlags.Instance);

        public static ConstructorBuilder StartBuilding(this ConstructorBuilder constructorBuilder, MethodInfo jsonOptionsSetter)
        {
            var generator = constructorBuilder.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Newobj, JsonOptionsConstructor);
            generator.Emit(OpCodes.Dup);
            generator.Emit(OpCodes.Ldc_I4, 1);
            generator.Emit(OpCodes.Callvirt, JsonOptionsWriteIntendedProperty.GetSetMethod());
            generator.Emit(OpCodes.Call, jsonOptionsSetter);

            return constructorBuilder;
        }

        public static void InitializeBoolValue(this ConstructorBuilder constructorBuilde, PropertyInfo targetProperty)
        {
            var generator = constructorBuilde.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Call, targetProperty.GetSetMethod());
        }

        public static void InitializeStringValue(this ConstructorBuilder constructorBuilde, Type type, PropertyInfo jsonOptionsProperty, PropertyInfo targetProperty, bool isList)
        {
            var initializeMethod = InitializeStringValueMethod(type);
            var generator = constructorBuilde.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, jsonOptionsProperty.GetGetMethod());
            generator.Emit(isList ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            generator.EmitCall(OpCodes.Call, initializeMethod, null);
            generator.Emit(OpCodes.Call, targetProperty.GetSetMethod());
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, OnInitializedMethod);
        }

        public static void FinishBuilding(this ConstructorBuilder constructorBuilder)
        {
            var generator = constructorBuilder.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, ComponentBaseConstructor);
            generator.Emit(OpCodes.Ret);
        }
    }
}