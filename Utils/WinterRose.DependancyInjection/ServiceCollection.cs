using System.Reflection;
using WinterRose.Uris;

namespace WinterRose.DependancyInjection;

public class ServiceCollection : IServiceProvider
{
    private readonly Dictionary<Type, List<ServiceDescriptor>> descriptors;
    private readonly Dictionary<Type, object> singletons = new Dictionary<Type, object>();

    public ServiceCollection(List<ServiceDescriptor> descriptors)
    {
        this.descriptors = new Dictionary<Type, List<ServiceDescriptor>>();

        foreach (ServiceDescriptor descriptor in descriptors)
        {
            if (!this.descriptors.TryGetValue(descriptor.ServiceType, out List<ServiceDescriptor>? list))
            {
                list = new List<ServiceDescriptor>();
                this.descriptors.Add(descriptor.ServiceType, list);
            }

            list.Add(descriptor);
        }

        ServiceDescriptor serviceProvider = new(
            typeof(IServiceProvider),
            GetType(),
            ServiceLifetime.Singleton);
        // should be only one IServiceProvider, this one
        this.descriptors[typeof(IServiceProvider)] = [serviceProvider];
        singletons[serviceProvider.ServiceType] = this;
    }

    public IEnumerable<T> ResolveAll<T>()
    {
        ResolutionContext context = new ResolutionContext(this);

        foreach (ServiceDescriptor descriptor in GetDescriptors(typeof(T)))
        {
            yield return (T)ResolveFromDescriptor(
                typeof(T),
                descriptor,
                context);
        }
    }

    public T Resolve<T>()
    {
        return (T)Resolve(typeof(T), new ResolutionContext(this));
    }
    
    private T Resolve<T>(ResolutionContext context) =>  (T)Resolve(typeof(T), context);

    public object Resolve(Type type)
    {
        return Resolve(type, new ResolutionContext(this));
    }

    internal object Resolve(Type type, ResolutionContext context)
    {
        if (context.ResolutionStack.Contains(type))
        {
            throw new InvalidOperationException(
                $"Circular dependency detected while resolving {type.FullName}"
            );
        }

        context.ResolutionStack.Push(type);

        try
        {
            if (TryGetSingleton(type, out object singletonInstance))
                return singletonInstance;

            if (context.ScopedInstances.TryGetValue(type, out object scopedInstance))
                return scopedInstance;

            List<ServiceDescriptor> descriptors = GetDescriptors(type)
                .Where(d => d.IsCompatible(Resolve<IOSEnvironment>(context)))
                .ToList();
            ServiceDescriptor constructionDescriptor;

            if (descriptors.Count == 0)
            {
                if (TryGetGenericDescriptor(type, out constructionDescriptor))
                {
                    // open generic resolved
                }
                else if (!type.IsAbstract)
                {
                    constructionDescriptor = new ServiceDescriptor(
                        type,
                        type,
                        ServiceLifetime.Transient
                    );
                }
                else
                {
                    throw new InvalidOperationException(
                        $"No service registration found for {type.FullName}"
                    );
                }
            }
            else
            {
                constructionDescriptor = descriptors[0];
            }

            Type serviceType = type;
            Type implementationType = constructionDescriptor.ImplementationType;

            if (serviceType.IsGenericType)
            {
                Type[] genericArgs = serviceType.GetGenericArguments();

                if (constructionDescriptor.ServiceType.IsGenericTypeDefinition)
                    serviceType = constructionDescriptor.ServiceType.MakeGenericType(genericArgs);

                if (implementationType.IsGenericTypeDefinition)
                    implementationType = implementationType.MakeGenericType(genericArgs);
            }

            ConstructorInfo constructor = SelectConstructor(implementationType, context);

            ParameterInfo[] parameters = constructor.GetParameters();
            object[] resolvedParameters = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                resolvedParameters[i] =
                    ResolveParameter(parameters[i], parameters[i].ParameterType, context);
            }

            object instance = constructor.Invoke(resolvedParameters);

            ApplyActivation(instance, serviceType, constructionDescriptor);

            if (constructionDescriptor.Lifetime == ServiceLifetime.Singleton)
            {
                SetSingleton(type, instance);
            }
            else if (constructionDescriptor.Lifetime == ServiceLifetime.Scoped)
            {
                context.ScopedInstances[type] = instance;
            }

