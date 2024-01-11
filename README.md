# CompatibilityChecker

# Compatibility Checker is deprecated!
Firstly, thank you everyone for downloading this mod, I honestly thought this small personal project of mine wouldn't be of use to anyone, especially in this scale. But now lets address the elephant in the room and answer some questions.
# Why deprecate?
Why the INJUSTICE!? Why not actively update it!? After promises of creating a usable UI!? You lied to us! 😒😒<br/> Now. calm down. I'll explain:
<br/><br/>**After some consideration**, I have deemed myself incompetent for maintaining such a widely used mod such as the Compatibility Checker. And so.. I shall forcefully pass the torch down to more competent developers! haha!
<br/><br/>I shall state that due to my inexpertise, and lack of knowledge regarding C#, me attempting to revive this mod will only inevitably slow down the process of me eventually deprecating it and building up false promises..<br/> But alas, not all hope is lost!<br/><br/>I didn't do this just on a whim, as eventually there *will* be better replacements for Compatibility Checker given some time in the future. Check out the [Modding Discord](https://discord.gg/lcmod) for more information. <br/>(Namely: Lobby Mod List Syncing Library & LethalModManager)

Given the recent influx of mods appearing on Thunderstore, this mod would require *complete* overhaul for it to work as its intended to. Which I sadly do not have enough time to do so, especially in a timely manner.. (blame my lazy bones for that)<br/>
And as a punch in the gut, this mod had already harbored *numerous* of silly, unresolved issues such as, but not limited to:<br/>
eg:
* A slight... difficulty in locating the right mod. *sad trombone*
* Potential hiccups when encountering special characters (particularly in mods with foreign letters).
* Juggling the responsibilities of updating and maintaining this mod solo.
* Issues with Lethal Company v49
* Challenge moons not showing up in the server list.
* Issues with slow internet connections.
* Horrible code
* This mod just... isnt it

(And let's be real, am just a regular person indulging in some modding fun – not exactly the go-to expert for a widely used community mod.)

# FAQ:
### I liked the lobby search feature, are there any other mods that has that feature?
* Absolutely! Don't worry; I made another mod just for that feature called: [Better Lobbies](https://thunderstore.io/c/lethal-company/p/Ryokune/Better_Lobbies/)<br/>
(not to be confused with [Bigger Lobby](https://thunderstore.io/c/lethal-company/p/bizzlemip/BiggerLobby/))
### Would there be a replacement for Compatibility Checker?
* There are a few that is currently being made in the [Modding Discord](https://discord.gg/lcmod). None are currently released but I will update this mod in order to redirect users to said mods once they release.
### Is this mod Open Source?
* Yep! It's liscensed as MIT. [GitHub](https://github.com/VisualError/CompatibilityChecker/)

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
