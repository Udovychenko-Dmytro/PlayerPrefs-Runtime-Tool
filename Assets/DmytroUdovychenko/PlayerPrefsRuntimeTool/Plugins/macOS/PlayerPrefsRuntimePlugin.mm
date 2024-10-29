// PlayerPrefsRuntimePlugin.mm macOS
// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2024 Dmytro Udovychenko. All rights reserved.
// ====================================================
#import <Foundation/Foundation.h>

// Exported function to get PlayerPrefs as JSON string
extern "C" char* GetPlayerPrefsJSON()
{
    @autoreleasepool {
        // Retrieve the application's Bundle ID
        NSString *bundleId = [[NSBundle mainBundle] bundleIdentifier];
        
        // Log BunfleId
        NSLog(@"GetPlayerPrefsJSON called. Bundle ID: %@", bundleId);

        // Construct the path to the plist file using the Bundle ID
        NSString *plistPath = [NSString stringWithFormat:@"~/Library/Preferences/%@.plist", bundleId];
        plistPath = [plistPath stringByExpandingTildeInPath];

        // Log file path
        NSLog(@"Reading plist from path: %@", plistPath);
        // Read the contents of the plist file into an NSDictionary
        NSDictionary *dict = [NSDictionary dictionaryWithContentsOfFile:plistPath];

        if (!dict) {
            NSLog(@"Failed to read plist. Returning empty JSON.");
            // Allocate memory for an empty JSON string
            char* emptyString = (char*)malloc(3);
            strcpy(emptyString, "{}");
            return emptyString;
        }

        // Convert the NSDictionary to JSON data
        NSError *error;
        NSData *jsonData = [NSJSONSerialization dataWithJSONObject:dict options:0 error:&error];

        if (!jsonData) {
            NSLog(@"Error converting PlayerPrefs to JSON: %@", error);
            // Allocate memory for an empty JSON string
            char* emptyString = (char*)malloc(3);
            strcpy(emptyString, "{}");
            return emptyString;
        }

        // Initialize an NSString with the JSON data
        NSString *jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
        // Log Json data
        NSLog(@"JSON String: %@", jsonString);

        // Get a C-style string from the NSString
        const char* utf8String = [jsonString UTF8String];
        size_t length = strlen(utf8String) + 1;

        // Allocate memory for the C-style string and copy it
        char* buffer = (char*)malloc(length);
        strcpy(buffer, utf8String);

        return buffer;
    }
}

// Exported function to free allocated memory
extern "C" void FreeMemory(char* ptr)
{
    if (ptr != NULL) {
        free(ptr);
    }
}
