# Localization Module
Localization Module is a submodule that allows to register new I2 localization strings.

## Usage example:
Make sure to add `[CoreLibSubmoduleDependency(nameof(LocalizationModule))]` to your plugin attributes. This will load the submodule.

Now in your plugin `Load()` method write:
```cs
LocalizationModule.AddTerm("TermID", "English Translation", "Chinese Translation");
```
`TermID` must be a unique identifier, which was not already localized. Chinese translation parameter is optional. All other unspecified languages will default to English translation.

If you need to add more languages use verbose version:
```cs
AddTerm("TermID", new Dictionary<string, string> { { "en", "English Translation" }, { "zh-CN", "Chinese Translation" }, /*...*/ });
```
