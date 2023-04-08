// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Astro/" + nameof(StarSystem))]
    public class StarSystem : DatasourceRoot
    {
		[BeginFoldout("Orbit")]
		[SerializeField, ComponentReference, Tooltip("The id of the '"+nameof(AstroObject)+"' whose position will act as the center of the star system.")]
		private SerializableGuid _orbitAroundAstroObjectId;
		[SerializeField, Tooltip("A value by which we multiply the distance of all the orbiting objects to move them further or closer."), EndFoldout]
		private double _sizeMultiplier;

		[BeginFoldout("Universal Time")]
		[SerializeField, Tooltip("When enabled the current time is automatically updated and synchronized to Coordinated Universal Time (UTC).")]
		private bool _live;
		[SerializeField, Tooltip("Current year, format: 1999.")]
#if UNITY_EDITOR
		[ConditionalEnable(nameof(GetEnableCustomTimeFields))]
#endif
		private int _year;
		[SerializeField, Tooltip("Current month, format: 1 - 12.")]
#if UNITY_EDITOR
		[ConditionalEnable(nameof(GetEnableCustomTimeFields))]
#endif
		private int _month;
		[SerializeField, Tooltip("Current day, format: 1 - (29 ,30, 31) depending on the 'month'.")]
#if UNITY_EDITOR
		[ConditionalEnable(nameof(GetEnableCustomTimeFields))]
#endif
		private int _day;
		[SerializeField, Tooltip("Current hour, format: 0 - 23.")]
#if UNITY_EDITOR
		[ConditionalEnable(nameof(GetEnableCustomTimeFields))]
#endif
		private int _hour;
		[SerializeField, Tooltip("Current minute, format: 0 - 59.")]
#if UNITY_EDITOR
		[ConditionalEnable(nameof(GetEnableCustomTimeFields))]
#endif
		private int _minute;
		[SerializeField, Tooltip("Current second, format: 0 - 59.")]
#if UNITY_EDITOR
		[ConditionalEnable(nameof(GetEnableCustomTimeFields))]
#endif
		private int _second;
		[SerializeField, Tooltip("Current millisecond, format: 0 - 999."), EndFoldout]
#if UNITY_EDITOR
		[ConditionalEnable(nameof(GetEnableCustomTimeFields))]
#endif
		private int _millisecond;

		[SerializeField, HideInInspector]
		private AstroObject _orbitAroundAstroObject;

#if UNITY_EDITOR
		protected bool GetEnableCustomTimeFields()
        {
			return !live;
        }
#endif

		protected override void IterateOverComponentReference(Action<SerializableGuid, Action> callback)
		{
			base.IterateOverComponentReference(callback);

			if (_orbitAroundAstroObjectId != null)
				callback(_orbitAroundAstroObjectId, UpdateOrbitAroundAstroObject);
		}

		protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
			base.InitializeSerializedFields(initializingContext);

			InitValue(value => orbitAroundAstroObjectId = value, SerializableGuid.Empty, () => { return GetDuplicateComponentReferenceId(orbitAroundAstroObjectId, orbitAroundAstroObject, initializingContext); }, initializingContext);
			InitValue(value => sizeMultiplier = value, 1.0d, initializingContext);
			InitValue(value => live = value, true, initializingContext);
			InitValue(value => year = value, 2000, initializingContext);
			InitValue(value => month = value, 1, initializingContext);
			InitValue(value => day = value, 1, initializingContext);
			InitValue(value => hour = value, 0, initializingContext);
			InitValue(value => minute = value, 0, initializingContext);
			InitValue(value => second = value, 0, initializingContext);
			InitValue(value => millisecond = value, 0, initializingContext);
		}

		public AstroObject orbitAroundAstroObject
		{
			get => _orbitAroundAstroObject; 
			set { orbitAroundAstroObjectId = value != Disposable.NULL ? value.id : SerializableGuid.Empty; }
		}

        /// <summary>
        /// The id of the <see cref="DepictionEngine.AstroObject"/> whose position will act as the center of the star system.
        /// </summary>
        [Json]
		public SerializableGuid orbitAroundAstroObjectId
		{
			get { return _orbitAroundAstroObjectId; }
			set
			{
				SetValue(nameof(orbitAroundAstroObjectId), value, ref _orbitAroundAstroObjectId, (newValue, oldValue) =>
				{
					UpdateOrbitAroundAstroObject();
				});
			}
		}

		private void UpdateOrbitAroundAstroObject()
		{
            SetValue(nameof(orbitAroundAstroObject), GetComponentFromId<AstroObject>(orbitAroundAstroObjectId), ref _orbitAroundAstroObject);
		}

        /// <summary>
        /// A value by which we multiply the distance of all the orbiting objects to move them further or closer. 
        /// </summary>
        [Json]
		public double sizeMultiplier
		{
			get { return _sizeMultiplier; }
			set { SetValue(nameof(sizeMultiplier), value, ref _sizeMultiplier); }
		}

		/// <summary>
		/// When enabled the current time is automatically updated and synchronized to Coordinated Universal Time (UTC).
		/// </summary>
		[Json]
		private bool live
		{
			get { return _live; }
			set { SetValue(nameof(live), value, ref _live); }
		}

		/// <summary>
		/// Current year, format: 1999.
		/// </summary>
		[Json]
		private int year
		{
			get { return _year; }
			set { SetValue(nameof(year), value, ref _year); }
		}

        /// <summary>
        /// Current month, format: 1 - 12.
        /// </summary>
        [Json]
		private int month
		{
			get { return _month; }
			set { SetValue(nameof(month), ValidateMonth(value), ref _month); }
		}

        /// <summary>
        /// Validates that the number is within the valid months of the year range.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int ValidateMonth(int value)
        {
			if (value < 1)
				value = 1;
			if (value > 12)
				value = 12;
			return value;
		}

        /// <summary>
        /// Current day, format: 1 - (29 ,30, 31) depending on the 'month'/>.
        /// </summary>
        [Json]
		private int day
		{
			get { return _day; }
			set { SetValue(nameof(day), ValidateDay(value, month), ref _day); }
		}

        /// <summary>
        /// Validates that the number is within the valid days of the month range.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int ValidateDay(int value, int month)
		{
			int maxDay = 31;
			switch (month)
			{
				case 2: maxDay = 29; break;
				case 4: maxDay = 30; break;
				case 6: maxDay = 30; break;
				case 9: maxDay = 30; break;
				case 11: maxDay = 30; break;
			}
			if (value < 1)
				value = 1;
			if (value > maxDay)
				value = maxDay;
			return value;
		}

        /// <summary>
        /// Current hour, format: 0 - 23.
        /// </summary>
        [Json]
		private int hour
		{
			get { return _hour; }
			set { SetValue(nameof(hour), ValidateHour(value), ref _hour); }
		}

        /// <summary>
        /// Validates that the number is within the valid hours range.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int ValidateHour(int value)
		{
			if (value < 0)
				value = 0;
			if (value > 23)
				value = 23;
			return value;
		}

        /// <summary>
        /// Current minute, format: 0 - 59.
        /// </summary>
        [Json]
		private int minute
		{
			get { return _minute; }
			set { SetValue(nameof(minute), ValidateMinute(value), ref _minute); }
		}

        /// <summary>
        /// Validates that the number is within the valid mminutes range.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int ValidateMinute(int value)
		{
			if (value < 0)
				value = 0;
			if (value > 59)
				value = 59;
			return value;
		}

        /// <summary>
        /// Current second, format: 0 - 59.
        /// </summary>
        [Json]
		private int second
		{
			get { return _second; }
			set { SetValue(nameof(second), ValidateSecond(value), ref _second); }
		}

        /// <summary>
        /// Validates that the number is within the valid seconds range.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int ValidateSecond(int value)
		{
			if (value < 0)
				value = 0;
			if (value > 59)
				value = 59;
			return value;
		}

        /// <summary>
        /// Current millisecond, format: 0 - 999.
        /// </summary>
        [Json]
		private int millisecond
		{
			get { return _millisecond; }
			set { SetValue(nameof(millisecond), ValidateMillisecond(value), ref _millisecond); }
		}

		/// <summary>
		/// Validates that the number is within the valid milliseconds range.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static int ValidateMillisecond(int value)
		{
			if (value < 0)
				value = 0;
			if (value > 999)
				value = 999;
			return value;
		}

		public void SetTime(DateTime time)
        {
			live = false;
			year = time.Year;
			month = time.Month;
			day = time.Day;
			hour = time.Hour;
			minute = time.Minute;
			second = time.Second;
			millisecond = time.Millisecond;
		}

		public DateTime GetTime()
        {
			return new DateTime(year, month, day, hour, minute, second, millisecond);
		}

		protected override bool ResetTransform()
		{
			return false;
		}

		public override bool PreHierarchicalUpdate()
		{
			if (base.PreHierarchicalUpdate())
			{
				if (isActiveAndEnabled && live)
				{
					DateTime time = DateTime.UtcNow;
					year = time.Year;
					month = time.Month;
					day = time.Day;
					hour = time.Hour;
					minute = time.Minute;
					second = time.Second;
					millisecond = time.Millisecond;
				}

				return true;
			}
			return false;
		}
    }
}
