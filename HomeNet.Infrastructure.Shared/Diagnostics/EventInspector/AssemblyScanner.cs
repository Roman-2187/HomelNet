using System.Reflection;
using HomeNet.Infrastructure.Shared.Enums;

namespace HomeNet.Infrastructure.Shared.Diagnostics.EventInspector
{
    public static class AssemblyScanner
    {
        private const byte OpCodeCall = 0x28;
        private const byte OpCodeCallVirt = 0x6F;

        public static void ScanPublishers(
            Dictionary<string, ComponentNode> nodes,
            Dictionary<Type, MessageState> registry,
            Func<string, ComponentNode> getOrCreateNode)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName != null && a.FullName.StartsWith("HomeNet"));

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (!type.IsClass || type.IsAbstract) continue;

                    string componentName = type.Name.StartsWith("<") && type.DeclaringType != null
                        ? type.DeclaringType.Name
                        : type.Name;

                    var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
                    var methods = type.GetMethods(flags).Cast<MethodBase>().Concat(type.GetConstructors(flags));

                    foreach (var method in methods)
                    {
                        try
                        {
                            // 🔥 ПРОПУСКАЕМ методы из системных сборок и Avalonia (они не наши)
                            var declaringTypeName = method.DeclaringType?.FullName ?? "";
                            if (declaringTypeName.StartsWith("System") || declaringTypeName.StartsWith("Avalonia"))
                                continue;

                            var body = method.GetMethodBody();
                            byte[] il = body?.GetILAsByteArray();
                            if (il == null || il.Length == 0) continue;

                            ParseMethodIL(il, method, componentName, getOrCreateNode, registry);
                        }
                        catch { }
                    }
                }
            }
        }

        private static void ParseMethodIL(
            byte[] il,
            MethodBase method,
            string componentName,
            Func<string, ComponentNode> getOrCreateNode,
            Dictionary<Type, MessageState> registry)
        {
            int i = 0;
            while (i < il.Length)
            {
                byte opCode = il[i++];
                if (opCode != OpCodeCall && opCode != OpCodeCallVirt) continue;
                if (i + 4 > il.Length) break;

                int token = BitConverter.ToInt32(il, i);
                i += 4;

                try
                {
                    // 🔥 Дополнительная защита: если токен вне диапазона — пропускаем
                    if (token <= 0 || token >= il.Length) continue;

                    var called = method.Module.ResolveMethod(token);

                    if (called != null && called.IsGenericMethod)
                    {
                        string name = called.Name;
                        if (name == "Publish" || name == "PublishAsync")
                        {
                            var genericArgs = called.GetGenericArguments();
                            if (genericArgs == null || genericArgs.Length == 0) continue;

                            var eventType = genericArgs[0];

                            // 🔥 Игнорируем системные типы
                            if (eventType.FullName != null && eventType.FullName.StartsWith("System."))
                                continue;

                            var node = getOrCreateNode(componentName);

                            if (!node.PublishedMessages.Contains(eventType))
                                node.PublishedMessages.Add(eventType);

                            if (!registry.ContainsKey(eventType))
                                registry[eventType] = MessageState.None;

                            registry[eventType] |= MessageState.Published;
                        }
                    }
                }
                catch (Exception)
                {
                    // 🔥 Тихий игнор: сканер не должен ронять приложение
                }
            }
        }
    }
}