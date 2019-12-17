using System;
using System.Reflection;
using System.Reflection.Emit;

namespace BlazorFly.Grpc.Extensions
{
    internal static class PropertyBuilderExtensions
    {
        public static PropertyBuilder ImplementGetter(this PropertyBuilder property, TypeBuilder type, FieldBuilder field)
        {
            var getterMethod = type.DefineMethod(
                $"get_{property.Name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                property.PropertyType,
                Type.EmptyTypes);

            var generator = getterMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, field);
            generator.Emit(OpCodes.Ret);

            property.SetGetMethod(getterMethod);

            return property;
        }

        public static PropertyBuilder ImplementSetter(this PropertyBuilder property, TypeBuilder type, FieldBuilder field)
        {
            var setterMethod = type.DefineMethod(
                $"set_{property.Name}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                typeof(void),
                new[] { property.PropertyType });

            var generator = setterMethod.GetILGenerator();

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Stfld, field);
            generator.Emit(OpCodes.Ret);

            property.SetSetMethod(setterMethod);

            return property;
        }
    }
}