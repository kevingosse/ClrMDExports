using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace ClrMDExports
{
    public class DelegateBuilder
    {
        public static Type GenerateDelegateType(MethodInfo targetMethod)
        {
            var assembly = new AssemblyName
            {
                Version = new Version(1, 0, 0, 0),
                Name = Guid.NewGuid().ToString().Replace("-", string.Empty)
            };

            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assembly, AssemblyBuilderAccess.Run);
            var modbuilder = assemblyBuilder.DefineDynamicModule("MyModule");
            
            // Create a delegate that has the same signature as the method we would like to hook up to
            var typeBuilder = modbuilder.DefineType("MyDelegateType", TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass, typeof(MulticastDelegate));

            var unmanagedFunctionPointerAttributeBuilder = new CustomAttributeBuilder(typeof(UnmanagedFunctionPointerAttribute).GetConstructor(new Type[] { typeof(CallingConvention) }), new object[] { CallingConvention.StdCall });
            typeBuilder.SetCustomAttribute(unmanagedFunctionPointerAttributeBuilder);
            
            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(object), typeof(IntPtr) });
            constructorBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            // Grab the parameters of the method
            var parameters = targetMethod.GetParameters();

            var paramTypes = new Type[parameters.Length + 1];

            paramTypes[0] = typeof(IntPtr);

            for (int i = 0; i < parameters.Length; i++)
            {
                paramTypes[i + 1] = parameters[i].ParameterType;
            }

            // Define the Invoke method for the delegate
            var methodBuilder = typeBuilder.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, targetMethod.ReturnType, paramTypes);
            
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                var attributes = ParameterAttributes.None;

                UnmanagedType? unmanagedType = null;

                bool outAttribute = false;

                foreach (var customAttribute in parameter.CustomAttributes)
                {
                    if (customAttribute.AttributeType == typeof(MarshalAsAttribute))
                    {
                        attributes |= ParameterAttributes.HasFieldMarshal;
                        unmanagedType = (UnmanagedType)customAttribute.ConstructorArguments[0].Value;
                    }

                    if (customAttribute.AttributeType == typeof(InAttribute))
                    {
                        attributes |= ParameterAttributes.In;
                    }

                    if (customAttribute.AttributeType == typeof(OutAttribute))
                    {
                        attributes |= ParameterAttributes.Out;
                        outAttribute = true;
                    }
                }

                var pbr = methodBuilder.DefineParameter(i + 2, attributes, null);

                if (outAttribute)
                {
                    var outAttributeBuilder = new CustomAttributeBuilder(typeof(OutAttribute).GetConstructor(new Type[0]), new object[0]);
                    pbr.SetCustomAttribute(outAttributeBuilder);
                }

                if (unmanagedType != null)
                {
                    var marshalAsAttributeBuilder = new CustomAttributeBuilder(typeof(MarshalAsAttribute).GetConstructor(new Type[] { typeof(UnmanagedType) }), new object[] { unmanagedType.Value });
                    pbr.SetCustomAttribute(marshalAsAttributeBuilder);
                }
            }

            methodBuilder.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            return typeBuilder.CreateTypeInfo();
        }
    }
}