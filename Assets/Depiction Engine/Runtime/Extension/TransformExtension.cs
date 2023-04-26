// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public static class TransformExtension
    {
        public static void SetPosition(this Transform transform, Vector3Double value)
        {
            transform.position += (Vector3)(value - transform.GetPosition());
        }

        public static Vector3Double GetPosition(this Transform transform)
        {
            Vector3Double position;

            TransformDouble parentTransformDouble = transform.GetComponentInParent<TransformDouble>();
            if (parentTransformDouble != Disposable.NULL)
                position = parentTransformDouble.position + (Vector3Double)(transform.position - parentTransformDouble.transform.position);
            else
                position = TransformDouble.AddOrigin(transform.position);

            return position;
        }
    }
}
