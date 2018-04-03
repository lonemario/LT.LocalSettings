using LT.LocalSettings;
using LT.RandomGen;
using System;

namespace LT.LocalSettingsTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var iv = "uyXXuhv9k2tM3152";
            var pass = "w[To1;$HGLViM4]n";

            SettingsManager.Initialize(iv,pass,"UserTest","AppTest");

            var setting = SettingsManager.Get("Parametro1");

            if (setting != null)
                SettingsManager.Delete(setting);

            Console.WriteLine($"Number of setting: {SettingsManager.Count}");

            SettingsManager.Save(new LocalSettings.Models.Setting { Name="Parametro1", Value = "Prova valore Parametro1 ..." });

            TestMethod();

            Console.WriteLine($"Number of setting: {StaticClass.Count()}");

            Console.ReadKey();

        }

        static void TestMethod()
        {
            for (int i = 0; i < 100; i++)
            {
                SettingsManager.Save(new LocalSettings.Models.Setting {
                    Name = RandomGenerator.GenerateText(RandomGen.Data.Kinds.Things,1),
                    Value = RandomGenerator.GenerateText(RandomGen.Data.Kinds.Things, RandomGenerator.GenericInt(10,1))
                });
            }
        }
    }
}
