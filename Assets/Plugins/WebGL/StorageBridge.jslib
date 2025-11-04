mergeInto(LibraryManager.library, {
    SaveToAsyncStorage: function(keyPtr, valuePtr) {
        var key = UTF8ToString(keyPtr);
        var value = UTF8ToString(valuePtr);
        
        // Send message to React Native WebView
        if (window.ReactNativeWebView) {
            window.ReactNativeWebView.postMessage(JSON.stringify({
                type: 'SAVE_DATA',
                key: key,
                value: value
            }));
            console.log('📤 Sent to React Native:', key, value);
        } else {
            // Fallback to localStorage for testing in browser
            localStorage.setItem(key, value);
            console.log('💾 Saved to localStorage:', key, value);
        }
    },
    
    GetFromAsyncStorage: function(keyPtr) {
        var key = UTF8ToString(keyPtr);
        
        if (window.ReactNativeWebView) {
            window.ReactNativeWebView.postMessage(JSON.stringify({
                type: 'GET_DATA',
                key: key
            }));
            console.log('📤 Requesting from React Native:', key);
        } else {
            // Fallback to localStorage
            var value = localStorage.getItem(key) || '';
            // Call Unity callback
            var data = key + ':' + value;
            SendMessage('StorageBridge', 'ReceiveAsyncStorageValue', data);
            console.log('📥 Loaded from localStorage:', key, value);
        }
    },
    
    SaveAllPlayerPrefs: function(jsonDataPtr) {
        var jsonData = UTF8ToString(jsonDataPtr);
        
        console.log('💾 SaveAllPlayerPrefs called with data:', jsonData);
        
        if (window.ReactNativeWebView) {
            window.ReactNativeWebView.postMessage(JSON.stringify({
                type: 'SAVE_ALL_DATA',
                data: jsonData
            }));
            console.log('📤 Sent all data to React Native');
        } else {
            // Fallback to localStorage
            try {
                var dataObj = JSON.parse(jsonData);
                if (dataObj && dataObj.data) {
                    for (var key in dataObj.data) {
                        localStorage.setItem(key, dataObj.data[key]);
                    }
                }
                console.log('💾 Saved all data to localStorage');
            } catch (e) {
                console.error('Error saving all data:', e);
            }
        }
    },
    
    CloseWebView: function() {
        console.log('🚪 CloseWebView called');
        
        if (window.ReactNativeWebView) {
            console.log('🚪 Sending CLOSE_WEBVIEW message to React Native');
            window.ReactNativeWebView.postMessage(JSON.stringify({
                type: 'CLOSE_WEBVIEW'
            }));
            console.log('✅ Message sent successfully');
        } else {
            console.log('⚠️ Not in WebView, cannot close');
            alert('Exit button clicked (not in React Native WebView)');
        }
    }
});