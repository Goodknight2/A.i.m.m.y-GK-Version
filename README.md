Aimmy is a universal AI-Based Aim Alignment Mechanism developed by BabyHamsta, MarsQQ, and Taylor to make gaming more accessible for users who have difficulty aiming.
Aimmy also provides an easy to use user-interface, a wide set of features and customizability options which makes Aimmy a great option for anyone who wants to use and tailor an Aim Alignment Mechanism for a specific game without having to code.

Aimmy is 100% free to use. This means no ads, no key system, and no paywalled features. Aimmy is not, and will never be for sale for the end user, and is considered a source-available product, **not open source** as we actively discourage other developers from making commercial forks of Aimmy.

Please do not confuse Aimmy as an open-source project, we are not, and we have never been one.

Want to connect with us? Join our [Discord Server](https://discord.gg/aimmy)

If you want to share Aimmy with your friends use our [website!](https://aimmy.dev/)

In this version, all of the Aimmy strings have been renamed, so now the code does not contain the word Aimmy
# Warning
To not get banned in games like Bloodstrike, make sure you
- Don't name the folder where Aimmy is anything with Aimmy in it
- Don't open Aimmy's Discord channel while in the lobby or in-game
- Don't always alt-tab to Aimmy while in-game or in lobby
- You can still get temp-banned for 2 hours if you get reported too much
- If you still got banned, check the folder name or other running processes.

# Disclaimer
This is a fork of [Aimmy](https://github.com/Babyhamsta/Aimmy/), if any problems ask us on [discord](discord.gg/aimmy).
## What is CUDA
> **What's CUDA?**

```Cuda is pretty much just the better version of "DirectML" and uses Nvidia's GPU power to make it more smoother and faster```

> **What's TensorRT?**

```Pretty much an add-on for Cuda. While it does make your gameplay smoother and faster, it's a double edge sword by making your models loading time drastically slower for 1st time instances```

> **What's DirectML?**

```Think of it as a mid lvl AI that relies on your GPU to work good```

> **How does the AI work?**

```Using the imported models (pictures), it will then scan the game as you play and look for players that match the models (pictures)```
## Setup
- Download and Install the x64 version of [.NET Runtime 8.0.X.X](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.2-windows-x64-installer)
- Download and Install the x64 version of [.NET Runtime 7.0.X.X](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-7.0.20-windows-x64-installer)
- Download and Install the x64 version of [Visual C++ Redistributable](https://aka.ms/vs/17/release/vc_redist.x64.exe)
- Download Aimmy from [Releases](Make sure it's the Aimmy zip and not Source zip)
- Extract and run totallynotspotifyv2.exe
- Go to the troubleshooting section if you have issues.
## Arduino Setup(Credit to Slow Potato for making this guide)
- If using a HOST SHIELD select "Y" when prompted if you don't have one just select "N".
-  HOST SHIELD is "recommended" for games on STEAM, EA, Battlenet etc... since the newest patch 10/15/25 blocks 2nd mouse inputs such as Leonardo R3 alone without a HOST SHIELD, DDxoft, Mouse Events, Razer Drivers, LG Hub Drivers. [If using without HOST SHIELD this will still work on other games that block out most external mouse inputs]
-  Video instructions Below
-  https://github.com/user-attachments/assets/5d933af0-6dc1-425c-90f5-f920b4b94c04
### WIN11 USERs getting "wmic" error
- Do the following command in Powershell as Admin:   Add-WindowsCapability -online -name WMIC
- Then re-run .bat as Admin.
- If still fails run this command: dism /online /add-capability /capabilityname:WMIC~~~~
### KNOWN UNSUPPORTED MOUSES:
- Logitech G300s, Logitech Hero 502, Logitech G102
- Any 2-in-1 Keyboard and Mouse with a single receiver
