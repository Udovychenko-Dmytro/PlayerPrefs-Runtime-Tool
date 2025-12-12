// PlayerPrefsRuntimePlugin.mm macOS
// ====================================================
// PlayerPrefsRuntime Tool - Unity Plugin
// Author: Dmytro Udovychenko
// Contact: https://www.linkedin.com/in/dmytro-udovychenko/
// License: MIT
// © 2025 Dmytro Udovychenko. All rights reserved.
// ====================================================
#import <Foundation/Foundation.h>

extern "C" char* GetPlayerPrefsJSON()
{
    @autoreleasepool {
        // Retrieve the application's Bundle ID
        NSString *bundleId = [[NSBundle mainBundle] bundleIdentifier];
        
        // Log BundleId
        NSLog(@"[PlayerPrefsRuntime] GetPlayerPrefsJSON called. Bundle ID: %@", bundleId);
        
        if (!bundleId || [bundleId length] == 0) {
            NSLog(@"[PlayerPrefsRuntime] Invalid Bundle ID");
            
            char* emptyString = (char*)malloc(3);
            strcpy(emptyString, "{}");
            return emptyString;
        }

        // Construct the path to the plist file using the Bundle ID
        NSString *plistPath = [NSString stringWithFormat:@"~/Library/Preferences/%@.plist", bundleId];
        plistPath = [plistPath stringByExpandingTildeInPath];

        // Log file path
        NSLog(@"[PlayerPrefsRuntime] Reading plist from path: %@", plistPath);
        
        // Check if file exists
        if (![[NSFileManager defaultManager] fileExistsAtPath:plistPath]) {
            NSLog(@"[PlayerPrefsRuntime] PlayerPrefs plist file does not exist at path: %@", plistPath);
            
            char* emptyString = (char*)malloc(3);
            strcpy(emptyString, "{}");
            return emptyString;
        }

        // Read the contents of the plist file into an NSDictionary
        NSDictionary *dict = [NSDictionary dictionaryWithContentsOfFile:plistPath];

        if (!dict) {
            NSLog(@"[PlayerPrefsRuntime] Failed to read plist or plist is empty");
            
            char* emptyString = (char*)malloc(3);
            strcpy(emptyString, "{}");
            return emptyString;
        }

        // Log the number of entries found
        NSLog(@"[PlayerPrefsRuntime] Found %lu PlayerPrefs entries", (unsigned long)[dict count]);

        // Convert the NSDictionary to JSON data
        NSError *error;
        NSData *jsonData = [NSJSONSerialization dataWithJSONObject:dict options:0 error:&error];

        if (!jsonData) {
            NSLog(@"[PlayerPrefsRuntime] Error converting PlayerPrefs to JSON: %@", error);
            
            char* emptyString = (char*)malloc(3);
            strcpy(emptyString, "{}");
            return emptyString;
        }

        // Initialize an NSString with the JSON data
        NSString *jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
        
        if (!jsonString) {
            NSLog(@"[PlayerPrefsRuntime] Failed to create JSON string from data");
            
            char* emptyString = (char*)malloc(3);
            strcpy(emptyString, "{}");
            return emptyString;
        }
        
        // Log Json data (truncated for long strings)
        if ([jsonString length] > 1000) {
            NSString *truncatedJson = [jsonString substringToIndex:1000];
            NSLog(@"[PlayerPrefsRuntime] JSON String (truncated): %@...", truncatedJson);
        } else {
            NSLog(@"[PlayerPrefsRuntime] JSON String: %@", jsonString);
        }

        const char* utf8String = [jsonString UTF8String];
        if (!utf8String) {
            NSLog(@"[PlayerPrefsRuntime] Failed to get UTF8 string from JSON");
            
            char* emptyString = (char*)malloc(3);
            strcpy(emptyString, "{}");
            return emptyString;
        }
        
        size_t length = strlen(utf8String) + 1;
        
        // Validate length to prevent excessive memory allocation
        if (length > 1024 * 1024) { // 1MB limit
            NSLog(@"[PlayerPrefsRuntime] JSON string too large (%zu bytes), truncating", length);
            length = 1024 * 1024;
        }

        char* buffer = (char*)malloc(length);
        if (buffer == NULL) {
            NSLog(@"[PlayerPrefsRuntime] Failed to allocate memory for JSON string");
            
            char* emptyString = (char*)malloc(3);
            strcpy(emptyString, "{}");
            return emptyString;
        }
        
        strncpy(buffer, utf8String, length - 1);
        buffer[length - 1] = '\0'; // Ensure null termination

        return buffer;
    }
}

extern "C" void FreeMemory(char* ptr)
{
    if (ptr != NULL) {
        free(ptr);
    }
}
