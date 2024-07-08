# Latest Component Changed VSC

The Latest Component Changed VSC extension for Visual Studio displays the latest changed component in the development environment based on the configuration in the .gitconfig file.

## Features

- Displays the latest changed component in the Visual Studio status bar.
- Automatically updates when changes occur in the .gitconfig file.


## Usage

1. Clone, open the project and select Compile Solution in Visual Studio
2. Install the extension manually from the "project/bin/Debug/latest-component-changed-vs.vsix" path
3. NOTE: Close editor before installs extension
4. The latest changed component will be displayed in the Visual Studio status bar automatically.
5. Compatibility and personal use with conventional commit environment

## Configuration

No additional configuration is required. The extension automatically retrieves information from the .gitconfig file.

## Build

To create .vsix extension file, you need to Compile Solution option in Visual Studio and the debugger creates the .vsix file in project/bin/Debug/ path

## Notes

- Ensure that the `variable.latest-component-changed` variable is properly configured in your .gitconfig file for accurate results.
- The extension is designed to provide information about the latest changed component based on the development environment configuration.
