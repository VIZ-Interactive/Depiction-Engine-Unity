// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Astro/" + nameof(AstroObject))]
    public class AstroObject : DatasourceRoot
    {
        /// <summary>
        /// Solar system planets, satellite and star.
        /// </summary>
        public enum PlanetType
        {
            Select,
            Sun,
            Moon,
            Mercury,
            Venus,
            Earth,
            Mars,
            Jupiter,
            Saturn,
            Uranus,
            Neptune,
            Pluto
        };
   
        public const double DEFAULT_MASS = 1000000000000000000.0f;

#if UNITY_EDITOR
        [BeginFoldout("Astro")]
        [SerializeField, ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Sets the properties of the AstroObject to a set of predefined values."), EndFoldout]
        private PlanetType _astroPresets;
        private PlanetType astroPresets { set { SetPlanetPreset(value); } }
#endif

        public override bool IsPhysicsObject()
        {
            return true;
        }

        protected override double GetDefaultMass()
        {
            return DEFAULT_MASS;
        }

        public virtual void SetPlanetPreset(PlanetType planet)
        {
            double presetMass = GetPlanetMass(planet);
            if (presetMass != 0.0d)
                mass = presetMass;
        }

        public static double GetPlanetMass(PlanetType planet)
        {
            double mass = 0.0d;

            switch (planet)
            {
                case PlanetType.Mercury:
                    mass = 3.285e+23;
                    break;
                case PlanetType.Venus:
                    mass = 4.867e+24;
                    break;
                case PlanetType.Earth:
                    mass = 5.972e+24;
                    break;
                case PlanetType.Mars:
                    mass = 6.39e+23;
                    break;
                case PlanetType.Jupiter:
                    mass = 1.898e+27;
                    break;
                case PlanetType.Saturn:
                    mass = 5.683e+26;
                    break;
                case PlanetType.Uranus:
                    mass = 8.681e+25;
                    break;
                case PlanetType.Neptune:
                    mass = 1.024e+26;
                    break;
                case PlanetType.Pluto:
                    mass = 1.30900e+22;
                    break;
                case PlanetType.Moon:
                    mass = 7.34767309e+22;
                    break;
                case PlanetType.Sun:
                    mass = 1.989e+30;
                    break;
            }

            return mass;
        }

        protected override bool ResetTransform()
        {
            return false;
        }

        public virtual Vector3 GetGravitationalForce(Object objectBase)
        {
            double distance = Vector3Double.Distance(transform.position, objectBase.transform.position);
            Vector3Double direction = (objectBase.transform.position - transform.position).normalized;
            return GetGravitationalForce(objectBase, distance, direction);
        }

        private readonly double GRAVITY_CONSTANT = 6.67d * Math.Pow(10.0d, -11.0d);
        protected Vector3 GetGravitationalForce(Object objectBase, double distance, Vector3Double direction)
        {
            Vector3 gravitationalForce = Vector3.zero;

            if (distance != 0.0d)
            {
                double force = GRAVITY_CONSTANT * (mass * objectBase.mass / Math.Pow(distance, 2.0d));
                gravitationalForce = force * direction;
            }

            return gravitationalForce;
        }
    }
}
