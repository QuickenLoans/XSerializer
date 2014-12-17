using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using XSerializer.Encryption;

namespace XSerializer
{
    public static class ModuleInitializer // Future devs: Do not change the name of this class
    {
        private static readonly string _iEncryptionProviderName = typeof(IEncryptionProvider).AssemblyQualifiedName;
        private static readonly string _iEncryptionProviderProviderName = typeof(IEncryptionProviderProvider).AssemblyQualifiedName;

        public static void Run() // Future devs: Do not change the signature of this method
        {
            SetCurrentEncryptionProvider();
        }

        private static void SetCurrentEncryptionProvider()
        {
            if (_iEncryptionProviderName == null || _iEncryptionProviderProviderName == null)
            {
                return;
            }

            try
            {
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += AppDomainOnReflectionOnlyAssemblyResolve;

                var prioritizedGroupsOfCandidateTypes =
                    GetAssemblyFiles()
                        .SelectMany(GetCandidateTypes)
                        .GroupBy(x => x.Priority, item => item.Type)
                        .OrderByDescending(g => g.Key);

                foreach (var candidateTypes in prioritizedGroupsOfCandidateTypes.Select(g => g.ToList()))
                {
                    if (candidateTypes.Count != 1)
                    {
                        WriteToEventLog(candidateTypes);
                        continue;
                    }

                    var encryptionProvider = GetEncryptionProvider(candidateTypes[0]);

                    if (encryptionProvider != null)
                    {
                        EncryptionProvider.Current = encryptionProvider;
                        return;
                    }
                }
            }
            finally
            {
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= AppDomainOnReflectionOnlyAssemblyResolve;
            }
        }

        private static Assembly AppDomainOnReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            return Assembly.ReflectionOnlyLoad(args.Name);
        }

        private static IEnumerable<string> GetAssemblyFiles()
        {
            try
            {
                return
                    Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll")
                        .Concat(Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory, "*.exe"));
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }

        private static IEnumerable<PrioritizedType> GetCandidateTypes(string assemblyFile)
        {
            try
            {
                var assembly = Assembly.ReflectionOnlyLoadFrom(assemblyFile);

                if (assembly.FullName == typeof(ModuleInitializer).Assembly.FullName)
                {
                    return Enumerable.Empty<PrioritizedType>();
                }

                return
                    assembly.GetTypes()
                        .Where(t =>
                            t.IsClass
                            && !t.IsAbstract
                            && t.IsPublic
                            && t.AssemblyQualifiedName != null
                            && t.GetInterfaces().Any(i =>
                                i.AssemblyQualifiedName == _iEncryptionProviderProviderName
                                || i.AssemblyQualifiedName == _iEncryptionProviderName)
                            && t.HasDefaultishConstructor())
                        .Select(t => GetPrioritizedType(t.AssemblyQualifiedName))
                        .Where(x => x != null);
            }
            catch
            {
                return Enumerable.Empty<PrioritizedType>();
            }
        }

        private static bool HasDefaultishConstructor(this Type type)
        {
            return
                type.GetConstructor(Type.EmptyTypes) != null
                || type.GetConstructors().Any(ctor => ctor.GetParameters().All(HasDefaultValue));
        }

        private static bool HasDefaultValue(ParameterInfo parameter)
        {
            const ParameterAttributes hasDefaultValue =
                ParameterAttributes.HasDefault | ParameterAttributes.Optional;

            return (parameter.Attributes & hasDefaultValue) == hasDefaultValue;
        }

        private static PrioritizedType GetPrioritizedType(string assemblyQualifiedName)
        {
            try
            {
                var type = Type.GetType(assemblyQualifiedName);

                if (type == null)
                {
                    return null;
                }

                var encryptionProviderAttribute =
                    (EncryptionProviderAttribute)Attribute.GetCustomAttribute(type, typeof(EncryptionProviderAttribute));

                var priority =
                    encryptionProviderAttribute != null // If EncryptionProviderAttribute was specified...
                        ? encryptionProviderAttribute.Priority // ...use its priority.
                        : type.GetInterfaces().Any(i => i.AssemblyQualifiedName == _iEncryptionProviderProviderName)
                            ? -1 // IEncryptionProviderProvider has a higher default priority...
                            : -2; // ...than IEncryptionProvider.

                return new PrioritizedType
                {
                    Type = type,
                    Priority = priority
                };
            }
            catch
            {
                return null;
            }
        }

        private static void WriteToEventLog(List<Type> duplicatePriorityTypes)
        {
            // TODO: Implement
        }

        private static IEncryptionProvider GetEncryptionProvider(Type candidateType)
        {
            try
            {
                object instance;

                if (candidateType.GetConstructor(Type.EmptyTypes) != null)
                {
                    instance = Activator.CreateInstance(candidateType);
                }
                else
                {
                    var ctor =
                        candidateType.GetConstructors()
                            .OrderByDescending(c => c.GetParameters().Length)
                            .First(c => c.GetParameters().All(HasDefaultValue));

                    var args = ctor.GetParameters().Select(p => p.DefaultValue).ToArray();

                    instance = Activator.CreateInstance(candidateType, args);
                }

                var encryptionProviderProvider = instance as IEncryptionProviderProvider;
                if (encryptionProviderProvider != null)
                {
                    return encryptionProviderProvider.GetEncryptionProvider();
                }

                var encryptionProvider = instance as IEncryptionProvider;
                if (encryptionProvider != null)
                {
                    return encryptionProvider;
                }

                // How did we even get here? Answer me that, Mr. Compiler!
                return null;
            }
            catch
            {
                return null;
            }
        }

        private class PrioritizedType
        {
            public Type Type { get; set; }
            public int Priority { get; set; }
        }
    }
}