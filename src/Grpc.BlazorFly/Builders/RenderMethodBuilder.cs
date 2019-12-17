using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Grpc.BlazorFly.Builders
{
    internal class RenderMethodBuilder
    {
        private readonly ILGenerator _generator;

        // RenderTreeBuilder methods
        private static readonly MethodInfo OpenRegionMethod = typeof(RenderTreeBuilder)
            .GetMethod(nameof(RenderTreeBuilder.OpenRegion),
            new[] { typeof(int) });

        private static readonly MethodInfo CloseRegionMethod = typeof(RenderTreeBuilder)
            .GetMethod(nameof(RenderTreeBuilder.CloseRegion));

        private static readonly MethodInfo OpenElementMethod = typeof(RenderTreeBuilder)
            .GetMethod(nameof(RenderTreeBuilder.OpenElement),
            new[] { typeof(int), typeof(string) });

        private static readonly MethodInfo CloseElementMethod = typeof(RenderTreeBuilder)
            .GetMethod(nameof(RenderTreeBuilder.CloseElement));

        private static readonly MethodInfo AddContentMethod = typeof(RenderTreeBuilder)
            .GetMethod(nameof(RenderTreeBuilder.AddContent),
            new[] { typeof(int), typeof(string) });

        private static readonly MethodInfo AddMarkupContentMethod = typeof(RenderTreeBuilder)
            .GetMethod(nameof(RenderTreeBuilder.AddMarkupContent),
            new[] { typeof(int), typeof(string) });

        private static readonly MethodInfo AddAttributeMethod = typeof(RenderTreeBuilder)
            .GetMethod(nameof(RenderTreeBuilder.AddAttribute),
            new[] { typeof(int), typeof(string), typeof(string) });

        private static readonly MethodInfo AddAttributeGenericMethod = typeof(RenderTreeBuilder)
            .GetMethods()
            .Where(method => method.Name == nameof(RenderTreeBuilder.AddAttribute) && method.ContainsGenericParameters)
            .First()
            .MakeGenericMethod(typeof(MouseEventArgs));

        private static readonly MethodInfo SetUpdatesAttributeNameMethod = typeof(RenderTreeBuilder)
            .GetMethod(nameof(RenderTreeBuilder.SetUpdatesAttributeName),
            new[] { typeof(string) });

        private static readonly MethodInfo StringConcatMethod = typeof(string)
            .GetMethod(nameof(string.Concat),
            new[] { typeof(string), typeof(string) });

        // BindConverter methods
        private static readonly MethodInfo BindConverterFromValueMethod = typeof(BindConverter)
            .GetMethod(nameof(BindConverter.FormatValue),
            new[] { typeof(string), typeof(CultureInfo) });

        // EventCallBack-related methods
        private static readonly MethodInfo FactoryCreateBinderMethod = typeof(EventCallbackFactoryBinderExtensions)
            .GetMethod(nameof(EventCallbackFactoryBinderExtensions.CreateBinder),
            new[] { typeof(EventCallbackFactory), typeof(object), typeof(Action<string>), typeof(string), typeof(CultureInfo) });

        private static readonly MethodInfo FactoryCreateMethod = typeof(EventCallbackFactory)
            .GetMethod(nameof(EventCallbackFactory.Create),
            1,
            new[] { typeof(object), typeof(Func<Task>) })
            .MakeGenericMethod(typeof(MouseEventArgs));

        private static readonly FieldInfo FactoryField = typeof(EventCallback)
            .GetField(nameof(EventCallback.Factory));

        // Action/Func related methods
        private static readonly ConstructorInfo ActionForStringConstructorMethod = typeof(Action<string>)
            .GetConstructors()[0];

        private static readonly ConstructorInfo ActionForVoidConstructorMethod = typeof(Action)
            .GetConstructors()[0];

        public RenderMethodBuilder(ILGenerator generator)
        {
            _generator = generator;
        }

        public RenderMethodBuilder OpenRegion(int sequence)
        {
            _generator.Emit(OpCodes.Ldarg_1);
            _generator.Emit(OpCodes.Ldc_I4, sequence);
            _generator.Emit(OpCodes.Callvirt, OpenRegionMethod);

            return this;
        }

        public RenderMethodBuilder CloseRegion()
        {
            _generator.Emit(OpCodes.Ldarg_1);
            _generator.Emit(OpCodes.Callvirt, CloseRegionMethod);

            return this;
        }

        public RenderMethodBuilder OpenElement(int sequence, string elementName)
        {
            _generator.Emit(OpCodes.Ldarg_1);
            _generator.Emit(OpCodes.Ldc_I4, sequence);
            _generator.Emit(OpCodes.Ldstr, elementName);
            _generator.Emit(OpCodes.Callvirt, OpenElementMethod);

            return this;
        }

        public RenderMethodBuilder CloseElement()
        {
            _generator.Emit(OpCodes.Ldarg_1);
            _generator.Emit(OpCodes.Callvirt, CloseElementMethod);

            return this;
        }

        public RenderMethodBuilder AddContent(int sequence, string textContent)
        {
            _generator.Emit(OpCodes.Ldarg_1);
            _generator.Emit(OpCodes.Ldc_I4, sequence);
            _generator.Emit(OpCodes.Ldstr, textContent);
            _generator.Emit(OpCodes.Callvirt, AddContentMethod);

            return this;
        }

        public RenderMethodBuilder AddContent(int sequence, PropertyInfo property)
        {
            _generator.Emit(OpCodes.Ldarg_1);
            _generator.Emit(OpCodes.Ldc_I4, sequence);
            _generator.Emit(OpCodes.Ldarg_0);
            _generator.Emit(OpCodes.Call, property.GetGetMethod());
            _generator.Emit(OpCodes.Callvirt, AddContentMethod);

            return this;
        }

        public RenderMethodBuilder AddMarkupContent(int sequence, string markupContent)
        {
            _generator.Emit(OpCodes.Ldarg_1);
            _generator.Emit(OpCodes.Ldc_I4, sequence);
            _generator.Emit(OpCodes.Ldstr, markupContent);
            _generator.Emit(OpCodes.Callvirt, AddMarkupContentMethod);

            return this;
        }

        public RenderMethodBuilder AddAttribute(int sequence, string name, string value)
        {
            _generator.Emit(OpCodes.Ldarg_1);
            _generator.Emit(OpCodes.Ldc_I4, sequence);
            _generator.Emit(OpCodes.Ldstr, name);
            _generator.Emit(OpCodes.Ldstr, value);
            _generator.Emit(OpCodes.Callvirt, AddAttributeMethod);

            return this;
        }

        public RenderMethodBuilder AddAttribute(int sequence, string name, string baseValue, MethodInfo conditionGetter, string optionalValue)
        {
            var endLabel = _generator.DefineLabel();
            _generator.Emit(OpCodes.Ldarg_1);
            _generator.Emit(OpCodes.Ldc_I4, sequence);
            _generator.Emit(OpCodes.Ldstr, name);
            _generator.Emit(OpCodes.Ldstr, baseValue);
            _generator.Emit(OpCodes.Ldarg_0);
            _generator.Emit(OpCodes.Call, conditionGetter);
            _generator.Emit(OpCodes.Brtrue, endLabel);            
            _generator.Emit(OpCodes.Ldstr, optionalValue);
            _generator.EmitCall(OpCodes.Call, StringConcatMethod, null);
            _generator.MarkLabel(endLabel);
            _generator.Emit(OpCodes.Callvirt, AddAttributeMethod);

            return this;
        }

        public RenderMethodBuilder AddAttribute(int sequence, string name, MethodInfo callback)
        {
            _generator.Emit(OpCodes.Ldarg_1);
            _generator.Emit(OpCodes.Ldc_I4, sequence);
            _generator.Emit(OpCodes.Ldstr, name);
            _generator.Emit(OpCodes.Ldsfld, FactoryField);
            _generator.Emit(OpCodes.Ldarg_0);
            _generator.Emit(OpCodes.Ldarg_0);
            _generator.Emit(OpCodes.Ldftn, callback);
            _generator.Emit(OpCodes.Newobj, ActionForVoidConstructorMethod);
            _generator.Emit(OpCodes.Callvirt, FactoryCreateMethod);
            _generator.Emit(OpCodes.Callvirt, AddAttributeGenericMethod);

            return this;
        }

        public RenderMethodBuilder AddAttribute(int sequence, string name, MethodInfo setter, MethodInfo getter)
        {
            _generator.Emit(OpCodes.Ldarg_1);
            _generator.Emit(OpCodes.Ldc_I4, sequence);
            _generator.Emit(OpCodes.Ldstr, name);
            _generator.Emit(OpCodes.Ldsfld, FactoryField);
            _generator.Emit(OpCodes.Ldarg_0);
            _generator.Emit(OpCodes.Ldarg_0);
            _generator.Emit(OpCodes.Ldftn, setter);
            _generator.Emit(OpCodes.Newobj, ActionForStringConstructorMethod);
            _generator.Emit(OpCodes.Ldarg_0);
            _generator.Emit(OpCodes.Call, getter);
            _generator.Emit(OpCodes.Ldnull);
            _generator.EmitCall(OpCodes.Call, FactoryCreateBinderMethod, null);
            _generator.Emit(OpCodes.Callvirt, AddAttributeGenericMethod);

            return this;
        }

        public RenderMethodBuilder AddAttribute(int sequence, string name, PropertyInfo targetProperty)
        {
            _generator.Emit(OpCodes.Ldarg_1);
            _generator.Emit(OpCodes.Ldc_I4, sequence);
            _generator.Emit(OpCodes.Ldstr, name);
            _generator.Emit(OpCodes.Ldarg_0);
            _generator.Emit(OpCodes.Call, targetProperty.GetGetMethod());
            _generator.Emit(OpCodes.Ldnull);
            _generator.Emit(OpCodes.Call, BindConverterFromValueMethod);
            _generator.Emit(OpCodes.Callvirt, AddAttributeMethod);

            return this;
        }

        public RenderMethodBuilder SetUpdatesAttributeName(string updatesAttributeName)
        {
            _generator.Emit(OpCodes.Ldarg_1);
            _generator.Emit(OpCodes.Ldstr, updatesAttributeName);
            _generator.Emit(OpCodes.Callvirt, SetUpdatesAttributeNameMethod);

            return this;
        }

        public void Build()
        {
            _generator.Emit(OpCodes.Ret);
        }
    }
}