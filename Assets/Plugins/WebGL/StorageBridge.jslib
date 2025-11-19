// Save this as: Assets/Plugins/WebGL/StorageBridge.jslib
// This version works in BOTH React Native WebView AND regular browsers

mergeInto(LibraryManager.library, {
    SaveToAsyncStorage: function(keyPtr, valuePtr) {
        var key = UTF8ToString(keyPtr);
        var value = UTF8ToString(valuePtr);
        
        try {
            // Check if running in React Native WebView
            if (window.ReactNativeWebView && typeof window.ReactNativeWebView.postMessage === 'function') {
                window.ReactNativeWebView.postMessage(JSON.stringify({
                    type: 'SAVE_DATA',
                    key: key,
                    value: value
                }));
                console.log('📤 [RN WebView] Sent to React Native:', key);
            } else {
                // Fallback to localStorage for regular browser
                localStorage.setItem(key, value);
                console.log('💾 [Browser] Saved to localStorage:', key, '=', value);
            }
        } catch (e) {
            console.error('❌ Error in SaveToAsyncStorage:', e);
        }
    },
    
    GetFromAsyncStorage: function(keyPtr) {
        var key = UTF8ToString(keyPtr);
        
        try {
            if (window.ReactNativeWebView && typeof window.ReactNativeWebView.postMessage === 'function') {
                window.ReactNativeWebView.postMessage(JSON.stringify({
                    type: 'GET_DATA',
                    key: key
                }));
                console.log('📤 [RN WebView] Requesting from React Native:', key);
            } else {
                // Fallback to localStorage for regular browser
                var value = localStorage.getItem(key) || '';
                var data = key + ':' + value;
                
                // Send back to Unity
                if (typeof unityInstance !== 'undefined' && unityInstance) {
                    unityInstance.SendMessage('StorageBridge', 'ReceiveAsyncStorageValue', data);
                    console.log('📥 [Browser] Loaded from localStorage:', key, '=', value);
                } else {
                    console.warn('⚠️ Unity instance not available yet');
                }
            }
        } catch (e) {
            console.error('❌ Error in GetFromAsyncStorage:', e);
        }
    },
    
    SaveAllPlayerPrefs: function(jsonDataPtr) {
        var jsonData = UTF8ToString(jsonDataPtr);
        
        console.log('💾 SaveAllPlayerPrefs called');
        
        try {
            if (window.ReactNativeWebView && typeof window.ReactNativeWebView.postMessage === 'function') {
                // React Native WebView mode
                window.ReactNativeWebView.postMessage(JSON.stringify({
                    type: 'SAVE_ALL_DATA',
                    data: jsonData
                }));
                console.log('📤 [RN WebView] Sent all data to React Native');
            } else {
                // Browser mode - save to localStorage
                try {
                    var dataObj = JSON.parse(jsonData);
                    
                    if (dataObj && dataObj.data) {
                        // Data is wrapped in { data: {...} }
                        for (var key in dataObj.data) {
                            localStorage.setItem(key, dataObj.data[key]);
                        }
                        console.log('💾 [Browser] Saved all data to localStorage (' + Object.keys(dataObj.data).length + ' keys)');
                    } else {
                        // Data is flat object
                        for (var key in dataObj) {
                            localStorage.setItem(key, dataObj[key]);
                        }
                        console.log('💾 [Browser] Saved flat data to localStorage');
                    }
                } catch (parseError) {
                    console.error('❌ Error parsing JSON data:', parseError);
                    console.error('Data received:', jsonData);
                }
            }
        } catch (e) {
            console.error('❌ Error in SaveAllPlayerPrefs:', e);
        }
    },
    
    LoadAllPlayerPrefs: function() {
        console.log('📥 LoadAllPlayerPrefs called');
        
        try {
            if (window.ReactNativeWebView && typeof window.ReactNativeWebView.postMessage === 'function') {
                // React Native WebView mode - request data
                window.ReactNativeWebView.postMessage(JSON.stringify({
                    type: 'LOAD_ALL_DATA'
                }));
                console.log('📤 [RN WebView] Requested all data from React Native');
            } else {
                // Browser mode - load from localStorage
                var allData = {};
                
                // Read all items from localStorage
                for (var i = 0; i < localStorage.length; i++) {
                    var key = localStorage.key(i);
                    var value = localStorage.getItem(key);
                    allData[key] = value;
                }
                
                console.log('📥 [Browser] Loaded ' + Object.keys(allData).length + ' keys from localStorage');
                
                // Send back to Unity
                if (typeof unityInstance !== 'undefined' && unityInstance) {
                    var jsonData = JSON.stringify({ data: allData });
                    unityInstance.SendMessage('StorageBridge', 'ReceiveAllPlayerPrefs', jsonData);
                    console.log('✅ [Browser] Sent all data to Unity');
                } else {
                    console.warn('⚠️ Unity instance not available yet, data will be read on demand');
                }
            }
        } catch (e) {
            console.error('❌ Error in LoadAllPlayerPrefs:', e);
        }
    },
    
    CloseWebView: function() {
        console.log('🚪 CloseWebView called');
        
        try {
            if (window.ReactNativeWebView && typeof window.ReactNativeWebView.postMessage === 'function') {
                console.log('🚪 [RN WebView] Sending CLOSE_WEBVIEW message');
                window.ReactNativeWebView.postMessage(JSON.stringify({
                    type: 'CLOSE_WEBVIEW'
                }));
                console.log('✅ Message sent successfully');
            } else {
                console.log('⚠️ [Browser] Not in WebView');
                
                // In browser, you could redirect or show a message
                if (confirm('Close game? (This will reload the page)')) {
                    window.location.reload();
                }
            }
        } catch (e) {
            console.error('❌ Error in CloseWebView:', e);
        }
    }
});