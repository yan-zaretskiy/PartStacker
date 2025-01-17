# PartStacker

A community fork of https://tomvanderzanden.nl/partstacker/

## To build this project on Windows

You will need:

* Visual Studio 2022, including the ".NET desktop development" workload
* A command line that has `git` installed

### Grab the code

Open your command line to a directory where you want to put the PartStacker code. Then run this.

```
git clone --recurse-submodules --jobs 8 https://github.com/PartStackerCommunity/PartStacker.git
```

After this, there will be a new folder called "PartStacker" at whatever location you ran this command from. All of the contents of this repository will be in that folder.

### Build the dependencies

Go into the "PartStacker" folder. Then, open `Dependencies/MonoGame/MonoGame.Framework.WindowsDX.sln` in Visual Studio and do the following.

* Menu bar -> "Build" -> "Batch Build"
* Select all the checkboxes until the "Build" column
* Click the "Rebuild" button
* Wait for it to finish
* After this, you can close Visual Studio.

You only need to do this once, not every time you want to build PartStacker.

### Build and run PartStacker itself

In the "PartStacker" folder, open `PartStacker.sln` in Visual Studio. Click the solid green arrow at the top. If all goes well, PartStacker will build and immediately start running.
