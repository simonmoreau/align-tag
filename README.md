<p align="center"><img width=12.5% src="https://raw.githubusercontent.com/simonmoreau/align-tag/master/AlignTag/Resources/AlignAppIcon.png"></p>
<h1 align="center">
  Align
</h1>

<h4 align="center">Align everything in Revit</h4>

# Overview

The Align tool allows you to align, distribute or organize selected elements, annotations, tags and text along the axis you specify. Furthermore, the Arrange feature will automatically neatly place your tags around the current view.

The Align plug-in for Autodesk® Revit® can help to save time while producing complex drawings with large sets of annotation.

Just select a few elements and the Align tool will sort them for you.

![Overview](https://raw.githubusercontent.com/simonmoreau/align-tag/master/AlignTag/Resources/alignTags.gif)

# Getting Started

Edit _AlignTag.csproj_, and make sure that the following lines a correctly pointing to your Revit installation folder:
* Line 27:     <StartProgram>$(ProgramW6432)\Autodesk\Revit Preview Release\Revit.exe</StartProgram>
* Line 37:     <StartProgram>$(ProgramW6432)\Autodesk\Revit Preview Release\Revit.exe</StartProgram>
* Line 42:     <HintPath>$(ProgramW6432)\Autodesk\Revit Preview Release\RevitAPI.dll</HintPath>
* Line 46:     <HintPath>$(ProgramW6432)\Autodesk\Revit Preview Release\RevitAPIUI.dll</HintPath>
* Line 140 to 143: <PostBuildEvent>...</PostBuildEvent>

Open the solution in Visual Studio 2017, buid it, and hit "Start" to run Revit in debug mode.

## Installation

There is two ways to install this plugin in Revit:

### The easy way

Download the installer on the [Autodesk App Exchange](https://apps.autodesk.com/RVT/en/Detail/Index?id=2903508825431715905&appLang=en&os=Win32_64)

### The (not so) easy way

You install Align just [like any other Revit add-in](http://help.autodesk.com/view/RVT/2018/ENU/?guid=GUID-4FFDB03E-6936-417C-9772-8FC258A261F7), by copying the add-in manifest (_"AlignTag.addin"_), the assembly DLL (_"AlignTag.dll"_) and the associated help file (_"AlignHelp.chm"_) to the Revit Add-Ins folder (%APPDATA%\Autodesk\Revit\Addins\2018).

If you specify the full DLL pathname in the add-in manifest, it can also be located elsewhere. However, this DLL, its dependanties and help files must be locted in the same folder.

Futhermore, the Visual Studio solution contain all the necessary post-build scripts to copy these files into appropriates folders.

## Built With

* .NET Framework 4.7 and [Visual Studio Community](https://www.visualstudio.com/vs/community/)
* The Visual Studio Revit C# and VB add-in templates from [The Building Coder](http://thebuildingcoder.typepad.com/blog/2017/04/revit-2018-visual-studio-c-and-vb-net-add-in-wizards.html)

# Development

Want to contribute? Great, I would be happy to integrate your improvements!

To fix a bug or enhance an existing module, follow these steps:

* Fork the repo
* Create a new branch (`git checkout -b improve-feature`)
* Make the appropriate changes in the files
* Add changes to reflect the changes made
* Commit your changes (`git commit -am 'Improve feature'`)
* Push to the branch (`git push origin improve-feature`)
* Create a Pull Request

# Bug / Feature Request

If you find a bug (connection issue, error while uploading, ...), kindly open an issue [here](https://github.com/simonmoreau/align-tag/issues/new) by including a screenshot of your problem and the expected result.

If you'd like to request a new function, feel free to do so by opening an issue [here](https://github.com/simonmoreau/align-tag/issues/new). Please include workflows samples and their corresponding results.

# License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

# Contact information

This software is an open-source project mostly maintained by myself, Simon Moreau. If you have any questions or request, feel free to contact me at [simon@bim42.com](mailto:simon@bim42.com) or on Twitter [@bim42](https://twitter.com/bim42?lang=en).

# Revision list

| **Version Number** | **Version Description** |
| :-------------: |:-------------|
1.3.0|Add support for every Revit element. Align every element according to its bounding box. Bug fix. Support for Autodesk® Revit® 2018 Version.
1.2.0|Add support for Text, Keynote Tag, Room and Space Tags. Align every tag according to its bounding box. Add Align Center and Align Middle. Support for Autodesk® Revit® 2017 Version.
1.1.0|Support for Autodesk® Revit® 2016 Version. Add the Arrange Tags feature.
1.0.0|First Release