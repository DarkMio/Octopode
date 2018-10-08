namespace Octopode.Core {
    public static class LightningHelper {

        public static int PossibleColorGroups(ColorMode mode) {
            switch(mode) {
                case ColorMode.Alternating:
                case ColorMode.Alert:
                    return 2;
                case ColorMode.Breathing:
                case ColorMode.Fading:
                case ColorMode.CoveringMarquee:
                case ColorMode.Pulse:               
                    return 8;
                case ColorMode.SpectrumWave:
                    return 0;
                default:
                    return 1;
            }
        } 
    }
}