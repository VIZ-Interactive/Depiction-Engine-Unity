// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
	[AddComponentMenu(SceneManager.NAMESPACE + "/Controller/" + nameof(OrbitController))]
    [RequireComponent(typeof(AstroObject))]
	public class OrbitController : ControllerBase
    {
        private const double J2000_DAYS = 10956.0d;

		[BeginFoldout("Orbit")]
		[SerializeField, Tooltip("Tells the controller what kind of orbit to follow.")]
		private AstroObject.PlanetType _orbit;
		[SerializeField, Tooltip("A value by which we multiply the distance of the orbiting object to move it further or closer."), EndFoldout]
		private double _sizeMultiplier;

		protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
		{
			base.InitializeSerializedFields(initializingState);

			InitValue(value => orbit = value, GetComponent<Star>() != Disposable.NULL ? AstroObject.PlanetType.Sun : AstroObject.PlanetType.Earth, initializingState);
			InitValue(value => sizeMultiplier = value, 1.0d, initializingState);
		}

        /// <summary>
        /// Tells the controller what kind of orbit to follow.
        /// </summary>
        [Json]
		public AstroObject.PlanetType orbit
		{
			get { return _orbit; }
			set { SetValue(nameof(orbit), value, ref _orbit); }
		}

        /// <summary>
        /// A value by which we multiply the distance of the orbiting object to move it further or closer.
        /// </summary>
        [Json]
		public double sizeMultiplier
		{
			get { return _sizeMultiplier; }
			set { SetValue(nameof(sizeMultiplier), value, ref _sizeMultiplier); }
		}

		public StarSystem GetStarSystem()
        {
			return transform.parentObject as StarSystem; 
        }

		private Orbit _orbitCalculator;
		private Orbit GetOrbit(AstroObject.PlanetType astroObjectType)
        {
			Type astroObjectClassType = GetClassTypeOrbitType(astroObjectType);
			if (_orbitCalculator == null || _orbitCalculator.GetType() != astroObjectClassType)
				_orbitCalculator = Activator.CreateInstance(astroObjectClassType) as Orbit;
			return _orbitCalculator;
		}

		private Type GetClassTypeOrbitType(AstroObject.PlanetType orbitType)
        {
			switch (orbitType)
			{
				case AstroObject.PlanetType.Mercury:
					return typeof(Mercury);
				case AstroObject.PlanetType.Venus:
					return typeof(Venus);
				case AstroObject.PlanetType.Earth:
					return typeof(Earth);
				case AstroObject.PlanetType.Mars:
					return typeof(Mars);
				case AstroObject.PlanetType.Jupiter:
					return typeof(Jupiter);
				case AstroObject.PlanetType.Saturn:
					return typeof(Saturn);
				case AstroObject.PlanetType.Uranus:
					return typeof(Uranus);
				case AstroObject.PlanetType.Neptune:
					return typeof(Neptune);
				case AstroObject.PlanetType.Pluto:
					return typeof(Pluto);
				case AstroObject.PlanetType.Moon:
					return typeof(Moon);
				case AstroObject.PlanetType.Sun:
					return typeof(Sun);
			}
			return null;
		}

		private Camera _currentCamera;
		private TransformComponents2Double _localTransformComponents;
		public override void UpdateControllerTransform(Camera camera)
		{
			base.UpdateControllerTransform(camera);

			if (isActiveAndEnabled)
			{
				if (_localTransformComponents == null)
					_localTransformComponents = new TransformComponents2Double();

				_currentCamera = camera;
				if (GetOrbitTransformComponents(ref _localTransformComponents, _currentCamera))
				{
					transform.localPosition = _localTransformComponents.position;
					transform.localRotation = _localTransformComponents.rotation;
				}

				_currentCamera = null;
			}
		}

		protected override bool TransformControllerCallback(LocalPositionParam localPositionParam, LocalRotationParam localRotationParam, LocalScaleParam localScaleParam, Camera camera = null)
		{
			if (base.TransformControllerCallback(localPositionParam, localRotationParam, localScaleParam, camera))
			{
				//Prevent any undesirable changes
				if (_currentCamera == Disposable.NULL)
				{
					localPositionParam.SetValue(transform.localPosition);
					localRotationParam.SetValue(transform.localRotation);
				}

				return true;
			}
			return false;
		}

		Matrix4x4Double _orbitMatrix;
		private TransformComponents2Double _orbitAroundGeoAstroObjectTransformComponents;
		private bool GetOrbitTransformComponents(ref TransformComponents2Double transformComponents, Camera camera)
        {
			OrbitController orbitAroundController = null;

			StarSystem starSystem = GetStarSystem();
			if (starSystem != Disposable.NULL)
			{
				GetOrbitTransformComponents(ref transformComponents);

				Vector3Double localPosition = transformComponents.position;
				QuaternionDouble localRotation = transformComponents.rotation;

				if (starSystem.orbitAroundAstroObject != Disposable.NULL)
					orbitAroundController = starSystem.orbitAroundAstroObject.controller as OrbitController;

				if (orbitAroundController != Disposable.NULL)
				{
					if (_orbitAroundGeoAstroObjectTransformComponents == null)
						_orbitAroundGeoAstroObjectTransformComponents = new TransformComponents2Double();
					orbitAroundController.GetOrbitTransformComponents(ref _orbitAroundGeoAstroObjectTransformComponents);

					Vector3Double orbitAroundLocalPosition = _orbitAroundGeoAstroObjectTransformComponents.position;
					QuaternionDouble orbitAroundLocalRotation = _orbitAroundGeoAstroObjectTransformComponents.rotation;

					if (_orbitMatrix == null)
						_orbitMatrix = new Matrix4x4Double();

					Matrix4x4Double starSystemToOrbitAroundGeoAstroObjectMatrix = Matrix4x4Double.TRS(ref _orbitMatrix, orbitAroundLocalPosition, orbitAroundLocalRotation, Vector3Double.one).fastinverse;

                    //Transform starSystem relative position/rotation to orbitAroundGeoAstroObject relative position/rotation
                    localPosition = starSystemToOrbitAroundGeoAstroObjectMatrix.MultiplyPoint3x4(localPosition);
					localRotation = QuaternionDouble.Inverse(orbitAroundLocalRotation) * localRotation;

                    GeoAstroObject orbitAroundGeoAstroObject = orbitAroundController.objectBase as GeoAstroObject;
					if (orbitAroundGeoAstroObject.IsFlat())
					{
						if (orbitAroundController != this && camera != Disposable.NULL)
						{
							Vector3Double cameraLocalPosition = starSystem.transform.InverseTransformPoint(camera.transform.position);

							Matrix4x4Double starSystemToCenteredFlatOrbitAroundGeoAstroObjectMatrix = Matrix4x4Double.TRS(ref _orbitMatrix, Vector3Double.zero, QuaternionDouble.identity, orbitAroundGeoAstroObject.transform.localScale).fastinverse;
							GeoCoordinate3Double cameraSurfaceGeoCoordinate = MathPlus.GetGeoCoordinateFromLocalPoint(starSystemToCenteredFlatOrbitAroundGeoAstroObjectMatrix.MultiplyPoint3x4(cameraLocalPosition), 0.0f, orbitAroundGeoAstroObject.radius, orbitAroundGeoAstroObject.size);
							cameraSurfaceGeoCoordinate.altitude = 0.0d;

							Vector3Double cameraSphericalSurfaceLocalPosition = MathPlus.GetLocalPointFromGeoCoordinate(cameraSurfaceGeoCoordinate, 1.0f, orbitAroundGeoAstroObject.radius, orbitAroundGeoAstroObject.size);
							QuaternionDouble cameraSphericalSurfaceRotation = MathPlus.GetUpVectorFromGeoCoordinate(cameraSurfaceGeoCoordinate, 1.0f);
							Matrix4x4Double starSystemToOrbitAroundSphericalCameraSurfaceMatrix = Matrix4x4Double.TRS(ref _orbitMatrix, cameraSphericalSurfaceLocalPosition, cameraSphericalSurfaceRotation, orbitAroundGeoAstroObject.transform.localScale).fastinverse;

							localPosition = starSystemToOrbitAroundSphericalCameraSurfaceMatrix.MultiplyPoint3x4(localPosition);
							localRotation = QuaternionDouble.Inverse(cameraSphericalSurfaceRotation) * localRotation;
						}
					}
					else
					{
						QuaternionDouble quarterTurn = QuaternionDouble.Euler(-90.0d, 0.0d, 0.0d);
						localPosition = quarterTurn * localPosition;
						localRotation = quarterTurn * localRotation;
					}
				}
	
				transformComponents.position = localPosition * sizeMultiplier * starSystem.sizeMultiplier;
				transformComponents.rotation = localRotation;

				return true;
			}
			return false;
		}

		private void GetOrbitTransformComponents(ref TransformComponents2Double transformComponents)
		{
			GetOrbitTransformComponents(ref transformComponents, GetOrbit(orbit));
		}

		private TransformComponents2Double _earthTransformComponents;
		private Earth _earthOrbit;
		private void GetOrbitTransformComponents(ref TransformComponents2Double transformComponents, Orbit orbit)
		{
			StarSystem starSystem = GetStarSystem();
			double time = ((starSystem != Disposable.NULL ? starSystem.GetTime() : DateTime.Now.ToUniversalTime()) - DateTime.SpecifyKind(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), DateTimeKind.Utc)).TotalDays - J2000_DAYS;

			transformComponents.position = orbit.GetPosition(time);
			transformComponents.rotation = orbit.GetOrientation(time);

			//Add sizeMultiplier
			if (orbit is Moon)
			{
				if (_earthTransformComponents == null)
					_earthTransformComponents = new TransformComponents2Double();
				if (_earthOrbit == null)
					_earthOrbit = new Earth();

				GetOrbitTransformComponents(ref _earthTransformComponents, _earthOrbit);

				transformComponents.position += _earthTransformComponents.position;
			}

			//Shift from Y to Z aligned poles
			transformComponents.rotation *= QuaternionDouble.AngleAxis(-90.0d, Vector3Double.right);
		}

		//Planetary orbit Math formulas and constants can be found:
		//https://www.stjarnhimlen.se/comp/ppcomp.html
		//https://www.stjarnhimlen.se/comp/tutorial.html
		//https://www.mathworks.com/matlabcentral/mlc-downloads/downloads/submissions/23051/versions/2/previews/SolarAzEl.m/index.html
		//https://en.wikipedia.org/wiki/Pluto
		//http://www.stargazing.net/kepler/moon.html

		private class Orbit
		{
			private double _axialTilt;
			private double _rotationPeriod;

			private Vector3Double _axialTiltAxis;
			private double _rotationStart;

			public Orbit(double axialTilt, double rotationPeriod)
			{
				_axialTilt = axialTilt;
				_rotationPeriod = rotationPeriod;

				_axialTiltAxis = Vector3Double.right;
			}

			public Orbit(double axialTilt, double rotationPeriod, Vector3Double axialTiltAxis)
			{
				_axialTilt = axialTilt;
				_rotationPeriod = rotationPeriod;

				_axialTiltAxis = axialTiltAxis;
			}

			public Orbit(double axialTilt, double rotationPeriod, Vector3Double axialTiltAxis, double rotationStart)
			{
				_axialTilt = axialTilt;
				_rotationPeriod = rotationPeriod;

				_axialTiltAxis = axialTiltAxis;
				_rotationStart = rotationStart;
			}

			public Vector3Double GetPosition(double jd)
			{
				return GetPositionAU(jd) * 1.496e+11d;
			}

			protected virtual Vector3Double GetPositionAU(double jd)
			{
				return Vector3Double.zero;
			}

			protected virtual double GetAxialTilt(double jd)
            {
				return _axialTilt;
			}

			public QuaternionDouble GetOrientation(double jd)
            {
				return QuaternionDouble.AngleAxis(GetAxialTilt(jd), _axialTiltAxis) * QuaternionDouble.AngleAxis(GetRotation(jd) + 90.0f, Vector3Double.up);
			}

			protected virtual double GetRotation(double jd)
			{
				if (_rotationPeriod == 0.0d)
					return 0.0d;

				return -(360.0d * (jd / _rotationPeriod) + _rotationStart) % 360.0d;
			}

			protected Vector3Double GetVectorFromGeoCoordinate(GeoCoordinate3Double geoCoordinate)
            {
				double cosLat = Cos(geoCoordinate.latitude);

				return new Vector3Double(
					geoCoordinate.altitude * cosLat * Cos(geoCoordinate.longitude), 
					geoCoordinate.altitude * Sin(geoCoordinate.latitude), 
					geoCoordinate.altitude * cosLat * Sin(geoCoordinate.longitude));
			}

			protected double Atan2(double y, double x)
			{
				return Math.Atan2(y, x) * MathPlus.RAD2DEG;
			}

			protected double Cos(double d)
			{
				return Math.Cos(d * MathPlus.DEG2RAD);
			}

			protected double Sin(double a)
			{
				return Math.Sin(a * MathPlus.DEG2RAD);
			}

			protected double Diff(double v1, double v2)
			{
				double double0 = Normalize(v1 - v2);

				if (double0 < 180.0d)
					return double0;

				return double0 - 360.0d;
			}

			protected double Normalize(double v)
			{
				double num = v % 360.0d + 1.0000000458137E-18 - 1.0000000458137E-18;

				if (num >= 0.0d)
					return num;

				return num + 360.0d;
			}

			protected double NormalizeDouble(double v)
			{
				double num = v % 360.0d;

				if (num >= 0)
					return num;

				return num + 360.0d;
			}
		}

		private class Elliptic : Orbit
		{
			protected const double TOLERANCE = 0.001d;

			private double _longitudeOfPerihelion0;
			private double _longitudeOfPerihelion1;
			private double _eccentricity0;
			private double _eccentricity1;
			private double _meanAnomaly0;
			private double _meanAnomaly1;

			private double _ascendingNode0;
			private double _ascendingNode1;
			private double _inclination0;
			private double _inclination1;
			private double _meanDistanceSun0;
			private double _meanDistanceSun1;

			public Elliptic(double axialTilt, double rotationPeriod, Vector3Double axialTiltAxis, double rotationStart, double longitudeOfPerihelion0, double longitudeOfPerihelion1, double eccentricity0, double eccentricity1, double meanAnomaly0, double meanAnomaly1, double ascendingNode0 = 0.0d, double ascendingNode1 = 0.0d, double inclination0 = 0.0d, double inclination1 = 0.0d, double meanDistanceSun0 = 0.0d, double meanDistanceSun1 = 0.0d) : base(axialTilt, rotationPeriod, axialTiltAxis, rotationStart)
			{
				_longitudeOfPerihelion0 = longitudeOfPerihelion0;
				_longitudeOfPerihelion1 = longitudeOfPerihelion1;
				_eccentricity0 = eccentricity0;
				_eccentricity1 = eccentricity1;
				_meanAnomaly0 = meanAnomaly0;
				_meanAnomaly1 = meanAnomaly1;
				_ascendingNode0 = ascendingNode0;
				_ascendingNode1 = ascendingNode1;
				_inclination0 = inclination0;
				_inclination1 = inclination1;
				_meanDistanceSun0 = meanDistanceSun0;
				_meanDistanceSun1 = meanDistanceSun1;
			}

			public Elliptic(double axialTilt, double rotationPeriod, Vector3Double axialTiltAxis, double longitudeOfPerihelion0, double longitudeOfPerihelion1, double eccentricity0, double eccentricity1, double meanAnomaly0, double meanAnomaly1, double ascendingNode0 = 0.0d, double ascendingNode1 = 0.0d, double inclination0 = 0.0d, double inclination1 = 0.0d, double meanDistanceSun0 = 0.0d, double meanDistanceSun1 = 0.0d) : base(axialTilt, rotationPeriod, axialTiltAxis)
			{
				_longitudeOfPerihelion0 = longitudeOfPerihelion0;
				_longitudeOfPerihelion1 = longitudeOfPerihelion1;
				_eccentricity0 = eccentricity0;
				_eccentricity1 = eccentricity1;
				_meanAnomaly0 = meanAnomaly0;
				_meanAnomaly1 = meanAnomaly1;
				_ascendingNode0 = ascendingNode0;
				_ascendingNode1 = ascendingNode1;
				_inclination0 = inclination0;
				_inclination1 = inclination1;
				_meanDistanceSun0 = meanDistanceSun0;
				_meanDistanceSun1 = meanDistanceSun1;
			}

			public Elliptic(double axialTilt, double rotationPeriod, double longitudeOfPerihelion0, double longitudeOfPerihelion1, double eccentricity0, double eccentricity1, double meanAnomaly0, double meanAnomaly1, double ascendingNode0 = 0.0d, double ascendingNode1 = 0.0d, double inclination0 = 0.0d, double inclination1 = 0.0d, double meanDistanceSun0 = 0.0d, double meanDistanceSun1 = 0.0d) : base(axialTilt, rotationPeriod)
			{
				_longitudeOfPerihelion0 = longitudeOfPerihelion0;
				_longitudeOfPerihelion1 = longitudeOfPerihelion1;
				_eccentricity0 = eccentricity0;
				_eccentricity1 = eccentricity1;
				_meanAnomaly0 = meanAnomaly0;
				_meanAnomaly1 = meanAnomaly1;
				_ascendingNode0 = ascendingNode0;
				_ascendingNode1 = ascendingNode1;
				_inclination0 = inclination0;
				_inclination1 = inclination1;
				_meanDistanceSun0 = meanDistanceSun0;
				_meanDistanceSun1 = meanDistanceSun1;
			}

			protected override Vector3Double GetPositionAU(double jd)
			{
				return GetPositionAU(jd, GetLongitudeOfPerihelion(jd), GetEccentricity(jd), GetMeanAnomaly(jd));
			}

			private double GetLongitudeOfPerihelion(double jd)
            {
				return NormalizeDouble(_longitudeOfPerihelion0 + _longitudeOfPerihelion1 * jd);
			}

			private double GetEccentricity(double jd)
			{
				return NormalizeDouble(_eccentricity0 + _eccentricity1 * jd);
			}

			private double GetMeanAnomaly(double jd)
			{
				return NormalizeDouble(_meanAnomaly0 + _meanAnomaly1 * jd);
			}

			private double GetAscendingNode(double jd)
			{
				return NormalizeDouble(_ascendingNode0 + _ascendingNode1 * jd);
			}

			private double GetInclination(double jd)
			{
				return NormalizeDouble(_inclination0 + _inclination1 * jd);
			}

			private double GetMeanDistanceSun(double jd)
			{
				return _meanDistanceSun0 + _meanDistanceSun1 * jd;
			}

			protected virtual Vector3Double GetPositionAU(double jd, double longitudeOfPerihelion, double eccentricity, double meanAnomaly)
			{
				double E = WithinTolerance(meanAnomaly + MathPlus.RAD2DEG * eccentricity * Sin(meanAnomaly) * (1.0d + eccentricity * Cos(meanAnomaly)), eccentricity, meanAnomaly, TOLERANCE);

				double meanDistanceSun = GetMeanDistanceSun(jd);
				double double0 = meanDistanceSun * (Cos(E) - eccentricity);
				double double1 = meanDistanceSun * Math.Sqrt(1.0d - eccentricity * eccentricity) * Sin(E);
				double v = Normalize(Atan2(double1, double0));
				double r = Math.Sqrt(double0 * double0 + double1 * double1);
				double double2 = v + longitudeOfPerihelion;
				double double3 = Sin(double2);
				double double4 = Cos(double2);

				double ascendingNode = GetAscendingNode(jd);
				double sinAscendingNode = Sin(ascendingNode);
				double cosAscendingNode = Cos(ascendingNode);

				double inclination = GetInclination(jd);
				double sinInclination = Sin(inclination);
				double cosInclination = Cos(inclination);

				return new Vector3Double(
					r * (cosAscendingNode * double4 - sinAscendingNode * double3 * cosInclination),
					r * double3 * sinInclination,
					r * (sinAscendingNode * double4 + cosAscendingNode * double3 * cosInclination));
			}

			private double WithinTolerance(double E, double Eccentricity, double meanAnomaly, double tolerance)
			{
				double newE = E - (E - MathPlus.RAD2DEG * Eccentricity * Sin(E) - meanAnomaly) / (1.0d - Eccentricity * Cos(E));
				if (Math.Abs(Diff(newE, E)) > tolerance)
					newE = WithinTolerance(newE, Eccentricity, meanAnomaly, tolerance);
				return newE;
			}

			protected override double GetRotation(double jd)
			{
				return GetRotation(jd, GetLongitudeOfPerihelion(jd), GetMeanAnomaly(jd));
			}

			protected virtual double GetRotation(double jd, double longitudeOfPerihelion, double meanAnomaly)
			{
				return base.GetRotation(jd);
			}
		}

		private class Earth : Elliptic
		{
			public Earth() : base
			(
				axialTilt: 23.4393d,
				rotationPeriod: 0.9972697d,

				longitudeOfPerihelion0: 282.9404d,
				longitudeOfPerihelion1: 4.70935E-05d,
				eccentricity0: 0.016709d,
				eccentricity1: -1.151E-09d,
				meanAnomaly0: 356.047d,
				meanAnomaly1: 0.985600233d
			)
			{ }

			protected override Vector3Double GetPositionAU(double jd, double longitudeOfPerihelion, double eccentricity, double meanAnomaly)
			{
				double E = WithinTolerance(meanAnomaly + MathPlus.RAD2DEG * eccentricity * Sin(meanAnomaly) * (1.0d + eccentricity * Cos(meanAnomaly)), eccentricity, meanAnomaly, TOLERANCE);

				double double1 = Cos(E) - eccentricity;
				double double2 = Math.Sqrt(1d - eccentricity * eccentricity) * Sin(E);
				double v = Normalize(Atan2(double2, double1));

				return GetVectorFromGeoCoordinate(new GeoCoordinate3Double(0.0d, Normalize(v + longitudeOfPerihelion + 180.0d), Math.Sqrt(double1 * double1 + double2 * double2)));
			}

			private double WithinTolerance(double E, double eccentricity, double meanAnomaly, double tolerance)
			{
				double newE = E - (E - MathPlus.RAD2DEG * eccentricity * Sin(E) - meanAnomaly) / (1.0d - E * Cos(E));
				if (Math.Abs(Diff(newE, E)) > tolerance)
					newE = WithinTolerance(newE, eccentricity, meanAnomaly, tolerance);
				return newE;
			}

			protected override double GetAxialTilt(double jd)
			{
				return base.GetAxialTilt(jd) - 3.563E-07d * jd;
			}

			protected override double GetRotation(double jd, double longitudeOfPerihelion, double meanAnomaly)
			{
				double sunsMeanLongitude = Normalize(meanAnomaly + longitudeOfPerihelion);
				return -Normalize(sunsMeanLongitude + (jd - Math.Floor(jd)) * 360.0d);
			}
		}

        private class Moon : Elliptic
		{
			public Moon() : base
			(
				axialTilt: 1.5424d,
				rotationPeriod: 27.3215828d,

				longitudeOfPerihelion0: 318.0634d,
				longitudeOfPerihelion1: 0.164357319d,
				eccentricity0: 0.0549d,
				eccentricity1: 0.0d,
				meanAnomaly0: 115.3654d,
				meanAnomaly1: 13.0649929d,

				ascendingNode0: 125.1228d,
				ascendingNode1: -0.05295381d,
				inclination0: 5.1454d,
				inclination1: 0.0d,
				meanDistanceSun0: 0.00256955529d,
				meanDistanceSun1: 0.0d
			)
			{ }

			protected override double GetRotation(double jd)
			{
				Vector3Double position = base.GetPositionAU(jd);

				return -Normalize(Atan2(position.z, position.x));
			}
		}

        private class Pluto : Orbit
		{
			public Pluto() : base
			(
				axialTilt: 119.591d,
				rotationPeriod: 6.3872d
			)
			{ }

			protected override Vector3Double GetPositionAU(double jd)
			{
				double double1 = 50.03d + 0.0334596522d * jd;
				double double2 = 238.95d + 0.003968789d * jd;

				return GetVectorFromGeoCoordinate(new GeoCoordinate3Double(
					-3.9082d - 5.453d * Sin(double2) - 14.975d * Cos(double2) + 3.527d * Sin(2d * double2) + 1.673d * Cos(2d * double2) - 1.051d * Sin(3d * double2) + 0.328d * Cos(3.0d * double2) + 0.179d * Sin(4.0d * double2) - 0.292d * Cos(4.0d * double2) + 0.019d * Sin(5.0d * double2) + 0.1d * Cos(5.0d * double2) - 0.031d * Sin(6.0d * double2) - 0.026d * Cos(6.0d * double2) + 0.011d * Cos(double1 - double2),
					238.9508d + 0.00400703d * jd - 19.799d * Sin(double2) + 19.848d * Cos(double2) + 0.897d * Sin(2d * double2) - 4.956d * Cos(2d * double2) + 0.61d * Sin(3d * double2) + 1.211d * Cos(3d * double2) - 0.341d * Sin(4d * double2) - 0.19d * Cos(4d * double2) + 0.128d * Sin(5d * double2) - 0.034d * Cos(5d * double2) - 0.038d * Sin(6d * double2) + 0.031d * Cos(6d * double2) + 0.02d * Sin(double1 - double2) - 0.01d * Cos(double1 - double2),
					40.72d + 6.68d * Sin(double2) + 6.9d * Cos(double2) - 1.18d * Sin(2d * double2) - 0.03d * Cos(2d * double2) + 0.15d * Sin(3d * double2) - 0.14d * Cos(3d * double2)));
			}
		}

        private class Neptune : Elliptic
		{
			public Neptune() : base
			(
				axialTilt: 28.32d,
				rotationPeriod: 0.67125d,
				axialTiltAxis: new Vector3Double(-1.0d, -1.0d, 0.0d).normalized,

				longitudeOfPerihelion0: 272.8461d,
				longitudeOfPerihelion1: -6.027E-06d,
				eccentricity0: 0.008606d,
				eccentricity1: 2.15E-09d,
				meanAnomaly0: 260.2471d,
				meanAnomaly1: 0.005995147d,

				ascendingNode0: 131.7806d,
				ascendingNode1: 3.0173E-05d,
				inclination0: 1.77d,
				inclination1: -2.55E-07d,
				meanDistanceSun0: 30.05826d,
				meanDistanceSun1: 3.313E-08d
			)
			{ }
		}

        private class Mars : Elliptic
		{
			public Mars() :base
			(
				axialTilt: 25.19d,
				rotationPeriod: 1.02595675d,
				axialTiltAxis: new Vector3Double(0.0d, 0.0d, -1.0d),
				rotationStart: -120.0d,

				longitudeOfPerihelion0: 286.5016d,
				longitudeOfPerihelion1: 2.92961E-05d, 
				eccentricity0: 0.093405d,
				eccentricity1: 2.516E-09d,
				meanAnomaly0: 18.6021d,
				meanAnomaly1: 0.5240208d,

				ascendingNode0: 49.5574d,
				ascendingNode1: 2.11081E-05d,
				inclination0: 1.8497d,
				inclination1: -1.78E-08d,
				meanDistanceSun0: 1.523688d,
				meanDistanceSun1: 0.0d
			){}
		}

        private class Mercury : Elliptic
		{
			public Mercury() : base
			(
				axialTilt: 0.027d,
				rotationPeriod: 58.6462d,

				longitudeOfPerihelion0: 29.1241d,
				longitudeOfPerihelion1: 1.01444E-05d,
				eccentricity0: 0.205635d,
				eccentricity1: 5.59E-10d,
				meanAnomaly0: 168.6562d,
				meanAnomaly1: 4.09233427d,

				ascendingNode0: 48.3313d,
				ascendingNode1: 3.24587E-05d,
				inclination0: 7.0047d,
				inclination1: 5E-08d,
				meanDistanceSun0: 0.387098d,
				meanDistanceSun1: 0.0d
			)
			{ }
		}

        private class Jupiter : Elliptic
		{
			public Jupiter() : base
			(
				axialTilt: 3.13d,
				rotationPeriod: 0.41354d,

				longitudeOfPerihelion0: 273.8777d,
				longitudeOfPerihelion1: 1.64505E-05d,
				eccentricity0: 0.048498d,
				eccentricity1: 4.469E-09d,
				meanAnomaly0: 19.895d,
				meanAnomaly1: 0.0830853d,

				ascendingNode0: 100.4542d,
				ascendingNode1: 2.76854E-05d,
				inclination0: 1.303d,
				inclination1: -1.557E-07d,
				meanDistanceSun0: 5.20256d,
				meanDistanceSun1: 0.0d
			){}
		}

        private class Saturn : Elliptic
		{
			public Saturn() : base
			(
				axialTilt: 26.73d,
				rotationPeriod: 0.44401d,

				longitudeOfPerihelion0: 339.3939d,
				longitudeOfPerihelion1: 2.97661E-05d,
				eccentricity0: 0.055546d,
				eccentricity1: -9.499E-09d,
				meanAnomaly0: 316.967d,
				meanAnomaly1: 0.03344423d,

				ascendingNode0: 113.6634d,
				ascendingNode1: 2.3898E-05d,
				inclination0: 2.4886d,
				inclination1: -1.081E-07d,
				meanDistanceSun0: 9.55475d,
				meanDistanceSun1: 0.0d
			){}
		}

        private class Uranus : Elliptic
		{
			public Uranus() : base
			(
				axialTilt: 97.77d,
				rotationPeriod: -0.71833d,

				longitudeOfPerihelion0: 96.6612d,
				longitudeOfPerihelion1: 3.0565E-05d,
				eccentricity0: 0.047318d,
				eccentricity1: 7.45E-09d,
				meanAnomaly0: 142.5905d,
				meanAnomaly1: 0.0117258057d,

				ascendingNode0: 74.0005d,
				ascendingNode1: 1.3978E-05d,
				inclination0: 0.7733d,
				inclination1: 1.9E-08d,
				meanDistanceSun0: 19.18171d,
				meanDistanceSun1: -1.55E-08d
			){}
		}

        private class Venus : Elliptic
		{
			public Venus() : base
			(
				axialTilt: 177.3d,
				rotationPeriod: 243.018d,

				longitudeOfPerihelion0: 54.891d,
				longitudeOfPerihelion1: 1.38374E-05d,
				eccentricity0: 0.006773d,
				eccentricity1: -1.302E-09d,
				meanAnomaly0: 48.0052d,
				meanAnomaly1: 1.60213017d,

				ascendingNode0: 76.6799d,
				ascendingNode1: 2.4659E-05d,
				inclination0: 3.3946d,
				inclination1: 2.75E-08d,
				meanDistanceSun0: 0.72333d,
				meanDistanceSun1: 0.0d
			){}
		}

        private class Sun : Orbit
		{
			public Sun() : base
			(
				axialTilt: 7.25d,
				rotationPeriod: 25.05d
			){}
		}
	}
}
