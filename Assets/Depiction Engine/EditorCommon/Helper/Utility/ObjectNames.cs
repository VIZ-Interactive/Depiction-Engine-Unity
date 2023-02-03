// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

#if UNITY_EDITOR
namespace DepictionEngine.Editor
{
    public class ObjectNames
    {
        public static string GetInspectorTitle(UnityEngine.Object obj, bool originalTitle = false)
        {
            _originalTitle = originalTitle;

            string title = UnityEditor.ObjectNames.GetInspectorTitle(obj);

            _originalTitle = false;

            return title;
        }

        private static bool _originalTitle;
        private static void PatchedPostGetInspectorTitle(UnityEngine.Object obj, ref string __result)
        {
            if (!_originalTitle && obj is IScriptableBehaviour)
            {
                string nameOverride = (obj as IScriptableBehaviour).inspectorComponentNameOverride;

                if (!string.IsNullOrEmpty(nameOverride))
                    __result = nameOverride;
            }
        }
    }
}
#endif
