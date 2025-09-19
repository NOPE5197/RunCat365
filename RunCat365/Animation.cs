// Copyright 2025 Takuto Nakamura
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

namespace RunCat365
{
    internal enum AnimationThreshold
    {
        Percent25,
        Percent50,
        Percent75,
        Percent100
    }

    internal static class AnimationThresholdExtensions
    {
        internal static string GetString(this AnimationThreshold threshold)
        {
            return threshold switch
            {
                AnimationThreshold.Percent25 => "25%",
                AnimationThreshold.Percent50 => "50%",
                AnimationThreshold.Percent75 => "75%",
                AnimationThreshold.Percent100 => "100%",
                _ => "50%"
            };
        }

        internal static float GetValue(this AnimationThreshold threshold)
        {
            return threshold switch
            {
                AnimationThreshold.Percent25 => 25.0f,
                AnimationThreshold.Percent50 => 50.0f,
                AnimationThreshold.Percent75 => 75.0f,
                AnimationThreshold.Percent100 => 100.0f,
                _ => 50.0f
            };
        }

        internal static bool TryParse(string? value, out AnimationThreshold threshold)
        {
            threshold = value switch
            {
                "25%" => AnimationThreshold.Percent25,
                "50%" => AnimationThreshold.Percent50,
                "75%" => AnimationThreshold.Percent75,
                "100%" => AnimationThreshold.Percent100,
                _ => AnimationThreshold.Percent50
            };
            return true;
        }
    }

    internal enum AnimationMultiplier
    {
        X1_25,
        X1_5,
        X1_75,
        X2
    }

    internal static class AnimationMultiplierExtensions
    {
        internal static string GetString(this AnimationMultiplier multiplier)
        {
            return multiplier switch
            {
                AnimationMultiplier.X1_25 => "1.25",
                AnimationMultiplier.X1_5 => "1.5",
                AnimationMultiplier.X1_75 => "1.75",
                AnimationMultiplier.X2 => "2",
                _ => "2"
            };
        }

        internal static float GetValue(this AnimationMultiplier multiplier)
        {
            return multiplier switch
            {
                AnimationMultiplier.X1_25 => 1.25f,
                AnimationMultiplier.X1_5 => 1.5f,
                AnimationMultiplier.X1_75 => 1.75f,
                AnimationMultiplier.X2 => 2.0f,
                _ => 2.0f
            };
        }

        internal static bool TryParse(string? value, out AnimationMultiplier multiplier)
        {
            multiplier = value switch
            {
                "1.25" => AnimationMultiplier.X1_25,
                "1.5" => AnimationMultiplier.X1_5,
                "1.75" => AnimationMultiplier.X1_75,
                "2" => AnimationMultiplier.X2,
                _ => AnimationMultiplier.X2
            };
            return true;
        }
    }
}
