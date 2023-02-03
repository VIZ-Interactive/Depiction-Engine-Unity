// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

namespace DepictionEngine
{
    public class ButtonAttribute : CustomAttribute
    {
        public static float kDefaultButtonWidth = 300;

        public readonly string methodName;

        private float _buttonWidth = kDefaultButtonWidth;
        public float buttonWidth
        {
            get { return _buttonWidth; }
            set { _buttonWidth = value; }
        }

        public ButtonAttribute(string methodName)
        {
            this.methodName = methodName;
        }
    }
}