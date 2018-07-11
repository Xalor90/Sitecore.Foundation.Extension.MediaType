# Media Type Extension for Sitecore 9

This extension will allow you to add support for custom Media Types to Sitecore, such as SVG.

## License
[MIT](/LICENSE.md)

## How to install:
- To begin, clone this repo into an existing Sitecore 9, Helix-based Solution under `/src/Foundation/Extension/MediaType`.
- Add the appropriate version of `Sitecore.Kernel` to this project from NuGet and build the project.
- Copy the contents of `/App_Config` into your primary web project alongside your existing `/App_Config` files.
- Copy the contents of `/sitecore` into your primary web project alongside your existing `/sitecore` files.
- Add a reference of this project to your primary web app and rebuild the entire Solution.

## How to use:
- Upload SVG to Sitecore Media Library.
- Set the `Width`, `Height`, and `Alt` fields on the Media Item (Sitecore handles the rest).
