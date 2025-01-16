# PartStacker

A community fork of https://tomvanderzanden.nl/partstacker/

## To build this project on Windows

You will need:

* Visual Studio 2022
* A command line that has `git` installed

### Grab the code

```
git clone --recurse-submodules --jobs 8 git@github.com:PartStackerCommunity/PartStacker.git
```

### Build the dependencies

Open `Dependencies/MonoGame/MonoGame.Framework.WindowsDX.sln`, then do the following.

* Menu bar -> "Build" -> "Batch Build"
* Select all the checkboxes until the "Build" column
* Click the "Rebuild" button
* Wait for it to finish

You only need to do this once, not every time you want to build PartStacker.

### Build PartStacker itself

Then open `PartStacker.sln` in VisualStudio, and build.