            return instance;
        }
        finally
        {
            context.ResolutionStack.Pop();
        }
    }
    
    private void ApplyActivation(object instance, Type serviceType, ServiceDescriptor constructionDescriptor)
    {
        IEnumerable<ServiceDescriptor> activationDescriptors = GetActivationDescriptors(serviceType, constructionDescriptor);

        foreach (ServiceDescriptor activationDescriptor in activationDescriptors)
        {
            foreach (var config in activationDescriptor.Configurators)
            {
                config(instance);
            }
        }
    }
    
    private IEnumerable<ServiceDescriptor> GetActivationDescriptors(Type serviceType, ServiceDescriptor constructionDescriptor)
    {
        if (!this.descriptors.TryGetValue(serviceType, out List<ServiceDescriptor> descriptors))
            yield break;

        foreach (ServiceDescriptor descriptor in descriptors)
        {
            // you can refine this later (flags, attributes, etc.)
            if (descriptor.ServiceType == constructionDescriptor.ServiceType)
                yield return descriptor;
        }
    }

    private ConstructorInfo SelectConstructor(Type implementationType, ResolutionContext context)
    {
        ConstructorInfo[] constructors = implementationType.GetConstructors();

        if (constructors.Length == 0)
        {
            throw new InvalidOperationException(
                $"No public constructors found for {implementationType.FullName}"
            );
        }

        List<ConstructorInfo> validConstructors = new List<ConstructorInfo>();

        foreach (ConstructorInfo constructor in constructors)
        {
            ParameterInfo[] parameters = constructor.GetParameters();

            // 1. parameterless constructor is always valid
            if (parameters.Length == 0)
            {
                validConstructors.Add(constructor);
                continue;
            }

            // 2. check if ALL parameters are resolvable (registered OR auto-constructable)
            bool isValid = true;

            foreach (ParameterInfo parameter in parameters)
            {
                Type parameterType = parameter.ParameterType;

                if (!CanResolve(parameterType, context))
                {
                    isValid = false;
                    break;
                }
            }

            if (isValid)
            {
                validConstructors.Add(constructor);
            }
        }

        if (validConstructors.Count == 0)
        {
            throw new InvalidOperationException(
                $"No valid constructor found for {implementationType.FullName}. " +
                $"None of the constructors can be fully satisfied from registered services."
            );
        }

        // pick "best" valid constructor (most parameters = most expressive)
        ConstructorInfo selected = validConstructors[0];

        for (int i = 1; i < validConstructors.Count; i++)
        {
            if (validConstructors[i].GetParameters().Length > selected.GetParameters().Length)
            {
                selected = validConstructors[i];
            }
        }

        return selected;
    }


    private object ResolveParameter(
        ParameterInfo parameter,
        Type parameterType,
        ResolutionContext context)
    {
        Type? specificallyType = GetSpecificallyType(parameter);

        if (parameterType.IsArray)
        {
            Type itemType = parameterType.GetElementType()!;

            IEnumerable<object> items = specificallyType != null
                ? ResolveSpecificAll(itemType, specificallyType, context)
                : ResolveAll(itemType, context);

            Array array = Array.CreateInstance(itemType, items.Count());

            int index = 0;
            foreach (object item in items)
            {
                array.SetValue(item, index++);
            }

            return array;
        }

        if (parameterType.IsGenericType &&
            parameterType.GetGenericTypeDefinition() == typeof(List<>))
        {
            Type itemType = parameterType.GetGenericArguments()[0];

            IEnumerable<object> items = specificallyType != null
                ? ResolveSpecificAll(itemType, specificallyType, context)
                : ResolveAll(itemType, context);

            Type listType = typeof(List<>).MakeGenericType(itemType);
            System.Collections.IList list =
                (System.Collections.IList)Activator.CreateInstance(listType)!;

            foreach (object item in items)
            {
                list.Add(item);
            }

            return list;
        }

        if (specificallyType != null)
        {
            return ResolveSpecificImplementation(
                parameterType,
                specificallyType,
                context);
        }

        return Resolve(parameterType, context);
    }

    private Type? GetSpecificallyType(ParameterInfo parameter)
    {
        object[] attributes = parameter.GetCustomAttributes(false);

        foreach (object attribute in attributes)
        {
            Type attrType = attribute.GetType();

            if (attrType.IsGenericType &&
                attrType.GetGenericTypeDefinition().Name == "SpecificallyAttribute`1")
            {
                return attrType.GetGenericArguments()[0];
            }
        }

        return null;
    }

    private IEnumerable<object> ResolveSpecificAll(
        Type serviceType,
        Type implementationType,
        ResolutionContext context)
    {
        foreach (ServiceDescriptor descriptor in GetDescriptors(serviceType))
        {
            if (descriptor.ImplementationType != implementationType)
                continue;

            yield return ResolveFromDescriptor(
                serviceType,
                descriptor,
                context);
        }
    }

    private object ResolveSpecificImplementation(
        Type serviceType,
        Type implementationType,
        ResolutionContext context)
    {
        List<ServiceDescriptor> descriptors = GetDescriptors(serviceType)
            .Where(d => d.IsCompatible(Resolve<IOSEnvironment>(context)))
            .ToList();

        if (descriptors.Count == 0)
        {
            throw new InvalidOperationException(
                $"No registrations found for {serviceType.FullName}"
            );
        }

        ServiceDescriptor? match = null;

        foreach (ServiceDescriptor descriptor in descriptors)
        {
            if (descriptor.ImplementationType != implementationType)
                continue;

            if (match != null)
            {
                throw new InvalidOperationException(
                    $"Multiple registrations found for {serviceType.FullName} -> {implementationType.FullName}"
                );
            }

            match = descriptor;
        }

        if (match == null)
        {
            throw new InvalidOperationException(
                $"No implementation {implementationType.FullName} registered for {serviceType.FullName}"
            );
        }

        return ResolveFromDescriptor(serviceType, match, context);
    }

    private IEnumerable<object> ResolveAll(
        Type serviceType,
        ResolutionContext context)
    {
        foreach (ServiceDescriptor descriptor in GetDescriptors(serviceType))
        {
            yield return ResolveFromDescriptor(
                serviceType,
                descriptor,
                context);
        }
    }

    private object ResolveFromDescriptor(
        Type serviceType,
        ServiceDescriptor descriptor,
        ResolutionContext context)
    {
        Type implementationType = descriptor.ImplementationType;

        if (serviceType.IsGenericType)
        {
            Type[] genericArgs = serviceType.GetGenericArguments();

            if (descriptor.ServiceType.IsGenericTypeDefinition)
                serviceType = descriptor.ServiceType.MakeGenericType(genericArgs);

            if (implementationType.IsGenericTypeDefinition)
                implementationType = implementationType.MakeGenericType(genericArgs);
        }

        ConstructorInfo constructor = SelectConstructor(implementationType, context);

        ParameterInfo[] parameters = constructor.GetParameters();
        object[] resolvedParameters = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            resolvedParameters[i] =
                ResolveParameter(parameters[i], parameters[i].ParameterType, context);
        }

        object instance = constructor.Invoke(resolvedParameters);

        return instance;
    }

    private bool TryGetGenericDescriptor(Type requestedType, out ServiceDescriptor descriptor)
    {
        descriptor = null;

        if (!requestedType.IsGenericType)
            return false;

        Type genericDefinition = requestedType.GetGenericTypeDefinition();

        foreach (List<ServiceDescriptor> descriptorList in descriptors.Values)
        {
            foreach (ServiceDescriptor entry in descriptorList)
            {
                if (!entry.ServiceType.IsGenericTypeDefinition)
                    continue;

                if (entry.ServiceType == genericDefinition)
                {
                    descriptor = entry;
                    return true;
                }
            }
        }

        return false;
    }

    private bool CanResolve(Type type, ResolutionContext context)
    {
        if (type.IsArray)
            return true;

        if (type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(List<>))
            return true;

        if (TryGetSingleton(type, out _))
            return true;

        if (context.ScopedInstances.ContainsKey(type))
            return true;

        if (this.descriptors.TryGetValue(type, out List<ServiceDescriptor> descriptors) &&
            descriptors.Count > 0)
            return true;

        if (TryGetGenericDescriptor(type, out _))
            return true;

        return !type.IsAbstract;
    }

    internal bool TryGetDescriptors(Type type, out List<ServiceDescriptor> descriptors)
    {
        return this.descriptors.TryGetValue(type, out descriptors);
    }

    internal bool TryGetSingleton(Type type, out object instance)
    {
        return singletons.TryGetValue(type, out instance);
    }

    private List<ServiceDescriptor> GetDescriptors(Type type)
    {
        if (this.descriptors.TryGetValue(type, out List<ServiceDescriptor> descriptors))
            return descriptors;

        return new List<ServiceDescriptor>();
    }

    internal void SetSingleton(Type type, object instance)
    {
        singletons[type] = instance;
    }
}