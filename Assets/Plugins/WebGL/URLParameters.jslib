mergeInto(LibraryManager.library, {
    GetURLParameter: function(param) {
        var paramStr = UTF8ToString(param);
        var url = window.location.href;
        var urlObj = new URL(url);
        
        // Try to get from query parameters first (?id=123)
        var value = urlObj.searchParams.get(paramStr);
        
        // If not in query params, try to extract from path (/play-pet/123)
        if (!value && paramStr === 'id') {
            var pathMatch = url.match(/\/play-pet\/(\d+)/);
            if (pathMatch && pathMatch[1]) {
                value = pathMatch[1];
            }
        }
        
        if (value) {
            var bufferSize = lengthBytesUTF8(value) + 1;
            var buffer = _malloc(bufferSize);
            stringToUTF8(value, buffer, bufferSize);
            return buffer;
        }
        
        return null;
    },
    
    GetOriginURL: function() {
        // Returns the origin (protocol + host + port) where Unity is being served
        // Example: if opened from http://192.168.1.5:3000/play-pet/622
        // This returns: http://192.168.1.5:3000
        var origin = window.location.origin;
        
        if (origin) {
            var bufferSize = lengthBytesUTF8(origin) + 1;
            var buffer = _malloc(bufferSize);
            stringToUTF8(origin, buffer, bufferSize);
            return buffer;
        }
        
        return null;
    }
});