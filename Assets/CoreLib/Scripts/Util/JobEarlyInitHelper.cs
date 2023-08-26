using System;
using System.Linq;
using System.Reflection;
using Unity.Jobs.LowLevel.Unsafe;

namespace CoreLib.Util
{
    public static class JobEarlyInitHelper
    {
        private static readonly string ProducerAttributeName = typeof(JobProducerTypeAttribute).FullName;
        
        public static void PerformJobEarlyInit(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                VisitJobTypes(type);
            }
        }
	
        private static void VisitJobTypes(Type jobType)
        {
            if (jobType.IsGenericType) return;
            
            Type[] interfaces = jobType.GetInterfaces();
            if (interfaces.Length > 0 && jobType.IsValueType)
            {
                foreach (var iface in interfaces)
                {
                    foreach (var attr in iface.CustomAttributes)
                    {
                        if (attr.AttributeType.FullName != ProducerAttributeName) continue;
                        
                        var producerType = (Type)attr.ConstructorArguments[0].Value;
                        var method = FindInitMethod(producerType);
                        if (method == null) continue;
                        
                        var genericMethod = method.MakeGenericMethod(jobType);
                        genericMethod.Invoke(null, Array.Empty<object>());
                        CoreLibMod.Log.LogInfo($"Register Job {jobType.FullName}");
                    }
                }
            }

            foreach (var nestedType in jobType.GetNestedTypes())
            {
                VisitJobTypes(nestedType);
            }
        }
        
        public static MethodInfo FindInitMethod(Type producerType)
        {
            MethodInfo methodToCall = null;
            while (producerType != null)
            {
                methodToCall = producerType.GetMethods().FirstOrDefault((x) => x.Name == "EarlyJobInit" && x.GetParameters().Length == 0 && x.IsStatic && x.IsPublic);

                if (methodToCall != null)
                    break;

                producerType = producerType.DeclaringType;
            }

            return methodToCall;
        }
    }
}