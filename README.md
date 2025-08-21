# LT.LocalSettings
[![NuGet](https://img.shields.io/nuget/v/Nuget.Core.svg)](https://www.nuget.org/packages/LT.LocalSettings)

A cross platform library for store and manage configurations settings in encrypted mode in the local user machine

## Prerequisites

### .NETStandard 2.0
```
LT.Cripto (>= 1.0.2)
Newtonsoft.Json (>= 13.0.2)
System.ComponentModel.Annotations (>= 4.4.0)
```

## Example 

The SettingsManager must be initialized before use. Use Init(string initVector, string passPhrase, string user, string app) method. It is sufficient to initialize the class only once in the application

```c#
static void Main(string[] args)
{
    //change with your values
    var iv = "uyXXuhv9k2tM3152";
    var pass = "w[To1;$HGLViM4]n";

    SettingsManager.Init(iv,pass,"UserTest","AppTest");

    Console.WriteLine($"Number of settings: {StaticClass.Count()}");

    var setting = SettingsManager.Get("Parametro1");

    if (setting != null)
        SettingsManager.Delete(setting);

    Console.WriteLine($"Number of settings: {SettingsManager.Count}");

    SettingsManager.Save(new LocalSettings.Models.Setting { Name="Parametro1", Value = "Prova valore Parametro1 ..." });
}
```

### Authors

- [Mario Righi](http://www.mariorighi.com)

### License

This project is licensed under the [MIT License](https://choosealicense.com/licenses/mit/)