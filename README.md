# CompatibilityChecker (Thanks Bobbie for the name suggestion.)

## Overview

Enhances your multiplayer experience by notifying you of the required mods when failing to join modded servers. (Required both by host and client.)

## Features

- **Automatic Mod Notification**: Modded servers will now notify you of the required mods upon a non-successful join.

## Installation

1. Ensure you have [BepInEx](https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/) installed.
2. Download the latest release of the CompatibilityChecker mod from [Thunderstore](https://thunderstore.io/c/lethal-company/p/Ryokune/CompatibilityChecker/).
3. Extract the contents into your Lethal Company's `BepInEx/plugins` folder.

## How It Works

- The mod automatically notifies you of required mods when joining modded servers.
- Mod information is shared between players through the Steam lobby.

## Support and Feedback

If you encounter any issues or have suggestions for improvement, feel free to [report them on GitHub](https://github.com/VisualError/CompatibilityChecker/issues).

Enjoy a more informed multiplayer experience with CompatibilityChecker!


# Version 1.0.0
- Initial release.

# Version 1.0.1
- Fixed server list scrolling.
- Added server indicator if server host has CompatibilityChecker.

# Version 1.0.2
- Fixed not all mods being loaded by the BepInEx chainloader.

# Version 1.0.3
- Fixed server list not compatible with MoreCompany.

# Version 1.0.4
- Fixed more bugs.

# Version 1.0.5
- Accesses the Thunderstore API for easier mod downloads (access the links from the logs/console).
- Added a warning if you are using an outdated/unknown version of CompatibilityChecker.
- Only shows you mods with the server-sided category using the Thunderstore API.

# Version 1.0.6
- Made logs more clear.
- Removed lobby name limit on server list (Text overflow issue not fixed.)