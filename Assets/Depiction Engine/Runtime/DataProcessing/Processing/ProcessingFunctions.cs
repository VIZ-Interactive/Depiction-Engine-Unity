// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

namespace DepictionEngine
{
    public class ProcessingFunctions
    {
        protected static T GetInstance<T>() where T : IDisposable
        {
            return InstanceManager.Instance(false).CreateInstance<T>();
        }

        public static T CreatePropertyModifier<T>() where T : PropertyModifier
        {
            return GetInstance<T>();
        }
    }
}
