# SubtitlesAPI

A centralized API for fetching localized subtitles.

## Feedback

Got any feedback or found bugs? Please do post it in [the Github Issues page](https://github.com/JanGuillermo/LethalCompany/issues)!

## Usage

If you are creating a mod and you want to add subtitles for any custom sounds, it's pretty simple! Add the mod .dll file to your project references, and just add the following code:

```cs
using static SubtitlesAPI.SubtitlesAPI;

try
{
  Localization.AddTranslation("CustomSound", "Custom subtitle");
}
catch
{
  Debug.Log("SubtitlesAPI Mod not Found");
}
```

No need to even require players to use this mod!

### Methods

- `SubtitlesAPI.Localization.AddTranslation(string soundFileName, string subtitleText);`
- `SubtitlesAPI.Localization.AddTranslation(Dictionary<string, string>);`
  - Where `Dictionary<string, string>` is a dictionary with `Key: soundFileName`, `Value: subtitleText`.
- `SubtitlesAPI.Localization.AddDialogueTranslation(string sound, List<(float, string)> subtitles);`
- `SubtitlesAPI.Localization.AddDialogueTranslation(Dictionary<string, List<(float, string)>> translationsToAdd);`
  - Where `Dictionary<string, List<(float, string)>>` is a dictionary with `Key: soundFileName`, `Value: subtitles`.

The method will return a boolean, or list of booleans for the dictionary.

The result will be true if the mod was able to add the subtitle, and false if not - which is likely due to the translation for the soundFileName already being created. Maybe another mod has the same sound file name?

## Contributing

Do you want to add or change any subtitles? Do you want to create a new locale for another language? Please do! Create a fork of this repo, make additions or changes there, and submit a pull request.

### Additional Subtitles

Open up the locale you want to modify (ex. EnglishSubtitleLocalization.cs), and add to the dictionary!

### SubtitlesAPI.Localization.Translations

The dictionary uses the following format for new entries:

```cs
{ "SoundFileName" , "Subtitle to add" },
```

### SubtitlesAPI.Localization.DialogueTranslations

The dictionary uses the following format for new entries:

```cs
{
  "F0DaysLeftAlert",
  new()
  {
    (0, "[Company Jingle plays]"),
    (4.969f, "Report to the company building immediately"),
    (7.189f, "to sell your scrap metal and other goods."),
    (9.758f, "You have zero days left to meet the profit quota."),
    (13.085f, "You can use the terminal to route"),
    (14.874f, "the autopilot to the company building."),
  }
},
```

### New Locales

To create new locales, just add a new file in the Locales folder, use the ISubtitleLocalization interface, change the Locale to the one being added, and add translations to the dictionary!

#### Example Locale

```cs
public class EnglishSubtitleLocalization : ISubtitleLocalization
{
  public string Locale => "en";

  public Dictionary<string, string> Translations => new(StringComparer.OrdinalIgnoreCase) {
    { "AirHorn1", "Air horn plays" },
  }

  public Dictionary<string, List<(float, string)>> DialogueTranslations => new(StringComparer.OrdinalIgnoreCase)
    {
      {
        "F0DaysLeftAlert",
        new()
        {
          (0, "[Company Jingle plays]"),
          (4.969f, "Report to the company building immediately"),
          (7.189f, "to sell your scrap metal and other goods."),
          (9.758f, "You have zero days left to meet the profit quota."),
          (13.085f, "You can use the terminal to route"),
          (14.874f, "the autopilot to the company building."),
        }
      },
    };
}
```
