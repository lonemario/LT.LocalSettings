using LT.LocalSettings;

namespace LT.LocalSettingsTest
{
    public static class StaticClass
    {
        public static int Count()
        {
            return SettingsManager.Count;
        }
    }
}
