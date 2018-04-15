﻿// Copyright (C) 2018 Tyler Szabo
//
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License along with this program.  If not, see <http://www.gnu.org/licenses/>.

using GLedApiDotNet.LedSettings;
using System;
using System.Collections;
using System.Collections.Generic;

namespace GLedApiDotNet
{
    public class RGBFusionMotherboard : IRGBFusionMotherboard
    {
        private class MotherboardLedLayoutImpl : IMotherboardLedLayout
        {
            private readonly LedType[] myLayout;

            internal MotherboardLedLayoutImpl(byte[] rawLayout)
            {
                myLayout = new LedType[rawLayout.Length];
                for (int i = 0; i < myLayout.Length; i++)
                {
                    myLayout[i] = (LedType)rawLayout[i];
                }
            }

            public LedType this[int i] => myLayout[i];

            public int Length => myLayout.Length;

            public IEnumerator<LedType> GetEnumerator()
            {
                foreach(LedType led in myLayout)
                {
                    yield return led;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<LedType>)myLayout).GetEnumerator();
        }

        private class MotherboardLedSettingsImpl : IMotherboardLedSettings
        {
            private LedSetting[] ledSettings;

            private bool dirty;

            internal void WriteToApi(Raw.GLedAPIv1_0_0Wrapper api)
            {
                if (dirty) {
                    api.SetLedData(this);
                    dirty = false;
                }
            }

            internal MotherboardLedSettingsImpl(int numSettings, LedSetting defaultSetting)
            {
                dirty = true;
                ledSettings = new LedSetting[numSettings];
                for (int i = 0; i < ledSettings.Length; i++)
                {
                    ledSettings[i] = defaultSetting;
                }
            }

            public LedSetting this[int i]
            {
                get => ledSettings[i];
                set
                {
                    dirty = true;
                    ledSettings[i] = value;
                }
            }

            public int Length => ledSettings.Length;

            public IEnumerator<LedSetting> GetEnumerator()
            {
                foreach(LedSetting ledSetting in ledSettings)
                {
                    yield return ledSetting;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<LedSetting>)ledSettings).GetEnumerator();
        }

        private Raw.GLedAPIv1_0_0Wrapper api;

        private int maxDivisions;
        public int MaxDivisions
        {
            get
            {
                return maxDivisions;
            }
        }

        private MotherboardLedLayoutImpl layout;
        public IMotherboardLedLayout Layout
        {
            get
            {
                return layout;
            }
        }

        private MotherboardLedSettingsImpl ledSettings;
        public IMotherboardLedSettings LedSettings
        {
            get
            {
                return ledSettings;
            }
        }

        internal RGBFusionMotherboard(Raw.GLedAPIv1_0_0Wrapper wrapperAPI)
        {
            api = wrapperAPI;

            string ver = api.GetSdkVersion();
            if (string.IsNullOrEmpty(ver))
            {
                throw new GLedAPIException(string.Format("GLedApi returned empty version"));
            }

            api.Initialize();

            maxDivisions = api.GetMaxDivision();
            if (maxDivisions == 0)
			{
                throw new GLedAPIException("No divisions");
            }

            layout = new MotherboardLedLayoutImpl(api.GetLedLayout(maxDivisions));

            ledSettings = new MotherboardLedSettingsImpl(maxDivisions, new OffLedSetting());
        }

        public RGBFusionMotherboard() : this(new Raw.GLedAPIv1_0_0Wrapper())
        {
        }

        public void SetAll(LedSetting ledSetting)
        {
            for (int i = 0; i < ledSettings.Length; i++)
            {
                ledSettings[i] = ledSetting;
            }

            Set();
        }

        public void Set(params int[] divisions)
        {
            int applyDivs = 0;
            foreach (int division in divisions)
            {
                if (division < 0 || division >= maxDivisions)
                {
                    throw new ArgumentOutOfRangeException("divisions", division, "all divisions must be between 0 and MaxDivisions");
                }
                applyDivs |= (1 << division);
            }

            ledSettings.WriteToApi(api);

            // Calling with no explicit divisions sets all
            api.Apply(applyDivs == 0 ? -1 : applyDivs);
        }
    }
}
