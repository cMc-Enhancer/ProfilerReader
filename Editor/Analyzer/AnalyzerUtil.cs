using System;
using System.Collections.Generic;

namespace UTJ.ProfilerReader.Analyzer
{
    public static class AnalyzerUtil
    {
        public static List<IAnalyzer> CreateSourceTestAnalyzer()
        {
            return new List<IAnalyzer>
            {
                new MainThreadMethodsStatisticsAnalyzer(),
                new SoundMethodPerFrameAnalyzer(),
                new SoundMethodsStatisticsAnalyzer(),
                new RenderingAnalyzer(),
            };
        }

        public static List<IAnalyzer> CreateAllAnalyzer()
        {
            var types = GetInterfaceTypes<IAnalyzer>();
            return InstanciateObjectsOfType<IAnalyzer>(types);
        }

        private static List<T> InstanciateObjectsOfType<T>(List<Type> types) where T : class
        {
            List<T> ret = new List<T>();
            foreach (var t in types)
            {
                if (t.IsAbstract)
                {
                    continue;
                }

                var inst = Activator.CreateInstance(t) as T;
                ret.Add(inst);
            }

            return ret;
        }

        public static List<Type> GetInterfaceTypes<T>()
        {
            List<Type> ret = new List<Type>();
            var domain = AppDomain.CurrentDomain;
            var assemblies = domain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    var interfaces = type.GetInterfaces();

                    foreach (var interfacetype in interfaces)
                    {
                        if (interfacetype == typeof(T) && !type.IsAbstract)
                        {
                            ret.Add(type);
                        }
                    }
                }
            }

            return ret;
        }
    }
}