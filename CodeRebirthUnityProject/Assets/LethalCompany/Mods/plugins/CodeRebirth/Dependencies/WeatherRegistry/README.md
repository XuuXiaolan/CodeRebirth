# WeatherRegistry

A Lethal Company mod for controlling game's weather system.

## Features

- A system for registering custom weathers and weather effects
- Weight-based weather selection system
- Level-based weather filtering system

## Weight-based weather selection system

Contrary to the vanilla algorithm, this mod uses a weight-based system for selecting weathers. You can set the weights based on 3 criteria:

1. Level weight: the weight of the weather based on specific level
2. Weather-to-weather weight: the weight of the weather based on the previous weather
3. Default weight: the base weight of the weather

During the weather selection process, the algorithm will try to apply the weights in the order listed above.

## License

This project is licensed under [GNU Lesser General Public License v3.0](https://github.com/AndreyMrovol/LethalWeatherRegistry/blob/main/LICENSE.md).

## Credits

This project uses [LethalCompanyTemplate](https://github.com/LethalCompany/LethalCompanyTemplate), licensed under [MIT License](https://github.com/LethalCompany/LethalCompanyTemplate/blob/main/LICENSE).

This project uses code from [WeatherTweaks](https://github.com/AndreyMrovol/LethalWeatherTweaks/tree/main), licensed under [CC BY-NC-ND 4.0](https://github.com/AndreyMrovol/LethalWeatherTweaks/blob/main/LICENSE.md).

This project uses code from [LethalLib](https://thunderstore.io/c/lethal-company/p/Evaisa/LethalLib/), licensed under [MIT License](https://github.com/EvaisaDev/LethalLib/blob/main/LICENSE).

This project uses code from [LethalLevelLoader](https://thunderstore.io/c/lethal-company/p/IAmBatby/LethalLevelLoader/), licensed under [MIT License](https://github.com/IAmBatby/LethalLevelLoader/blob/main/LICENSE).

This project uses code from [LC-SimpleWeatherDisplay](https://thunderstore.io/c/lethal-company/p/SylviBlossom/SimpleWeatherDisplay/), licensed under [MIT License](https://github.com/SylviBlossom/LC-SimpleWeatherDisplay/blob/main/LICENSE).
