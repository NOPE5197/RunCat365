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

using System.Diagnostics.CodeAnalysis;

namespace RunCat365
{
    enum FPSMaxLimit
    {
        FPS60,
        FPS40,
        FPS30,
        FPS20,
        FPS10,
    }

    internal static class FPSMaxLimitExtension
    {
        internal static string GetString(this FPSMaxLimit fpsMaxLimit)
        {
            return fpsMaxLimit switch
            {
                FPSMaxLimit.FPS60 => "60fps",
                FPSMaxLimit.FPS40 => "40fps",
                FPSMaxLimit.FPS30 => "30fps",
                FPSMaxLimit.FPS20 => "20fps",
                FPSMaxLimit.FPS10 => "10fps",
                _ => "",
            };
        }

        internal static int GetMinInterval(this FPSMaxLimit fPSMaxLimit)
        {
            return fPSMaxLimit switch
            {
                FPSMaxLimit.FPS60 => 16,
                FPSMaxLimit.FPS40 => 25,
                FPSMaxLimit.FPS30 => 33,
                FPSMaxLimit.FPS20 => 50,
                FPSMaxLimit.FPS10 => 100,
                _ => 25,
            };
        }

        internal static bool TryParse([NotNullWhen(true)] string? value, out FPSMaxLimit result)
        {
            if (value is null)
            {
                result = FPSMaxLimit.FPS40;
                return false;
            }
            result = value switch
            {
                "60fps" => FPSMaxLimit.FPS60,
                "40fps" => FPSMaxLimit.FPS40,
                "30fps" => FPSMaxLimit.FPS30,
                "20fps" => FPSMaxLimit.FPS20,
                "10fps" => FPSMaxLimit.FPS10,
                _ => FPSMaxLimit.FPS40,
            };
            return true;
        }
    }
}
