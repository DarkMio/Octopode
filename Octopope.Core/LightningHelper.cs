using System.Collections.Generic;
using System.Linq;

namespace Octopode.Core {
    public class LightSetting {
        public readonly ColorMode mode;
        public readonly int minColorOptions, maxColorOptions;
        public readonly bool canSetSpeed;
        public readonly bool canSetMoving;
        public readonly bool canSetDirection;
        public readonly int minLedSize, maxLedSize;
        public readonly bool canSetLedSize;

        public List<int> ColorOptions { get; set; }
        public AnimationSpeed Speed { get; set; }
        public bool Moving { get; set; }
        public bool Forward { get; set; }
        public int LedSize { get; set; }
        
        public LightSetting(ColorMode mode) {
            this.mode = mode;
            minColorOptions = LightningHelper.MinimumColorOptions(mode);
            maxColorOptions = LightningHelper.MaximumColorOptions(mode);
            canSetSpeed = LightningHelper.CanSetSpeed(mode);
            canSetMoving = LightningHelper.CanSetMoving(mode);
            canSetDirection = LightningHelper.CanSetDirection(mode);
            canSetLedSize = LightningHelper.CanSetLedSize(mode);
            minLedSize = LightningHelper.MinimumLedSize(mode);
            maxLedSize = LightningHelper.MaximumLedSize(mode);
        }

        public LightSetting(ColorMode mode, AnimationSpeed speed, bool moving, bool forward, int ledSize,
                             params int[] colors) : this(mode) {
            Speed = speed;
            Moving = moving;
            Forward = forward;
            LedSize = ledSize;
            ColorOptions = colors.ToList();
        }
    }


    public static class LightningHelper {
        public static int MinimumColorOptions(ColorMode mode) {
            switch(mode) {
                case ColorMode.SpectrumWave:
                case ColorMode.WaterCooler:
                    return 0;
                case ColorMode.Fixed:
                case ColorMode.Marquee:
                case ColorMode.Wings:
                case ColorMode.Loading:
                    return 1;
                case ColorMode.Alternating:
                    return 2;
                default:
                    return 8;
            }
        }

        public static int MaximumColorOptions(ColorMode mode) {
            switch(mode) {
                case ColorMode.SpectrumWave:
                case ColorMode.WaterCooler:
                    return 0;
                case ColorMode.Alternating:
                case ColorMode.TaiChi:
                    return 2;
                case ColorMode.Breathing:
                case ColorMode.Fading:
                case ColorMode.CoveringMarquee:
                case ColorMode.Pulse:
                    return 8;
                default:
                    return 1;
            }
        }

        public static ColorMode[] PossibleColorModes(LightChannel channel) {
            switch(channel) {
                case LightChannel.Both:
                    return new[] {
                        ColorMode.Fixed, ColorMode.Breathing, ColorMode.Fading, ColorMode.CoveringMarquee,
                        ColorMode.Pulse, ColorMode.SpectrumWave
                    };
                case LightChannel.Logo:
                    return new[] {
                        ColorMode.Fixed, ColorMode.Breathing, ColorMode.Fading, ColorMode.Pulse, ColorMode.SpectrumWave
                    };
                case LightChannel.Rim:
                    return new[] {
                        ColorMode.Fixed, ColorMode.Breathing, ColorMode.Fading, ColorMode.Marquee,
                        ColorMode.CoveringMarquee, ColorMode.Pulse, ColorMode.SpectrumWave, ColorMode.Alternating,
                        ColorMode.Wings, ColorMode.TaiChi, ColorMode.WaterCooler, ColorMode.Loading
                    };
                default:
                    System.Diagnostics.Debug.WriteLine("Fall through should not have happened!");
                    return null;
            }
        }

        public static bool CanSetSpeed(ColorMode mode) {
            switch(mode) {
                case ColorMode.Fixed:
                case ColorMode.Loading:
                    return false;
                default:
                    return true;
            }
        }

        public static bool CanSetDirection(ColorMode mode) {
            switch(mode) {
                case ColorMode.Fixed:
                case ColorMode.Breathing:
                case ColorMode.Fading:
                case ColorMode.Pulse:
                case ColorMode.Wings:
                case ColorMode.TaiChi:
                case ColorMode.WaterCooler:
                case ColorMode.Loading:
                    return false;
                default:
                    return true;
            }
        }

        public static bool CanSetMoving(ColorMode mode) {
            return mode == ColorMode.Alternating;
        }

        public static bool CanSetLedSize(ColorMode mode) {
            return mode == ColorMode.Marquee;
        }

        public static int MaximumLedSize(ColorMode mode) {
            return mode == ColorMode.Marquee ? 3 : 0;
        }

        public static int MinimumLedSize(ColorMode mode) {
            return mode == ColorMode.Marquee ? 6 : 0;
        }
    }
}