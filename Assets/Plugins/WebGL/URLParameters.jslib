mergeInto(LibraryManager.library, {
    GetURLParameter: function(param) {
        var paramStr = UTF8ToString(param);
        
        // PRIORITY 1: Check URL hash (for local bundle)
        // Example: file:///android_asset/petGame/index.html#id=622&apiUrl=http://...
        var hash = window.location.hash.substring(1); // Remove the '#'
        if (hash) {
            var params = new URLSearchParams(hash);
            var value = params.get(paramStr);
            
            if (value) {
                console.log('✅ GetURLParameter (from hash):', paramStr, '=', value);
                var bufferSize = lengthBytesUTF8(value) + 1;
                var buffer = _malloc(bufferSize);
                stringToUTF8(value, buffer, bufferSize);
                return buffer;
            }
        }
        
        // PRIORITY 2: Check query parameters (for server-hosted)
        // Example: http://server/play-pet?id=622
        var urlObj = new URL(window.location.href);
        var value = urlObj.searchParams.get(paramStr);
        
        if (value) {
            console.log('✅ GetURLParameter (from query):', paramStr, '=', value);
            var bufferSize = lengthBytesUTF8(value) + 1;
            var buffer = _malloc(bufferSize);
            stringToUTF8(value, buffer, bufferSize);
            return buffer;
        }
        
        // PRIORITY 3: Try to extract from path (legacy support)
        // Example: http://server/play-pet/622
        if (paramStr === 'id') {
            var pathMatch = window.location.href.match(/\/play-pet\/(\d+)/);
            if (pathMatch && pathMatch[1]) {
                value = pathMatch[1];
                console.log('✅ GetURLParameter (from path):', paramStr, '=', value);
                var bufferSize = lengthBytesUTF8(value) + 1;
                var buffer = _malloc(bufferSize);
                stringToUTF8(value, buffer, bufferSize);
                return buffer;
            }
        }
        
        console.warn('⚠️ GetURLParameter: No value found for', paramStr);
        return null;
    },
    
    GetOriginURL: function() {
        // PRIORITY 1: Check hash for apiUrl (for local bundle)
        // Example: #id=622&apiUrl=http://192.168.1.2:3000
        var hash = window.location.hash.substring(1);
        if (hash) {
            var params = new URLSearchParams(hash);
            var apiUrl = params.get('apiUrl');
            
            if (apiUrl) {
                console.log('✅ GetOriginURL (from hash):', apiUrl);
                var bufferSize = lengthBytesUTF8(apiUrl) + 1;
                var buffer = _malloc(bufferSize);
                stringToUTF8(apiUrl, buffer, bufferSize);
                return buffer;
            }
        }
        
        // PRIORITY 2: Use origin if server-hosted (not file://)
        if (window.location.protocol !== 'file:') {
            var origin = window.location.origin;
            console.log('✅ GetOriginURL (from origin):', origin);
            var bufferSize = lengthBytesUTF8(origin) + 1;
            var buffer = _malloc(bufferSize);
            stringToUTF8(origin, buffer, bufferSize);
            return buffer;
        }
        
        // FALLBACK: Return default if nothing found
        var fallback = 'http://192.168.1.2:3000';
        console.warn('⚠️ GetOriginURL: Using fallback:', fallback);
        var bufferSize = lengthBytesUTF8(fallback) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(fallback, buffer, bufferSize);
        return buffer;
    }
});