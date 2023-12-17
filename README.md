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

# Version 1.1.0

- **Display Notification Aspect Ratio Fix:** Resolved aspect ratio issues with the display notification for a more consistent user experience.

- **Enhanced Logs:** Logs now provide additional information, including `Mod.GUID`, `Mod.Version`, `Mod.Link`, and `Mod.Downloads`, offering more insight into the mods.

- **Improved Thunderstore API Load Time:** Thunderstore API loading performance has been optimized for a faster experience.

- **Refined Mod Dependency Handling:** The mod now sets required mods to server-side mods only, streamlining compatibility checks and improving overall functionality.

- **Server-Side Mod Version Display:** CompatibilityChecker now displays the version information for each server-side mod the host has, giving users more transparency.

- **Outdated Mods Notification:** Logs will now indicate if any of your mods are outdated, helping you stay up-to-date with the latest versions.

- **In-Game CompatibilityChecker Status:** Receive in-game notifications if your CompatibilityChecker mod is outdated, ensuring you are aware of the latest updates.

- **Server Compatibility Check:** CompatibilityChecker will inform you if the server you're joining doesn't have CompatibilityChecker installed, helping you make informed decisions about server compatibility.

# Version 1.1.1 (QuickFix)
- **Fixed**: Infinite Loading Bug.

# Version 1.1.4
- **Fixed**: Missing mods logs not showing any information.
- **Fixed**: Possible incompatible mods information should now show up in your mod logs.
- **Added**: Lobby search functionality! YIPPIE!!!
- **Next patch**: Join via lobby code.

# Version 1.1.5
- **Added**: Copy server ID, ability to join with server id.

# Version 1.1.6
- **Fixed**: Server search breaking if you put in an invalid lobby code.