[![license](https://img.shields.io/badge/license-MIT-red.svg)](LICENSE)
[![version](https://img.shields.io/badge/package-download-brightgreen.svg)](PlayerPrefsRuntimeTool.unitypackage)

# PlayerPrefsRuntime Tool
`version 1.0`
## Overview

**PlayerPrefsRuntime Tool** is a Unity plugin that allows you to retrieve all `PlayerPrefs` at runtime across multiple platforms, including Android, iOS, Windows, and macOS. This tool is invaluable for 
debugging, analytics, and ensuring the integrity of player preferences within your Unity projects.

---
## Features

- **Cross-Platform Support:** Compatible with `Android`, `iOS`, `Windows`, and `macOS`.
- **Runtime Access:** Retrieve all `PlayerPrefs` as a dictionary directly from device for easy manipulation.
- **Logging:** Display all `PlayerPrefs` data in the Unity Console for debugging purposes.
- **Extensible Architecture:** Easily extendable to support additional platforms or functionalities.
---
## Installation

### 1. **Download the Plugin**

Clone the repository or download the latest release from GitLab.

```bash
git clone https://github.com/Udovychenko-Dmytro/PlayerPrefs-Runtime-Tool.git
```

OR

1. Download the plugin package.
2. Import it into your Unity project via `Assets > Import Package > Custom Package`.
3. Follow the setup instructions.


### 2. **Enable the Tool**

To enable the PlayerPrefsRuntime Tool, define the `PLAYER_PREFS_RUNTIME_TOOL` scripting symbol:

Go to `Edit > Project Settings > Player`.
Under the Other Settings tab, find Scripting Define Symbols.
Add PLAYER_PREFS_RUNTIME_TOOL to the list, separated by a semicolon if other symbols are present.

## Usage
```bash
public class PlayerPrefsRuntimeExample : MonoBehaviour
    {
        private void Start()
        {
#if PLAYER_PREFS_RUNTIME_TOOL
            PlayerPrefsRuntime.AddTestPlayerPrefs(); // add playerpref data
            PlayerPrefsRuntime.LogAllPlayerPrefs();  // log all playerpref data to console
            
            // retrieve all PlayerPrefs as Dictionary <key, value>
            Dictionary<string, object> allPrefs = PlayerPrefsRuntime.GetAllPlayerPrefs();
#endif
        }
    }

```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contact

For any questions, suggestions, or feedback, please contact:

- **Linked-in:** [https://www.linkedin.com/in/dmytro-udovychenko/](https://www.linkedin.com/in/dmytro-udovychenko/)