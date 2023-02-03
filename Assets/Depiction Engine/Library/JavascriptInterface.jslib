mergeInto(LibraryManager.library, {

    SendExternalMessageInternal: function (instanceId, parameters) {
        receiveMessage(UTF8ToString(instanceId), UTF8ToString(parameters));
    }

});