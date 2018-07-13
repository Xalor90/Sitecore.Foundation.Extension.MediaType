# Media Type Extension for Sitecore 9

This extension will allow you to add support for custom Media Types to Sitecore, such as SVG.

## License
[MIT](/LICENSE.md)

## How to install:
- To begin, clone this repo into an existing Sitecore 9, Helix-based Solution under `/src/Foundation/Extension`.
- Build project and ensure NuGet restores missing NuGet packages.
- Copy the contents of `/App_Config` into your primary web project alongside your existing `/App_Config` files.
- Copy the contents of `/sitecore` into your primary web project alongside your existing `/sitecore` files.
- Add a reference of this project to your primary web app and rebuild the entire Solution.

## How to use:
- Upload SVG to Sitecore Media Library.
- Set the `Width`, `Height`, and `Alt` fields on the Media Item (Sitecore handles the rest).

## Attribution:
- [Nikola Gotsev](https://sitecorecorner.com/2015/11/23/sitecore-svg-support/)

There are many tutorials out there on how to add SVG support to Sitecore, but they all overlook certain problems that this particular article by Nikola Gotsev (link above) addresses quite nicely. He also has a [Bitbucket repository](https://bitbucket.org/nsgocev/sitecore-svg/src/master/) for this solution, but I have structured mine specifically for use with a Helix-based Sitecore solution.
