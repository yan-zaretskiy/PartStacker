# PartStacker

A tool to pack 3D mesh files into a minimal bounding box.

A community rewrite of Tom van der Zanden's beloved [PartStacker](https://github.com/TomvdZanden/PartStacker/).

## Download the latest release

[**Click here**](https://github.com/PartStackerCommunity/PartStacker/releases/tag/v0.2) for the latest release.

## Build this project

**If any of the following doesn't make sense, please see the [in-depth build instructions](./docs/building/building.md).**

You'll need

* CMake
* A C++ compiler that supports C++23

First, clone the repo with its submodules

```
git clone --recurse-submodules https://github.com/PartStackerCommunity/PartStacker.git
```

Navigate to `./external/glew/auto`, and run `make`

Then, back at the root directory, configure and build with CMake

```
cmake . -B build
cmake --build build
```

The PartStacker GUI will be an executable called `PartStackerGUI`
