// Copyright (C) 2023 by VIZ Interactive Media Inc. <contact@vizinteractive.io> | Licensed under MIT license (see LICENSE.md for details)

using DepictionEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets
{
    [ExecuteAlways]
    public class Class1 : MonoBehaviour
    {
        public Texture2D texture;

        [SerializeField]
        public TextureModifier test;

        public void Awake()
        {
            if(texture == null)
                texture = new Texture2D(2,2);
            if (test == null)
                test = new TextureModifier().Init(new Texture2D(2,2));
        }

        public void Update()
        {
            Debug.Log(texture+", "+test);
        }

    }
}
