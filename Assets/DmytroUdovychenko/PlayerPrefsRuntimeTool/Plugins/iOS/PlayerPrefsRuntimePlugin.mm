// PlayerPrefsRuntimePlugin.mm iOS
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
        NSLog(@"[PlayerPrefsRuntime] GetPlayerPrefsJSON called");
        NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
        NSDictionary *dict = [defaults dictionaryRepresentation];
        
        if (!dict) {
            NSLog(@"[PlayerPrefsRuntime] Failed to retrieve UserDefaults dictionary");
            // Return an empty JSON string allocated with malloc
            char* emptyString = (char*)malloc(3);
            strcpy(emptyString, "{}");
            return emptyString;
        }
        
        NSMutableDictionary *serializableDict = [NSMutableDictionary dictionary];
        
        for (NSString *key in dict) {
            id value = dict[key];
            // Filter only supported data types (NSString and NSNumber)
            if ([value isKindOfClass:[NSString class]] ||
                [value isKindOfClass:[NSNumber class]]) {
                serializableDict[key] = value;
                // Log key-value pairs for debugging
                NSLog(@"[PlayerPrefsRuntime] Key: %@, Value: %@", key, value);
            } else {
                NSLog(@"[PlayerPrefsRuntime] Skipping unsupported type for key: %@, type: %@", key, [value class]);
            }
        }
        
        // Convert NSDictionary to JSON
        NSError *error;
        NSData *jsonData = [NSJSONSerialization dataWithJSONObject:serializableDict options:0 error:&error];
        
        if (!jsonData) {
            NSLog(@"[PlayerPrefsRuntime] Error converting PlayerPrefs to JSON: %@", error);
            // Return an empty JSON string allocated with malloc
            char* emptyString = (char*)malloc(3);
            strcpy(emptyString, "{}");
            return emptyString;
        }
        
        NSString *jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
        NSLog(@"[PlayerPrefsRuntime] JSON String: %@", jsonString);
        
        const char* utf8String = [jsonString UTF8String];
        size_t length = strlen(utf8String) + 1;
        
        char* buffer = (char*)malloc(length);
        if (buffer == NULL) {
            NSLog(@"[PlayerPrefsRuntime] Failed to allocate memory for JSON string");
            // Return an empty JSON string allocated with malloc
            char* emptyString = (char*)malloc(3);
            strcpy(emptyString, "{}");
            return emptyString;
        }
        
        strcpy(buffer, utf8String);
        
        return buffer;
    }
}

extern "C" void FreeMemory(char* ptr)
{
    if (ptr != NULL) {
        free(ptr);
    }
}

